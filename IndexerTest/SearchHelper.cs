using Microsoft.Web.WebView2.Wpf;
using SimplifiedIndexerLib.Helpers;
using SimplifiedIndexerLib.Index;
using SimplifiedIndexerLib.IndexSearch;
using SimplifiedIndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using System.Windows;

namespace IndexerTest
{
    public class SearchHelper
    {
        WebView2 _webView;
        public async void Search(CancellationTokenSource cts, string query, short adjacency, WebView2 webView)
        {
            var start = DateTime.Now;
            _webView = webView;

            cts?.Cancel();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            try
            {
                string htmlStyle = "font-family:Segoe UI; direction:rtl;";
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
                        GenerateSnippets(result, text);

                        foreach (var snippet in result.Snippets)
                        {
                            if (token.IsCancellationRequested)
                                return;

                            string html = $@"
<li>
   <b>Document {result.DocId}</b><br/>
   <small style='color:gray;'>{Path.GetFileName(result.DocPath)}</small><br/>
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


        void GenerateSnippets(SearchResult result, string docText, int windowSize = 100)
        {
            result.Snippets = new List<string>();
            if (string.IsNullOrEmpty(docText) ||
                result.MatchedPositions == null ||
                result.MatchedPositions.Count == 0)
                return;            

            foreach (var matchPositions in result.MatchedPositions)
            {
                if (matchPositions == null || matchPositions.Length == 0)
                    continue;

                var tokens = new TokenStream(docText).Tokens;
                var postings = new List<Postings>(matchPositions.Length);
                foreach (var position in matchPositions)
                {
                    var match = tokens[position];
                    postings.Add(new Postings(position, match.Index, match.Length));
                }

                tokens = null;

                if (postings.Count == 0)
                    continue;

                // Compute snippet bounds
                int matchStart = postings.Min(p => p.StartIndex);
                int matchEnd = postings.Max(p => p.StartIndex + p.Length);
                int snippetStart = Math.Max(0, matchStart - windowSize);
                int snippetEnd = Math.Min(docText.Length, matchEnd + windowSize);
                string snippet = docText.Substring(snippetStart, snippetEnd - snippetStart);

                // Highlight matched tokens efficiently
                var highlights = postings
                    .OrderBy(p => p.StartIndex)
                    .Select(p => new
                    {
                        RelativeStart = p.StartIndex - snippetStart,
                        p.Length
                    })
                    .Where(h => h.RelativeStart < snippet.Length && h.RelativeStart + h.Length > 0)
                    .ToList();

                var sb = new StringBuilder(snippet);
                for (int i = highlights.Count - 1; i >= 0; i--)
                {
                    var h = highlights[i];
                    int relStart = Math.Max(0, h.RelativeStart);
                    int len = Math.Min(h.Length, Math.Max(0, snippet.Length - relStart));
                    if (len <= 0) continue;

                    sb.Insert(relStart + len, "</mark>");
                    sb.Insert(relStart, "<mark>");
                }

                result.Snippets.Add(sb.ToString());
            }
        }

    }
}
