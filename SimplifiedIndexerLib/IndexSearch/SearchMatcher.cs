using System.Collections.Generic;

namespace SimplifiedIndexerLib.IndexSearch
{
    public static class SearchMatcher
    {
        public static IEnumerable<SearchResult> OrderedAdjacencyMatch(
            Dictionary<int, List<List<int>>> docs, int adjacency)
        {
            /*  Adjust adjacency by +1 because position differences are off by one:
                For example, if two words are adjacent (positions 10 and 11),
                their distance is 1, not 0. So user adjacency=0 (exact phrase)
                should allow position diff=1. This keeps "adjacency" meaning
                consistent with "number of words between terms".    */
            adjacency++; 

            foreach (var docEntry in docs) // no need to sort docs
            {
                var postingsLists = docEntry.Value;
                if (postingsLists.Count == 0)
                    continue;

                var resultForDoc = new SearchResult { DocId = docEntry.Key  };

                var firstList = postingsLists[0];
                int[] indices = new int[postingsLists.Count]; // per postings list pointers

                foreach (var startPos in firstList)
                {
                    bool valid = true;
                    int prevPos = startPos;
                    var match = new Postings[postingsLists.Count];
                    match[0] = new Postings { Position = startPos };

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

                        match[i] = new Postings { Position = plist[idx] };
                        prevPos = plist[idx];
                        indices[i] = idx; // store position for reuse
                    }

                    if (valid)
                        resultForDoc.Matches.Add(match);
                }

                yield return resultForDoc;
            }
        }
    }
}
