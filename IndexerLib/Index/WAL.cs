using IndexerLib.Tokens;
using IndexerLib.Index;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using IndexerLib.IndexManger;

namespace IndexerLib.Index
{
    /// <summary>
    /// WAL (Write-Ahead Log) implementation.
    /// Buffers token writes in memory and flushes them to disk when threshold is reached.
    /// Uses background merging of flushed index files.
    /// </summary>
    public class WAL : IDisposable
    {
        // In-memory log buffer (key = token string, value = serialized token bytes)
        private readonly ConcurrentQueue<KeyValuePair<string, byte[]>> _logQueue = new ConcurrentQueue<KeyValuePair<string, byte[]>>();
        int _threshHold;         // Threshold for when to flush (based on memory availability)
        short mergeCountdown = 25;

        /// <summary>
        /// Initializes WAL with dynamic flush threshold based on available memory.
        /// </summary>
        public WAL(float memoryUsagePercent = 10)
        {
            _threshHold = CalculateDynamicThreshold(memoryUsagePercent);
        }

        /// <summary>
        /// Dynamically calculates flush threshold based on system memory availability.
        /// </summary>
        private int CalculateDynamicThreshold(float percent)
        {
            try
            {
                using (var pc = new PerformanceCounter("Memory", "Available MBytes"))
                {
                    float availableMb = pc.NextValue();
                    float targetUsageMb = availableMb * (percent / 100f);
                    return (int)(targetUsageMb * 1_000); // Convert MB to approx. entries
                }
            }
            catch (Exception ex)
            {
                // If performance counters fail, fall back to a fixed threshold
                Console.WriteLine($"Failed to calculate threshold: {ex.Message}");
                return 1_000_000; // fallback = 1M entries
            }
        }

        /// <summary>
        /// Logs a token into the WAL (enqueues in memory).
        /// Flushes to disk if buffer exceeds threshold.
        /// </summary>
        public void Log(string key, Token token)
        {
            if (_logQueue.Count > _threshHold)
                Flush();

            byte[] serialized = Serializer.SerializeToken(token);
            _logQueue.Enqueue(new KeyValuePair<string, byte[]>(key, serialized));
        }

        /// <summary>
        /// Flushes all buffered tokens to disk.
        /// Groups by key, writes combined data, and enqueues file for merge.
        /// </summary>
        public void Flush()
        {
            var groupedData = new Dictionary<string, List<byte[]>>();

            // Dequeue all items and group by key
            while (_logQueue.TryDequeue(out var item))
            {
                if (!groupedData.ContainsKey(item.Key))
                    groupedData[item.Key] = new List<byte[]>();

                groupedData[item.Key].Add(item.Value);
            }

            if (groupedData.Count == 0)
                return;

            Console.WriteLine("Flushing...");

            int flushCount = groupedData.Count;
            int flushIndex = 0;

            // Progress reporting every second
            System.Timers.Timer progressTimer = new System.Timers.Timer(1000);
            progressTimer.Elapsed += (sender, e) =>
                Console.WriteLine("Flushing: " + flushIndex + "\\" + flushCount);

            progressTimer.Start();

            string indexPath;
            using (var writer = new IndexWriter())
            {
                foreach (var entry in groupedData)
                {
                    flushIndex++;
                    var combined = entry.Value.SelectMany(b => b).ToArray();
                    writer.Put(data: combined, entry.Key);
                }
                indexPath = writer.TokenStorePath;
            }

            // Cleanup
            progressTimer.Stop();
            progressTimer.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Console.WriteLine("Flush done");

            mergeCountdown--;
            if (mergeCountdown == 0)
            {
                IndexMerger.Merge();
                mergeCountdown = 25;
            }

        }

        /// <summary>
        /// Ensures flush + merge before disposal.
        /// </summary>
        public void Dispose()
        {
            Flush();
            IndexMerger.Merge();
            WordsStore.SortWordsByIndex();
        }
    }
}
