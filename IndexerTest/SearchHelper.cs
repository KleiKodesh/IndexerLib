using Microsoft.Web.WebView2.Wpf;
using SimplifiedIndexerLib.Helpers;
using SimplifiedIndexerLib.Index;
using SimplifiedIndexerLib.IndexSearch;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using System.Windows;

namespace IndexerTest
{
    public class SearchHelper
    {
        WebView2 _webView;
        private CancellationTokenSource cts;
        public async void Search(string query, short adjacency, WebView2 webView)
        {
            var start = DateTime.Now;
            _webView = webView;

            cts?.Cancel();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            try
            {
                string htmlStyle = "font-family:Segoe UI; direction:rtl; text-align: justify;";
                _webView.NavigateToString($"<html><body style='{htmlStyle}' id='results'><ol></ol></body></html>");

                using (var docIdStore = new DocIdStore())
                {
                    // Get the search results IEnumarable (these only contain DocId and MatchedPositions)
                    var results = SearchEngine.Execute(query, adjacency).ToList();
                    Console.WriteLine("Results time: " + (DateTime.Now - start));

                    // === Stage 1: Producer (read file text) ===
                    var readBlock = new TransformBlock<SearchResult, (SearchResult, string)>(result =>
                    {
                        result.DocPath = docIdStore.GetPathById(result.DocId);
                        string text = TextExtractor.GetText(result.DocPath);
                        return (result, text);
                    },
                    new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount,
                        BoundedCapacity = Environment.ProcessorCount,
                        CancellationToken = token
                    });

                    // === Stage 2: Consumer (tokenize + build snippet) ===
                    var buildBlock = new ActionBlock<(SearchResult, string)>(tuple =>
                    {
                        var (result, text) = tuple;
                        result.DocPath = docIdStore.GetPathById(result.DocId);
                        SnippetBuilder.GenerateSnippets(result, text);

                        foreach (var snippet in result.Snippets)
                        {
                            if (token.IsCancellationRequested)
                                return;

                            string html = $@"
                                    <li>
                                       <b>{Path.GetFileName(result.DocPath)}</b><br/>
                                       <small style='color:gray;'>Document {result.DocId}</small><br/>
                                       {snippet}<br/>
                                    </li>";

                            FlushHtml(html, token);
                        }
                    },
                    new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount, // allow 2 tokenizers concurrently
                        BoundedCapacity = 2,
                        CancellationToken = token
                    });

                    // Link producer → consumer
                    readBlock.LinkTo(buildBlock, new DataflowLinkOptions { PropagateCompletion = true });

                    // Feed producer
                    foreach (var result in results)
                    {
                        if (token.IsCancellationRequested)
                            break;
                        await readBlock.SendAsync(result, token);
                    }

                    readBlock.Complete();

                    // Wait for all work to finish
                    await buildBlock.Completion;
                }
            }
            catch (OperationCanceledException)
            {
                // cancelled
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var elapsed = DateTime.Now - start;
            Console.WriteLine($"Total time: {elapsed.TotalSeconds:F2} seconds ({elapsed.TotalMilliseconds:F0} ms)");
        }

        private void FlushHtml(string html, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                string js = $@"
var container = document.querySelector('#results ol');
container.insertAdjacentHTML('beforeend', `{html}`);";
                _webView.ExecuteScriptAsync(js);
            });
        }
    }
}
