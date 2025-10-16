using SimplifiedIndexerLib.Helpers;
using SimplifiedIndexerLib.Index;
using SimplifiedIndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SimplifiedIndexerLib.IndexSearch
{
    public static class SnippetBuilder
    {
        public static void GenerateSnippets(SearchResult result, string docText, int windowSize = 100)
        {
            result.Snippets = new List<string>();
            if (string.IsNullOrEmpty(docText) ||
                result.Matches == null ||
                result.Matches.Count == 0)
                return;

            new TokenStream().Tokenize(result, docText);

            foreach (var match in result.Matches)
            {
                // Compute snippet bounds
                int matchStart = match.Min(p => p.StartIndex);
                int matchEnd = match.Max(p => p.StartIndex + p.Length);
                int snippetStart = Math.Max(0, matchStart - windowSize);
                int snippetEnd = Math.Min(docText.Length, matchEnd + windowSize);
                string snippet = docText.Substring(snippetStart, snippetEnd - snippetStart);

                // Highlight matched tokens efficiently
                var highlights = match
                    .OrderBy(p => p.StartIndex)
                    .Select(p => new
                    {
                        RelativeStart = p.StartIndex - snippetStart,
                        p.Length
                    })
                    .Where(h => h.RelativeStart < snippet.Length && h.RelativeStart + h.Length > 0)
                    .ToList();

                var sb = new StringBuilder(snippet);
                for (int i = highlights.Count - 1; i >= 0; i--)
                {
                    var h = highlights[i];
                    int relStart = Math.Max(0, h.RelativeStart);
                    int len = Math.Min(h.Length, Math.Max(0, snippet.Length - relStart));
                    if (len <= 0) continue;

                    sb.Insert(relStart + len, "</mark>");
                    sb.Insert(relStart, "<mark>");
                }

                // remove accidental leftover tags
                snippet = Regex.Replace(sb.ToString(), @"<(?!/?mark\b)[^>]*>|(^[^<]*>)|(<[^>]*$)", "").Trim();

                result.Snippets.Add(snippet);
            }
        }
    }
}
