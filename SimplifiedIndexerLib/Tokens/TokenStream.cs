namespace SimplifiedIndexerLib.Tokens
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    public class SimpleMatch
    {
        public int Index { get; set; }
        public int Length { get; set; }
    }

    /// <summary>
    /// Token stream for snippet building (returns original offsets)
    /// Matches the same word detection rules as Tokenizer
    /// </summary>
    public static class TokenStream
    {
        private const int MinWordLength = 2;
        private const int MaxWordLength = 44;

        public static List<SimpleMatch> Build(string text)
        {
            var result = new List<SimpleMatch>();
            var sb = new StringBuilder();
            int i = 0;

            while (i < text.Length)
            {
                char c = text[i];

                // start token
                if (char.IsLetter(c) || c == '_')
                {
                    int wordStart = i;
                    sb.Clear();

                    while (i < text.Length)
                    {
                        c = text[i];

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

                        var cat = CharUnicodeInfo.GetUnicodeCategory(c);

                        if (char.IsLetter(c) || c == '_' ||
                            cat == UnicodeCategory.NonSpacingMark ||
                            cat == UnicodeCategory.SpacingCombiningMark ||
                            cat == UnicodeCategory.EnclosingMark)
                        {
                            // append only base letters/underscores
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
                            break;
                        }
                    }

                    // normalize and trim underscores from edges
                    string cleaned = sb.ToString().Normalize(NormalizationForm.FormC).Trim('_');

                    if (cleaned.Length >= MinWordLength && cleaned.Length <= MaxWordLength)
                    {
                        result.Add(new SimpleMatch
                        {
                            Index = wordStart,
                            Length = i - wordStart
                        });
                    }
                }
                else
                {
                    // skip HTML/quotes outside words as well
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

            return result;
        }
    }
}
