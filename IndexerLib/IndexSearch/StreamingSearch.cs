using IndexerLib.Helpers;
using IndexerLib.Index;
using IndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexerLib.IndexSearch
{
    public static class StreamingSearch
    {
        public static IEnumerable<SearchResult> Execute(string query, short adjacency = 2)
        {
            var termQueries = QueryParser.GenerateWordPositions(query);
            var streamerLists = new List<TokenStreamerList>(termQueries.Length);

            using (var indexReader = new IndexReader())
            using (var reader = new MyBinaryReader(indexReader._dataStream))
            {
                // Build token streamers for all query terms
                for (int x = 0; x < termQueries.Length; x++)
                {
                    var streamerList = new TokenStreamerList();
                    for (int i = 0; i < termQueries[x].IndexPositions.Count; i++)
                    {
                        IndexKey key = indexReader.GetKeyByIndex(termQueries[x].IndexPositions[i]);
                        streamerList.AddStreamer(new TokenStreamer(reader, key));
                    }
                    streamerLists.Add(streamerList);
                }

                // Main streaming loop
                while (true)
                {
                    int minDocId = streamerLists.Min(l => l.MinDocId);

                    // collect postings only for this doc
                    var postingsLists = new List<Postings[]>(streamerLists.Count);
                    foreach (var list in streamerLists)
                    {
                        if (list.MinDocId == minDocId)
                        {
                            var current = list.CurrentPostings.ToArray();
                            if (current != null && current.Length > 0)
                                postingsLists.Add(current.ToArray());
                        }
                    }

                    // Perform adjacency/span match
                    // Only process documents containing *all* query terms
                    if (postingsLists.Count == streamerLists.Count)
                    {
                        var match = OrderedAdjacencyMatch(minDocId, postingsLists, adjacency);
                        if (match != null)
                            yield return match;
                    }

                    // Advance only those lists pointing at minDocId
                    bool anyAdvanced = false;
                    foreach (var list in streamerLists)
                    {
                        if (list.MinDocId == minDocId)
                            anyAdvanced |= list.MoveNext();
                    }

                    if (!anyAdvanced)
                        yield break;
                }
            }
        }

        static SearchResult OrderedAdjacencyMatch(int docId, List<Postings[]> postingsLists, int adjacency)
        {
            /*  Adjust adjacency by +1 because position differences are off by one:
               For example, if two words are adjacent (positions 10 and 11),
               their distance is 1, not 0. So user adjacency=0 (exact phrase)
               should allow position diff=1. This keeps "adjacency" meaning
               consistent with "number of words between terms".    */
            adjacency++;

            int termCount = postingsLists.Count;
            if (termCount == 0)
                return null;

            var resultForDoc = new SearchResult
            {
                DocId = docId,
                MatchedPostings = new List<Postings[]>()
            };

            var firstList = postingsLists[0];
            var indices = new int[termCount]; // per-list cursor

            for (int i = 0; i < firstList.Length; i++)
            {
                var start = firstList[i];
                var currentMatch = new Postings[termCount];
                currentMatch[0] = start;

                int prevPos = start.Position;
                bool valid = true;

                // Sequentially advance through other lists
                for (int listIdx = 1; listIdx < termCount; listIdx++)
                {
                    var plist = postingsLists[listIdx];
                    int cursor = indices[listIdx];

                    // advance until position > prevPos
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
                        // No match — but next positions might still fit a later startPosting
                        // Keep the cursor here for future iterations
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

            if (resultForDoc.MatchedPostings.Count > 0)
                return resultForDoc;

            return null;
        }
    }
}

