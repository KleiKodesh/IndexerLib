using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexerLib.IndexSearch
{
    public class Snippet
    {
        public int DocId { get; set; }                 // Document ID
        public string DocPath { get; set; } 
        public List<int> MatchPositions { get; set; }  // Exact positions of the match
        public string Text { get; set; }               // Extracted snippet text
    }
}
