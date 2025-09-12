using IndexerLib.Helpers;
using IndexerLib.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IndexerLib.IndexSearch
{
    public static class SnippetBuilder
    {
        /// <summary>
        /// Builds a single highlighted snippet per SearchResult.
        /// Assumes result.MatchedPostings contains all postings that belong to one snippet.
        /// </summary>
        public static void BuildSnippets(ref List<SearchResult> searchResults, int windowSize = 100)
        {
            if (searchResults == null) return;

            Console.WriteLine("Building Snippets...");
            using (var docIdStore = new DocIdStore())
            {
                foreach (var result in searchResults)
                {
                    string docPath = docIdStore.GetPathById(result.DocId);
                    string docText = TextExtractor.GetText(docPath);
                    if (string.IsNullOrEmpty(docText) || result.MatchedPostings == null || result.MatchedPostings.Length == 0)
                    {
                        result.Snippet = null;
                        continue;
                    }

                    // overall match span across all postings (one snippet)
                    int matchStart = result.MatchedPostings.Min(p => p.StartIndex);
                    int matchEnd = result.MatchedPostings.Max(p => p.StartIndex + p.Length);

                    int snippetStart = Math.Max(0, matchStart - windowSize);
                    int snippetEnd = Math.Min(docText.Length, matchEnd + windowSize);
                    string snippet = docText.Substring(snippetStart, snippetEnd - snippetStart);

                    // prepare highlight ranges relative to snippet start
                    var highlights = result.MatchedPostings
                        .OrderBy(p => p.StartIndex)
                        .Select(p => new
                        {
                            RelativeStart = p.StartIndex - snippetStart,
                            Length = p.Length
                        })
                        // allow partial overlap with snippet (clip later)
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

                    snippet = Regex.Replace(snippet, @"<(?!/?mark\b)[^>]*>|(^[^<]*>)|(<[^>]*$)", "").Trim();          

                    result.Snippet = snippet;
                }
            }
            Console.WriteLine("Snippets complete.");
        }
    }
}
