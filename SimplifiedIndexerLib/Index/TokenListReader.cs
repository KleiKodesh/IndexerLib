using SimplifiedIndexerLib.IndexSearch;
using SimplifiedIndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.IO;

namespace SimplifiedIndexerLib.Index
{
    public class TokenListReader : IndexReader
    {
        public List<List<Token>> GetByIndex(List<TermQuery> indexLists)
        {
            // Pre-allocate result lists
            var tokenLists = new List<List<Token>>(indexLists.Count);
            for (int i = 0; i < indexLists.Count; i++)
                tokenLists.Add(new List<Token>(indexLists[i].Positions.Count));

            // Build request map: pos → list indices
            var requestMap = BuildRequestMap(indexLists);
            if (requestMap.Count == 0)
                return tokenLists;

            // Step 1: collect blocks by scanning index sequentially
            var blocks = ReadIndexEntriesSorted(requestMap);

            // Step 2: sort blocks by dataOffset for sequential reads
            blocks.Sort((a, b) => a.offset.CompareTo(b.offset));

            // Step 3: read & distribute
            DistributeTokens(blocks, tokenLists);

            return tokenLists;
        }

        private static Dictionary<int, List<int>> BuildRequestMap(List<TermQuery> indexLists)
        {
            int estimatedSize = 0;
            for (int i = 0; i < indexLists.Count; i++)
                estimatedSize += indexLists[i].Positions.Count;

            var requestMap = new Dictionary<int, List<int>>(estimatedSize);

            for (int listIdx = 0; listIdx < indexLists.Count; listIdx++)
            {
                var positions = indexLists[listIdx].Positions;
                for (int j = 0; j < positions.Count; j++)
                {
                    int pos = positions[j];
                    if (!requestMap.TryGetValue(pos, out var lists))
                    {
                        lists = new List<int>(1);
                        requestMap[pos] = lists;
                    }
                    lists.Add(listIdx);
                }
            }

            return requestMap;
        }

        private List<(long offset, int length, List<int> lists)> ReadIndexEntriesSorted(Dictionary<int, List<int>> requestMap)
        {
            // Sort positions so index entries are read sequentially
            var positions = new List<int>(requestMap.Keys);
            positions.Sort();

            var blocks = new List<(long offset, int length, List<int> lists)>(positions.Count);

            for (int i = 0; i < positions.Count; i++)
            {
                int pos = positions[i];
                long entryOffset = _indexStart + (pos * RecordSize);

                if (entryOffset + RecordSize > _indexStart + _indexLength)
                    continue; // skip invalid

                _indexStream.Seek(entryOffset + 32, SeekOrigin.Begin); // skip hash

                long dataOffset = _indexReader.ReadInt64();
                int dataLength = _indexReader.ReadInt32();

                blocks.Add((dataOffset, dataLength, requestMap[pos]));
            }

            return blocks;
        }

        private void DistributeTokens(List<(long offset, int length, List<int> lists)> blocks, List<List<Token>> tokenLists)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                var (offset, length, lists) = blocks[i];
                var data = ReadBlock(offset, length);
                if (data == null || data.Length == 0)
                    continue;

                var tokenGroup = Serializer.DeserializeTokenGroup(data);

                for (int j = 0; j < lists.Count; j++)
                {
                    int listIdx = lists[j];
                    tokenLists[listIdx].AddRange(tokenGroup);
                }
            }
        }
    }
}
