using IndexerLib.Helpers;
using IndexerLib.Index;
using IndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IndexerLib.IndexSearch
{
    public static class SnippetBuilder
    {
        public static void GenerateSnippets(SearchResult result, DocIdStore docIdStore, int windowSize = 100)
        {
            result.DocPath = docIdStore.GetPathById(result.DocId);
            string docText = TextExtractor.GetText(result.DocPath);

            result.Snippets = new List<string>();

            if (string.IsNullOrEmpty(docText) || result.MatchedPostings == null || result.MatchedPostings.Count == 0)
            {
                return;
            }

            foreach (var postings in result.MatchedPostings)
            {
                if (postings == null || postings.Length == 0)
                    continue;

                // overall span across all postings for this match
                int matchStart = postings.Min(p => p.Index);
                int matchEnd = postings.Max(p => p.Index + p.Length);

                int snippetStart = Math.Max(0, matchStart - windowSize);
                int snippetEnd = Math.Min(docText.Length, matchEnd + windowSize);
                string snippet = docText.Substring(snippetStart, snippetEnd - snippetStart);

                // prepare highlight ranges relative to snippet start
                var highlights = postings
                    .OrderBy(p => p.Index)
                    .Select(p => new
                    {
                        RelativeStart = p.Index - snippetStart,
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
