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
        /// <summary>
        /// Unique identifier doc that contains this token.
        /// </summary>
        public int DocId { get; set; }

        /// <summary>
        /// Collection of postings that store information about 
        /// occurrences of this token within this doc (e.g. positions etc.)
        /// </summary>
        public List<Posting> Postings { get; set; } = new List<Posting>();
    }

    public class Posting
    {
        public int Position { get; set; } // position of the word relative to word count
        public int StartIndex { get; set; } // postion of first char of the word relative to char count
        public int Length { get; set; } // length of the word
    }
}
