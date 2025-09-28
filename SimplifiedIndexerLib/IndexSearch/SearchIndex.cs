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
            var tokenLists = GetTokenListsByPos(wordLists);

            Console.WriteLine("Grouping by doc..." + DateTime.Now);
            var validDocs = GroupAndFilterByDocId(tokenLists);

            Console.WriteLine("Generating results..." + DateTime.Now);
            var results = OrderedAdjacencyMatch(validDocs, adjacency);

            Console.WriteLine("Search complete. Elapsed: " + (DateTime.Now - startTime));
            return results;
        }


        //faster method using premapped index keys by comparing them to wordstore
        static List<List<Token>> GetTokenListsByPos(List<List<int>> posLists)
        {
            var tokenLists = new List<List<Token>>(posLists.Count);
            for (int i = 0; i < posLists.Count; i++)
                tokenLists.Add(new List<Token>());

            using (var reader = new IndexReader())
            {
                for (int x = 0; x < posLists.Count; x++)
                {
                    foreach (var pos in posLists[x])
                    {
                        //Console.WriteLine(DateTime.Now);
                        var data = reader.GetDataByIndex(pos);
                        //Console.WriteLine(DateTime.Now);
                        if (data != null)
                        {
                            var tokenGroup = Serializer.DeserializeTokenGroup(data);
                            tokenLists[x].AddRange(tokenGroup);
                        }
                        //Console.WriteLine(DateTime.Now);
                    }
                }
            }
            return tokenLists;
        }

        //supposed to filter docs that dont have a count of token lists simililar to original token lists
        static Dictionary<int, List<Token>> GroupAndFilterByDocId(List<List<Token>> tokenLists)
        {
            var result = new Dictionary<int, List<Token>>();
            short counter = 0;

            foreach (var tokenList in tokenLists)
            {
                var docGroups = tokenList.GroupBy(t => t.DocId);
                foreach (var docGroup in docGroups)
                {
                    var positions = docGroup
                        .SelectMany(t => t.Postions)
                        .OrderBy(p => p)
                        .ToList();

                    if (positions.Count == 0)
                        continue; // 🚀 skip if no positions for this query term in this doc

                    if (counter == 0)
                    {
                        // first term initializes the doc
                        result[docGroup.Key] = new List<Token>
                {
                    new Token { DocId = docGroup.Key, Postions = positions }
                };
                    }
                    else if (result.ContainsKey(docGroup.Key))
                    {
                        // only add if doc already has matches for previous terms
                        result[docGroup.Key].Add(new Token
                        {
                            DocId = docGroup.Key,
                            Postions = positions
                        });
                    }
                }
                counter++;
            }

            // final cleanup: ensure doc has matches for *all* query terms
            var requiredCount = tokenLists.Count;
            result = result
                .Where(kvp => kvp.Value.Count == requiredCount)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return result;
        }

        public static IEnumerable<SearchResult> OrderedAdjacencyMatch(
      Dictionary<int, List<Token>> validDocs, int adjacency)
        {
            adjacency -= 1; // adjust adjacency (bugfix)

            foreach (var docEntry in validDocs.OrderBy(kvp => kvp.Key))
            {
                var resultForDoc = new SearchResult
                {
                    DocId = docEntry.Key,
                    MatchedPositions = new List<int[]>()
                };

                // For each doc, get ordered positions for each token
                var postingsLists = docEntry.Value
                    .Select(t => t.Postions.OrderBy(p => p).ToList())
                    .Where(p => p.Count > 0)
                    .ToList();

                if (postingsLists.Count != docEntry.Value.Count)
                {
                    yield return resultForDoc; // empty result for this doc
                    continue;
                }

                int i0 = 0;
                while (i0 < postingsLists[0].Count)
                {
                    var currentMatch = new int[postingsLists.Count];
                    currentMatch[0] = postingsLists[0][i0];
                    int prevPos = currentMatch[0];

                    bool valid = true;
                    for (int listIdx = 1; listIdx < postingsLists.Count; listIdx++)
                    {
                        var plist = postingsLists[listIdx];
                        int j = 0;

                        // find next position greater than prevPos
                        while (j < plist.Count && plist[j] - prevPos <= 0)
                            j++;

                        if (j >= plist.Count || plist[j] - prevPos > adjacency)
                        {
                            valid = false;
                            break;
                        }

                        currentMatch[listIdx] = plist[j];
                        prevPos = plist[j];
                    }

                    if (valid)
                    {
                        resultForDoc.MatchedPositions.Add(currentMatch);
                    }

                    i0++;
                }

                yield return resultForDoc;
            }
        }
    }
}
