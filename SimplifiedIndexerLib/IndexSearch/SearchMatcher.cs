using System.Collections.Generic;

namespace SimplifiedIndexerLib.IndexSearch
{
    public static class SearchMatcher
    {
        public static IEnumerable<SearchResult> OrderedAdjacencyMatch(
            Dictionary<int, List<List<int>>> docs, int adjacency)
        {
            adjacency--; // adjust adjacency

            foreach (var docEntry in docs) // no need to sort docs
            {
                var postingsLists = docEntry.Value;
                if (postingsLists.Count == 0)
                    continue;

                var resultForDoc = new SearchResult
                {
                    DocId = docEntry.Key,
                    MatchedPositions = new List<int[]>()
                };

                var firstList = postingsLists[0];
                int[] indices = new int[postingsLists.Count]; // per postings list pointers

                foreach (var startPos in firstList)
                {
                    bool valid = true;
                    int prevPos = startPos;
                    var match = new int[postingsLists.Count];
                    match[0] = startPos;

                    // sequentially advance each postings pointer
                    for (int i = 1; i < postingsLists.Count; i++)
                    {
                        var plist = postingsLists[i];
                        var idx = indices[i];

                        // advance idx until position > prevPos and within adjacency
                        while (idx < plist.Count && plist[idx] <= prevPos)
                            idx++;

                        if (idx >= plist.Count || plist[idx] - prevPos > adjacency)
                        {
                            valid = false;
                            break;
                        }

                        match[i] = plist[idx];
                        prevPos = plist[idx];
                        indices[i] = idx; // store position for reuse
                    }

                    if (valid)
                        resultForDoc.MatchedPositions.Add(match);
                }

                yield return resultForDoc;
            }
        }
    }
}
