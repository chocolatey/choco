namespace chocolatey.infrastructure.commandline
{
    using System;
    using System.Threading;

    /// <summary>
    ///   Because sometimes you to timeout a readkey instead of blocking infinitely.
    /// </summary>
    /// <remarks>
    ///   Based on http://stackoverflow.com/a/18342182/18475
    /// </remarks>
    public class ReadKeyTimeout : IDisposable
    {
        private readonly AutoResetEvent _backgroundResponseReset;
        private readonly AutoResetEvent _foregroundResponseReset;
        private ConsoleKeyInfo _input;
        private readonly Thread _responseThread;

        private bool _isDisposing;

        private ReadKeyTimeout()
        {
            _backgroundResponseReset = new AutoResetEvent(false);
            _foregroundResponseReset = new AutoResetEvent(false);
            _responseThread = new Thread(console_read_key)
            {
                IsBackground = true
            };
            _responseThread.Start();
        }

        private void console_read_key()
        {
            while (true)
            {
                _backgroundResponseReset.WaitOne();
                _input = Console.ReadKey(intercept:true);
                _foregroundResponseReset.Set();
            }
        }

        public static ConsoleKeyInfo read_key(int timeoutMilliseconds)
        {
            using (var readLine = new ReadKeyTimeout())
            {
                readLine._backgroundResponseReset.Set();

                return readLine._foregroundResponseReset.WaitOne(timeoutMilliseconds) ?
                           readLine._input
                           : new ConsoleKeyInfo('\0',ConsoleKey.Enter,false,false,false);
            }
        }

        public void Dispose()
        {
            if (_isDisposing) return;

            _isDisposing = true;
            _responseThread.Abort();
            _backgroundResponseReset.Close();
            _backgroundResponseReset.Dispose();
            _foregroundResponseReset.Close();
            _foregroundResponseReset.Dispose();
        }

    }
}