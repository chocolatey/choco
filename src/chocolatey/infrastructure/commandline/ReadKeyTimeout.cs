// Copyright © 2017 - 2018 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
// You may obtain a copy of the License at
// 
// 	http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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