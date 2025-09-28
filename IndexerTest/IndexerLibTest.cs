//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace IndexerTest
//{
//    // change idea to virtualiztion one day for now just limit results to 20000
//    public async Task Search()
//    {
//        cts?.Cancel();
//        cts?.Dispose();
//        cts = new CancellationTokenSource();
//        var token = cts.Token;

//        try
//        {
//            string query = SearchBox.Text;
//            short adjacency = (short)AdjacencySettingsBox.Value;

//            string htmlStyle = "font-family:Segoe UI; direction:rtl;";
//            WebView.NavigateToString($"<html><body style='{htmlStyle}' id='results'></body></html>");

//            using (var docIdStore = new DocIdStore())
//            {
//                await Task.Run(() =>
//                {
//                    var results = SearchIndex.Execute(query, adjacency).ToList();
//                    SnippetBuilder.BuildSnippets(results);

//                    foreach (var result in results)
//                    {
//                        if (token.IsCancellationRequested)
//                            break; // stop gracefully

//                        string snippet = $@"
//<div style='margin-bottom:12px;'>
//   <b>Document {result.DocId}</b><br/>
//   <small style='color:gray;'>{Path.GetFileName(result.DocPath)}</small><br/>
//   {result.Snippet}<br/>
//</div>";

//                        Application.Current.Dispatcher.InvokeAsync(() =>
//                        {
//                            string js = $@"
//var container = document.getElementById('results');
//container.insertAdjacentHTML('beforeend', `{snippet}`);";
//                            WebView.ExecuteScriptAsync(js);
//                        });
//                    }
//                }, token);
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            // expected, ignore
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine(ex.Message);
//        }
//    }
//}
