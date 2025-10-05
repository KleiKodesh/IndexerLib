using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IndexerLib.Index
{
    public class IndexReader : IndexerBase, IDisposable
    {
        protected readonly FileStream _indexStream;   // used only for index traversal
        protected readonly BinaryReader _indexReader;

        protected readonly FileStream _dataStream;    // used only for block reads

        protected readonly long _indexStart;
        protected readonly long _indexLength;

        protected const int RecordSize = 32 + sizeof(long) + sizeof(int);
        // 32 bytes hash + 8 bytes offset + 4 bytes length = 44 bytes per record

        private IEnumerator<IndexKey> _enumerator;
        public IEnumerator<IndexKey> Enumerator
        {
            get
            {
                if (_enumerator == null)
                    _enumerator = GetAllKeys().GetEnumerator();
                return _enumerator;
            }
        }

        public IndexReader(string path = "")
        {
            if (!string.IsNullOrEmpty(path))
                TokenStorePath = path;

            EnsureTokenStorePath();

            // one stream for reading index records
            _indexStream = new FileStream(TokenStorePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 81920);
            _indexReader = new BinaryReader(_indexStream, Encoding.UTF8, leaveOpen: true);

            // separate stream for block data
            _dataStream = new FileStream(TokenStorePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 81920);

            (_indexStart, _indexLength) = LocateIndex();
        }

        private (long indexStart, long indexLength) LocateIndex()
        {
            if (_indexStream.Length < sizeof(ulong))
                throw new InvalidDataException("Invalid index file: too small.");

            _indexStream.Seek(-sizeof(ulong), SeekOrigin.End);
            ulong footer = _indexReader.ReadUInt64();

            ushort magic = (ushort)(footer >> 48);
            if (magic != MagicMarker)
                throw new InvalidDataException("Invalid magic marker. File might be corrupted or incompatible.");

            long indexLength = (long)(footer & 0xFFFFFFFFFFFF); // low 48 bits
            long indexStart = _indexStream.Length - sizeof(ulong) - indexLength;

            if (indexStart < 0)
                throw new InvalidDataException("Invalid index length in footer.");

            return (indexStart, indexLength);
        }

        public byte[] GetDataByIndex(int index)
        {
            long entryOffset = _indexStart + (index * RecordSize);

            if (entryOffset + RecordSize > _indexStart + _indexLength)
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range for index table.");

            _indexStream.Seek(entryOffset + 32, SeekOrigin.Begin); // skip hash

            long dataOffset = _indexReader.ReadInt64();
            int dataLength = _indexReader.ReadInt32();

            return ReadBlock(dataOffset, dataLength);
        }

        public byte[] ReadBlock(long offset, int length)
        {
            _dataStream.Seek(offset, SeekOrigin.Begin);
            byte[] data = new byte[length];
            _dataStream.Read(data, 0, length);

            return data;
        }

        public IEnumerable<IndexKey> GetAllKeys()
        {
            _indexStream.Seek(_indexStart, SeekOrigin.Begin);
            long recordCount = _indexLength / RecordSize;

            for (long i = 0; i < recordCount; i++)
            {
                if (_indexStream.Position + RecordSize > _indexStart + _indexLength)
                    yield break;

                yield return new IndexKey(_indexReader);
            }
        }


        //public IEnumerable<KeyValuePair<string, List<Token>>> GetAllTokens()
        //{
        //    int recordCount = (int)_indexLength / RecordSize;

        //    var words = WordsStore.GetWords().ToList();

        //    for (int i = 0; i < recordCount; i++)
        //    {
        //        var data = GetDataByIndex(i);
        //        var tokens = Serializer.DeserializeTokenGroup(data);

        //        yield return new KeyValuePair<string, List<Token>>(words[i], tokens);
        //    }
        //}

        public void Dispose()
        {
            _indexReader?.Dispose();
            _indexStream?.Dispose();
            _dataStream?.Dispose();
        }
    }
}
