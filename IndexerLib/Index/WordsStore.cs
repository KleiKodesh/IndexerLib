using IndexerLib.Index;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IndexerLib.IndexManger
{
    /// <summary>
    /// Manages a persistent list of words stored in a text file ("Keys.txt") inside the Index folder.
    /// Provides functionality to read and add words in a sorted manner.
    /// </summary>
    public static class WordsStore
    {
        public static readonly string _filePath = new IndexerBase().WordsStorePath;
        /// <summary>
        /// Reads all words from the words file if it exists.
        /// Returns an empty sequence if the file does not exist.
        /// </summary>
        public static IEnumerable<string> GetWords()
        {
            return File.Exists(_filePath)
                ? File.ReadLines(_filePath)
                : Enumerable.Empty<string>();
        }

        /// <summary>
        /// Adds a set of new words to the existing list, 
        /// merges them with any words already saved in the file, 
        /// sorts them alphabetically, 
        /// and writes them back to the words file.
        /// </summary>
        /// <param name="newWords">HashSet of new words to add</param>
        public static void AddWords(HashSet<string> newWords)
        {
            // Merge existing words into the set to avoid duplicates
            foreach (string word in GetWords())
                newWords.Add(word);

            // Write the merged set back to the file in sorted order
            File.WriteAllLines(_filePath, newWords.OrderBy(w => w));
        }
    }
}
