﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
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
using System.Threading.Tasks;
using chocolatey.infrastructure.logging;

namespace chocolatey.infrastructure.commands
{
    /// <summary>
    /// Execute a method or function
    /// </summary>
    public sealed class Execute
    {
        private readonly TimeSpan _timespan;

        /// <summary>
        ///   The number of seconds to wait for an operation to complete.
        /// </summary>
        /// <param name="timeoutInSeconds">The timeout in seconds.</param>
        /// <returns></returns>
        public static Execute WithTimeout(int timeoutInSeconds)
        {
            return new Execute(TimeSpan.FromSeconds(timeoutInSeconds));
        }

        /// <summary>
        ///   The timespan to wait for an operation to complete.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public static Execute WithTimeout(TimeSpan timeout)
        {
            return new Execute(timeout);
        }

        private Execute(TimeSpan timespan)
        {
            _timespan = timespan;
        }

        /// <summary>
        ///   Runs an operation with a timeout.
        /// </summary>
        /// <typeparam name="T">The type to return</typeparam>
        /// <param name="function">The function to execute.</param>
        /// <param name="timeoutDefaultValue">The timeout default value.</param>
        /// <returns>The results of the function if completes within timespan, otherwise returns the default value.</returns>
        public T Command<T>(Func<T> function, T timeoutDefaultValue)
        {
            if (function == null)
            {
                return timeoutDefaultValue;
            }

            var cancelToken = new CancellationTokenSource();
            cancelToken.Token.ThrowIfCancellationRequested();
            var task = Task<T>.Factory.StartNew(function, cancelToken.Token); //,TaskCreationOptions.LongRunning| TaskCreationOptions.AttachedToParent);

            if (_timespan.TotalSeconds < 1d)
            {
                // 0 means infinite
                task.Wait();
            }
            else
            {
                task.Wait(_timespan);
            }

            if (task.IsCompleted)
            {
                return task.Result;
            }

            cancelToken.Cancel();
            this.Log().Warn(ChocolateyLoggers.Important, () => @"Chocolatey timed out waiting for the command to finish. The timeout
 specified (or the default value) was '{0}' seconds. Perhaps try a
 higher `--execution-timeout`? See `choco -h` for details.".FormatWith(_timespan.TotalSeconds));

            return timeoutDefaultValue;

            //T result = timeoutDefaultValue;
            //var thread = new Thread(() => result = function());
            //thread.Start();

            //bool completed = thread.Join((int)TimeSpan.FromSeconds(timeoutInSeconds).TotalMilliseconds);
            //if (!completed) thread.Abort();

            //return result;
        }

        /// <summary>
        ///   Calls a method with a timeout.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>True if it finishes executing, false otherwise.</returns>
        public bool Command(Action action)
        {
            if (action == null)
            {
                return false;
            }

            var completed = false;

            var cancelToken = new CancellationTokenSource();
            cancelToken.Token.ThrowIfCancellationRequested();
            var task = Task.Factory.StartNew(action, cancelToken.Token);
            if (_timespan.TotalSeconds < 1d)
            {
                // 0 means infinite
                task.Wait();
            }
            else
            {
                task.Wait(_timespan);
            }

            completed = task.IsCompleted;

            if (!completed)
            {
                cancelToken.Cancel();
                this.Log().Warn(ChocolateyLoggers.Important, () => @"Chocolatey timed out waiting for the command to finish. The timeout
 specified (or the default value) was '{0}' seconds. Perhaps try a
 higher `--execution-timeout`? See `choco -h` for details.".FormatWith(_timespan.TotalSeconds));

            }

            return completed;
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static Execute with_timeout(int timeoutInSeconds)
            => WithTimeout(timeoutInSeconds);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static Execute with_timeout(TimeSpan timeout)
            => WithTimeout(timeout);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public T command<T>(Func<T> function, T timeoutDefaultValue)
            => Command(function, timeoutDefaultValue);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public bool command(Action action)
            => Command(action);
#pragma warning restore IDE0022, IDE1006
    }
}
