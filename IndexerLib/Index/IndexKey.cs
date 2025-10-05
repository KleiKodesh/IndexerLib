
using IndexerLib.Helpers;
using System.IO;

namespace IndexerLib.Index
{
    public class IndexKey
    {
        public byte[] Hash { get; set; }
        public long Offset { get; set; }
        public int Length { get; set; }

        public IndexKey() { }

        public IndexKey(BinaryReader reader)
        {
            Hash = reader.ReadBytes(32);
            Offset = reader.ReadInt64();
            Length = reader.ReadInt32();
        }

    }
}