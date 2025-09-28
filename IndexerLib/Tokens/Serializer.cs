using IndexerLib.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace IndexerLib.Tokens
{
    public static class Serializer
    {
        // Note: using 7-bit encoded integers to conserve space.
        // Using custom binary writer and reader to implement 7-bit encoding in .NET Framework.
        public static byte[] SerializeToken(Token token)
        {
            using (var stream = new MemoryStream())
            using (var writer = new MyBinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write7BitEncodedInt(token.DocId);
                writer.Write7BitEncodedInt(token.Postings.Count); // Store posting count for efficient deserialization
                
                var postings = token.Postings.OrderBy(x => x.Position);

                // Delta encoding for pstings to reduce size:  Instead of writing absolute values, we store the differences from the previous posting.
                int prevPos = 0;
                int prevStart = 0;

                foreach (var posting in postings)
                {
                    // Write deltas instead of absolute values
                    writer.Write7BitEncodedInt(posting.Position - prevPos);
                    writer.Write7BitEncodedInt(posting.StartIndex - prevStart);
                    writer.Write7BitEncodedInt(posting.Length);

                    // Update previous values for next iteration
                    prevPos = posting.Position;
                    prevStart = posting.StartIndex;
                }

                return stream.ToArray(); // Return serialized byte array
            }
        }

        public static IEnumerable<Token> DeserializeTokenGroup(byte[] data)
        {
            if (data == null)
                yield break; 

            using (var stream = new MemoryStream(data))
            using (var reader = new MyBinaryReader(stream, Encoding.UTF8))
            {
                while (stream.Position < stream.Length)
                {
                    var token = DeserializeToken(reader);
                    if (token == null)
                        continue;

                    yield return token;
                }
            }
        }

        public static Token DeserializeToken(MyBinaryReader reader)
        {
            try
            {
                var token = new Token { DocId = reader.Read7BitEncodedInt() }; // Read token id

                int count = reader.Read7BitEncodedInt(); // Number of postings
                int prevPos = 0, prevStart = 0;

                // Read postings using delta decoding
                for (int i = 0; i < count; i++)
                {
                    prevPos += reader.Read7BitEncodedInt();   // Add delta to previous Position
                    prevStart += reader.Read7BitEncodedInt(); // Add delta to previous StartIndex
                    int len = reader.Read7BitEncodedInt();    // Read Length

                    token.Postings.Add(new Postings
                    {
                        Position = prevPos,
                        StartIndex = prevStart,
                        Length = len
                    });
                }

                return token;
            }
            catch (EndOfStreamException)
            {
                return null; // Return null if stream ended unexpectedly
            }
        }
    }
}
