using SimplifiedIndexerLib.Helpers;
using SimplifiedIndexerLib.Index;
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
        int _i, _len, _pos;

        public Dictionary<string, Token> Tokens => _tokens;

        public Tokenizer(string text, int docId)
        {
            _text = text;
            _docId = docId;
            _len = text.Length;
            Tokenize();
        }

        void Tokenize()
        {
            while (_i < _len)
            {
                char c = _text[_i];
                if (c.IsHebrewOrLatinLetter())
                    ReadWord(c);
                else if (c == '<')
                    SkipHtmlTag();
                _i++;
            }
        }

        void ReadWord(char first)
        {
            _sb.Clear();
            _sb.Append(first);
            _i++;

            while (_i < _len)
            {
                char c = _text[_i];

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

                _i++;
            }

            AddToken();
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
            t.Postions.Add(_pos++);
        }

        void SkipHtmlTag()
        {
            _i++;
            while (_i < _len && _text[_i] != '>') _i++;
        }
    }
}
