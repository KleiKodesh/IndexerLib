
namespace IndexerLib.Helpers
{
    using System;
    using System.Timers;

    public class ConsoleDotAnimator : IDisposable
    {
        private readonly Timer _timer;
        private int _dotCount = 0;
        private readonly int _maxDots;
        private readonly object _lock = new object();

        public ConsoleDotAnimator(int intervalMs = 2000, int maxDots = 3)
        {
            _maxDots = maxDots;
            _timer = new Timer(intervalMs);
            _timer.Elapsed += TimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lock)
            {
                if (_dotCount < _maxDots)
                {
                    Console.Write(".");
                    _dotCount++;
                }
                else
                {
                    // Erase previous dots
                    Console.Write(new string('\b', _maxDots));
                    Console.Write(new string(' ', _maxDots));
                    Console.Write(new string('\b', _maxDots));

                    Console.Write(".");
                    _dotCount = 1;
                }
            }
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();

            // Clean up remaining dots
            lock (_lock)
            {
                Console.WriteLine(); // Move to next line after animation
            }
        }
    }

}
