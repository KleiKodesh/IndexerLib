using SimplifiedIndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimplifiedIndexerLib.IndexSearch
{
    public static class TokenGrouping
    {
        public static Dictionary<int, List<List<int>>> Execute(List<List<Token>> tokenLists)
        {
            var result = new Dictionary<int, List<List<int>>>();
            int requiredCount = tokenLists.Count;

            for (int i = 0; i < tokenLists.Count; i++)
            {
                var tokenList = tokenLists[i];
                if (tokenList == null || tokenList.Count == 0)
                    break;

                foreach (var kv in GroupById(tokenList))
                {
                    if (i == 0)
                        result[kv.Key] = new List<List<int>> { kv.Value };
                    else if (result.TryGetValue(kv.Key, out var existing))
                        existing.Add(kv.Value);
                    else
                        continue;

                    kv.Value.Sort();
                }

                tokenList.Clear();
            }

            // keep only docs that appear in all tokenLists
            var filtered = result
                .Where(r => r.Value.Count >= requiredCount)
                .OrderBy(r => r.Key)
                .ToDictionary(r => r.Key, r => r.Value);

            return filtered;
        }

        static Dictionary<int, List<int>> GroupById(List<Token> tokenList)
        {
            var grouped = new Dictionary<int, List<int>>();

            foreach (var t in tokenList)
            {
                if (!grouped.TryGetValue(t.DocId, out var posList))
                {
                    posList = new List<int>();
                    grouped[t.DocId] = posList;
                }
                posList.AddRange(t.Postions);
            }

            return grouped;
        }
    }
}
