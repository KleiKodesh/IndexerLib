
namespace IndexerLib.Tokens
{
    public class Posting
    {
        public int Position { get; set; } // position of word in doc
        public int StartIndex { get; set; } // postion of first char in document chars
        public int Length { get; set; } // length of word
    }
}
