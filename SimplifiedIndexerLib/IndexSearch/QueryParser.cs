using SimplifiedIndexerLib.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplifiedIndexerLib.IndexSearch
{
    internal class QueryParser
    {
         // uses an iterator to calcaulte postion of word in index based on its postion in the wordstore
        public static List<List<int>> GenerateWordPositions(string query)
        {
            var splitQuery = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var wordsStore = WordsStore.GetWords();

            // Prepare result structure
            var result = new List<List<int>>(splitQuery.Length);
            for (int i = 0; i < splitQuery.Length; i++)
                result.Add(new List<int>());

            // Iterate with index tracking
            int position = 0;
            foreach (var word in wordsStore)
            {
                for (int x = 0; x < splitQuery.Length; x++)
                {
                    if (IsWildcardMatch(splitQuery[x], word))
                        result[x].Add((position));
                }
                position++;
            }

            return result;
        }

        private static bool IsWildcardMatch(string pattern, string input)
        {
            // Step 0: pre-check length
            int minLen = 0, maxLen = 0;
            foreach (var ch in pattern)
            {
                if (ch == '?')
                {
                    minLen++;
                    maxLen++;
                }
                else if (ch == '*')
                {
                    maxLen += 5; // * matches 0..5 chars
                }
                else
                {
                    minLen++;
                    maxLen++;
                }
            }

            if (input.Length < minLen || input.Length > maxLen)
                return false; // cannot possibly match

            // Step 1: original matching
            int p = 0, s = 0;
            int starIdx = -1, match = 0, starCount = 0;

            while (s < input.Length)
            {
                if (p < pattern.Length && pattern[p] == input[s])
                {
                    p++;
                    s++;
                }
                else if (p < pattern.Length && pattern[p] == '*')
                {
                    starIdx = p++;
                    match = s;
                    starCount = 0;
                }
                else if (p < pattern.Length && pattern[p] == '?')
                {
                    p++;
                }
                else if (starIdx != -1 && starCount < 5)
                {
                    p = starIdx + 1;
                    s = ++match;
                    starCount++;
                }
                else
                {
                    return false;
                }
            }

            while (p < pattern.Length && (pattern[p] == '*' || pattern[p] == '?'))
                p++;

            return p == pattern.Length;
        }
    }
}
