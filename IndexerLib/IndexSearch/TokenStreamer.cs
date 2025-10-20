using IndexerLib.Helpers;
using IndexerLib.Index;
using IndexerLib.Tokens;

namespace IndexerLib.IndexSearch
{
    /// <summary>
    /// Sequentially reads (streams) <see cref="Token"/> entries from a binary segment 
    /// within the inverted index file.
    ///
    /// Each <see cref="TokenStreamer"/> corresponds to a token block in the custom binary file 
    /// that forms part of the inverted index. 
    /// It acts as a lightweight, forward-only iterator over token data stored on disk,
    /// conceptually similar to <see cref="System.Collections.IEnumerator"/>.
    /// 
    /// By streaming tokens instead of loading them all into memory, 
    /// this class enables efficient processing of very large index files.
    /// It forms the foundation of the <see cref="StreamingSearch"/> class — 
    /// a RAM-efficient, streaming-based search mechanism.
    ///
    /// In essence, <see cref="TokenStreamer"/> is a thin wrapper around a <see cref="MyBinaryReader"/>,
    /// maintaining the current read position within a segment and exposing the current <see cref="Token"/>.
    /// </summary>
    public sealed class TokenStreamer
    {
        private readonly MyBinaryReader _reader;
        private readonly long _end;
        private long _pos;

        /// <summary>
        /// Gets the current <see cref="Token"/> that was read from the index stream 
        /// by the most recent call to <see cref="MoveNext"/>.
        /// </summary>
        public Token Current { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenStreamer"/> class for the specified index segment.
        /// Each <paramref name="key"/> defines where (offset + length) this term’s data resides
        /// within the global index stream. The streamer iterates sequentially from <c>key.Offset</c>
        /// up to <c>key.Offset + key.Length</c>.
        /// </summary>
        /// <param name="reader">
        /// A globally shared binary reader used to read tokens across all token streams.
        /// </param>
        /// <param name="key">
        /// The index key that defines the byte range of this segment within the index.
        /// </param>
        public TokenStreamer(MyBinaryReader reader, IndexKey key)
        {
            _reader = reader;
            _end = key.Offset + key.Length;
            _pos = key.Offset;

            // Prime the streamer by reading the first token in the block.
            MoveNext();
        }

        /// <summary>
        /// Advances the streamer to the next <see cref="Token"/> in the binary segment.
        /// </summary>
        /// <returns>
        /// <c>true</c> if another token was successfully read; 
        /// otherwise, <c>false</c> if the end of the segment was reached.
        /// </returns>
        public bool MoveNext()
        {
            if (_pos >= _end)
                return false; // Reached end of the block.

            // Position the stream at the last known read offset.
            if (_reader.BaseStream.Position != _pos)
                _reader.BaseStream.Position = _pos;


            // Deserialize and store the next token.
            Current = Serializer.DeserializeToken(_reader);

            // Update pointer to next token position.
            _pos = _reader.BaseStream.Position;
            return true;
        }
    }
}
