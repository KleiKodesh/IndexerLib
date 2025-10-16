using IndexerLib.Index;
using Microsoft.WindowsAPICodePack.Dialogs;
using IndexerLib.IndexSearch;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using IndexerLib.Tokens;
using IndexerLib.Helpers;

namespace IndexerTest
{
    public partial class MainWindow : Window
    {

        private CancellationTokenSource _cts = new CancellationTokenSource();
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
            string query = SearchBox.Text;
            var adjacency = (short)AdjacencySettingsBox.Value;
            var start = DateTime.Now;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            string htmlStyle = "font-family:Segoe UI; direction:rtl; text-align: justify;";
            WebView.NavigateToString($"<html><body style='{htmlStyle}' id='results'><ol></ol></body></html>");

            try
            {
                await Task.Run(() =>
                {
                    using (var docIdStore = new DocIdStore())
                    {
                        var results = SearchEngine.Execute(query, adjacency).ToList();

                        foreach (var result in results)
                        {
                            token.ThrowIfCancellationRequested();

                            result.DocPath = docIdStore.GetPathById(result.DocId);
                            //string text = TextExtractor.GetText(result.DocPath);
                            SnippetBuilder.GenerateSnippets(result, docIdStore);

                            foreach (var snippet in result.Snippets)
                            {
                                token.ThrowIfCancellationRequested();

                                string html = $@"
        <li>
            <b>{Path.GetFileName(result.DocPath)}</b><br/>
            <small style='color:gray;'>Document {result.DocId}</small><br/>
            {snippet}<br/>
        </li>";

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (token.IsCancellationRequested) return;

                                    string js = $@"
        var container = document.querySelector('#results ol');
        container.insertAdjacentHTML('beforeend', `{html}`);";

                                    WebView?.ExecuteScriptAsync(js);
                                });
                            }
                        }
                    }
                }, token);
            }
            catch (OperationCanceledException)
            {
                // search cancelled — do nothing
            }
            catch (Exception ex)
            {
                Console.WriteLine("Search error: " + ex.Message);
            }

            Console.WriteLine("Total Search Time: " + (DateTime.Now - start));
        }


        //        public async void Search()
        //        {
        //            string query = SearchBox.Text;
        //            var adjacency = (short)AdjacencySettingsBox.Value;
        //            var start = DateTime.Now;

        //            _cts?.Cancel();
        //            _cts?.Dispose();
        //            _cts = new CancellationTokenSource();
        //            var token = _cts.Token;

        //            string htmlStyle = "font-family:Segoe UI; direction:rtl; text-align: justify;";
        //            WebView.NavigateToString($"<html><body style='{htmlStyle}' id='results'><ol></ol></body></html>");

        //            try
        //            {
        //                await Task.Run(() =>
        //                {
        //                    using (var docIdStore = new DocIdStore())
        //                    {
        //                        var results = SearchEngine.Execute(query, adjacency).ToList();

        //                        foreach (var result in results)
        //                        {
        //                            token.ThrowIfCancellationRequested();

        //                            result.DocPath = docIdStore.GetPathById(result.DocId);
        //                            string text = TextExtractor.GetText(result.DocPath);
        //                            SnippetBuilder.GenerateSnippets(result, text);

        //                            foreach (var snippet in result.Snippets)
        //                            {
        //                                token.ThrowIfCancellationRequested();

        //                                string html = $@"
        //<li>
        //    <b>{Path.GetFileName(result.DocPath)}</b><br/>
        //    <small style='color:gray;'>Document {result.DocId}</small><br/>
        //    {snippet}<br/>
        //</li>";

        //                                Application.Current.Dispatcher.Invoke(() =>
        //                                {
        //                                    if (token.IsCancellationRequested) return;

        //                                    string js = $@"
        //var container = document.querySelector('#results ol');
        //container.insertAdjacentHTML('beforeend', `{html}`);";

        //                                    WebView?.ExecuteScriptAsync(js);
        //                                });
        //                            }
        //                        }
        //                    }
        //                }, token);
        //            }
        //            catch (OperationCanceledException)
        //            {
        //                // search cancelled — do nothing
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine("Search error: " + ex.Message);
        //            }

        //            Console.WriteLine("Total Search Time: " + (DateTime.Now - start));
        //        }


        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            //var filePicker = new Microsoft.Win32.OpenFileDialog();
            //if (filePicker.ShowDialog() == true)
            //{
            //    var tokens = new Tokenizer(filePicker.FileName, 0).Tokens;
            //    foreach (var token in tokens)
            //    {
            //        return;
            //    }
            //}
        }
    }
}
