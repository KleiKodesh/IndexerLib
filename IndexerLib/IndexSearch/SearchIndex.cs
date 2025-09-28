using IndexerLib.Index;
using IndexerLib.IndexManger;
using IndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexerLib.IndexSearch
{
    public static class SearchIndex
    {
        public static IEnumerable<SearchResult> Execute(string query, short adjacency = 2)
        {
            var startTime = DateTime.Now;
            Console.WriteLine("Parsing query..." + DateTime.Now);
            //var wordLists = GenerateWordLists(query);
            var wordLists = GenerateWordPositions(query);

            if (wordLists.Count > 3)
                adjacency = (short)(adjacency * (wordLists.Count - 2) + wordLists.Count);
            else
                adjacency = (short)(adjacency + wordLists.Count);

            Console.WriteLine("Querying index..." + DateTime.Now);
            //var tokenLists = GetTokenLists(wordLists);
            //var tokenLists = GetTokenListsByPos(wordLists);

            //Console.WriteLine("Grouping by doc..." + DateTime.Now);
            //var validDocs = GroupAndFilterByDocId(tokenLists);

            //Console.WriteLine("Generating results..." + DateTime.Now);
            //var results = OrderedAdjacencyMatch(validDocs, adjacency);

            Console.WriteLine("Search complete. Elapsed: " + (DateTime.Now - startTime));
            return new List<SearchResult>();
        }




        //uses an iterator to calcaulte postion of word in index based on its postion in the wordstore
        static List<List<int>> GenerateWordPositions(string query)
        {
            var splitQuery = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var wordsStore = WordsStore.GetWords();

            //Prepare result structure
           var result = new List<List<int>>(splitQuery.Length); 
            for (int i = 0; i < splitQuery.Length; i++)
                result.Add(new List<int>());

            //Iterate with index tracking
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


        static bool IsWildcardMatch(string pattern, string input)
        {
            int p = 0, s = 0;
            int starIdx = -1, match = 0, starCount = 0;

            while (s < input.Length)
            {
                if (p < pattern.Length && pattern[p] == input[s])
                {
                    //exact char match
                    p++;
                    s++;
                }
                else if (p < pattern.Length && pattern[p] == '*')
                {
                    //*found, can match up to 5 chars
                   starIdx = p++;
                    match = s;
                    starCount = 0;
                }
                else if (p < pattern.Length && pattern[p] == '?')
                {
                    //optional char
                   p++;
                }
                else if (starIdx != -1 && starCount < 5)
                {
                    //let* consume another char(but max 5)
                    p = starIdx + 1;
                    s = ++match;
                    starCount++;
                }
                else
                {
                    return false;
                }
            }

            //consume remaining *and ? in pattern
            while (p < pattern.Length && (pattern[p] == '*' || pattern[p] == '?'))
                p++;

            return p == pattern.Length;
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
                    Console.WriteLine(DateTime.Now);
                    var data = reader.GetTokenDataByPos(pos);
                    Console.WriteLine(DateTime.Now);
                    if (data != null)
                    {
                        var tokenGroup = Serializer.DeserializeTokenGroup(data);
                        tokenLists[x].AddRange(tokenGroup);
                    }
                    Console.WriteLine(DateTime.Now);
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
                var postings = docGroup
                    .SelectMany(t => t.Postings)
                    .OrderBy(p => p.Position)
                    .ToList();

                if (postings.Count == 0)
                    continue; // 🚀 skip if no real postings for this query term in this doc

                if (counter == 0)
                {
                    //first term initializes the doc
                   result[docGroup.Key] = new List<Token>
           {
                    new Token { DocId = docGroup.Key, Postings = postings }
           };
                }
                else if (result.ContainsKey(docGroup.Key))
                {
                    //only add if doc already has matches for previous terms

                   result[docGroup.Key].Add(new Token
                   {
                       DocId = docGroup.Key,
                       Postings = postings
                   });
                    }
            }
            counter++;
        }

        //final cleanup: ensure doc has postings for *all * query terms

       var requiredCount = tokenLists.Count;
       result = result
           .Where(kvp => kvp.Value.Count == requiredCount)
           .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return result;
    }


    public static IEnumerable<SearchResult> OrderedAdjacencyMatch(
  Dictionary<int, List<Token>> validDocs,
  int adjacency)
    {
        adjacency = adjacency - 1;
        foreach (var docEntry in validDocs.OrderBy(kvp => kvp.Key)) // order by doc 
        {
            var postingsLists = docEntry.Value
                .Select(t => t.Postings)
                .Where(p => p.Count > 0)
                .ToList();

            if (postingsLists.Count != docEntry.Value.Count)
                continue;

            int i0 = 0;
            while (i0 < postingsLists[0].Count)
            {
                var currentMatch = new Postings[postingsLists.Count];
                currentMatch[0] = postingsLists[0][i0];
                int prevPos = currentMatch[0].Position;

                bool valid = true;
                for (int listIdx = 1; listIdx < postingsLists.Count; listIdx++)
                {
                    var plist = postingsLists[listIdx];
                    int j = 0;

                    //find the first posting within adjacency after prevPos
                        while (j < plist.Count && plist[j].Position - prevPos <= 0)
                        j++;

                    if (j >= plist.Count || plist[j].Position - prevPos > adjacency)
                    {
                        valid = false;
                        break;
                    }

                    currentMatch[listIdx] = plist[j];
                    prevPos = plist[j].Position;
                }

                if (valid)
                {
                    yield return new SearchResult
                    {
                        DocId = docEntry.Key,
                        MatchedPostings = currentMatch
                    };
                }

                i0++;
            }
        }
    }

}
}


////static List<List<string>> GenerateWordLists(string query)
////{
////    var splitQuery = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
////    var wordsStore = WordsStore.GetWords();

////    var result = new List<List<string>>(splitQuery.Count);
////    for (int i = 0; i < splitQuery.Count; i++)
////        result.Add(new List<string>());

////    foreach (var word in wordsStore)
////        for (int x = 0; x < splitQuery.Count; x++)
////            if (IsWildcardMatch(splitQuery[x], word))
////                result[x].Add(word);

////    return result;
////}

//static List<List<Token>> GetTokenLists(List<List<string>> wordLists)
//{
//    var tokenLists = new List<List<Token>>(wordLists.Count);
//    for (int i = 0; i < wordLists.Count; i++)
//        tokenLists.Add(new List<Token>());

//    using (var reader = new IndexReader())
//    {
//        for (int x = 0; x < wordLists.Count; x++)
//        {
//            foreach (string word in wordLists[x])
//            {
//                //Console.WriteLine(DateTime.Now);
//                var data = reader.GetTokenData(word);
//                //Console.WriteLine(DateTime.Now);
//                if (data != null)
//                {
//                    var tokenGroup = Serializer.DeserializeTokenGroup(data);
//                    tokenLists[x].AddRange(tokenGroup);
//                }
//                //Console.WriteLine(DateTime.Now);
//            }
//        }
//    }
//    return tokenLists;
//}