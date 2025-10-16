//using SimplifiedIndexerLib.Helpers;
//using SimplifiedIndexerLib.Index;
//using SimplifiedIndexerLib.IndexSearch;
//using SimplifiedIndexerLib.Tokens;
//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Threading.Tasks.Dataflow;

//namespace SimplifiedIndexerLib.Pipeline
//{
//    public class SearchPipeline
//    {
//        private readonly CancellationToken _token;

//        public SearchPipeline(CancellationToken token)
//        {
//            _token = token;
//        }

//        public async Task RunAsync(IEnumerable<SearchResult> results, DocIdStore docIdStore, Action<SearchResult> onSnippetReady)
//        {
//            // ────────────────────────────────────────────────
//            // 1️⃣ FILE READING STAGE (I/O bound)
//            // Reads file contents asynchronously.
//            // - Up to 2 files are processed concurrently.
//            // - Keeps a small queue to limit memory usage.
//            // ────────────────────────────────────────────────
//            var readFileBlock = new TransformBlock<SearchResult, (SearchResult, string)>(
//                async result =>
//                {
//                    result.DocPath = docIdStore.GetPathById(result.DocId);
//                    string text = await Task.Run(() => TextExtractor.GetText(result.DocPath), _token);
//                    return (result, text);
//                },
//                new ExecutionDataflowBlockOptions
//                {
//                    BoundedCapacity = 4,              // Limit queue size to avoid memory bloat
//                    MaxDegreeOfParallelism = 2,       // Read up to 2 files at once
//                    CancellationToken = _token
//                });

//            // ────────────────────────────────────────────────
//            // 2️⃣ SNIPPET GENERATION STAGE (CPU bound)
//            // Tokenizes text and generates snippets for matched positions.
//            // - Runs in parallel for up to 2 CPU tasks.
//            // - Uses bounded capacity to prevent overloading CPU threads.
//            // ────────────────────────────────────────────────
//            var snippetBlock = new TransformBlock<(SearchResult, string), SearchResult>(
//                async tuple =>
//                {
//                    var (result, text) = tuple;

//                    await Task.Run(() =>
//                    {
//                        if (string.IsNullOrEmpty(text) ||
//                            result.MatchedPositions == null ||
//                            result.MatchedPositions.Count == 0)
//                            return;

//                        SnippetBuilder.GenerateSnippets(result, docIdStore);
//                    }, _token);

//                    return result;
//                },
//                new ExecutionDataflowBlockOptions
//                {
//                    BoundedCapacity = 4,
//                    MaxDegreeOfParallelism = 2,       // Two concurrent snippet builders
//                    CancellationToken = _token
//                });

//            // ────────────────────────────────────────────────
//            // 3️⃣ RENDERING STAGE (UI bound)
//            // Passes completed results to the UI thread (or callback).
//            // - Runs single-threaded to ensure thread-safety of UI operations.
//            // ────────────────────────────────────────────────
//            var renderBlock = new ActionBlock<SearchResult>(
//                result => onSnippetReady(result),
//                new ExecutionDataflowBlockOptions
//                {
//                    BoundedCapacity = 2,
//                    MaxDegreeOfParallelism = 1,       // Sequential UI updates
//                    CancellationToken = _token
//                });

//            // ────────────────────────────────────────────────
//            // PIPELINE CONNECTIONS
//            // readFileBlock → snippetBlock → renderBlock
//            // Dataflow completion is propagated downstream.
//            // ────────────────────────────────────────────────
//            readFileBlock.LinkTo(snippetBlock, new DataflowLinkOptions { PropagateCompletion = true });
//            snippetBlock.LinkTo(renderBlock, new DataflowLinkOptions { PropagateCompletion = true });

//            // ────────────────────────────────────────────────
//            // FEED INPUT RESULTS INTO PIPELINE
//            // ────────────────────────────────────────────────
//            foreach (var r in results)
//            {
//                if (_token.IsCancellationRequested)
//                    break;

//                await readFileBlock.SendAsync(r, _token);
//            }

//            // Mark input as complete and wait for final output.
//            readFileBlock.Complete();
//            await renderBlock.Completion;
//        }
//    }
//}
