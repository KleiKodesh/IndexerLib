using IndexerLib.Helpers;
using IndexerLib.Index;
using IndexerLib.IndexSearch;
using IndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace IndexerLib.Sample
{
    public static class IndexManager
    {
        public static void CreateIndex(string directory)
        {
            // Collect txt + pdf files
            var files = Directory
                .EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                            f.ToLower().EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var indexStart = DateTime.Now;
            int fileCount = files.Count;
            int currentIndex = -1;

            Timer progressTimer = new Timer(2000); // every 2s
            progressTimer.Elapsed += (sender, e) =>
                Console.WriteLine($"File Progress: {currentIndex} / {fileCount}");
            progressTimer.Start();

            var wal = new WAL();
                foreach (var file in files)
                {
                    currentIndex++;
                    try
                    {
                        string content = TextExtractor.GetText(file);

                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            var tokens = RegexTokenizer.Tokenize(content, file);
                            foreach (var token in tokens)
                                wal.Log(token.Key, token.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading {file}: {ex.Message}");
                    }
                }
            

            progressTimer.Stop();
            progressTimer.Dispose();
            wal.Dispose();

            Console.WriteLine($"Indexing complete! start time: {indexStart} end time: {DateTime.Now} total time: {DateTime.Now - indexStart}");
        }

        public static List<SearchResult> Search(string query)
        {
            var results = SearchIndex.Execute(query);
            SnippetBuilder.BuildSnippets(ref results);
            return results;
        }
    }
}
