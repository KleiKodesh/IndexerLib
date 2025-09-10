using IndexerLib.Index;
using System.Collections.Generic;
using System.Text;

namespace IndexerLib.Tokens
{
    /// <summary>
    /// Tokenizer class responsible for splitting text into Hebrew word tokens 
    /// and tracking their positions within the text.
    /// </summary>
    public static class Tokenizer
    {
        /// <summary>
        /// Tokenizes a given text into a dictionary of tokens with associated postings (occurrences).
        /// </summary>
        /// <param name="text">The input text to tokenize.</param>
        /// <param name="path">The file/document path used to associate tokens with a source ID.</param>
        /// <returns>A dictionary of tokens, keyed by the cleaned word.</returns>
        public static Dictionary<string, Token> Tokenize(string text, string path)
        {
            // Normalize text: lowercase and replace non-breaking space marker
            text = text.ToLower().Replace("nbsp", "####");

            bool inWord = false;              // Tracks if currently inside a word
            bool doubleQuotesDetected = false; // Tracks handling of quotation marks inside words

            var tokens = new Dictionary<string, Token>(); // Stores all tokens with their postings
            int position = 1;                // Sequential token position within text
            int currentIndex = -1;           // Current index in the text

            StringBuilder stringBuilder = new StringBuilder(); // Builds current word

            // Map path to unique ID via IdStore
            int pathId = 0;
            using (var docIdStore = new DocIdStore())
                pathId = docIdStore.Add(path);

            // Iterate over each character in the text
            for (int i = 0; i < text.Length; i++)
            {
                currentIndex = i;
                char c = text[i];

                // Replace Hebrew maqaf (־) with space for consistency
                if (c == '־')
                    c = ' ';

                // Skip over HTML tags (<...>)
                if (c == '<')
                {
                    int tagEnd = text.IndexOf('>', i);
                    if (tagEnd != -1)
                    {
                        i = tagEnd; // Jump past the tag
                        continue;
                    }
                }

                // Handle Hebrew letters and diacritics
                if (IsHebrewLetterOrDiacritic(c))
                {
                    if (doubleQuotesDetected)
                    {
                        // If a quote was encountered before, add it back to the word
                        stringBuilder.Append('"');
                        doubleQuotesDetected = false;
                    }
                    stringBuilder.Append(c);
                    inWord = true;
                }
                // Allow apostrophes inside words
                else if (inWord && c == '\'')
                {
                    stringBuilder.Append(c);
                }
                // If a quote appears inside a word, delay adding it
                else if (inWord && c == '"')
                {
                    doubleQuotesDetected = true;
                }
                // If the current character is not part of a word, end the current token
                else
                {
                    if (stringBuilder.Length > 0)
                        AddWord();

                    doubleQuotesDetected = false;
                    inWord = false;
                }

                // avoid artifically long words in pdf with ocr
                if (stringBuilder.Length > 30)
                    stringBuilder.Clear();
            }

            // Handle last word if the text ends in one
            if (stringBuilder.Length > 0)
            {
                AddWord();
            }

            /// <summary>
            /// Internal helper method to finalize a word and add it to the tokens dictionary.
            /// </summary>
            void AddWord()
            {
                string word = stringBuilder.ToString();

                // Remove Hebrew diacritics (nikud) for normalization
                string cleanedWord = word.RemoveHebrewDiactrics();

                // Reset string builder for next word
                stringBuilder.Clear();

                // Add token if not already in dictionary
                if (!tokens.ContainsKey(cleanedWord))
                    tokens[cleanedWord] = new Token { DocId = pathId };

                // Record posting (occurrence) for this word
                tokens[cleanedWord].Postings.Add(new Posting
                {
                    Length = word.Length,
                    Position = position,
                    StartIndex = currentIndex - word.Length
                });

                position++;
            }

            return tokens;
        }

        /// <summary>
        /// Determines whether a character is a Hebrew letter or diacritic (U+0591–U+05C7).
        /// </summary>
        private static bool IsHebrewLetterOrDiacritic(char c)
        {
            return char.IsLetter(c) || (c >= '\u0591' && c <= '\u05C7');
        }

        /// <summary>
        /// Removes Hebrew diacritics (nikud and cantillation marks) from a string.
        /// </summary>
        public static string RemoveHebrewDiactrics(this string input)
        {
            var sb = new StringBuilder(input.Length);

            foreach (var c in input)
                if (c > 1487 || c < 1425) // Range 1425–1487 contains Hebrew diacritics
                    sb.Append(c);

            return sb.ToString();
        }
    }
}
