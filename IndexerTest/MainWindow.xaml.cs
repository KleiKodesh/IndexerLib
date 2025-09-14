using IndexerLib.Helpers;
using IndexerLib.Index;
using IndexerLib.IndexManger;
using IndexerLib.IndexSearch;
using IndexerLib.Sample;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IndexerTest
{
    public partial class MainWindow : Window
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        public MainWindow()
        {
            InitializeComponent();
            WebView.EnsureCoreWebView2Async();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { Search(); e.Handled = true; }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }


        private void CreateIndexButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CommonOpenFileDialog
                {
                    IsFolderPicker = true
                };

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string directory = dialog.FileName;
                    IndexManager.CreateIndex(directory, new string[] { ".txt", ".pdf" }, MemoryUsageBox.Value);
                }
            }
            catch (Exception ex) {Console.WriteLine(ex.Message); }
        }


        // change idea to virtualiztion one day for now just limit results to 20000
        public async void Search()
        {
            // Cancel any previous search
            cts.Cancel();
            cts = new CancellationTokenSource();
            var token = cts.Token;
            int counter = 0;

            try
            {
                string query = SearchBox.Text;
                short adjacency = (short)AdjacencySettingsBox.Value;

                string htmlStyle = "font-family:Segoe UI; direction:rtl;";
                string initialHtml = $"<html><body style='{htmlStyle}' id='results'></body></html>";
                WebView.NavigateToString(initialHtml);

                await Task.Run(async () =>
                {
                    using (var docIdStore = new DocIdStore())
                    {
                        foreach (var result in SearchIndex.Execute(query, adjacency))
                        {
                            if (counter++ > 2000) return;
                            token.ThrowIfCancellationRequested(); // ✅ will stop immediately

                            SnippetBuilder.GenerateSnippet(result, docIdStore);

                            string safePath = WebUtility.HtmlEncode(result.DocPath ?? "");
                            string snippetHtml = $@"
                        <div style='margin-bottom:12px;'>
                            <b>Document {result.DocId}</b><br/>
                            <small style='color:gray;'>Path: {safePath}</small><br/>
                            {result.Snippet}<br/>
                        </div>";

                            await Application.Current.Dispatcher.InvokeAsync(async () =>
                            {
                                string js = $@"
                            var container = document.getElementById('results');
                            container.insertAdjacentHTML('beforeend', `{snippetHtml}`);
                        ";
                                await WebView.ExecuteScriptAsync(js);
                            }, System.Windows.Threading.DispatcherPriority.Background, token); // ✅ pass token
                        }
                    }
                }, token); // ✅ pass token to Task.Run
            }
            catch (OperationCanceledException)
            {
                // Expected when a new search starts before previous finishes — ignore
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }




        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            WordsStore.SortWordsByIndex();
        }

        void TokenizerTest()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = false, // pick a single file
                Filters = { new CommonFileDialogFilter("Text files", "*.txt") }
            };

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;

            string filePath = dialog.FileName;
            string text = System.IO.File.ReadAllText(filePath);

            // Tokenize
            var tokensDict = IndexerLib.Tokens.RegexTokenizer.Tokenize(text, filePath);
            var tokens = new List<string>(tokensDict.Keys);

            // Build HTML list
            var sb = new StringBuilder();
            sb.AppendLine("<html><body style='font-family:Segoe UI; direction:rtl;'>");
            sb.AppendLine("<h3>Tokens:</h3>");
            sb.AppendLine("<ul>");
            foreach (var token in tokens)
                sb.AppendLine($"<li>{WebUtility.HtmlEncode(token)}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("</body></html>");

            WebView.NavigateToString(sb.ToString());
        }
    }
}
