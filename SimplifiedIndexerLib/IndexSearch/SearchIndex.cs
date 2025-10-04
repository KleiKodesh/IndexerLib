using SimplifiedIndexerLib.Index;
using SimplifiedIndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimplifiedIndexerLib.IndexSearch
{
    public static class SearchIndex
    {
        public static IEnumerable<SearchResult> Execute(string query, short adjacency = 2)
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
            //var tokenLists = GetTokenLists(wordLists);
            List<List<Token>> tokenLists;
            using (var reader = new TokenListReader())
                tokenLists = reader.GetByIndex(wordLists);

            Console.WriteLine("Grouping by doc..." + DateTime.Now);
            var validDocs = GroupAndFilterByDocId(tokenLists);

            Console.WriteLine("Generating results..." + DateTime.Now);
            var results = OrderedAdjacencyMatch(validDocs, adjacency);

            Console.WriteLine("Search complete. Elapsed: " + (DateTime.Now - startTime));
            return results;
        }

        static Dictionary<int, List<Token>> GroupAndFilterByDocId(List<List<Token>> tokenLists)
        {
            if (tokenLists == null || tokenLists.Count == 0)
                return new Dictionary<int, List<Token>>();

            int requiredCount = tokenLists.Count;
            var result = new Dictionary<int, List<Token>>(4096);
            var docTermCount = new Dictionary<int, int>(4096);

            // we’ll reuse one dictionary to avoid reallocation each loop
            var grouped = new Dictionary<int, List<int>>(4096);

            for (int listIndex = 0; listIndex < tokenLists.Count; listIndex++)
            {
                var tokenList = tokenLists[listIndex];
                if (tokenList == null || tokenList.Count == 0)
                    return new Dictionary<int, List<Token>>(); // no docs can match if one term has none

                grouped.Clear();

                // merge all tokens with same docId
                for (int i = 0; i < tokenList.Count; i++)
                {
                    var t = tokenList[i];
                    if (!grouped.TryGetValue(t.DocId, out var posList))
                    {
                        posList = new List<int>();
                        grouped[t.DocId] = posList;
                    }
                    // positions may already be sorted; no need to sort unless required
                    posList.AddRange(t.Postions);
                }

                // integrate into result
                foreach (var kv in grouped)
                {
                    var docId = kv.Key;
                    var positions = kv.Value;

                    if (listIndex == 0)
                    {
                        result[docId] = new List<Token>
                {
                    new Token { DocId = docId, Postions = positions }
                };
                        docTermCount[docId] = 1;
                    }
                    else if (result.TryGetValue(docId, out var existing))
                    {
                        existing.Add(new Token { DocId = docId, Postions = positions });
                        docTermCount[docId]++;
                    }
                }
            }

            // filter only docs that appear in all terms
            var final = new Dictionary<int, List<Token>>(docTermCount.Count);
            foreach (var kv in docTermCount)
            {
                if (kv.Value == requiredCount)
                    final[kv.Key] = result[kv.Key];
            }

            return final;
        }


        public static IEnumerable<SearchResult> OrderedAdjacencyMatch(
      Dictionary<int, List<Token>> validDocs, int adjacency)
        {
            adjacency -= 1; // adjust adjacency

            foreach (var docEntry in validDocs.OrderBy(kvp => kvp.Key))
            {
                var resultForDoc = new SearchResult
                {
                    DocId = docEntry.Key,
                    MatchedPositions = new List<int[]>()
                };

                var postingsLists = docEntry.Value
                    .Select(t => t.Postions.OrderBy(p => p).ToList())
                    .Where(p => p.Count > 0)
                    .ToList();

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
