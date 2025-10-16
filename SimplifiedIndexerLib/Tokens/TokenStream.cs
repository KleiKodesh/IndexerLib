using SimplifiedIndexerLib.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SimplifiedIndexerLib.Tokens
{
    public readonly struct SimpleToken
    {
        public int Index { get; }
        public int Length { get; }

        public SimpleToken(int index, int length)
        {
            Index = index;
            Length = length;
        }
    }

    /// <summary>
    /// Ultra-fast token stream for Hebrew + English text.
    /// Skips HTML tags and diacritics. Returns only offsets (index + length).
    /// </summary>
    public class TokenStream
    {
        const int MinWordLength = 2, MaxWordLength = 44;
        readonly string _text;
        int _i, _len;


        readonly List<SimpleToken> _tokens = new List<SimpleToken>(25000);
        public List<SimpleToken> Tokens => _tokens;

        public TokenStream(string text)
        {
            _text = text;
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
            int index = _i;
            int length = 1;
            _i++;

            while (_i < _len)
            {
                char c = _text[_i];

                // IsDiacritic htmltags and " inside a word are advanced but not appended
                if (c.IsHebrewOrLatinLetter())
                    length++;
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

            if (length >= MinWordLength && length <= MaxWordLength)
                _tokens.Add(new SimpleToken(index, _i - index));
        }

        void SkipHtmlTag()
        {
            _i++;
            while (_i < _len && _text[_i] != '>') _i++;
        }
    }
}

