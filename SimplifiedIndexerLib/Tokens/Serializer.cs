using SimplifiedIndexerLib.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimplifiedIndexerLib.Tokens
{
    public static class Serializer
    {
        // Serializes a Token using 7-bit encoded integers with delta encoding for positions.
        // That way, smaller integers are written, which compresses better with Write7BitEncodedInt.
        public static byte[] SerializeToken(Token token)
        {
            using (var stream = new MemoryStream())
            using (var writer = new MyBinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write7BitEncodedInt(token.DocId);

                var orderedPositions = token.Postions.OrderBy(p => p).ToList();
                writer.Write7BitEncodedInt(orderedPositions.Count);

                int prev = 0;
                foreach (var pos in orderedPositions)
                {
                    int delta = pos - prev;
                    writer.Write7BitEncodedInt(delta);
                    prev = pos;
                }

                return stream.ToArray();
            }
        }

        public static List<Token> DeserializeTokenGroup(byte[] data)
        {
            var tokens = new List<Token>();

            if (data != null)
            {
                using (var stream = new MemoryStream(data))
                using (var reader = new MyBinaryReader(stream, Encoding.UTF8))
                {
                    while (stream.Position < stream.Length)
                    {
                        var token = DeserializeToken(reader);
                        if (token != null)
                            tokens.Add(token);
                    }
                }
            }

            return tokens;
        }

        public static Token DeserializeToken(MyBinaryReader reader)
        {
            try
            {
                var token = new Token { DocId = reader.Read7BitEncodedInt() };

                int count = reader.Read7BitEncodedInt();
                int prev = 0;

                for (int i = 0; i < count; i++)
                {
                    int delta = reader.Read7BitEncodedInt();
                    int pos = prev + delta;
                    token.Postions.Add(pos);
                    prev = pos;
                }

                return token;
            }
            catch (EndOfStreamException)
            {
                return null;
            }
        }
    }
}
