using SimplifiedIndexerLib.Index;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimplifiedIndexerLib.IndexSearch
{
    internal class QueryParser
    {
        public static List<List<int>> GenerateWordPositions(string query)
        {
            var splitQuery = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var words = WordsStore.GetWords().ToList();
            var result = new List<List<int>>();

            foreach (var term in splitQuery)
            {
                var positions = new List<int>();
                if (!term.Contains('*') && !term.Contains('?'))
                {
                    // Exact match using Array.IndexOf
                    int pos = words.IndexOf(term);
                    while (pos != -1)
                    {
                        positions.Add(pos);
                        pos = words.IndexOf(term, pos + 1);
                    }
                }
                else
                {
                    // Wildcard match
                    for (int i = 0; i < words.Count; i++)
                    {
                        if (IsWildcardMatch(term, words[i]))
                            positions.Add(i);
                    }
                }
                result.Add(positions);
            }


            return result;
        }

        private static bool IsWildcardMatch(string pattern, string input)
        {
            return MatchHelper(pattern, 0, input, 0);
        }

        private static bool MatchHelper(string pattern, int pIdx, string input, int sIdx)
        {
            while (pIdx < pattern.Length)
            {
                if (pattern[pIdx] == '*')
                {
                    // * matches 0 or more characters
                    for (int k = sIdx; k <= input.Length; k++)
                    {
                        if (MatchHelper(pattern, pIdx + 1, input, k))
                            return true;
                    }
                    return false;
                }
                else if (pattern[pIdx] == '?')
                {
                    // ? matches zero or one character: try both
                    if (MatchHelper(pattern, pIdx + 1, input, sIdx)) return true; // zero chars
                    if (sIdx < input.Length && MatchHelper(pattern, pIdx + 1, input, sIdx + 1)) return true; // one char
                    return false;
                }
                else
                {
                    // literal match
                    if (sIdx >= input.Length || pattern[pIdx] != input[sIdx])
                        return false;
                    pIdx++;
                    sIdx++;
                }
            }

            // pattern exhausted, input must also be exhausted
            return sIdx == input.Length;
        }


    }
}
