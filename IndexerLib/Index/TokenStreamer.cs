using IndexerLib.Helpers;
using IndexerLib.Tokens;
using System.Collections.Generic;
using System.Linq;

namespace IndexerLib.Index
{
    /// <summary>
    /// Represents a collection of <see cref="TokenStreamer"/> instances that can be advanced in sync
    /// based on their current <see cref="Token.DocId"/> values.
    /// </summary>
    public class TokenStreamerList : List<TokenStreamer> //?? use arrays instead of lists??
    {
        /// <summary>
        /// Gets the lowest <see cref="Token.DocId"/> currently pointed to by any active streamer.
        /// </summary>
        public int MinDocId { get; private set; }


        public TokenStreamerList()
        {
            MinDocId = int.MaxValue;
        }

        public void AddStreamer(TokenStreamer streamer)
        {
            Add(streamer);
            if (Count == 1)
                MinDocId = streamer.Current.DocId;
            else if (streamer.Current.DocId < MinDocId)
                MinDocId = streamer.Current.DocId;
        }

        /// <summary>
        /// Advances all <see cref="TokenStreamer"/> instances that currently point to <see cref="MinDocId"/>.
        /// Updates <see cref="MinDocId"/> to the next smallest available document ID.
        /// Streamers that reach the end of their data are removed from the list.
        /// </summary>
        /// <returns><c>true</c> if at least one streamer advanced; otherwise, <c>false</c>.</returns>
        public bool MoveNext()
        {
            bool advanced = false;

            // Iterate backward to safely remove streamers inline while looping.
            for (int i = Count - 1; i >= 0; i--)
            {
                var streamer = this[i];

                if (streamer.Current.DocId <= MinDocId)
                {
                    if (streamer.MoveNext())
                        advanced = true;
                    else // Remove exhausted streamers
                        RemoveAt(i);
                }
            }

            if (advanced && Count > 0)
                MinDocId = this.Min(s => s.Current.DocId);

            return advanced;
        }

        /// <summary>
        /// Returns an enumerable of all <see cref="TokenStreamer"/> instances
        /// currently positioned at the current <see cref="MinDocId"/>.
        /// </summary>
        public IEnumerable<Postings> CurrentPostings =>
            this.Where(ts => ts.Current.DocId == MinDocId)
            .SelectMany(s => s.Current.Postings)
            .OrderBy(p => p.Position);
        //OrderBy(s => s.Postings) assumes Postings is comparable and meaningful here.
        // however Postings is a complex listt, you might want to sort differently(e.g., by position).
    }

    /// <summary>
    /// Streams <see cref="Token"/> entries sequentially from a binary index segment.
    /// </summary>
    public class TokenStreamer
    {
        private readonly MyBinaryReader _reader;
        private readonly long _end;
        private long _pos;

        /// <summary>
        /// Gets the current <see cref="Token"/> in the stream.
        /// </summary>
        public Token Current { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="TokenStreamer"/> for the specified index segment.
        /// </summary>
        /// <param name="reader">The binary reader to use for reading tokens.</param>
        /// <param name="key">The index key defining the segment range.</param>
        public TokenStreamer(MyBinaryReader reader, IndexKey key)
        {
            _reader = reader;
            _end = key.Offset + key.Length;
            _pos = key.Offset;
            MoveNext();
        }

        /// <summary>
        /// Advances the streamer to the next <see cref="Token"/> in the segment.
        /// </summary>
        /// <returns><c>false</c> if there are no more tokens to read; otherwise, <c>true</c>.</returns>
        public bool MoveNext()
        {
            if (_pos >= _end)
                return false;

            _reader.BaseStream.Position = _pos;
            Current = Serializer.DeserializeToken(_reader);
            _pos = _reader.BaseStream.Position;
            return true;
        }
    }
}
