using IndexerLib.Helpers;
using IndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IndexerLib.Index
{
    /// <summary>
    /// Handles deletion of specific documents from the index file.
    /// This process rebuilds the index excluding all tokens that belong
    /// to the specified DocIds, without altering the DocId store itself.
    /// </summary>
    public static class Deletion
    {
        /// <summary>
        /// Deletes all tokens belonging to the given document paths from the index.
        /// </summary>
        public static void Execute()
        {
            var files = SelectFiles();
            if (files == null || files.Length == 0)
                return;

            var startTime = DateTime.Now;
            Console.WriteLine("Deleting Files...");

            // Convert file paths → DocIds
            var docIdsToDelete = new HashSet<int>();
            using (new ConsoleSpinner())
            {
                using (var docIdStore = new DocIdStore())
                {
                    foreach (var file in files)
                    {
                        int id = docIdStore.GetIdByPath(file);
                        if (id > 0)
                            docIdsToDelete.Add(id);
                    }
                }

                string oldIndexPath;

                using (var reader = new IndexReader())
                using (var writer = new IndexWriter())
                {
                    oldIndexPath = reader.TokenStorePath;

                    foreach (var entry in reader.EnumerateTokenGroups())
                    {
                        var key = entry.Key;
                        var tokenGroup = entry.Tokens.Where(t => !docIdsToDelete.Contains(t.DocId)).ToArray();
                        var data = Serializer.SerializeTokenGroup(tokenGroup);
                        writer.Put(key.Hash, data);
                    }
                }

                File.Delete(oldIndexPath);

                WordsStore.SortWordsByIndex();
            }

            Console.WriteLine("Deletion complete! Time elapsed: " + (DateTime.Now - startTime));
        }


        static string[] SelectFiles()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Files to Delete from Index",
                Filter = "All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var selectedFiles = dialog.FileNames;
                System.Diagnostics.Debug.WriteLine($"Selected {selectedFiles.Length} files.");

                return selectedFiles;
            }

            return new string[0];
        }
    }
}
