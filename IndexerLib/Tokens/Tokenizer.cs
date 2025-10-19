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
        readonly Dictionary<string, (Token Token, List<Postings> Postings)> _tokens;
        readonly StringBuilder _sb = new StringBuilder(48);
        int index;
        int wordCounter;

        public Dictionary<string, (Token Token, List<Postings> Postings)> Tokens => _tokens;

        public Tokenizer(string text, int docId)
        {
            _text = text;
            _docId = docId;
            _tokens = new Dictionary<string, (Token, List<Postings>)>(256, StringComparer.OrdinalIgnoreCase);

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

            foreach (var entry in Tokens)
                entry.Value.Token.Postings = entry.Value.Postings.ToArray();
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

           
            if (_sb.Length >= MinWordLength && _sb.Length <= MaxWordLength)
            {
                string w = _sb.ToString().Trim('\"');
                if (!_tokens.TryGetValue(w, out var entry))
                {
                    entry = (new Token { DocId = _docId }, new List<Postings>());
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
