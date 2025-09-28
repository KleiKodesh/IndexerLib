using IndexerLib.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace IndexerLib.Index
{
    /// <summary>
    /// Reads token data from a custom index file format.
    /// Supports retrieving stored blocks by string key or hash,
    /// as well as enumerating all indexed keys.
    /// </summary>
    public class IndexReader : IndexerBase, IDisposable
    {
        const ushort MagicMarker = 0xCAFE;  // Marker value used in footer to verify file integrity

        public readonly SHA256 Sha256;                   // Hashing algorithm used for generating key identifiers (SHA-256)
        public readonly ByteArrayComparer ByteComparer;  // Custom comparer for comparing byte[] hashes in binary search
        public FileStream FileStream { get; private set; }          // File stream pointing to the index file
        readonly MyBinaryReader _reader;           // Custom binary reader that supports 7-bit encoding

        private long _indexStart;     // The starting byte offset of the index table within the file
        private int _indexCount;      // Total number of entries (keys) in the index


        IEnumerator<IndexKey> _enumerator;
        public IEnumerator<IndexKey> Enumerator
        {
            get
            {
                if (_enumerator == null)
                    _enumerator = GetAllKeys().GetEnumerator();
                return _enumerator;
            }
        }

        /// <summary>
        /// Initializes an IndexReader instance for reading stored tokens.
        /// </summary>
        public IndexReader(string path = "")
        {
            if(!string.IsNullOrEmpty(path))
                TokenStorePath = path;

            EnsureTokenStorePath();
            if (!File.Exists(TokenStorePath))
                throw new Exception("Index file does not exist");

            FileStream = new FileStream(TokenStorePath, FileMode.Open, FileAccess.Read);
            _reader = new MyBinaryReader(FileStream, Encoding.UTF8, leaveOpen: true);
            Sha256 = SHA256.Create();
            ByteComparer = new ByteArrayComparer();

            LoadIndexMetadata();
        }

        /// <summary>
        /// Loads index metadata (footer containing magic marker and index start position).
        /// </summary>
        void LoadIndexMetadata()
        {
            if (FileStream.Length < 8) // Minimum size must contain footer
                return;

            // Seek to last 8 bytes (footer structure)
            FileStream.Seek(-8, SeekOrigin.End);
            ulong footer = new MyBinaryReader(FileStream).ReadUInt64();

            // Validate footer magic marker (high 16 bits)
            if ((ushort)(footer >> 48) != MagicMarker)
                throw new InvalidDataException("Invalid footer/magic marker");

            // Extract index length (low 48 bits of footer)
            long indexLength = (long)(footer & 0xFFFFFFFFFFFF);

            // Calculate starting offset of index table
            _indexStart = FileStream.Length - 8 - indexLength;

            // Read total number of index entries
            FileStream.Seek(_indexStart, SeekOrigin.Begin);
            _indexCount = _reader.ReadInt32();
        }

        /// <summary>
        /// Enumerates all keys (hash, offset, length) stored in the index.
        /// </summary>
        public IEnumerable<IndexKey> GetAllKeys()
        {
            if (FileStream.Length < 8)
                yield return null;

            FileStream.Seek(_indexStart, SeekOrigin.Begin);

            int keysCount = _reader.ReadInt32();
            for (int i = 0; i < keysCount; i++)
            {
                yield return new IndexKey
                {
                    Hash = _reader.ReadBytes(32),   // SHA-256 hash of the key
                    Offset = _reader.ReadInt64(),   // File offset of the data block
                    Length = _reader.ReadInt32()    // Size of the data block
                };
            }
        }

        /// <summary>
        /// Retrieves a stored block given its string key with precomputed code.
        /// </summary>
        public byte[] GetTokenDataByPos(int pos)
        {
           var indexKey = GetIndexKeyByPos(pos);

           if (indexKey == null)
                return null;

            // Seek to the data location and read the actual token data
            _reader.BaseStream.Seek(indexKey.Offset, SeekOrigin.Begin);
            return _reader.ReadBytes(indexKey.Length);
        }

        public IndexKey GetIndexKeyByPos(int pos)
        {

            if (pos < 0)
                return null;

            // Compute entry position directly
            long entryPos = _indexStart + 4 + (pos * 44); // pos = entry number
            _reader.BaseStream.Seek(entryPos, SeekOrigin.Begin);

            return new IndexKey
            {
                Hash = _reader.ReadBytes(32),
                Offset = _reader.ReadInt64(),
                Length = _reader.ReadInt32()
            };
        }

        /// <summary>
        /// Reads a data block from the file based on its index entry.
        /// </summary>
        public byte[] ReadBlock(IndexKey entry)
        {
            var prevPos = FileStream.Position; // Save current position to restore later

            FileStream.Seek(entry.Offset, SeekOrigin.Begin);

            byte[] buffer = new byte[entry.Length];
            _reader.Read(buffer, 0, buffer.Length);

            // Restore original stream position
            FileStream.Position = prevPos;

            return buffer;
        }

        /// <summary>
        /// Releases file and reader resources.
        /// </summary>
        public void Dispose()
        {
            _reader?.Dispose();
            FileStream.Dispose();
            Sha256?.Dispose();
        }
    }
}