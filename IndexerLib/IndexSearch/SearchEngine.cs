using IndexerLib.Index;
using IndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexerLib.IndexSearch
{
    public static class SearchEngine
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

            List<List<Token>> tokenLists;
            using (var reader = new TokenListReader())
                tokenLists = reader.GetByIndex(wordLists);

            Console.WriteLine("Grouping by doc..." + DateTime.Now);
            var docs = TokenGrouping.Execute(tokenLists);

            Console.WriteLine("Generating results..." + DateTime.Now);
            var results = SearchMatcher.OrderedAdjacencyMatch(docs, adjacency);

            Console.WriteLine("Search complete. Elapsed: " + (DateTime.Now - startTime));
            return results;
        }
    }
}
