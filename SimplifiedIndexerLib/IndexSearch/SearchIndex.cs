using SimplifiedIndexerLib.Index;
using SimplifiedIndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimplifiedIndexerLib.IndexSearch
{
    public static class SearchIndex
    {
        public static List<SearchResult> Execute(string query, short adjacency = 2)
        {
            var startTime = DateTime.Now;
            Console.WriteLine("Parsing query..." + DateTime.Now);
            //var wordLists = GenerateWordLists(query);
            var wordLists = QueryParser.GenerateWordPositions(query);

            if (wordLists.Count > 3)
                adjacency = (short)(adjacency * (wordLists.Count - 2) + wordLists.Count);
            else
                adjacency = (short)(adjacency + wordLists.Count);

            Console.WriteLine("Querying index..." + DateTime.Now);

            List<List<Token>> tokenLists;
            using (var reader = new TokenListReader())
                tokenLists = reader.GetByIndex(wordLists);

            Console.WriteLine("Grouping by doc..." + DateTime.Now);
            var docs = TokenGrouping.Execute(tokenLists);

            Console.WriteLine("Generating results..." + DateTime.Now);
            var results = OrderedAdjacencyMatch(docs, adjacency);

            Console.WriteLine("Search complete. Elapsed: " + (DateTime.Now - startTime));
            return results.ToList();
        }

        public static IEnumerable<SearchResult> OrderedAdjacencyMatch(
               Dictionary<int, List<List<int>>> docs, int adjacency)
        {
            adjacency -= 1; // adjust adjacency

            foreach (var docEntry in docs.OrderBy(kvp => kvp.Key))
            {
                var resultForDoc = new SearchResult
                {
                    DocId = docEntry.Key,
                    MatchedPositions = new List<int[]>()
                };

                var postingsLists = docEntry.Value;

                var firstList = postingsLists[0];
                foreach (var startPos in firstList)
                {
                    var currentMatch = new int[postingsLists.Count];
                    currentMatch[0] = startPos;
                    int prevPos = startPos;

                    bool valid = true;
                    for (int listIdx = 1; listIdx < postingsLists.Count; listIdx++)
                    {
                        var plist = postingsLists[listIdx];

                        // Binary search for first element > prevPos
                        int idx = plist.BinarySearch(prevPos + 1);
                        if (idx < 0) idx = ~idx; // BinarySearch returns complement if not found

                        if (idx >= plist.Count || plist[idx] - prevPos > adjacency)
                        {
                            valid = false;
                            break;
                        }

                        currentMatch[listIdx] = plist[idx];
                        prevPos = plist[idx];
                    }

                    if (valid)
                        resultForDoc.MatchedPositions.Add(currentMatch);
                }

                yield return resultForDoc;
            }
        }

    }
}
