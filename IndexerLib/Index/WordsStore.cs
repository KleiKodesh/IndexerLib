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
    /// Manages a persistent list of words stored in a text file ("Keys.txt") inside the Index folder.
    /// Provides functionality to read and add words in a sorted manner.
    /// </summary>
    public class WordsStore
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
            // Merge existing words into the set
            foreach (string word in GetWords())
                newWords.Add(word);

            File.WriteAllLines(_filePath, newWords);
        }


        public static void SortWordsByIndex()
        {
            var words = GetWords().ToList();
            using (var reader = new IndexReader())
            {
                var keys = reader.GetAllKeys().ToList();
                var byteComparer = new ByteArrayEqualityComparer();

                    // Create a dictionary mapping hash -> word
                    var wordMap = new Dictionary<byte[], string>(new ByteArrayEqualityComparer());
                    foreach (var word in words)
                    {
                        string normalizedWord = word.Normalize(NormalizationForm.FormC);
                        byte[] hash = reader.Sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedWord));
                        wordMap[hash] = word;
                    }

                    // Prepare sorted list based on index order
                    var sortedWords = new List<string>();
                    foreach (var key in keys)
                        if (wordMap.TryGetValue(key.Hash, out var word))
                            sortedWords.Add(word);
                        else sortedWords.Add(string.Empty);

                    // Write back to file
                    using (var writer = new StreamWriter(_filePath, false, Encoding.UTF8))
                    {
                        foreach (var word in sortedWords)
                            writer.WriteLine(word);
                    }
                

                Console.WriteLine("WordsStore sorted based on index order!");
            }
        }



        public ByteArrayEqualityComparer ByteComparer { get; } = new ByteArrayEqualityComparer();

        public void test()
        {
            var reader = new IndexReader("myfile.tks");
            var keys = reader.GetAllKeys().ToList();
            var words = WordsStore.GetWords().ToList();

            Console.WriteLine($"Index count: {keys.Count}, Word count: {words.Count}");

            for (int i = 0; i < Math.Min(keys.Count, words.Count); i++)
            {
                var expectedHash = reader.Sha256.ComputeHash(Encoding.UTF8.GetBytes(words[i]));
                if (!ByteComparer.Equals(keys[i].Hash, expectedHash))
                    Console.WriteLine($"Mismatch at {i}: word={words[i]}");
            }

        }
}
}
