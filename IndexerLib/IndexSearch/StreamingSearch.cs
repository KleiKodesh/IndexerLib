using IndexerLib.Helpers;
using IndexerLib.Index;
using IndexerLib.Tokens;
using System.Collections.Generic;
using System.Linq;

namespace IndexerLib.IndexSearch
{
    /// <summary>
    /// Provides a high-performance, streaming-based search mechanism that 
    /// sequentially reads token data directly from the binary inverted index file,
    /// without fully loading postings lists into memory.
    ///
    /// <para>
    /// The <see cref="StreamingSearch"/> class coordinates multiple
    /// <see cref="TokenStreamerList"/> instances — one for each query term —
    /// and performs synchronized iteration over their streamed token data.
    /// </para>
    ///
    /// <para>
    /// Unlike traditional in-memory search engines that pre-load entire posting lists,
    /// <see cref="StreamingSearch"/> reads compact token streams on-demand from disk.
    /// This enables extremely low RAM usage and efficient query execution, even
    /// on very large index files.
    /// </para>
    ///
    /// <para>
    /// Conceptually, this class performs a merge-like traversal across term streams:
    /// it continuously aligns all active <see cref="TokenStreamerList"/> instances by 
    /// their current document identifier (<see cref="Token.DocId"/>), collecting
    /// and matching postings from documents containing all query terms.
    /// </para>
    ///
    /// <para>
    /// Each iteration of the search loop:
    /// <list type="number">
    /// <item>Finds the lowest current document ID across all term streams.</item>
    /// <item>Collects all <see cref="Postings"/> belonging to that document for each term.</item>
    /// <item>Applies <see cref="OrderedAdjacencyMatch"/> to verify that the query terms 
    /// appear in proximity (controlled by <paramref name="adjacency"/>).</item>
    /// <item>Yields a <see cref="SearchResult"/> if the document satisfies the proximity condition.</item>
    /// <item>Advances only those term streams that have finished processing that document ID.</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// Together with <see cref="TokenStreamer"/> and <see cref="TokenStreamerList"/>,
    /// this class implements the full streaming query pipeline — 
    /// from low-level token deserialization to high-level phrase and adjacency matching.
    /// </para>
    /// </summary>
    public static class StreamingSearch
    {
        /// <summary>
        /// Executes a streaming search for the given <paramref name="query"/>,
        /// returning a sequence of <see cref="SearchResult"/> objects representing
        /// documents where the query terms appear within the specified adjacency range.
        ///
        /// <para>
        /// The method constructs <see cref="TokenStreamerList"/> collections for each
        /// parsed term in the query and continuously merges their document streams
        /// in a synchronized manner.
        /// </para>
        /// </summary>
        /// <param name="query">
        /// The textual query to be parsed and matched against the index.
        /// </param>
        /// <param name="adjacency">
        /// The maximum allowed word distance between consecutive query terms 
        /// (e.g., 0 for exact phrase matches, 1 for one-word gaps, etc.).
        /// </param>
        /// <returns>
        /// An enumerable of <see cref="SearchResult"/> instances, one per matching document.
        /// Each result contains all <see cref="Postings"/> sequences that satisfy
        /// the adjacency constraint.
        /// </returns>
        public static IEnumerable<SearchResult> Execute(string query, short adjacency = 2)
        {
            /*  Adjust adjacency by +1 because position differences are off by one: 
                For example, if two words are adjacent (positions 10 and 11), their distance is 1, not 0. 
                So user adjacency=0 (exact phrase) should allow position diff=1. 
                This keeps "adjacency" meaning consistent with "number of words between terms". */
            adjacency++;

            // streamerLists count corresponds to termQueries.Length because each entry directly 
            // represents one query term. Essentially, this translates the 
            // <see cref="TermQuery.IndexPositions"/> data into a parallel set of 
            // <see cref="TokenStreamerList"/> objects that will handle streaming 
            // for each term individually.
            TermQuery[] termQueries = QueryParser.GenerateWordPositions(query);
            var streamerLists = new List<TokenStreamerList>(termQueries.Length);

            // pass the IndexReader's _dataStream directly to MyBinaryReader 
            // so that all TokenStreamers share the same underlying stream and reader
            using (var indexReader = new IndexReader())
            using (var reader = new MyBinaryReader(indexReader._dataStream))                                                                            
            {
                // Initialize streamers for each term in the query
                for (int x = 0; x < termQueries.Length; x++)
                {
                    var streamerList = new TokenStreamerList();

                    // Each IndexKey points to a specific token block in the index file.
                    for (int i = 0; i < termQueries[x].IndexPositions.Count; i++)
                    {
                        IndexKey key = indexReader.GetKeyByIndex(termQueries[x].IndexPositions[i]);
                        streamerList.AddStreamer(new TokenStreamer(reader, key));
                    }

                    streamerLists.Add(streamerList);
                }

                // Core streaming loop — iterates through documents in ascending DocId order
                while (true)
                {
                    int minDocId = streamerLists.Min(l => l.MinDocId);

                    // Collect postings for the document currently at minDocId
                    var postingsLists = new List<Postings[]>(streamerLists.Count);
                    foreach (var list in streamerLists)
                    {
                        if (list.MinDocId == minDocId)
                        {
                            var current = list.CurrentPostings.ToArray();
                            if (current.Length > 0)
                                postingsLists.Add(current);
                        }
                    }

                    // Only process documents containing all query terms
                    if (postingsLists.Count == streamerLists.Count)
                    {
                        var match = OrderedAdjacencyMatch(minDocId, postingsLists, adjacency);
                        if (match != null)
                            yield return match;
                    }

                    // Advance all lists pointing to the processed document
                    bool anyAdvanced = false;
                    foreach (var list in streamerLists)
                    {
                        if (list.MinDocId == minDocId)
                            anyAdvanced |= list.MoveNext();
                    }

                    if (!anyAdvanced)
                        yield break; // All streams exhausted
                }
            }
        }

        /// <summary>
        /// Performs an ordered adjacency match across multiple postings lists
        /// for a single document, ensuring that term occurrences appear in
        /// sequential order within a given maximum word distance.
        ///
        /// <para>
        /// The algorithm performs a synchronized positional merge:
        /// starting from each posting of the first term, it seeks matching
        /// positions in the subsequent term lists that are within the
        /// specified <paramref name="adjacency"/> distance.
        /// </para>
        ///
        /// <para>
        /// The adjacency value is internally incremented by 1 to account for
        /// natural position offsets: e.g., two adjacent terms (positions 10 and 11)
        /// have a position difference of 1, not 0.
        /// </para>
        /// </summary>
        /// <param name="docId">The document identifier currently being processed.</param>
        /// <param name="postingsLists">
        /// A list of postings arrays, one per query term, all referring to the same document.
        /// </param>
        /// <param name="adjacency">
        /// The maximum allowed distance (in token positions) between consecutive terms.
        /// </param>
        /// <returns>
        /// A <see cref="SearchResult"/> if at least one matching positional sequence is found;
        /// otherwise, <c>null</c>.
        /// </returns>
        static SearchResult OrderedAdjacencyMatch(int docId, List<Postings[]> postingsLists, int adjacency)
        {
            int termCount = postingsLists.Count;
            if (termCount == 0)
                return null;

            var resultForDoc = new SearchResult
            {
                DocId = docId,
                MatchedPostings = new List<Postings[]>()
            };

            var firstList = postingsLists[0];
            var indices = new int[termCount]; // Cursor per list

            for (int i = 0; i < firstList.Length; i++)
            {
                var start = firstList[i];
                var currentMatch = new Postings[termCount];
                currentMatch[0] = start;

                int prevPos = start.Position;
                bool valid = true;

                // Sequentially match each subsequent term by increasing position
                for (int listIdx = 1; listIdx < termCount; listIdx++)
                {
                    var plist = postingsLists[listIdx];
                    int cursor = indices[listIdx];

                    // Advance until the next position exceeds the previous match
                    while (cursor < plist.Length && plist[cursor].Position <= prevPos)
                        cursor++;

                    if (cursor >= plist.Length)
                    {
                        valid = false;
                        break;
                    }

                    var next = plist[cursor];
                    int diff = next.Position - prevPos;

                    if (diff > adjacency)
                    {
                        // Too far apart — no valid sequence from this start position
                        valid = false;
                        break;
                    }

                    currentMatch[listIdx] = next;
                    prevPos = next.Position;
                    indices[listIdx] = cursor;
                }

                if (valid)
                    resultForDoc.MatchedPostings.Add(currentMatch);
            }

            return resultForDoc.MatchedPostings.Count > 0 ? resultForDoc : null;
        }
    }
}
