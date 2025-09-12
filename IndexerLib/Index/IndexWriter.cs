using IndexerLib.Helpers;
using IndexerLib.IndexManger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using static IndexerLib.Helpers.ByteArrayComparer;

namespace IndexerLib.Index
{
    /// <summary>
    /// IndexWriter is responsible for writing token data to the index storage,
    /// a costume binary file format is implemnted saving binary data with an index at its end for efficient lookup.
    /// a footer is created at the end of the file indicating the index section length.
    /// </summary>
    public class IndexWriter : IndexerBase, IDisposable
    {
        readonly FileStream _fileStream;        // Underlying file stream for writing data
        readonly MyBinaryWriter _writer;        // Custom binary writer for writing with encoding
        readonly SHA256 _sha256;                // Hashing algorithm used for word identifiers
        const ushort MagicMarker = 0xCAFE;      // Marker value used in footer to verify file integrity

        private long currentOffset = 0;         // Tracks current byte offset in file where next entry will be written

        // Maps word hashes -> index metadata (offset + length)
        Dictionary<byte[], IndexKey> Keys = new Dictionary<byte[], IndexKey>(new ByteArrayEqualityComparer());

        // Stores unique words seen during indexing
        HashSet<string> WordsSet = new HashSet<string>();

        public IndexWriter(string name = "")
        {
            if(!string.IsNullOrEmpty(name))
                TokenStorePath = Path.Combine(IndexDirectoryPath, name + ".tks");
            // Ensure the token store path is unique/valid before creating the stream
            EnsureUniqueTokenStorePath();

            // Open/create the underlying file to hold serialized tokens
            _fileStream = new FileStream(TokenStorePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            // Writer that supports writing primitive types + strings efficiently
            _writer = new MyBinaryWriter(_fileStream, Encoding.UTF8, leaveOpen: true);

            // SHA256 for hashing words -> stable 32-byte identifier
            _sha256 = SHA256.Create();
        }

        /// <summary>
        /// Writes token data to file and records its offset/length in the Keys dictionary.
        /// </summary>
        /// <param name="data">Serialized token data to write.</param>
        /// <param name="word">Associated word to hash and track.</param>
        public void Put(byte[] data, string word)
        {
            if (string.IsNullOrEmpty(word) || data == null)
                return;

            // Store the original word for persistence in a separate word set
            WordsSet.Add(word);

            // Compute SHA-256 hash of the word (32 bytes, fixed size)
            string normalizedWord = word.Normalize(NormalizationForm.FormC);
            byte[] hash = _sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedWord));

            Put(data, hash);
        }

        /// <summary>
        /// Writes token data to file using the provided hash as a key,
        /// and stores its offset/length in the Keys dictionary.
        /// </summary>
        /// <param name="data">Serialized token data to write.</param>
        /// <param name="hash">Precomputed SHA-256 hash to use as the key.</param>
        public void Put(byte[] data, byte[] hash)
        {
            if (hash == null || data == null)
                return;

            // Write serialized token data at the current file offset
            _writer.Write(data, 0, data.Length);

            // Record index entry for this stored data
            Keys[hash] = new IndexKey
            {
                Hash = hash,
                Offset = currentOffset,
                Length = data.Length
            };

            // Advance the file offset tracker
            currentOffset += data.Length;
        }


        /// <summary>
        /// Disposes resources and finalizes the index file by appending the index and footer.
        /// </summary>
        public void Dispose()
        {
            AppendKeys(); // Append the index section at the end of the file

            // Store all unique words separately for regex lookup
            WordsStore.AddWords(WordsSet);

            // Cleanup resources
            _writer.Flush();
            _writer.Dispose();
            _fileStream.Dispose();
            _sha256?.Dispose();
        }

        /// <summary>
        /// Writes the sorted key index (hash -> offset/length) to the file,
        /// followed by a footer containing a magic marker and index size.
        /// </summary>
        private void AppendKeys()
        {
            if (_fileStream == null)
                return;

            try
            {
                long indexStart = _fileStream.Position; // Start position of index section

                // Sort entries by hash for deterministic ordering
                var sortedIndex = Keys.OrderBy(kvp => kvp.Key, new ByteArrayComparer());

                // Write number of index entries
                _writer.Write(Keys.Count);

                // Write each index entry (hash, offset, length)
                foreach (var indexKey in sortedIndex)
                {
                    _writer.Write(indexKey.Key);          // 32-byte SHA256 hash
                    _writer.Write(indexKey.Value.Offset); // 64-bit offset
                    _writer.Write(indexKey.Value.Length); // 32-bit length
                }

                // Calculate how many bytes the index took
                long indexLength = _fileStream.Position - indexStart;

                // Build footer: high 16 bits = magic marker, low 48 bits = index length
                ulong footer = ((ulong)MagicMarker << 48) | (ulong)indexLength;

                // Write footer at end of file
                _writer.Write(footer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // For debugging purposes
            }
        }
    }
}
