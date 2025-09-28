using System.Collections.Generic;

namespace SimplifiedIndexerLib.Tokens
{
    public class Token
    {
        public int DocId { get; set; }

        public List<int> Postions { get; set; } = new List<int>();
    }
}
