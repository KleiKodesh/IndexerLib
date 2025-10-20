using IndexerLib.Tokens;
using IndexerLib.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace IndexerLib.Tokens
{
    /// <summary>
    /// Ultra-fast tokenizer for indexing (normalized words only)
    /// </summary>
    public class Tokenizer
    {
        const int MinWordLength = 2, MaxWordLength = 44;
        readonly string _text;
        readonly int _docId;
        readonly Dictionary<string, (int DocId, List<Postings> Postings)> _tokens;
        readonly StringBuilder _sb = new StringBuilder(48);
        int index;
        int wordCounter;

        public Dictionary<string, Token> Tokens
        {
            get
            {
                var dictionary = new Dictionary<string, Token>();
                foreach (var entry in _tokens)
                {
                    dictionary.Add(entry.Key, new Token
                    {
                        DocId = entry.Value.DocId,
                        Postings = entry.Value.Postings.ToArray(),
                    });
                }
                return dictionary;
            }
        }

        public Tokenizer(string text, int docId)
        {
            _text = text;
            _docId = docId;
            _tokens = new Dictionary<string, (int docId, List<Postings> postings)>(256, StringComparer.OrdinalIgnoreCase);

            Tokenize();
        }

        void Tokenize()
        {
            while (index < _text.Length)
            {
                char c = _text[index];
                if (IsHebrewOrLatinLetter(c))
                    ReadWord();
                else if (c == '<')
                    SkipHtmlTag();
                else
                    index++;
            }
        }

        void ReadWord()
        {
            int startIndex = index;
            _sb.Clear();

            while (index < _text.Length)
            {
                char c = _text[index];

                if (IsHebrewOrLatinLetter(c))
                    _sb.Append(c);
                else if (c == '<')
                    SkipHtmlTag();
                else if (IsDiacritic(c) || c == '\"')
                    {   /* skip diacritics and quotes */ }
                else
                    break;

                index++;
            }
         
            if (_sb.Length >= MinWordLength && _sb.Length <= MaxWordLength)
            {
                string w = _sb.ToString().Trim('\"');
                if (!_tokens.TryGetValue(w, out var entry))
                {
                    entry = ( _docId , new List<Postings>());
                    _tokens[w] = entry;
                }

                entry.Postings.Add(new Postings
                {
                    Position = wordCounter++,
                    Index = startIndex,
                    Length = index - startIndex,
                });
            }
        }

        public bool IsHebrewOrLatinLetter(char c)
                    => (c >= 'A' && c <= 'Z') ||
                       (c >= 'a' && c <= 'z') ||
                       (c >= 'א' && c <= 'ת');

        static bool IsDiacritic(char c)
        {
            // Hebrew nikud + taamim (but exclude maqaf \u05BE)
            if (c >= '\u0591' && c <= '\u05C7' && c != '\u05BE')
                return true;

            // Combining Diacritical Marks (U+0300–U+036F)
            if (c >= '\u0300' && c <= '\u036F')
                return true;

            // Combining Diacritical Marks Extended (U+1AB0–U+1AFF)
            if (c >= '\u1AB0' && c <= '\u1AFF')
                return true;

            // Combining Diacritical Marks Supplement (U+1DC0–U+1DFF)
            if (c >= '\u1DC0' && c <= '\u1DFF')
                return true;

            // Combining Diacritical Marks for Symbols (U+20D0–U+20FF)
            if (c >= '\u20D0' && c <= '\u20FF')
                return true;

            // Combining Half Marks (U+FE20–U+FE2F)
            if (c >= '\uFE20' && c <= '\uFE2F')
                return true;

            return false;
        }

        void SkipHtmlTag()
        {
            index++;
            while (index < _text.Length && _text[index] != '>')
                index++;
            if (index < _text.Length)
                index++; // move past '>'
        }
    }
}
