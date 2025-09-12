using IndexerLib.Tokens;
using System.Collections.Generic;

namespace IndexerLib.IndexSearch
{
    public class SearchResult
    {
        public int DocId { get; set; }                  // Document ID
        public string DocPath { get; set; }                  // Document Path
        public string Snippet { get; set; } //  highlighted snippet
        public Postings[] MatchedPostings { get; set; }
    }
}
