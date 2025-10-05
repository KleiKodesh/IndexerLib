using System.Collections.Generic;

namespace IndexerLib.Tokens
{
    /// <summary>
    /// Represents a single token (word or term) in the index.
    /// Each token has a unique Id and a list of postings 
    /// that track where this token occurs.
    /// </summary>
    public class Token
    {
        public int DocId { get; set; }
        public List<Postings> Postings { get; set; } = new List<Postings>();
    }

    public class Postings
    {
        public int Position { get; set; } // position of the word relative to word count
        public int StartIndex { get; set; } // postion of first char of the word relative to char count
        public int Length { get; set; } // length of the word
    }
}
