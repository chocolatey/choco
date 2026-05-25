// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using System;
using System.Threading;

namespace chocolatey.infrastructure.commandline
{
    /// <summary>
    ///   Because sometimes you to timeout a readline instead of blocking infinitely.
    /// </summary>
    /// <remarks>
    ///   Based on http://stackoverflow.com/a/18342182/18475
    /// </remarks>
    public class ReadLineTimeout : IDisposable
    {
        private readonly AutoResetEvent _backgroundResponseReset;
        private readonly AutoResetEvent _foregroundResponseReset;
        private string _input;
        private readonly Thread _responseThread;

        private bool _isDisposing;

        private ReadLineTimeout()
        {
            _backgroundResponseReset = new AutoResetEvent(false);
            _foregroundResponseReset = new AutoResetEvent(false);
            _responseThread = new Thread(ConsoleRead)
            {
                IsBackground = true
            };
            _responseThread.Start();
        }

        private void ConsoleRead()
        {
            while (true)
            {
                try
                {
                    _backgroundResponseReset.WaitOne();
                    _input = Console.ReadLine();
                    _foregroundResponseReset.Set();
                }
                catch (Exception)
                {
                    // Either there is no interactive console (input redirected /
                    // headless), or the caller already timed out and disposed the
                    // reset events. Swallow on this background thread so we never
                    // crash the process; the foreground caller returns its default.
                    return;
                }
            }
        }

        public static string Read(int timeoutMilliseconds)
        {
            using (var readLine = new ReadLineTimeout())
            {
                readLine._backgroundResponseReset.Set();

                return readLine._foregroundResponseReset.WaitOne(timeoutMilliseconds) ?
                           readLine._input
                           : null;
            }
        }

        public void Dispose()
        {
            if (_isDisposing)
            {
                return;
            }

            _isDisposing = true;
            // Thread.Abort is unsupported on .NET; the response thread is a
            // background thread, so it is reclaimed at process exit. Its body is
            // exception-guarded so it won't fault when the resets are disposed.
            _backgroundResponseReset.Close();
            _backgroundResponseReset.Dispose();
            _foregroundResponseReset.Close();
            _foregroundResponseReset.Dispose();
        }


#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string read(int timeoutMilliseconds)
            => Read(timeoutMilliseconds);
#pragma warning restore IDE0022, IDE1006
    }
}
