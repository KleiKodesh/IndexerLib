using IndexerLib.Helpers;
using IndexerLib.Index;
using IndexerLib.IndexSearch;
using IndexerLib.Tokens;
using Oztarnik.FsViewer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace IndexerLib.Sample
{
    public static class IndexManager
    {
        public static void CreateIndex(string directory, string[] extensions, int memoryUsage = 10)
        {
            try
            {
                // Collect txt + pdf files
                var files = OrderedEnumerateFiles(directory, extensions) .ToList();

                var indexStart = DateTime.Now;
                int fileCount = files.Count;
                int currentIndex = -1;

                Timer progressTimer = new Timer(2000); // every 2s
                progressTimer.Elapsed += (sender, e) =>
                {
                    Console.WriteLine($"File Progress: {currentIndex} / {fileCount}");
                };

                progressTimer.Start();

                var wal = new WAL(memoryUsage);
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message );
            }
        }

        static IEnumerable<string> OrderedEnumerateFiles(string directory, string[] extensions)
        {
            // Enumerate subdirectories in order
            foreach (var dir in Directory.GetDirectories(directory).OrderBy(dir =>
            {
                var index = Array.IndexOf(FsSort.DirectoryOrder, Path.GetFileName(dir));
                return index == -1 ? int.MaxValue : index;
            }).ThenBy(d => d))
                foreach (var file in OrderedEnumerateFiles(dir, extensions))
                    yield return file;

            // Enumerate files in alphabetical order
            foreach (var file in Directory.GetFiles(directory).OrderBy(file =>
            {
                var index = Array.IndexOf(FsSort.FileOrder, Path.GetFileNameWithoutExtension(file));
                return index == -1 ? int.MaxValue : index;
            }).ThenBy(f => f))
                if (extensions.Contains(Path.GetExtension(file).ToLower()))
                    yield return file;
        }

       
    }
}
