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
        readonly FileStream _fileStream;           // File stream pointing to the index file
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

            _fileStream = new FileStream(TokenStorePath, FileMode.Open, FileAccess.Read);
            _reader = new MyBinaryReader(_fileStream, Encoding.UTF8, leaveOpen: true);
            Sha256 = SHA256.Create();
            ByteComparer = new ByteArrayComparer();

            LoadIndexMetadata();
        }

        /// <summary>
        /// Loads index metadata (footer containing magic marker and index start position).
        /// </summary>
        void LoadIndexMetadata()
        {
            if (_fileStream.Length < 8) // Minimum size must contain footer
                return;

            // Seek to last 8 bytes (footer structure)
            _fileStream.Seek(-8, SeekOrigin.End);
            ulong footer = new MyBinaryReader(_fileStream).ReadUInt64();

            // Validate footer magic marker (high 16 bits)
            if ((ushort)(footer >> 48) != MagicMarker)
                throw new InvalidDataException("Invalid footer/magic marker");

            // Extract index length (low 48 bits of footer)
            long indexLength = (long)(footer & 0xFFFFFFFFFFFF);

            // Calculate starting offset of index table
            _indexStart = _fileStream.Length - 8 - indexLength;

            // Read total number of index entries
            _fileStream.Seek(_indexStart, SeekOrigin.Begin);
            _indexCount = _reader.ReadInt32();
        }

        /// <summary>
        /// Enumerates all keys (hash, offset, length) stored in the index.
        /// </summary>
        public IEnumerable<IndexKey> GetAllKeys()
        {
            if (_fileStream.Length < 8)
                yield return null;

            _fileStream.Seek(_indexStart, SeekOrigin.Begin);

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
            if (pos < 0)
                return null;

            // Compute entry position directly
            long entryPos = _indexStart + 4 + (pos * 44); // pos = entry number
            _reader.BaseStream.Seek(entryPos, SeekOrigin.Begin);

            // Read entry from index
            byte[] entryHash = _reader.ReadBytes(32);
            long offset = _reader.ReadInt64();
            int length = _reader.ReadInt32();

            // Seek to the data location and read the actual token data
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            return _reader.ReadBytes(length);
        }

        /// <summary>
        /// Reads a data block from the file based on its index entry.
        /// </summary>
        public byte[] ReadBlock(IndexKey entry)
        {
            var prevPos = _fileStream.Position; // Save current position to restore later

            _fileStream.Seek(entry.Offset, SeekOrigin.Begin);

            byte[] buffer = new byte[entry.Length];
            _reader.Read(buffer, 0, buffer.Length);

            // Restore original stream position
            _fileStream.Position = prevPos;

            return buffer;
        }

        /// <summary>
        /// Releases file and reader resources.
        /// </summary>
        public void Dispose()
        {
            _reader?.Dispose();
            _fileStream.Dispose();
            Sha256?.Dispose();
        }
    }
}

///// <summary>
///// Retrieves a stored block given its string key.
///// </summary>
//public byte[] GetTokenData(string key)
//{
//    var hash = Sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
//    return GetTokenDataByHash(hash);
//}

///// <summary>
///// Retrieves a stored block given its hash.
///// </summary>
//public byte[] GetTokenDataByHash(byte[] hash)
//{
//    if (hash == null)
//        return null;

//    // Find matching entry using binary search
//    var entry = BinarySearchIndex(hash);
//    if (entry == null)
//        return null;

//    // Read and return block from file
//    var block = ReadBlock(entry);
//    if (block == null)
//        return null;

//    return block;
//}

///// <summary>
///// Performs a binary search on the sorted index table to find a given hash.
///// </summary>
//IndexKey BinarySearchIndex(byte[] targetHash)
//{
//    int low = 0, high = _indexCount - 1;

//    while (low <= high)
//    {
//        int mid = (low + high) / 2;

//        // Calculate entry position: index start + 4-byte count header + N*entrySize
//        long entryPos = _indexStart + 4 + (mid * 44); // 44 = 32-byte hash + 8-byte offset + 4-byte length
//        _reader.BaseStream.Seek(entryPos, SeekOrigin.Begin);

//        // Read hash for comparison
//        byte[] hash = _reader.ReadBytes(32);

//        int cmp = ByteComparer.Compare(hash, targetHash);

//        if (cmp == 0) // Match found
//        {
//            long offset = _reader.ReadInt64();
//            int length = _reader.ReadInt32();

//            return new IndexKey
//            {
//                Hash = hash,
//                Offset = offset,
//                Length = length
//            };
//        }

//        // Narrow search range
//        if (cmp < 0)
//            low = mid + 1;
//        else
//            high = mid - 1;
//    }

//    return null; // Not found
//}
