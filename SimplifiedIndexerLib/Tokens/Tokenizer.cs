using SimplifiedIndexerLib.Helpers;
using SimplifiedIndexerLib.Index;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

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
        int index, WordCounter;

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
                    ReadWord(c);
                else if (c == '<')
                    SkipHtmlTag();
                else
                    index++;
            }
        }

        void ReadWord(char first)
        {
            _sb.Clear();
            _sb.Append(first);
            index++;

            while (index < _text.Length)
            {
                char c = _text[index];

                // IsDiacritic htmltags and " inside a word are advanced but not appended
                if (c.IsHebrewOrLatinLetter())
                    _sb.Append(c);
                else if (c == '<')
                    SkipHtmlTag();
                else if (c.IsDiacritic())
                { /* skip */ }
                else if (c == '\"')
                { /* skip */ }
                else
                    break;

                index++;
            }

            AddToken();
            index++;
        }

        void AddToken()
        {
            int len = _sb.Length;
            if (len < MinWordLength || len > MaxWordLength)
                return;

            string w = _sb.ToString().Trim('\"');
            Token t;
            if (!_tokens.TryGetValue(w, out t))
            {
                t = new Token { DocId = _docId };
                _tokens[w] = t;
            }
            t.Postions.Add(WordCounter++);
        }

        void SkipHtmlTag()
        {
            index++;
            while (index < _text.Length && _text[index] != '>') index++;
        }
    }
}
