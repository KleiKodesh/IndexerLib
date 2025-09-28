
namespace SimplifiedIndexerLib.Helpers
{
    using System;
    using System.Threading;

    public class ConsoleSpinner : IDisposable
    {
        private readonly char[] _sequence = new char[] { '|', '/', '-', '\\' };
        private int _current = 0;
        private readonly Timer _timer;

        public ConsoleSpinner(int interval = 350)
        {
            _timer = new Timer(Update, null, 0, interval);
        }

        private void Update(object state)
        {
            ClearChar();
            Console.Write(_sequence[_current]);
            _current = (_current + 1) % _sequence.Length;
        }

        public void Dispose()
        {
            _timer.Dispose();
            ClearChar();
        }

        void ClearChar()
        {
            Console.Write('\b');
            Console.Write(' ');
            Console.Write('\b');
        }
    }

}
