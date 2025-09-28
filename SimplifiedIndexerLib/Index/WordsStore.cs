using SimplifiedIndexerLib.Helpers;
using SimplifiedIndexerLib.Index;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SimplifiedIndexerLib.Index
{
    public class WordsStore
    {
        public static readonly string _filePath = new IndexerBase().WordsStorePath;

        public static IEnumerable<string> GetWords()
        {
            return File.Exists(_filePath)
                ? File.ReadLines(_filePath)
                : Enumerable.Empty<string>();
        }

        public static void AddWords(HashSet<string> newWords)
        {
            foreach (string word in GetWords())
                newWords.Add(word);

            File.WriteAllLines(_filePath, newWords);
        }

        public static void SortWordsByIndex()
        {
            int i = 0;
            int x = GetWords().Count();
            int y = 0;

            var wordMap = new Dictionary<byte[], string>(new ByteArrayEqualityComparer());

            using (var sha256 = SHA256.Create())
                foreach (var word in GetWords())
                {
                    var norm = word.Normalize(NormalizationForm.FormC);
                    wordMap[sha256.ComputeHash(Encoding.UTF8.GetBytes(norm))] = word;
                }

            using (var reader = new IndexReader())
            using (var writer = new StreamWriter(_filePath, false, Encoding.UTF8))
            {
                i = reader.GetAllKeys().Count();
                foreach (var key in reader.GetAllKeys())
                    writer.WriteLine(wordMap.TryGetValue(key.Hash, out var w) ? w : "");

                y = reader.GetAllKeys().Count();
            }
            
            int z = GetWords().Count();
            Console.WriteLine("WordsStore sorted based on index order!");
        }
    }
}
