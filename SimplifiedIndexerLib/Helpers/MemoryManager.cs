using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplifiedIndexerLib.Helpers
{
    public static class MemoryManager
    {
        public static int CalculateDynamicThreshold(float percent)
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
    }
}
