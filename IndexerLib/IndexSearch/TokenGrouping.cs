using IndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexerLib.IndexSearch
{
    public static class TokenGrouping
    {
        /// <summary>
        /// Groups token lists by document ID, aligning postings for multi-term queries.
        /// </summary>
        /// <param name="tokenLists">List of token lists (each representing a term).</param>
        public static Dictionary<int, List<List<Postings>>> Execute(List<List<Token>> tokenLists)
        {
            var result = new Dictionary<int, List<List<Postings>>>();
            int requiredCount = tokenLists.Count;

            for (int i = 0; i < tokenLists.Count; i++)
            {
                var tokenList = tokenLists[i];
                if (tokenList == null || tokenList.Count == 0)
                    break;

                foreach (var kv in GroupById(tokenList))
                {
                    if (i == 0)
                        result[kv.Key] = new List<List<Postings>> { kv.Value };
                    else if (result.TryGetValue(kv.Key, out var existing))
                        existing.Add(kv.Value);
                    else
                        continue;

                    // sort postings by position for each term
                    kv.Value.Sort((a, b) => a.Position.CompareTo(b.Position));
                }

                tokenList.Clear();
            }

            // keep only docs that appear in all tokenLists (intersection)
            foreach (var key in result.Keys.ToList())
                if (result[key].Count != requiredCount)
                    result.Remove(key);

            return result;
        }

        /// <summary>
        /// Groups tokens by DocId, merging all their postings for that doc.
        /// </summary>
        private static Dictionary<int, List<Postings>> GroupById(List<Token> tokenList)
        {
            var grouped = new Dictionary<int, List<Postings>>(tokenList.Count);

            foreach (var t in tokenList)
            {
                if (!grouped.TryGetValue(t.DocId, out var postings))
                {
                    postings = new List<Postings>();
                    grouped[t.DocId] = postings;
                }

                postings.AddRange(t.Postings);
            }

            return grouped;
        }
    }
}
