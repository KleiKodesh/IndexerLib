using System.Collections.Generic;

namespace SimplifiedIndexerLib.IndexSearch
{
    public class SearchResult
    {
        public int DocId { get; set; }      // Document ID
        public string DocPath { get; set; } // Document Path
        public List<string> Snippets { get; set; } // Highlighted snippet

        public List<Postings[]> Matches { get; set; } = new List<Postings[]>(); // Word positions that matched
    }

    public class Postings
    {
        public int Position { get; set; }     // position of the word relative to word count
        public int StartIndex { get; set; }   // position of first char of the word relative to char count
        public int Length { get; set; }       // length of the word
    }

}
