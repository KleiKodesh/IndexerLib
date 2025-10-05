using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IndexerLib.Index
{
    public class IndexerBase
    {
        public string IndexDirectoryPath { get; set; }
        public string TokenStorePath { get; set; }
        public string DocIdStorePath { get; set; }
        public string WordsStorePath { get; set; }

        public const short flushCap = 25;

        public const ushort MagicMarker = 0xCAFE;  // Marker value used in footer to verify file integrity

        public IndexerBase()
        {
            IndexDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index");
            if (!Directory.Exists(IndexDirectoryPath))
                Directory.CreateDirectory(IndexDirectoryPath);

            TokenStorePath = Path.Combine(IndexDirectoryPath, "tokenStore.tks");
            DocIdStorePath = Path.Combine(IndexDirectoryPath, "idStore.sqlite");
            WordsStorePath = Path.Combine(IndexDirectoryPath, "wordsStore.txt");
        }

        public void EnsureUniqueTokenStorePath()
        {
            while (File.Exists(TokenStorePath))
            {
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(TokenStorePath);
                var uniqueName = fileNameWithoutExt + "+.tks";
                TokenStorePath = Path.Combine(IndexDirectoryPath, uniqueName);
            }
        }

        public void EnsureTokenStorePath()
        {
            if(!File.Exists(TokenStorePath))
                TokenStorePath = Directory.EnumerateFiles(IndexDirectoryPath, "*.tks", SearchOption.AllDirectories)
                              .FirstOrDefault();

        }

        public List<string> TokenStoreFileList() => Directory.GetFiles(IndexDirectoryPath, "*.tks").ToList();
    }
}
