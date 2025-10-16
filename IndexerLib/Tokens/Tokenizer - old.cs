//using IndexerLib.Index;
//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Text;

//namespace IndexerLib.Tokens
//{
//    /// <summary>
//    /// Tokenizer for indexing (returns cleaned words only)
//    /// </summary>
//    public static class Tokenizer
//    {
//        private const int MinWordLength = 2;
//        private const int MaxWordLength = 44;

//        public static Dictionary<string, Token> Tokenize(string text, string path)
//        {
//            var tokens = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);
//            int docId;

//            using (var idStore = new DocIdStore())
//                docId = idStore.Add(path);

//            int wordPosition = 0;  // count by word
//            int i = 0;
//            var sb = new StringBuilder();

//            while (i < text.Length)
//            {
//                char c = text[i];

//                // Start token on a base letter or underscore
//                if (char.IsLetter(c) || c == '_')
//                {
//                    int startIndex = i;
//                    sb.Clear();

//                    while (i < text.Length)
//                    {
//                        c = text[i];

//                        // skip quotes and html tags within words
//                        if (c == '"')
//                        {
//                            i++;
//                            continue;
//                        }
//                        else if (c == '<')
//                        {
//                            int close = text.IndexOf('>', i + 1);
//                            if (close == -1) break;
//                            i = close + 1;
//                            continue;
//                        }

//                        var cat = CharUnicodeInfo.GetUnicodeCategory(c);

//                        // Accept valid word chars
//                        if (char.IsLetter(c) || c == '_' ||
//                            cat == UnicodeCategory.NonSpacingMark ||
//                            cat == UnicodeCategory.SpacingCombiningMark ||
//                            cat == UnicodeCategory.EnclosingMark)
//                        {
//                            // Append only visible base chars
//                            if (cat != UnicodeCategory.NonSpacingMark &&
//                                cat != UnicodeCategory.SpacingCombiningMark &&
//                                cat != UnicodeCategory.EnclosingMark)
//                            {
//                                sb.Append(c);
//                            }

//                            i++;
//                        }
//                        else
//                        {
//                            break;
//                        }
//                    }

//                    int endIndex = i;
//                    string cleaned = sb.ToString().Normalize(NormalizationForm.FormC).Trim('_');

//                    if (cleaned.Length >= MinWordLength && cleaned.Length <= MaxWordLength)
//                    {
//                        if (!tokens.TryGetValue(cleaned, out var token))
//                        {
//                            token = new Token { DocId = docId };
//                            tokens[cleaned] = token;
//                        }

//                        token.Postings.Add(new Postings
//                        {
//                            Position = wordPosition++,
//                            StartIndex = startIndex,
//                            Length = endIndex - startIndex
//                        });
//                    }
//                }
//                else
//                {
//                    // Skip tags/quotes outside tokens too
//                    if (c == '<')
//                    {
//                        int close = text.IndexOf('>', i + 1);
//                        if (close == -1) break;
//                        i = close + 1;
//                    }
//                    else if (c == '"')
//                    {
//                        i++;
//                    }
//                    else
//                    {
//                        i++;
//                    }
//                }
//            }

//            return tokens;
//        }
//    }
//}
