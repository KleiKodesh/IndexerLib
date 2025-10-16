using System.Collections.Generic;

namespace IndexerLib.Tokens
{
    /// <summary>
    /// Represents a single token (word or term) in the index.
    /// Each token is linked to a specific document (DocId) and
    /// contains a list of postings indicating its occurrences in the text.
    /// 
    /// <para>Tokens and postings are stored entirely as numeric values
    /// for compact storage and efficient binary serialization.
    /// This design allows seamless use with BinaryReader and BinaryWriter,
    /// enabling fast custom serialization and deserialization,
    /// and forming the foundation of the inverted index's costume binary storage format.</para>
    /// </summary>
    public class Token
    {
        /// <summary>
        /// Instead of storing the document path as a string, 
        /// the document is referenced by its DocId stored in SQLite.
        /// This is more compact, easier to serialize, and allows
        /// fast calculations and sorting.
        /// </summary>
        public int DocId { get; set; }

        /// <summary>
        /// A collection of postings representing all occurrences of this token
        /// within the document. Since a token may appear multiple times in a document,
        /// each occurrence is stored as a <see cref="Postings"/> entry rather than
        /// duplicating the token itself.
        /// </summary>
        public List<Postings> Postings { get; set; } = new List<Postings>();
    }

    /// <summary>
    /// Represents a single occurrence of a token in a document.
    /// Used for calculating spans, distances between tokens, and snippet extraction.
    /// </summary>
    public class Postings
    {
        /// <summary>
        /// The token's sequential position among all tokens in the document.
        /// Used for calculating spans and distances between words.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// The character index of the token's first character in the document text.
        /// Used for snippet extraction.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The token's length in characters (including diacritics, HTML tags, quotes, etc.).
        /// Used for snippet extraction.
        /// </summary>
        public int Length { get; set; }
    }
}
