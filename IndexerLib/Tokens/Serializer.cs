using IndexerLib.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IndexerLib.Tokens
{
    /// <summary>
    /// Provides efficient binary serialization and deserialization for <see cref="Token"/> objects,
    /// using compact encoding optimized for inverted index storage.
    ///
    /// <para><b>Compression Techniques (roughly 1/5 the normal size):</b></para>
    ///  <br> - <b>7-bit encoded integers</b>: Encodes small integers using fewer bytes. 
    ///  (Utilizes custom binary readers/writers for full 7-bit support in .NET Framework environments).</br>
    ///  <br> - <b>Delta encoding</b>: Stores differences (deltas) between consecutive postings instead of absolute values,
    ///   producing smaller numbers that further improved 7-bit compression.</br>
    /// </summary>
    public static class Serializer
    {
        /// <summary>
        /// Performs high-performance binary serialization of a single <see cref="Token"/> instance.
        /// Uses .NET’s native numeric serialization capabilities for speed and compactness.
        /// </summary>
        /// <param name="token">The token to serialize.</param>
        /// <returns>A compact <see cref="byte"/> array representing the serialized token.</returns>
        public static byte[] SerializeToken(Token token)
        {
            using (var stream = new MemoryStream())
            using (var writer = new MyBinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                Serialize(writer, token);
                return stream.ToArray();
            }
        }

        public static byte[] SerializeTokenGroup(List<Token> tokens)
        {
            using (var stream = new MemoryStream())
            using (var writer = new MyBinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                foreach (var token in tokens)
                    Serialize(writer, token);

                return stream.ToArray();
            }
        }

        static void Serialize(MyBinaryWriter writer, Token token)
        {
            // Write token-level metadata
            writer.Write7BitEncodedInt(token.DocId);
            writer.Write7BitEncodedInt(token.Postings.Count); // Store posting count for decoding

            // Ensure postings are ordered for delta encoding efficiency
            var postings = token.Postings.OrderBy(x => x.Position);

            // Initialize delta reference values
            int prevPos = 0;
            int prevStart = 0;

            foreach (var p in postings)
            {
                // Write deltas: store value differences rather than absolute values
                // Example: if current position = 105 and previous = 100, store 5 instead of 105
                writer.Write7BitEncodedInt(p.Position - prevPos);
                writer.Write7BitEncodedInt(p.Index - prevStart);
                writer.Write7BitEncodedInt(p.Length); // Length stored as-is (no delta needed)

                // Update reference values
                prevPos = p.Position;
                prevStart = p.Index;
            }
        }


        /// <summary>
        /// Deserializes a group of serialized <see cref="Token"/> objects from a single binary block.
        /// <para>All tokens belonging to the same key are stored as a contiguous group,
        /// which simplifies and accelerates group deserialization.</para>
        /// </summary>
        /// <param name="data">The binary data representing multiple serialized tokens.</param>
        /// <returns>An enumerable collection of deserialized <see cref="Token"/> objects.</returns>
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
                    if (token != null)
                        yield return token;
                }
            }
        }

        /// <summary>
        /// <br></br>Deserializes a single <see cref="Token"/> from a binary reader stream.
        /// Reconstructs full postings using delta decoding to recover absolute values.</br>
        /// Note: Reading also advances the stream position thus when current token is read it advances to postion of next token
        /// </summary>
        /// <param name="reader">The binary reader containing serialized token data.</param>
        /// <returns>The deserialized <see cref="Token"/>, or null if data is incomplete or corrupted.</returns>
        public static Token DeserializeToken(MyBinaryReader reader)
        {
            try
            {
                var token = new Token { DocId = reader.Read7BitEncodedInt() };

                int postingsCount = reader.Read7BitEncodedInt(); // Number of postings each token may occur more then once in a document
                int currentPos = 0, currentStart = 0;

                for (int i = 0; i < postingsCount; i++)
                {
                    // Delta decoding: restore original absolute values
                    currentPos += reader.Read7BitEncodedInt();
                    currentStart += reader.Read7BitEncodedInt();
                    int len = reader.Read7BitEncodedInt();

                    token.Postings.Add(new Postings
                    {
                        Position = currentPos,
                        Index = currentStart,
                        Length = len
                    });
                }

                return token;
            }
            catch (EndOfStreamException)
            {
                // Return null if stream ends unexpectedly (e.g., incomplete or corrupted data)
                return null;
            }
        }
    }
}