//using Microsoft.WindowsAPICodePack.Dialogs;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Text;
//using System.Text.Encodings.Web;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Windows;

//namespace IndexerTest
//{
//    internal class SimplifiedIndexerTest
//    {
//        public async void Search()
//        {
//            cts?.Cancel();
//            cts?.Dispose();
//            cts = new CancellationTokenSource();
//            var token = cts.Token;

//            try
//            {
//                string query = SearchBox.Text;
//                short adjacency = (short)AdjacencySettingsBox.Value;

//                string htmlStyle = "font-family:Segoe UI; direction:rtl;";
//                WebView.NavigateToString($"<html><body style='{htmlStyle}' id='results'></body></html>");

//                using (var docIdStore = new DocIdStore())
//                {
//                    await Task.Run(() =>
//                    {
//                        var results = SearchIndex.Execute(query, adjacency);

//                        foreach (var result in results)
//                        {
//                            SnippetBuilder.GenerateSnippets(result, docIdStore);

//                            var sb = new StringBuilder();
//                            foreach (var snippet in result.Snippets)
//                            {
//                                if (token.IsCancellationRequested)
//                                    return;

//                                sb.AppendLine($@"
//<div style='margin-bottom:12px;'>
//   <b>Document {result.DocId}</b><br/>
//   <small style='color:gray;'>{Path.GetFileName(result.DocPath)}</small><br/>
//   {snippet}<br/>
//</div>");
//                            }

//                            if (sb.Length > 0)
//                            {
//                                Application.Current.Dispatcher.Invoke(() =>
//                                {
//                                    string js = $@"
//var container = document.getElementById('results');
//container.insertAdjacentHTML('beforeend', `{sb}`);";
//                                    WebView.ExecuteScriptAsync(js);
//                                });
//                            }
//                        }
//                    }, token);
//                }
//            }
//            catch (Exception ex) { Console.WriteLine(ex.Message); }
//        }



//        private void DebugButton_Click(object sender, RoutedEventArgs e)
//        {
//            using (var reader = new IndexReader())
//            {
//                var tokens = reader.GetAllTokens().ToList();

//                var json = System.Text.Json.JsonSerializer.Serialize(
//                    tokens,
//                    new System.Text.Json.JsonSerializerOptions
//                    {
//                        WriteIndented = true, // pretty-print with newlines
//                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // show Unicode directly
//                    });

//                // Build the path to a temp file on the desktop
//                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
//                string tempFilePath = Path.Combine(desktopPath, "tokens_temp.json");

//                // Write the JSON to the file
//                File.WriteAllText(tempFilePath, json);
//            }
//        }


//        void TokenizerTest()
//        {
//            var dialog = new CommonOpenFileDialog
//            {
//                IsFolderPicker = false, // pick a single file
//                Filters = { new CommonFileDialogFilter("Text files", "*.txt") }
//            };

//            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;

//            string filePath = dialog.FileName;
//            string text = System.IO.File.ReadAllText(filePath);

//            // Tokenize
//            var tokensDict = SimplifiedIndexerLib.Tokens.RegexTokenizer.Tokenize(text, filePath);

//            // Build HTML list
//            var sb = new StringBuilder();
//            sb.AppendLine("<html><body style='font-family:Segoe UI; direction:rtl;'>");
//            sb.AppendLine("<h3>Tokens:</h3>");
//            sb.AppendLine("<ul>");
//            foreach (var token in tokensDict)
//            {
//                foreach (var pos in token.Value.Postions)
//                    sb.AppendLine($"<li>{pos + ", " + WebUtility.HtmlEncode(token.Key)}</li>");
//            }

//            sb.AppendLine("</ul>");
//            sb.AppendLine("</body></html>");

//            WebView.NavigateToString(sb.ToString());
//        }
//    }
//}
