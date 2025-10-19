using IndexerLib.Helpers;
using IndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexerLib.IndexSearch
{
    //// <summary>
    /// Represents a synchronized collection of <see cref="TokenStreamer"/> instances, 
    /// each responsible for streaming <see cref="Token"/> entries from a distinct index segment.
    ///
    /// <see cref="TokenStreamerList"/> coordinates iteration across multiple token streams, 
    /// advancing them in sync based on their current document identifiers (<see cref="Token.DocId"/>).
    /// This enables efficient merging and positional matching of term occurrences 
    /// during streaming-based search operations.
    ///
    /// Conceptually, it acts like a lightweight controller that manages several 
    /// forward-only enumerators (<see cref="TokenStreamer"/> objects), 
    /// ensuring that search algorithms can process multi-term queries 
    /// without loading all postings into memory.
    ///
    /// <para>
    /// It forms a key component of the <see cref="StreamingSearch"/> system, 
    /// which performs RAM-efficient, sequential index traversal directly from disk.
    /// </para>
    /// </summary>

    public class TokenStreamerList : List<TokenStreamer> //?? use arrays instead of lists??
    {
        /// <summary>
        /// Gets the lowest <see cref="Token.DocId"/> currently pointed to by any active <see cref="TokenStreamer"/>.
        /// </summary>
        public int MinDocId { get; private set; }

        public TokenStreamerList()
        {
            MinDocId = int.MaxValue;
        }

        /// <summary>
        /// Adds a new <see cref="TokenStreamer"/> to the collection and updates 
        /// the <see cref="MinDocId"/> property accordingly.
        /// 
        /// This custom add method ensures that whenever a new streamer is appended, 
        /// the list’s current minimum document identifier (<see cref="MinDocId"/>) 
        /// remains accurate and up-to-date.
        /// 
        /// Typically called during initialization of a <see cref="TokenStreamerList"/> 
        /// as individual streamers are created for each indexed term.
        /// </summary>
        /// <param name="streamer">
        /// The <see cref="TokenStreamer"/> instance to add to the collection.
        /// </param>
        public void AddStreamer(TokenStreamer streamer)
        {
            Add(streamer);
            if (Count == 1)
                MinDocId = streamer.Current.DocId;
            else if (streamer.Current.DocId < MinDocId)
                MinDocId = streamer.Current.DocId;
        }
        /// <summary>
        /// Advances all <see cref="TokenStreamer"/> instances currently positioned 
        /// at the minimum document identifier (<see cref="MinDocId"/>).
        ///
        /// Each streamer is evaluated in sequence:
        /// - If its current <see cref="Token.DocId"/> is equal to or less than <see cref="MinDocId"/>,
        ///   it is advanced to the next token in its segment.
        ///   This mechanism is crucial for <see cref="StreamingSearch"/>, as it enables sequential reading 
        ///   of token blocks directly from the binary index (i.e., document by document) 
        ///   without preloading entire postings lists into memory.
        /// - Streamers that reach the end of their available token data are removed 
        ///   from the collection.
        ///
        /// After all relevant streamers have been advanced, <see cref="MinDocId"/> 
        /// is recalculated to represent the smallest <see cref="Token.DocId"/> 
        /// among the remaining active streamers.
        ///
        /// This operation forms the backbone of the <see cref="StreamingSearch"/> process,
        /// ensuring that multiple token streams advance in a coordinated and efficient manner.
        /// It effectively implements Boolean OR semantics, as each <see cref="TokenStreamerList"/>
        /// represents the union of token streams corresponding to the individual terms
        /// within a single Boolean OR query.
        /// </summary>
        /// 
        /// <returns>
        /// <c>true</c> if at least one streamer successfully advanced to the next token; 
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool MoveNext()
        {
            bool advanced = false;

            // Iterate backward to safely remove streamers inline while looping.
            for (int i = Count - 1; i >= 0; i--)
            {
                var streamer = this[i];

                if (streamer.Current.DocId <= MinDocId)
                {
                    if (streamer.MoveNext())
                        advanced = true;
                    else // Remove exhausted streamers
                        RemoveAt(i);
                }
            }

            // Crucial step: after advancing any streamers, recompute MinDocId.
            // This ensures the StreamingSearch maintains a correct and synchronized merge-walk
            // across all active token streams.
            if (advanced && Count > 0)
                MinDocId = this.Min(s => s.Current.DocId);


            return advanced;
        }

        /// <summary>
        /// Returns an enumerable of all <see cref="Postings"/> instances from
        /// the <see cref="TokenStreamer.Current"/> entries that are currently
        /// positioned at the active document (<see cref="MinDocId"/>).
        ///
        /// The resulting sequence aggregates postings from all streamers that share
        /// the same document ID, ordered by their token positions within that document.
        /// These postings are typically used to construct a <see cref="SearchResult"/> instance
        /// during the streaming search process.
        /// </summary>
        public Postings[] CurrentPostings
        {
            get
            {
                var temp = new List<Postings>();
                foreach (var ts in this)
                {
                    if (ts.Current.DocId == MinDocId)
                        temp.AddRange(ts.Current.Postings);
                }

                var array = temp.ToArray();
                Array.Sort(array, (a, b) => a.Position.CompareTo(b.Position));
                return array;
            }
        }
    }
}    
