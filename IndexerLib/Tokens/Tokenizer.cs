using IndexerLib.Tokens;
using SimplifiedIndexerLib.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimplifiedIndexerLib.Tokens
{
    /// <summary>
    /// Ultra-fast tokenizer for indexing (normalized words only)
    /// </summary>
    public class Tokenizer
    {
        const int MinWordLength = 2, MaxWordLength = 44;
        readonly string _text;
        readonly int _docId;
        readonly Dictionary<string, Token> _tokens = new Dictionary<string, Token>(256, StringComparer.OrdinalIgnoreCase);
        readonly StringBuilder _sb = new StringBuilder(48);
        int index;
        int wordCounter;

        public Dictionary<string, Token> Tokens => _tokens;

        public Tokenizer(string text, int docId)
        {
            _text = text;
            _docId = docId;
            Tokenize();
        }

        void Tokenize()
        {
            while (index < _text.Length)
            {
                char c = _text[index];
                if (c.IsHebrewOrLatinLetter())
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

                if (c.IsHebrewOrLatinLetter())
                    _sb.Append(c);
                else if (c == '<')
                    SkipHtmlTag();
                else if (c.IsDiacritic() || c == '\"')
                    {   /* skip diacritics and quotes */ }
                else
                    break;

                index++;
            }

            int length = _sb.Length;
            if (length >= MinWordLength && length <= MaxWordLength)
            {
                string w = _sb.ToString().Trim('\"');
                if (!_tokens.TryGetValue(w, out var token))
                {
                    token = new Token { DocId = _docId };
                    _tokens[w] = token;
                }

                token.Postings.Add(new Postings
                {
                    Position = wordCounter++,
                    StartIndex = startIndex,
                    Length = length
                });
            }
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
