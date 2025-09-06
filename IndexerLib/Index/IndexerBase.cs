using System;
using System.IO;

namespace IndexerLib.Index
{
    public class IndexerBase
    {
        public string IndexDirectoryPath { get; set; }
        public string TokenStorePath { get; set; }
        public string DocIdStorePath { get; set; }
        public string WordsStorePath { get; set; }

        public IndexerBase()
        {
            IndexDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index");
            if (!Directory.Exists(IndexDirectoryPath))
                Directory.CreateDirectory(IndexDirectoryPath);

            TokenStorePath = Path.Combine(IndexDirectoryPath, "tokenStore.tks");
            DocIdStorePath = Path.Combine(IndexDirectoryPath, "idStore.str");
            WordsStorePath = Path.Combine(IndexDirectoryPath, "wordsStore.str"); 
        }

        public void EnsureUniqueTokenStorePath()
        {
            while (File.Exists(TokenStorePath))
            {
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(TokenStorePath);
                var uniqueName = fileNameWithoutExt + "+.str";
                TokenStorePath = Path.Combine(IndexDirectoryPath, uniqueName);
            }
        }
    }
}
