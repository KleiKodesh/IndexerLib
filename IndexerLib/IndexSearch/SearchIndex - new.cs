//using IndexerLib.Helpers;
//using IndexerLib.Index;
//using IndexerLib.IndexManger;
//using IndexerLib.Tokens;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;

//namespace IndexerLib.IndexSearch
//{
//    public static class SearchIndex
//    {
//        public static IEnumerable<SearchResult> Execute(string query, short adjacency = 2)
//        {
//            var startTime = DateTime.Now;
//            Console.WriteLine("Parsing query..." + DateTime.Now);

//            var posLists = GenerateWordPositions(query);

//            if (posLists.Count > 3)
//                adjacency = (short)(adjacency * (posLists.Count - 2) + posLists.Count);
//            else
//                adjacency = (short)(adjacency + posLists.Count);

//            Console.WriteLine("Opening token readers..." + DateTime.Now);
//            var tokenReaders = CreateTokenReaders(posLists);

//            Console.WriteLine("Grouping & filtering docs..." + DateTime.Now);
//            var validDocs = StreamAndGroupByDocId(tokenReaders);

//            Console.WriteLine("Generating results..." + DateTime.Now);
//            var results = OrderedAdjacencyMatch(validDocs, adjacency);

//            Console.WriteLine("Search complete. Elapsed: " + (DateTime.Now - startTime));
//            return results;
//        }

//        // Uses iterator to calculate position of word in index based on its position in the wordstore
//        private static List<List<int>> GenerateWordPositions(string query)
//        {
//            var splitQuery = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
//            var wordsStore = WordsStore.GetWords();

//            var result = new List<List<int>>(splitQuery.Length);
//            for (int i = 0; i < splitQuery.Length; i++)
//                result.Add(new List<int>());

//            int position = 0;
//            foreach (var word in wordsStore)
//            {
//                for (int x = 0; x < splitQuery.Length; x++)
//                {
//                    if (IsWildcardMatch(splitQuery[x], word))
//                        result[x].Add(position);
//                }
//                position++;
//            }

//            return result;
//        }

//        private static bool IsWildcardMatch(string pattern, string input)
//        {
//            int p = 0, s = 0;
//            int starIdx = -1, match = 0, starCount = 0;

//            while (s < input.Length)
//            {
//                if (p < pattern.Length && pattern[p] == input[s])
//                {
//                    p++;
//                    s++;
//                }
//                else if (p < pattern.Length && pattern[p] == '*')
//                {
//                    starIdx = p++;
//                    match = s;
//                    starCount = 0;
//                }
//                else if (p < pattern.Length && pattern[p] == '?')
//                {
//                    p++;
//                }
//                else if (starIdx != -1 && starCount < 5)
//                {
//                    p = starIdx + 1;
//                    s = ++match;
//                    starCount++;
//                }
//                else
//                {
//                    return false;
//                }
//            }

//            while (p < pattern.Length && (pattern[p] == '*' || pattern[p] == '?'))
//                p++;

//            return p == pattern.Length;
//        }

//        /// <summary>
//        /// Creates TokenReader objects for all positions.
//        /// </summary>
//        private static List<List<TokenReader>> CreateTokenReaders(List<List<int>> posLists)
//        {
//            var readers = new List<List<TokenReader>>(posLists.Count);

//            using (var indexReader = new IndexReader())
//            {
//                var stream = indexReader.FileStream;
//                var reader = new MyBinaryReader(stream);

//                for (int i = 0; i < posLists.Count; i++)
//                {
//                    var list = new List<TokenReader>();
//                    foreach (var pos in posLists[i])
//                    {
//                        var key = indexReader.GetIndexKeyByPos(pos);
//                        if (key != null)
//                            list.Add(new TokenReader(stream, reader, key));
//                    }
//                    readers.Add(list);
//                }
//            }

//            return readers;
//        }

//        /// <summary>
//        /// Streams all tokens from TokenReaders, grouping by DocId and filtering to keep only docs that match all terms.
//        /// </summary>
//        private static Dictionary<int, List<Token>> StreamAndGroupByDocId(List<List<TokenReader>> tokenReaderGroups)
//        {
//            var result = new Dictionary<int, List<Token>>();
//            short termIndex = 0;

//            foreach (var group in tokenReaderGroups)
//            {
//                var currentMatches = new Dictionary<int, List<Token>>();

//                foreach (var reader in group)
//                {
//                    Token token;
//                    while ((token = reader.ReadNextToken()) != null)
//                    {
//                        if (!currentMatches.ContainsKey(token.DocId))
//                            currentMatches[token.DocId] = new List<Token>();

//                        currentMatches[token.DocId].Add(token);
//                    }
//                }

//                if (termIndex == 0)
//                {
//                    foreach (var kvp in currentMatches)
//                        result[kvp.Key] = kvp.Value;
//                }
//                else
//                {
//                    var toRemove = result.Keys.Where(k => !currentMatches.ContainsKey(k)).ToList();
//                    foreach (var k in toRemove)
//                        result.Remove(k);

//                    foreach (var kvp in currentMatches)
//                        if (result.ContainsKey(kvp.Key))
//                            result[kvp.Key].AddRange(kvp.Value);
//                }

//                termIndex++;
//            }

//            return result;
//        }

//        public static IEnumerable<SearchResult> OrderedAdjacencyMatch(Dictionary<int, List<Token>> validDocs, int adjacency)
//        {
//            adjacency -= 1; // adjust adjacency (bugfix)

//            foreach (var docEntry in validDocs.OrderBy(kvp => kvp.Key))
//            {
//                var postingsLists = docEntry.Value
//                    .Select(t => t.Postings.OrderBy(p => p.Position).ToList())
//                    .Where(p => p.Count > 0)
//                    .ToList();

//                if (postingsLists.Count != docEntry.Value.Count)
//                    continue;

//                int i0 = 0;
//                while (i0 < postingsLists[0].Count)
//                {
//                    var currentMatch = new Postings[postingsLists.Count];
//                    currentMatch[0] = postingsLists[0][i0];
//                    int prevPos = currentMatch[0].Position;

//                    bool valid = true;
//                    for (int listIdx = 1; listIdx < postingsLists.Count; listIdx++)
//                    {
//                        var plist = postingsLists[listIdx];
//                        int j = 0;

//                        while (j < plist.Count && plist[j].Position - prevPos <= 0)
//                            j++;

//                        if (j >= plist.Count || plist[j].Position - prevPos > adjacency)
//                        {
//                            valid = false;
//                            break;
//                        }

//                        currentMatch[listIdx] = plist[j];
//                        prevPos = plist[j].Position;
//                    }

//                    if (valid)
//                    {
//                        yield return new SearchResult
//                        {
//                            DocId = docEntry.Key,
//                            MatchedPostings = currentMatch
//                        };
//                    }

//                    i0++;
//                }
//            }
//        }
//    }
//}
