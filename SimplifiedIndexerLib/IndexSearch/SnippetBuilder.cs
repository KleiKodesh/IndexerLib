using SimplifiedIndexerLib.Helpers;
using SimplifiedIndexerLib.Index;
using SimplifiedIndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimplifiedIndexerLib.IndexSearch
{
    public static class SnippetBuilder
    {
        public static void GenerateSnippets(SearchResult result, DocIdStore docIdStore, int windowSize = 100)
        {
            result.DocPath = docIdStore.GetPathById(result.DocId);
            string docText = TextExtractor.GetText(result.DocPath);

            result.Snippets = new List<string>();

            if (string.IsNullOrEmpty(docText) || result.MatchedPositions == null || result.MatchedPositions.Count == 0)
            {
                return;
            }

            var tokenStream = RegexTokenizer.TokenStream(docText);

            foreach (var matchPositions in result.MatchedPositions)
            {
                if (matchPositions == null || matchPositions.Length == 0)
                    continue;

                var postings = new List<Postings>(matchPositions.Length);
                foreach (var position in matchPositions)
                {
                    var match = tokenStream[position];
                    postings.Add(new Postings(position, match.Index, match.Length));
                }

                // overall span across all postings for this match
                int matchStart = postings.Min(p => p.StartIndex);
                int matchEnd = postings.Max(p => p.StartIndex + p.Length);

                int snippetStart = Math.Max(0, matchStart - windowSize);
                int snippetEnd = Math.Min(docText.Length, matchEnd + windowSize);
                string snippet = docText.Substring(snippetStart, snippetEnd - snippetStart);

                // prepare highlight ranges relative to snippet start
                var highlights = postings
                    .OrderBy(p => p.StartIndex)
                    .Select(p => new
                    {
                        RelativeStart = p.StartIndex - snippetStart,
                        Length = p.Length
                    })
                    .Where(h => h.RelativeStart < snippet.Length && h.RelativeStart + h.Length > 0)
                    .ToList();

                // insert marks from last to first so indices remain valid
                for (int i = highlights.Count - 1; i >= 0; i--)
                {
                    var h = highlights[i];
                    int relStart = Math.Max(0, h.RelativeStart);
                    int len = Math.Min(h.Length, Math.Max(0, snippet.Length - relStart));
                    if (len <= 0) continue;

                    snippet = snippet.Insert(relStart + len, "</mark>")
                                     .Insert(relStart, "<mark>");
                }

                // remove accidental leftover tags
                snippet = Regex.Replace(snippet, @"<(?!/?mark\b)[^>]*>|(^[^<]*>)|(<[^>]*$)", "").Trim();

                result.Snippets.Add(snippet);
            }
        }
    }
}
