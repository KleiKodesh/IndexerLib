using IndexerLib.Helpers;
using IndexerLib.Index;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace IndexerLib.IndexManger
{
    /// <summary>
    /// Provides a persistent store for all words in the index.
    /// Words are stored in a plain text file ("Keys.txt") inside the Index folder.
    /// Supports reading, adding, and optionally re-sorting words to match the index order.
    /// </summary>
    public class WordsStore
    {
        // Path to the file that stores all words
        public static readonly string _filePath = new IndexerBase().WordsStorePath;

        /// <summary>
        /// Reads all words from the words file, one per line.
        /// Returns an empty sequence if the file does not exist.
        /// </summary>
        public static IEnumerable<string> GetWords()
        {
            return File.Exists(_filePath)
                ? File.ReadLines(_filePath)
                : Enumerable.Empty<string>();
        }

        /// <summary>
        /// Adds a set of new words to the existing file:
        /// - Reads existing words from the file
        /// - Merges them with the provided set
        /// - Writes the merged set back to the file
        /// </summary>
        /// <param name="newWords">Collection of words to add</param>
        public static void AddWords(HashSet<string> newWords)
        {
            // Merge existing words into the new set (avoids duplicates automatically)
            foreach (string word in GetWords())
                newWords.Add(word);

            // Write the merged set back to the file
            File.WriteAllLines(_filePath, newWords);
        }

        /// <summary>
        /// Re-sorts all words based on the order of keys stored in the index.
        /// This ensures that the order of words in the file matches the order of their hashes in the index.
        /// Missing words (keys without a matching word) are written as empty lines.
        /// </summary>
        public static void SortWordsByIndex()
        {
            var words = GetWords().ToList();
            using (var reader = new IndexReader())
            {
                var keys = reader.GetAllKeys().ToList();

                // Create a dictionary mapping hash -> word for fast lookup
                var wordMap = new Dictionary<byte[], string>(new ByteArrayEqualityComparer());
                foreach (var word in words)
                {
                    string normalizedWord = word.Normalize(NormalizationForm.FormC);
                    byte[] hash = reader.Sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedWord));
                    wordMap[hash] = word;
                }

                // Build a new list of words in the order of keys from the index
                var sortedWords = new List<string>();
                foreach (var key in keys)
                {
                    if (wordMap.TryGetValue(key.Hash, out var word))
                        sortedWords.Add(word);
                    else
                        sortedWords.Add(string.Empty); // Placeholder for missing words
                }

                // Write the sorted words back to the file
                using (var writer = new StreamWriter(_filePath, false, Encoding.UTF8))
                {
                    foreach (var word in sortedWords)
                        writer.WriteLine(word);
                }

                Console.WriteLine("WordsStore sorted based on index order!");
            }
        }
    }
}
