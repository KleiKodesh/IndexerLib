using IndexerLib.Helpres;
using IndexerLib.Index;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IndexerLib.IndexManger
{
    public static class IndexMerger 
    {
        public static string Merge()
        {
            var startTime = DateTime.Now;
            Console.WriteLine($"Merge Start: {startTime}");

            var files = Directory.GetFiles(new IndexerBase().IndexDirectoryPath, "*.tks");

            string writerPath;
            using (var writer = new IndexWriter())
            {
                writerPath = writer.TokenStorePath;
                var indexReaders = new List<IndexReader>();

                foreach (var file in files)
                {
                    if (file == writer.TokenStorePath)
                        continue;

                    var newReader = new IndexReader();
                    var enumerator = newReader.GetAllKeys().GetEnumerator();
                    if (enumerator.MoveNext())
                        indexReaders.Add(newReader);
                }

                ReadAndMerge(indexReaders, writer);

                foreach (var indexReader in indexReaders)
                    indexReader.Dispose();

                foreach (var file in files)
                    if (File.Exists(file) && file != writer.TokenStorePath)
                        File.Delete(file);

            }

            var timeNow = DateTime.Now;
            Console.WriteLine($"Merge Ended: {timeNow} Total: {startTime - timeNow}");
            return writerPath;
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

            while (activeReaders.Count > 0)
            {
                // Find the smallest hash
                var minEntry = activeReaders
                    .Where(e => e.Enumerator.Current != null)
                    .OrderBy(e => e.Enumerator.Current.Hash, comparer)
                    .First();

                var currentHash = minEntry.Enumerator.Current.Hash;

                // Collect all readers with the same hash
                var matches = activeReaders
                   .Where(e => comparer.Compare(e.Enumerator.Current.Hash, currentHash) == 0)
                   .ToList();

                // Merge and write the block
                var merged = MergeBlocks(matches.Select(m => m));
                writer.Put(merged, currentHash);

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
            var mergedTokens = new Dictionary<int, byte[]>(); // tokenID → tokenBytes

            foreach (var reader in indexReaders)
            {
                var key = reader.Enumerator.Current;
                var block = reader.ReadBlock(key);
                if (block == null) continue;

                using (var ms = new MemoryStream(block))
                using (var bReader = new MyBinaryReader(ms, Encoding.UTF8))
                {
                    while (ms.Position < ms.Length)
                    {
                        var startPos = ms.Position;
                        var tokenId = bReader.Read7BitEncodedInt();
                        int count = bReader.Read7BitEncodedInt();
                        for (int i = 0; i < count * 3; i++)
                            bReader.Read7BitEncodedInt();

                        var length = ms.Position - startPos;
                        var buffer = new byte[length];
                        ms.Position = startPos;
                        bReader.Read(buffer, 0, buffer.Length);
                        mergedTokens[tokenId] = buffer;

                        //multply the count by *  lenngth of 7BitEncodedInt to get length of entry
                        //ms.Position = startPos;
                        // add to length the diff
                        // get block from start pos to finalazed length
                        // mergedTokens[tokenId] = block
                        // get next block
                    }
                }
            }

            // Merge all token bytes
            int totalSize = mergedTokens.Sum(t => t.Value.Length);
            var result = new byte[totalSize];
            int offset = 0;

            foreach (var pair in mergedTokens.OrderBy(p => p.Key)) // sort optional
            {
                Buffer.BlockCopy(pair.Value, 0, result, offset, pair.Value.Length);
                offset += pair.Value.Length;
            }

            return result;
        }
    }
}
