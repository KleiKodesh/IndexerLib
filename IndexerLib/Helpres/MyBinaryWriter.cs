using System.IO;
using System.Text;

namespace IndexerLib.Helpres
{
    class MyBinaryWriter : BinaryWriter
    {
        public MyBinaryWriter(Stream output)
           : base(output) { }
        public MyBinaryWriter(Stream output, Encoding encoding)
            : base(output, encoding) { }
        public MyBinaryWriter(Stream output, Encoding encoding, bool leaveOpen)
            : base(output, encoding, leaveOpen) { }

        public new void Write7BitEncodedInt(int value)
        {
            base.Write7BitEncodedInt(value);
        }
    }
}
