using IndexerLib.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IndexerLib.Index
{
    public static class IndexMerger
    {
        public static void Merge()
        {
            var startTime = DateTime.Now;
            Console.WriteLine($"Merge Start: {startTime}");

            var files = new IndexerBase().TokenStoreFileList();
            if (files.Count <= 1)
                return;

            string writerPath;
            using (var writer = new IndexWriter("merged"))
            {
                writerPath = writer.TokenStorePath;
                var indexReaders = new List<IndexReader>();

                foreach (var file in files)
                    indexReaders.Add(new IndexReader(file));

                ReadAndMerge(indexReaders, writer);

                foreach (var indexReader in indexReaders)
                    indexReader.Dispose();

                foreach (var file in files)
                    if (File.Exists(file) && file != writer.TokenStorePath)
                        File.Delete(file);

            }

            var timeNow = DateTime.Now;
            Console.WriteLine($"Merge Ended: {timeNow} Total: {timeNow - startTime}");
        }

        static void ReadAndMerge(List<IndexReader> indexReaders, IndexWriter writer)
        {
            var comparer = new ByteArrayComparer();

            // Preload enumerators
            var activeReaders = new List<IndexReader>();
            foreach (var reader in indexReaders)
            {
                if (reader.Enumerator.MoveNext())
                    activeReaders.Add(reader);
            }

            using (var spinner = new ConsoleSpinner())
                while (activeReaders.Count > 0)
                {
                    // Find the smallest hash
                    var minEntry = activeReaders
                        .Where(e => e.Enumerator.Current != null)
                        .OrderBy(e => e.Enumerator.Current.Hash, comparer)
                        .First();

                    var currentHash = minEntry.Enumerator.Current.Hash;

                    // Collect all readers with the same hash
                    var matches = activeReaders.Where(e => comparer.Compare(e.Enumerator.Current.Hash, currentHash) == 0).
                        OrderBy(m => m.TokenStorePath);

                    //// Merge and write the block
                    var merged = MergeBlocks(matches);
                    writer.Put(currentHash, merged);

                    // Advance all matched enumerators and remove finished ones
                    var stillActive = new List<IndexReader>();

                    foreach (var reader in activeReaders)
                    {
                        if (comparer.Compare(reader.Enumerator.Current.Hash, currentHash) == 0)
                        {
                            if (reader.Enumerator.MoveNext())
                                stillActive.Add(reader);
                        }
                        else
                        {
                            stillActive.Add(reader);
                        }
                    }

                    activeReaders = stillActive;
                }
        }

        static byte[] MergeBlocks(IEnumerable<IndexReader> indexReaders)
        {
            var mergedTokens = new Dictionary<int, byte[]>(); // DocId → serialized token bytes

            foreach (var reader in indexReaders)
            {
                var key = reader.Enumerator.Current;
                var block = reader.ReadBlock(key.Offset, key.Length);
                if (block == null || block.Length == 0)
                    continue;

                using (var ms = new MemoryStream(block))
                using (var bReader = new MyBinaryReader(ms, Encoding.UTF8))
                {
                    while (ms.Position < ms.Length)
                    {
                        // Read DocId and posting count
                        long startPos = ms.Position;
                        int docId = bReader.Read7BitEncodedInt();
                        int count = bReader.Read7BitEncodedInt();

                        // Skip through postings (PositionDelta, StartDelta, Length)
                        int prevPos = 0, prevStart = 0;
                        for (int i = 0; i < count; i++)
                        {
                            int posDelta = bReader.Read7BitEncodedInt();
                            int startDelta = bReader.Read7BitEncodedInt();
                            int len = bReader.Read7BitEncodedInt();
                            prevPos += posDelta;
                            prevStart += startDelta;
                        }

                        // Copy raw token data segment (just as before)
                        long endPos = ms.Position;
                        int tokenLength = (int)(endPos - startPos);
                        ms.Position = startPos;
                        var buffer = new byte[tokenLength];
                        bReader.Read(buffer, 0, buffer.Length);

                        // Store/overwrite DocId entry (last one wins)
                        mergedTokens[docId] = buffer;
                    }
                }
            }

            // Merge all token bytes (same as before)
            int totalSize = mergedTokens.Sum(t => t.Value.Length);
            var result = new byte[totalSize];
            int offset = 0;

            foreach (var pair in mergedTokens.OrderBy(p => p.Key))
            {
                Buffer.BlockCopy(pair.Value, 0, result, offset, pair.Value.Length);
                offset += pair.Value.Length;
            }

            return result;
        }
    }
}
