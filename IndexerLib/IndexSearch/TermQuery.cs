using System.Collections.Generic;

namespace IndexerLib.IndexSearch
{
    public class TermQuery
    {
        public string Term { get; }
        public List<int> IndexPositions { get; }

        public TermQuery(string term)
        {
            Term = term;
            IndexPositions = new List<int>();
        }
    }
}
