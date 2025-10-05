using SimplifiedIndexerLib.Index;
using System;
using System.Collections.Generic;
using System.Linq;

// test are inconclusive wich is fster this or the commented out version
namespace SimplifiedIndexerLib.IndexSearch
{
    public class TermQuery
    {
        public string Term { get; }
        public List<int> Positions { get; }

        public TermQuery(string term)
        {
            Term = term;
            Positions = new List<int>();
        }
    }

    public class QueryParser
    {
        public static List<TermQuery> GenerateWordPositions(string query)
        {
            var terms = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(t => new TermQuery(t))
                             .ToList();

            int index = 0;
            foreach (var word in WordsStore.GetWords())
            {
                foreach (var tq in terms)
                {
                    if (MatchWord(tq.Term, word))
                        tq.Positions.Add(index);
                }
                index++;
            }

            return terms;
        }

        // same wildcard matcher logic
        private static bool MatchWord(string pattern, string input)
        {
            int p = 0, s = 0, starIdx = -1, match = 0, starCount = 0;

            while (s < input.Length)
            {
                if (p < pattern.Length && (pattern[p] == input[s] || pattern[p] == '?'))
                {
                    p++; s++;
                }
                else if (p < pattern.Length && pattern[p] == '*')
                {
                    starIdx = p++;
                    match = s;
                    starCount = 0;
                }
                else if (starIdx != -1 && starCount < 5)
                {
                    p = starIdx + 1;
                    s = ++match;
                    starCount++;
                }
                else return false;
            }

            while (p < pattern.Length && (pattern[p] == '*' || pattern[p] == '?')) p++;
            return p == pattern.Length;
        }
    }
}



//using SimplifiedIndexerLib.Index;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace SimplifiedIndexerLib.IndexSearch
//{
//    public class QueryParser
//    {
//        public static List<TermQuery> GenerateWordPositions(string query)
//        {
//            var terms = query.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
//            var termQueries = terms.Select(t => new TermQuery(t)).ToList();
//            var matchers = termQueries.Select(tq => new TermMatcher(tq)).ToList();

//            int index = 0;
//            foreach (var word in WordsStore.GetWords())
//            {
//                foreach (var matcher in matchers)
//                {
//                    if (matcher.Match(word))
//                        matcher.Query.Positions.Add(index);
//                }
//                index++;
//            }
//            return termQueries;
//        }
//    }

//    public class TermQuery
//    {
//        public string Term { get; }
//        public List<int> Positions { get; }
//        public TermQuery(string term) { Term = term; Positions = new List<int>(); }
//    }

//    internal enum MatcherType { Exact, Prefix, Suffix, Contains, Complex }

//    public class TermMatcher
//    {
//        private readonly MatcherType _type;
//        private readonly string _pattern;
//        private readonly string _fixed;

//        public TermQuery Query { get; }

//        public TermMatcher(TermQuery query)
//        {
//            Query = query;
//            string t = query.Term;

//            if (!t.Contains("*") && !t.Contains("?"))
//            {
//                _type = MatcherType.Exact;
//                _fixed = t;
//            }
//            else if (t.StartsWith("*") && t.EndsWith("*") && t.LastIndexOf('*') == t.Length - 1 && !t.Contains("?"))
//            {
//                _type = MatcherType.Contains;
//                _fixed = t.Trim('*');
//            }
//            else if (t.StartsWith("*") && !t.Substring(1).Contains("*") && !t.Contains("?"))
//            {
//                _type = MatcherType.Suffix;
//                _fixed = t.Substring(1);
//            }
//            else if (t.EndsWith("*") && !t.Substring(0, t.Length - 1).Contains("*") && !t.Contains("?"))
//            {
//                _type = MatcherType.Prefix;
//                _fixed = t.Substring(0, t.Length - 1);
//            }
//            else
//            {
//                _type = MatcherType.Complex;
//                _pattern = t;
//            }
//        }

//        public bool Match(string input)
//        {
//            switch (_type)
//            {
//                case MatcherType.Exact:
//                    return input.Equals(_fixed, StringComparison.Ordinal);
//                case MatcherType.Prefix:
//                    return input.StartsWith(_fixed, StringComparison.Ordinal);
//                case MatcherType.Suffix:
//                    return input.EndsWith(_fixed, StringComparison.Ordinal);
//                case MatcherType.Contains:
//                    return input.IndexOf(_fixed, StringComparison.Ordinal) >= 0;
//                default:
//                    return ComplexMatch(input, _pattern);
//            }
//        }

//        private static bool ComplexMatch(string input, string pattern)
//        {
//            int p = 0, s = 0, starIdx = -1, match = 0;
//            while (s < input.Length)
//            {
//                if (p < pattern.Length && (pattern[p] == input[s] || pattern[p] == '?'))
//                { p++; s++; }
//                else if (p < pattern.Length && pattern[p] == '*')
//                { starIdx = p++; match = s; }
//                else if (starIdx != -1)
//                { p = starIdx + 1; s = ++match; }
//                else return false;
//            }
//            while (p < pattern.Length && (pattern[p] == '*' || pattern[p] == '?')) p++;
//            return p == pattern.Length;
//        }
//    }
//}

