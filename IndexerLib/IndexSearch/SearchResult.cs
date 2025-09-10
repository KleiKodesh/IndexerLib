using IndexerLib.Tokens;
using System.Collections.Generic;

namespace IndexerLib.IndexSearch
{
    public class SearchResult
    {
        public int DocId { get; set; }                  // Document ID
        public List<List<int>> Matches { get; set; }   // Each inner list = one valid match (positions of all query terms)
    }

}
