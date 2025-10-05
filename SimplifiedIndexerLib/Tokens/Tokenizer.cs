using SimplifiedIndexerLib.Index;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SimplifiedIndexerLib.Tokens
{
    /// <summary>
    /// Tokenizer for indexing (returns cleaned words only)
    /// </summary>
    public static class Tokenizer
    {
        private const int MinWordLength = 2;
        private const int MaxWordLength = 44;

        public static Dictionary<string, Token> Tokenize(string text, string path)
        {
            var tokens = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);
            int docId;

            using (var idStore = new DocIdStore())
                docId = idStore.Add(path);

            int position = 0;
            int i = 0;
            var sb = new StringBuilder();

            while (i < text.Length)
            {
                char c = text[i];

                // Start token only on a base letter or underscore (same as before)
                if (char.IsLetter(c) || c == '_')
                {
                    sb.Clear();

                    while (i < text.Length)
                    {
                        c = text[i];

                        // allow quotes and HTML tags inside a word (skip them)
                        if (c == '"')
                        {
                            i++;
                            continue;
                        }
                        else if (c == '<')
                        {
                            int close = text.IndexOf('>', i + 1);
                            if (close == -1) break;
                            i = close + 1;
                            continue;
                        }

                        // Determine Unicode category for this char
                        var cat = CharUnicodeInfo.GetUnicodeCategory(c);

                        // Accept letters, underscores and combining marks as part of the token stream.
                        // IMPORTANT: combining marks are accepted so they won't split a token,
                        // but we skip appending them to the "cleaned" word.
                        if (char.IsLetter(c) || c == '_' ||
                            cat == UnicodeCategory.NonSpacingMark ||
                            cat == UnicodeCategory.SpacingCombiningMark ||
                            cat == UnicodeCategory.EnclosingMark)
                        {
                            // Append only base letters and underscores; skip mark characters.
                            if (cat != UnicodeCategory.NonSpacingMark &&
                                cat != UnicodeCategory.SpacingCombiningMark &&
                                cat != UnicodeCategory.EnclosingMark)
                            {
                                sb.Append(c);
                            }

                            i++;
                        }
                        else
                        {
                            // Char not part of a token -> stop token accumulation
                            break;
                        }
                    }

                    // normalize and trim connector chars from the ends
                    string cleaned = sb.ToString().Normalize(NormalizationForm.FormC).Trim('_');

                    if (cleaned.Length >= MinWordLength && cleaned.Length <= MaxWordLength)
                    {
                        if (!tokens.TryGetValue(cleaned, out var token))
                        {
                            token = new Token { DocId = docId };
                            tokens[cleaned] = token;
                        }

                        token.Postions.Add(position++);
                    }
                }
                else
                {
                    // Skip tags/quotes outside tokens too
                    if (c == '<')
                    {
                        int close = text.IndexOf('>', i + 1);
                        if (close == -1) break;
                        i = close + 1;
                    }
                    else if (c == '"')
                    {
                        i++;
                    }
                    else
                    {
                        i++;
                    }
                }
            }

            return tokens;
        }
    }
}
