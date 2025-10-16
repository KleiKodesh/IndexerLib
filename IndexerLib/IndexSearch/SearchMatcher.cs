using IndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexerLib.IndexSearch
{
    public static class SearchMatcher
    {
        public static IEnumerable<SearchResult> OrderedAdjacencyMatch(
            Dictionary<int, List<List<Postings>>> docs, int adjacency)
        {
            /*  Adjust adjacency by +1 because position differences are off by one:
               For example, if two words are adjacent (positions 10 and 11),
               their distance is 1, not 0. So user adjacency=0 (exact phrase)
               should allow position diff=1. This keeps "adjacency" meaning
               consistent with "number of words between terms".    */
            adjacency++;

            foreach (var docEntry in docs.OrderBy(kvp => kvp.Key))
            {
                var postingsLists = docEntry.Value;
                int termCount = postingsLists.Count;
                if (termCount == 0)
                    continue;

                var resultForDoc = new SearchResult
                {
                    DocId = docEntry.Key,
                    MatchedPostings = new List<Postings[]>()
                };

                var firstList = postingsLists[0];
                var indices = new int[termCount]; // per-list cursor

                for (int i = 0; i < firstList.Count; i++)
                {
                    var start = firstList[i];
                    var currentMatch = new Postings[termCount];
                    currentMatch[0] = start;

                    int prevPos = start.Position;
                    bool valid = true;

                    // Sequentially advance through other lists
                    for (int listIdx = 1; listIdx < termCount; listIdx++)
                    {
                        var plist = postingsLists[listIdx];
                        int cursor = indices[listIdx];

                        // advance until position > prevPos
                        while (cursor < plist.Count && plist[cursor].Position <= prevPos)
                            cursor++;

                        if (cursor >= plist.Count)
                        {
                            valid = false;
                            break;
                        }

                        var next = plist[cursor];
                        int diff = next.Position - prevPos;

                        if (diff > adjacency)
                        {
                            // No match — but next positions might still fit a later startPosting
                            // Keep the cursor here for future iterations
                            valid = false;
                            break;
                        }

                        currentMatch[listIdx] = next;
                        prevPos = next.Position;
                        indices[listIdx] = cursor;
                    }

                    if (valid)
                        resultForDoc.MatchedPostings.Add(currentMatch);
                }

                if (resultForDoc.MatchedPostings.Count > 0)
                    yield return resultForDoc;
            }
        }
    }
}
