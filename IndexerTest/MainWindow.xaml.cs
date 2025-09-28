using SimplifiedIndexerLib.Index;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SimplifiedIndexerLib.IndexSearch;

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
                    IndexCreator.Execute(directory, new string[] { ".txt", ".pdf" }, MemoryUsageBox.Value);
                    //IndexManager.CreateIndex(directory, new string[] { ".txt", ".pdf" }, MemoryUsageBox.Value);
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }


        public async void Search()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            try
            {
                string query = SearchBox.Text;
                short adjacency = (short)AdjacencySettingsBox.Value;

                string htmlStyle = "font-family:Segoe UI; direction:rtl;";
                WebView.NavigateToString($"<html><body style='{htmlStyle}' id='results'></body></html>");

                using (var docIdStore = new DocIdStore())
                {
                    await Task.Run(() =>
                    {
                        var results = SearchIndex.Execute(query, adjacency);

                        foreach (var result in results)
                        {
                            SnippetBuilder.GenerateSnippets(result, docIdStore);

                            var sb = new StringBuilder();
                            foreach (var snippet in result.Snippets)
                            {
                                if (token.IsCancellationRequested)
                                    return;

                                sb.AppendLine($@"
        <div style='margin-bottom:12px;'>
           <b>Document {result.DocId}</b><br/>
           <small style='color:gray;'>{Path.GetFileName(result.DocPath)}</small><br/>
           {snippet}<br/>
        </div>");
                            }

                            if (sb.Length > 0)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    string js = $@"
        var container = document.getElementById('results');
        container.insertAdjacentHTML('beforeend', `{sb}`);";
                                    WebView.ExecuteScriptAsync(js);
                                });
                            }
                        }
                    }, token);
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }




        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
           
        }
    }
}
