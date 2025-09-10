using IndexerLib.Index;
using IndexerLib.Tokens;
using System;
using System.IO;
using System.Linq;
using System.Timers;
using Docnet.Core;          // Docnet library
using Docnet.Core.Models;

namespace ConsoleTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //string directory = @"C:\Users\Admin\source\repos\pcinfogmach\IndexerLib\IndexerLibConsoleDemo\bin\Debug\otzaria-library-main";
            //string directory = @"C:\אוצריא\אוצריא\תלמוד בבלי\סדר זרעים"; 
            //string directory = @"C:\אוצריא\אוצריא\תנך\תורה"; 
            //string directory = @"C:\אוצריא\אוצריא\תנך";
            string directory = @"C:\אוצריא\אוצריא";

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

            using (var wal = new WAL())
            using (var docLib = DocLib.Instance) // Docnet
            {
                foreach (var file in files)
                {
                    currentIndex++;
                    try
                    {
                        string content = string.Empty;

                        if (file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        {
                            // read plain text file
                            content = File.ReadAllText(file);
                        }
                        else if (file.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            // extract text from pdf using Docnet
                            using (var docReader = docLib.GetDocReader(File.ReadAllBytes(file), new PageDimensions()))
                            {
                                for (int i = 0; i < docReader.GetPageCount(); i++)
                                {
                                    using (var pageReader = docReader.GetPageReader(i))
                                    {
                                        content += pageReader.GetText(); // get text per page
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            var tokens = Tokenizer.Tokenize(content, file);
                            foreach (var token in tokens)
                                wal.Log(token.Key, token.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading {file}: {ex.Message}");
                    }
                }
            }

            progressTimer.Stop();
            progressTimer.Dispose();

            Console.WriteLine($"Indexing complete! start time: {indexStart} end time: {DateTime.Now} total time: {DateTime.Now - indexStart}");
        }
    }
}
