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
    /// Write-Ahead Log (WAL) implementation.
    /// Buffers token writes in memory and periodically flushes them to disk.
    /// Supports background merging of flushed index segments.
    /// </summary>
    public class WAL : IDisposable
    {
        // In-memory log buffer (key = token string, value = serialized token bytes)
        private readonly ConcurrentQueue<KeyValuePair<string, byte[]>> _logQueue = new ConcurrentQueue<KeyValuePair<string, byte[]>>();

        // Threshold for flushing data to disk (calculated dynamically from available memory)
        int _threshHold;

        // Countdown until next merge operation (merges after every 25 flushes)
        short mergeCountdown = 25;

        /// <summary>
        /// Initializes a new WAL instance with a dynamic flush threshold
        /// based on available system memory.
        /// </summary>
        /// <param name="memoryUsagePercent">
        /// Percentage of available memory to target for WAL buffer usage.
        /// Default is 10%.
        /// </param>
        public WAL(float memoryUsagePercent = 10)
        {
            _threshHold = CalculateDynamicThreshold(memoryUsagePercent);
        }

        /// <summary>
        /// Calculates a dynamic flush threshold based on system memory availability.
        /// Falls back to a fixed threshold if system counters are unavailable.
        /// </summary>
        private int CalculateDynamicThreshold(float percent)
        {
            try
            {
                using (var pc = new PerformanceCounter("Memory", "Available MBytes"))
                {
                    float availableMb = pc.NextValue();
                    float targetUsageMb = availableMb * (percent / 100f);
                    return (int)(targetUsageMb * 1_000); // Approximate entries per MB
                }
            }
            catch (Exception ex)
            {
                // If performance counter fails, fallback to a safe static threshold
                Console.WriteLine($"Failed to calculate threshold: {ex.Message}");
                return 1_000_000; // default = 1M entries
            }
        }

        /// <summary>
        /// Enqueues a token for writing.
        /// Triggers a flush when the in-memory buffer exceeds the threshold.
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
        /// Groups tokens by key, writes combined data to a new index file,
        /// and schedules a background merge when needed.
        /// </summary>
        public void Flush()
        {
            var groupedData = new Dictionary<string, List<byte[]>>();

            // Drain queue and group entries by key
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
                Console.WriteLine($"Flushing: {flushIndex}\\{flushCount}");
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

            // Cleanup resources
            progressTimer.Stop();
            progressTimer.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Console.WriteLine("Flush complete");

            mergeCountdown--;
            if (mergeCountdown == 0)
            {
                IndexMerger.Merge();
                mergeCountdown = 25;
            }
        }

        /// <summary>
        /// Ensures any pending data is flushed and merged before disposal.
        /// </summary>
        public void Dispose()
        {
            Flush();
            IndexMerger.Merge();
            WordsStore.SortWordsByIndex();
        }
    }
}
