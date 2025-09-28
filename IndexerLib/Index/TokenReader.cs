using IndexerLib.Helpers;
using IndexerLib.Tokens;
using System;
using System.IO;

namespace IndexerLib.Index
{
    public class TokenReader
    {
        private readonly FileStream _stream;
        private readonly MyBinaryReader _reader;
        private readonly long _start;
        private readonly long _end;
        private long position;
        private bool _endOfBlock;

        public TokenReader(FileStream stream, MyBinaryReader reader, IndexKey key)
        {
            _start = key.Offset;
            _end = key.Offset + key.Length;
            position = _start;
        }

        public Token ReadNextToken()
        {
            if (_endOfBlock || position >= _end)
                return null;

            lock (_stream)
            {
                _stream.Seek(position, SeekOrigin.Begin);

                try
                {
                    var token = Serializer.DeserializeToken(_reader);
                    position = _stream.Position;
                    return token;
                }
                catch (EndOfStreamException)
                {
                    _endOfBlock = true;
                    return null;
                }
            }
        }

        public void Reset()
        {
            position = _start;
            _endOfBlock = false;
        }
    }
}
