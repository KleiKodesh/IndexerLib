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
            List<string> files = SelectFiles();
            if (files == null || files.Count == 0)
                return;

            Console.WriteLine("Deleting Files...");
            
            // Convert file paths → DocIds
            var docIdsToDelete = new HashSet<int>();
            using(new ConsoleSpinner())
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

                foreach (var kvp in reader.EnumerateTokenGroups())
                {
                    var key = kvp.Key;
                    var tokenGroup = kvp.Value.Where(t=> !docIdsToDelete.Contains(t.DocId)).ToList();
                    var data = Serializer.SerializeTokenGroup(tokenGroup);
                    writer.Put(key.Hash, data);
                }
            }

            File.Delete(oldIndexPath);

            WordsStore.SortWordsByIndex();

            Console.WriteLine("Deletiion complete!");
        }


        static List<string> SelectFiles()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Files to Delete from Index",
                Filter = "All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var selectedFiles = dialog.FileNames.ToList();
                System.Diagnostics.Debug.WriteLine($"Selected {selectedFiles.Count} files.");

                return selectedFiles;
            }

            return new List<string>();
        }
    }
}
