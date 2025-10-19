using IndexerLib.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace IndexerLib.Index
{
    public class IndexWriter : IndexerBase, IDisposable
    {
        protected readonly FileStream _dataStream;
        protected readonly FileStream _indexKeyStream;
        protected readonly BinaryWriter _dataWriter;
        protected readonly BinaryWriter _indexKeyWriter;
        protected readonly string _tempIndexPath;
        protected readonly SHA256 _sha256;

        protected long currentOffset = 0; // instead of intf
        protected HashSet<string> _words;

        public IndexWriter(string name = "")
        {
            if (!string.IsNullOrEmpty(name))
                TokenStorePath = Path.Combine(IndexDirectoryPath, name + ".tks");

            EnsureUniqueTokenStorePath();
            _tempIndexPath = TokenStorePath + ".keys";

            _dataStream = new FileStream(TokenStorePath, FileMode.OpenOrCreate, FileAccess.Write,
                FileShare.None, bufferSize: 81920, FileOptions.SequentialScan);
            _indexKeyStream = new FileStream(_tempIndexPath, FileMode.OpenOrCreate, FileAccess.Write,
                FileShare.None, bufferSize: 81920, FileOptions.SequentialScan);

            _dataWriter = new BinaryWriter(_dataStream, Encoding.UTF8, leaveOpen: true);
            _indexKeyWriter = new BinaryWriter(_indexKeyStream, Encoding.UTF8, leaveOpen: true);

            _sha256 = SHA256.Create();
            _words = new HashSet<string>(); // HashSet in ensures the uniqueness of the words. 
        }

        public virtual void Put(string key, Stream stream)
        {
            if (stream == null || stream.Length == 0) return;

            if (stream.CanSeek) stream.Position = 0;
            stream.CopyTo(_dataWriter.BaseStream, bufferSize: 81920);

            var hash = _sha256.ComputeHash(Encoding.UTF8.GetBytes(key.Normalize(NormalizationForm.FormC)));

            // write index key
            _indexKeyWriter.Write(hash);
            _indexKeyWriter.Write(currentOffset);
            _indexKeyWriter.Write((int)stream.Length);

            checked { currentOffset += stream.Length; }
            _words.Add(key);
        }

        public void Put(byte[] hash, byte[] data)
        {
            if (hash == null || data == null)
                return;

            _dataWriter.Write(data, 0, data.Length);

            // write index key
            _indexKeyWriter.Write(hash);
            _indexKeyWriter.Write(currentOffset);
            _indexKeyWriter.Write(data.Length);

            checked { currentOffset += data.Length; }
        }

        public void Dispose()
        {
            _sha256.Dispose();

            _indexKeyWriter.Flush();
            _indexKeyWriter.Dispose();
            _indexKeyStream.Dispose();

            AppendKeys();

            _dataWriter.Flush();
            _dataWriter.Dispose();
            _dataStream.Dispose();

            if (_words.Count > 0)
                WordsStore.AddWords(_words);
        }

        void AppendKeys()
        {
            using (var tempIndexStream = new FileStream(_tempIndexPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(tempIndexStream, Encoding.UTF8, leaveOpen: true))
            {
                // Build footer: high 16 bits = magic marker, low 48 bits = index length
                ulong footer = ((ulong)MagicMarker << 48) | (ulong)tempIndexStream.Length;

                var entries = new List<IndexKey>();
                while (tempIndexStream.Position < tempIndexStream.Length)
                    entries.Add(new IndexKey(reader));

                var sortedIndex = entries.OrderBy(k => k.Hash, new ByteArrayComparer());

                // Append sorted index to data stream
                foreach (var entry in sortedIndex)
                {
                    _dataWriter.Write(entry.Hash);
                    _dataWriter.Write(entry.Offset);
                    _dataWriter.Write(entry.Length);
                }

                _dataWriter.Write(footer);
            }

            if (File.Exists(_tempIndexPath))
                File.Delete(_tempIndexPath);
        }
    }
}
