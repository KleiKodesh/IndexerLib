using IndexerLib.Index;
using IndexerLib.IndexManger;
using IndexerLib.IndexSearch;
using IndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace IndexerLib.IndexSearch
{
    public static class SearchIndex
    {
        public static List<SearchResult> Execute(string query, short adjacency = 2)
        {
            var startTime = DateTime.Now;
            Console.WriteLine("Parsing query..." + DateTime.Now);
            var wordLists = GenerateWordLists(query);

            if (wordLists.Count > 3)
                adjacency = (short)(adjacency * (wordLists.Count - 2) + wordLists.Count);
            else
                adjacency = (short)(adjacency + wordLists.Count);

            Console.WriteLine("Querying index..." + DateTime.Now);
            var tokenLists = GetTokenLists(wordLists);

            Console.WriteLine("Grouping by doc..." + DateTime.Now);
            var validDocs = GroupAndFilterByDocId(tokenLists);

            //foreach (var doc in validDocs)
            //    Console.WriteLine(doc.Key);
            //Console.WriteLine("Generating results..." + DateTime.Now);
            //var results = UnorderedAdjacencyMatch(validDocs, adjacency);

            Console.WriteLine("Search complete. Elapsed: " + (DateTime.Now - startTime));
            return null; /*results.OrderBy(r => r.DocId).ToList();*/
        }

        static List<List<string>> GenerateWordLists(string query)
        {
            var splitQuery = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var wordsStore = WordsStore.GetWords();

            var result = new List<List<string>>(splitQuery.Count);
            for (int i = 0; i < splitQuery.Count; i++)
                result.Add(new List<string>());

            foreach (var word in wordsStore)
                for (int x = 0; x < splitQuery.Count; x++)
                    if (IsWildcardMatch(splitQuery[x], word))
                        result[x].Add(word);

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
                    // exact char match
                    p++;
                    s++;
                }
                else if (p < pattern.Length && pattern[p] == '*')
                {
                    // * found, can match up to 5 chars
                    starIdx = p++;
                    match = s;
                    starCount = 0;
                }
                else if (p < pattern.Length && pattern[p] == '?')
                {
                    // optional char
                    p++;
                }
                else if (starIdx != -1 && starCount < 5)
                {
                    // let * consume another char (but max 5)
                    p = starIdx + 1;
                    s = ++match;
                    starCount++;
                }
                else
                {
                    return false;
                }
            }

            // consume remaining * and ? in pattern
            while (p < pattern.Length && (pattern[p] == '*' || pattern[p] == '?'))
                p++;

            return p == pattern.Length;
        }

        static List<List<Token>> GetTokenLists(List<List<string>> wordLists)
        {
            var tokenLists = new List<List<Token>>(wordLists.Count);
            for (int i = 0; i < wordLists.Count; i++)
                tokenLists.Add(new List<Token>());

            using (var reader = new IndexReader())
            {
                for (int x = 0; x < wordLists.Count; x++)
                {
                    foreach (string word in wordLists[x])
                    {
                        //Console.WriteLine(DateTime.Now);
                        var data = reader.GetTokenData(word);
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


        static Dictionary<int, List<Token>> GroupAndFilterByDocId(List<List<Token>> tokenLists)
        {
            var result = new Dictionary<int, List<Token>>();
            short counter = 0;

            foreach (var tokenList in tokenLists)
            {
                var docGroups = tokenList.GroupBy(t => t.DocId);
                foreach (var docGroup in docGroups)
                {
                    if (counter == 0)
                        result[docGroup.Key] = new List<Token>();
                    else if (!result.ContainsKey(docGroup.Key))
                        continue;

                    var postings = docGroup.SelectMany(t => t.Postings).OrderBy(p => p.Position);

                    result[docGroup.Key].Add(new Token
                    {
                        DocId = docGroup.Key,
                        Postings = postings.ToList()
                    });
                }
                counter++;
            }

            return result;
        }

        // calculate relative positions of postings 
        // So the rule is: 
        //All query terms must appear in the document. this is done at earlier stage already 
        // Terms can appear in any order. 
        // A match is valid only if every consecutive pair in the match is within adjacency distance. 
        // make sure to collect all valud matches for each doc adjust search result class if neccsary
        static List<SearchResult> UnorderedAdjacencyMatch(Dictionary<int, List<Token>> validDocs, short adjacency)
        {
            var results = new List<SearchResult>();

            foreach (var doc in validDocs)
            {
                // Flatten postings: (term index, position)
                var all = new List<(int term, int pos)>();
                for (int t = 0; t < doc.Value.Count; t++)
                    all.AddRange(doc.Value[t].Postings.Select(p => (t, p.Position)));

                if (all.Count == 0) continue;

                all = all.OrderBy(p => p.pos).ToList();
                int termCount = doc.Value.Count;

                var freq = new int[termCount];   // track term presence in window
                int have = 0;                    // how many unique terms in window
                int left = 0;

                var docMatches = new List<List<int>>();

                for (int right = 0; right < all.Count; right++)
                {
                    // expand window
                    if (freq[all[right].term]++ == 0) have++;

                    // shrink window while all terms covered
                    while (have == termCount)
                    {
                        int minPos = all[left].pos;
                        int maxPos = all[right].pos;

                        if (maxPos - minPos <= adjacency * (termCount - 1))
                        {
                            // collect every valid window (overlapping included)
                            var match = all
                                .Skip(left)
                                .Take(right - left + 1)
                                .Select(p => p.pos)
                                .ToList();

                            docMatches.Add(match);
                        }

                        // shrink from left
                        if (--freq[all[left].term] == 0) have--;
                        left++;
                    }
                }

                if (docMatches.Count > 0)
                {
                    results.Add(new SearchResult
                    {
                        DocId = doc.Key,
                        Matches = docMatches
                    });
                }
            }
            return results;
        }
    }
}
