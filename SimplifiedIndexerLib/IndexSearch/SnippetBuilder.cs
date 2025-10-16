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
            if (string.IsNullOrEmpty(docText) ||
                result.MatchedPositions == null ||
                result.MatchedPositions.Count == 0)
                return;

            // Flatten and find the highest token index we need
            int maxNeeded = result.MatchedPositions.Max(m => m.Max());

            // Create streaming enumerator from token stream
            var enumerator = new TokenStream(docText).Tokens.GetEnumerator();
            var tokenBuffer = new List<SimpleToken>();
            int currentIndex = -1;

            // helper local function to ensure we have tokens up to certain index
            bool EnsureTokenIndex(int target)
            {
                while (currentIndex < target && enumerator.MoveNext())
                {
                    tokenBuffer.Add(enumerator.Current);
                    currentIndex++;
                }
                return currentIndex >= target;
            }

            foreach (var matchPositions in result.MatchedPositions)
            {
                if (matchPositions == null || matchPositions.Length == 0)
                    continue;

                var postings = new List<Postings>(matchPositions.Length);
                foreach (var position in matchPositions)
                {
                    if (!EnsureTokenIndex(position))
                        break; // no more tokens available

                    var match = tokenBuffer[position];
                    postings.Add(new Postings(position, match.Index, match.Length));
                }

                if (postings.Count == 0)
                    continue;

                // overall span
                int matchStart = postings.Min(p => p.StartIndex);
                int matchEnd = postings.Max(p => p.StartIndex + p.Length);

                int snippetStart = Math.Max(0, matchStart - windowSize);
                int snippetEnd = Math.Min(docText.Length, matchEnd + windowSize);
                string snippet = docText.Substring(snippetStart, snippetEnd - snippetStart);

                // highlight matched tokens
                var highlights = postings
                    .OrderBy(p => p.StartIndex)
                    .Select(p => new
                    {
                        RelativeStart = p.StartIndex - snippetStart,
                        p.Length
                    })
                    .Where(h => h.RelativeStart < snippet.Length && h.RelativeStart + h.Length > 0)
                    .ToList();

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
                result.Snippets.Add(snippet);

                // Early break optimization: stop once we passed the needed token indices
                if (currentIndex > maxNeeded)
                    break;
            }
        }
    }
}
