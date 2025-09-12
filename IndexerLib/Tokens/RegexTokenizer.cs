using IndexerLib.Index;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IndexerLib.Tokens
{
    public static class RegexTokenizer
    {
        static readonly Regex WordRegex = new Regex( @"(?:[\p{L}\p{M}\p{Nd}_]+(?:<[^>]+>[\p{L}\p{M}\p{Nd}_]+)*)", RegexOptions.Compiled);
        private static readonly Regex CleanWordRegex = new Regex(@"(<.*?>)|\p{M}+", RegexOptions.Compiled);

        public static Dictionary<string, Token> Tokenize(string text, string path)
        {
            var tokens = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);
            int wordIndex = 1;
            int docId = 0;

            using (var idStore = new DocIdStore())
                docId = idStore.Add(path);           

            foreach (Match match in WordRegex.Matches(text))
            {
                string word = CleanWordRegex.Replace(match.Value.ToLowerInvariant(), "");

                if (word.Length <= 1) // skip one-char tokens
                    continue;

                if (!tokens.TryGetValue(word, out var token))
                {
                    token = new Token { DocId = docId };
                    tokens[word] = token;
                }

                token.Postings.Add(new Postings
                {
                    Position = wordIndex++,
                    StartIndex = match.Index,
                    Length = match.Length
                });
            }

            return tokens;
        }
    }
}
