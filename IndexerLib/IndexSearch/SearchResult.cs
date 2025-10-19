using IndexerLib.Tokens;
using System.Collections.Generic;

namespace IndexerLib.IndexSearch
{
    public class SearchResult
    {
        public int DocId { get; set; }      // Document ID
        public string DocPath { get; set; } // Document Path
        public string[] Snippets { get; set; } // Highlighted snippet

        public List<Postings[]> MatchedPostings { get; set; } //All Word position spans that matched
    }
}
