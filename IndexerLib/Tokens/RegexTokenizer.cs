//using IndexerLib.Index;
//using System;
//using System.Collections.Generic;
//using System.Text.RegularExpressions;

//namespace IndexerLib.Tokens
//{
//    public static class RegexTokenizer
//    {
//        // word regex exclude digits with support for either HTML tags or a literal " in the middle of a word.
//        static readonly Regex WordRegex = new Regex(@"(?:[\p{L}\p{M}_]+(?:(?:(?:<[^>]+>)|"")[\p{L}\p{M}_]+)*)", RegexOptions.Compiled);
//        private static readonly Regex CleanWordRegex = new Regex(@"(?:<.*?>)|[\p{M}""]+", RegexOptions.Compiled);

//        /// <summary>
//        /// Returns only valid raw matches from the text.
//        /// Filtering is applied using the cleaned word.
//        /// </summary>
//        public static List<Match> TokenStream(string text)
//        {
//            var result = new List<Match>();

//            foreach (Match match in WordRegex.Matches(text))
//            {
//                string cleaned = CleanWordRegex.Replace(match.Value, "");

//                if (cleaned.Length <= 1 || cleaned.Length >= 45)
//                    continue;

//                result.Add(match);
//            }

//            return result;
//        }

//        /// <summary>
//        /// Builds inverted index for a single document.
//        /// Uses the same filtering rules as TokenStream, so positions match.
//        /// </summary>
//        public static Dictionary<string, Token> Tokenize(string text, string path)
//        {
//            var tokens = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);
//            int docId;

//            using (var idStore = new DocIdStore())
//                docId = idStore.Add(path);

//            int position = 0;
//            foreach (Match match in WordRegex.Matches(text))
//            {
//                string cleaned = CleanWordRegex.Replace(match.Value, "");

//                if (cleaned.Length <= 1 || cleaned.Length >= 45)
//                    continue;

//                if (!tokens.TryGetValue(cleaned, out var token))
//                {
//                    token = new Token { DocId = docId };
//                    tokens[cleaned] = token;
//                }

//                token.Postions.Add(position++);
//            }

//            return tokens;
//        }
//    }
//}
