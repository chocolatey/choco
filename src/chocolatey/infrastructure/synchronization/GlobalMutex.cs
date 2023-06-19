// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.synchronization
{
    using System;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Threading;
    using logging;
    using platforms;

    /// <summary>
    ///   global mutex used for synchronizing multiple processes based on appguid
    /// </summary>
    /// <remarks>
    ///   Based on http://stackoverflow.com/a/7810107/2279385
    /// </remarks>
    public class GlobalMutex : IDisposable
    {
        private readonly bool _hasHandle = false;
        private const string AppGuid = "bd59231e-97d1-4fc0-a975-80c3fed498b7"; // matches the one in Assembly
        private Mutex _mutex;

        private void InitMutex()
        {
            this.Log().Trace("Initializing global mutex");
            var mutexId = "Global\\{{{0}}}".FormatWith(AppGuid);
            _mutex = new Mutex(initiallyOwned: false, name: mutexId);

            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);
            _mutex.SetAccessControl(securitySettings);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalMutex"/> class.
        /// </summary>
        /// <param name="timeOut">The time out in milliseconds.</param>
        /// <exception cref="System.TimeoutException">Timeout waiting for exclusive access to value.</exception>
        private GlobalMutex(int timeOut)
        {
            InitMutex();
            try
            {
                this.Log().Trace("Waiting on the mutex handle for {0} milliseconds".FormatWith(timeOut));
                _hasHandle = _mutex.WaitOne(timeOut < 0 ? Timeout.Infinite : timeOut, exitContext: false);

                if (_hasHandle == false)
                {
                    throw new TimeoutException("Timeout waiting for exclusive access to value.");
                }
            }
            catch (AbandonedMutexException)
            {
                _hasHandle = true;
            }
        }

        /// <summary>
        /// Enters the Global Mutex
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="timeout">The timeout in milliseconds.</param>
        public static void Enter(Action action, int timeout)
        {

            if (Platform.GetPlatform() == PlatformType.Windows)
            {
                using (new GlobalMutex(timeout))
                {
                    if (action != null) action.Invoke();
                }
            }
            else
            {
                if (action != null) action.Invoke();
            }
        }

        /// <summary>
        /// Enters the Global Mutex
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="func">The function to perform.</param>
        /// <param name="timeout">The timeout in seconds.</param>
        /// <returns></returns>
        public static T Enter<T>(Func<T> func, int timeout)
        {
            var returnValue = default(T);

            if (Platform.GetPlatform() == PlatformType.Windows)
            {
                using (new GlobalMutex(timeout))
                {
                    if (func != null) returnValue = func.Invoke();
                }
            }
            else
            {
                if (func != null) returnValue = func.Invoke();
            }

            return returnValue;
        }

        public void Dispose()
        {
            if (_mutex != null)
            {
                if (_hasHandle)
                {
                    _mutex.ReleaseMutex();
                }
                _mutex.Dispose();
            }
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void enter(Action action, int timeout)
            => Enter(action, timeout);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static T enter<T>(Func<T> func, int timeout)
            => Enter(func, timeout);
#pragma warning restore IDE1006
    }
}
