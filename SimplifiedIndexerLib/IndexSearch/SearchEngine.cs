using SimplifiedIndexerLib.Index;
using SimplifiedIndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimplifiedIndexerLib.IndexSearch
{
    public static class SearchEngine
    {
        public static IEnumerable<SearchResult> Execute(string query, short adjacency = 2)
        {
            var startTime = DateTime.Now;
            Console.WriteLine("Parsing query..." + DateTime.Now);
            //var wordLists = GenerateWordLists(query);
            var wordLists = QueryParser.GenerateWordPositions(query);

            Console.WriteLine("Querying index..." + DateTime.Now);

            List<List<Token>> tokenLists;
            using (var reader = new TokenListReader())
                tokenLists = reader.GetByIndex(wordLists);

            Console.WriteLine("Grouping by doc..." + DateTime.Now);
            var docs = TokenGrouping.Execute(tokenLists);

            Console.WriteLine("Generating results..." + DateTime.Now);
            var results = SearchMatcher.OrderedAdjacencyMatch(docs, adjacency);

            Console.WriteLine("Index Lookup Complete. Elapsed: " + (DateTime.Now - startTime));
            Console.WriteLine("Streaming results...");
            return results;
        }
    }
}
