using SimplifiedIndexerLib.Helpers;
using SimplifiedIndexerLib.IndexSearch;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimplifiedIndexerLib.Tokens
{
    public class TokenStream
    {
        const int MinWordLength = 2, MaxWordLength = 44;
        string _text;
        Dictionary<int, List<Postings>> _postingsByPosition;
        List<int> _positions;
        int _lastTarget;
        int index, wordCounter, wordLength;

        public void Tokenize(SearchResult searchResult, string text)
        {
            _text = text;
            index = 0;
            wordCounter = -1;
            wordLength = 0;

            // Group postings by position
            _postingsByPosition = searchResult.Matches
                .SelectMany(a => a)
                .GroupBy(p => p.Position)
                .ToDictionary(g => g.Key, g => g.ToList());

            if (_postingsByPosition.Count == 0)
                return;

            _positions = _postingsByPosition.Keys.OrderBy(p => p).ToList();
            _lastTarget = _positions[_positions.Count - 1];

            PopulateOffsets();
        }

        void PopulateOffsets()
        {
            while (index < _text.Length && wordCounter <= _lastTarget)
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
            int startIndex = index;
            index++;
            wordLength = 1;

            while (index < _text.Length)
            {
                char c = _text[index];
                if (c.IsHebrewOrLatinLetter())
                {
                    wordLength++;
                    index++;
                }
                else if (c == '<')
                    SkipHtmlTag();
                else if (c.IsDiacritic() || c == '\"')
                    index++;
                else
                    break;
            }

            AddToken(startIndex);
        }

        void AddToken(int start)
        {
            if (wordLength < MinWordLength || wordLength > MaxWordLength)
                return;

            wordCounter++;
            if (wordCounter > _lastTarget)
            {
                index = _text.Length;
                return;
            }

            List<Postings> postings;
            if (_postingsByPosition.TryGetValue(wordCounter, out postings))
            {
                int len = index - start;
                for (int i = 0; i < postings.Count; i++)
                {
                    postings[i].StartIndex = start;
                    postings[i].Length = len;
                }
            }
        }

        void SkipHtmlTag()
        {
            index++;
            while (index < _text.Length && _text[index] != '>')
                index++;
            index++;
        }
    }
}
