using IndexerLib.Helpers;
using IndexerLib.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexerLib.IndexSearch
{

    public static class SnippetBuilder
    {
        // Generate snippets for all matches in search results
        public static List<Snippet> BuildSnippets(List<SearchResult> searchResults, int windowSize = 100)
        {
            var startTime = DateTime.Now;
            Console.WriteLine("Building Snippets...");
            var snippets = new List<Snippet>();

            using (var docIdStore = new DocIdStore())
            foreach (var result in searchResults)
            {
                string docPath = docIdStore.GetPathById(result.DocId);
                string docText = TextExtractor.GetText(docPath);
                if (string.IsNullOrEmpty(docText)) continue;

                foreach (var match in result.Matches)
                {
                    int startPos = Math.Max(match.Min() - windowSize, 0);
                    int endPos = Math.Min(match.Max() + windowSize, docText.Length);

                    string snippetText = docText.Substring(startPos, endPos - startPos);

                    snippets.Add(new Snippet
                    {
                        DocId = result.DocId,
                        DocPath = docPath,
                        MatchPositions = match,
                        Text = snippetText
                    });
                }
            }

            Console.WriteLine("Snippets complete. Elapsed: " + (DateTime.Now - startTime));
            return snippets;
        }
    }
}
