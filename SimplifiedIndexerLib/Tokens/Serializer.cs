using SimplifiedIndexerLib.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimplifiedIndexerLib.Tokens
{
    public static class Serializer
    {
        // Serializes a Token using 7-bit encoded integers (no delta encoding).
        public static byte[] SerializeToken(Token token)
        {
            using (var stream = new MemoryStream())
            using (var writer = new MyBinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write7BitEncodedInt(token.DocId);
                writer.Write7BitEncodedInt(token.Postions.Count);
                foreach (var pos in token.Postions.OrderBy(p => p))
                    writer.Write7BitEncodedInt(pos);

                return stream.ToArray();
            }
        }

        public static List<Token> DeserializeTokenGroup(byte[] data)
        {
            var tokens = new List<Token>();

            if (data != null)
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

            return tokens;
        }

        public static Token DeserializeToken(MyBinaryReader reader)
        {
            try
            {
                var token = new Token { DocId = reader.Read7BitEncodedInt() };

                int count = reader.Read7BitEncodedInt();

                for (int i = 0; i < count; i++)
                    token.Postions.Add(reader.Read7BitEncodedInt()); // read absolute value

                return token;
            }
            catch (EndOfStreamException)
            {
                return null;
            }
        }
    }
}
