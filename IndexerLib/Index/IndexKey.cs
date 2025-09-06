
    namespace IndexerLib.Index
    {
        /// <summary>
        /// Represents a key entry in the index.
        /// Stores the hash identifier, the offset within the storage,
        /// and the length of the associated data block.
        /// </summary>
        public class IndexKey
        {
            /// <summary>
            /// The unique hash value that identifies the key (e.g., SHA).
            /// </summary>
            public byte[] Hash { get; set; }

            /// <summary>
            /// The position (offset) in the data store where the record begins.
            /// Useful for direct access without scanning the entire file.
            /// </summary>
            public long Offset { get; set; }

            /// <summary>
            /// The length of the stored data block in bytes.
            /// Helps in reading the correct segment of data starting from <see cref="Offset"/>.
            /// </summary>
            public int Length { get; set; }
        }
    }


