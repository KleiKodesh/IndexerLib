using System.Collections.Generic;

namespace IndexerLib.Tokens
{
    public class Token
    {
        public int SHA256 { get; set; } //unique id for token
        public List<Posting> Postings { get; set; } = new List<Posting>(); // token information
    }
}
