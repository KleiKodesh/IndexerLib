using System;

namespace SimplifiedIndexerLib.IndexSearch
{
    public class Postings
    {
        public int Position { get; private set; }     // position of the word relative to word count
        public int StartIndex { get; private set; }   // position of first char of the word relative to char count
        public int Length { get; private set; }       // length of the word

        public Postings(int position, int startIndex, int length)
        {
            Position = position;
            StartIndex = startIndex;
            Length = length;
        }
    }
}
