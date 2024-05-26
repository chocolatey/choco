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
using chocolatey.infrastructure.configuration;
using chocolatey.infrastructure.logging;

namespace chocolatey.infrastructure.tolerance
{
    /// <summary>
    /// Provides methods that are able to tolerate faults and recover
    /// </summary>
    public class FaultTolerance
    {
        private static bool InDebugMode()
        {
            var debugging = false;
            try
            {
                debugging = Config.GetConfigurationSettings().Debug;
            }
            catch
            {
                //move on - debugging is false
            }

            return debugging;
        }

        /// <summary>
        /// Tries an action the specified number of tries, warning on each failure and raises error on the last attempt.
        /// </summary>
        /// <param name="numberOfTries">The number of tries.</param>
        /// <param name="action">The action.</param>
        /// <param name="waitDurationMilliseconds">The wait duration in milliseconds.</param>
        /// <param name="increaseRetryByMilliseconds">The time for each try to increase the wait duration by in milliseconds.</param>
        /// <param name="isSilent">Log messages?</param>
        public static void Retry(int numberOfTries, Action action, int waitDurationMilliseconds = 100, int increaseRetryByMilliseconds = 0, bool isSilent = false)
        {
            if (action == null)
            {
                return;
            }

            var success = Retry(
                numberOfTries,
                () =>
                    {
                        action.Invoke();
                        return true;
                    },
                waitDurationMilliseconds,
                increaseRetryByMilliseconds,
                isSilent);
        }

        /// <summary>
        /// Tries a function the specified number of tries, warning on each failure and raises error on the last attempt.
        /// </summary>
        /// <typeparam name="T">The type of the return value from the function.</typeparam>
        /// <param name="numberOfTries">The number of tries.</param>
        /// <param name="function">The function.</param>
        /// <param name="waitDurationMilliseconds">The wait duration in milliseconds.</param>
        /// <param name="increaseRetryByMilliseconds">The time for each try to increase the wait duration by in milliseconds.</param>
        /// <returns>The return value from the function</returns>
        /// <exception cref="System.ApplicationException">You must specify a number of retries greater than zero.</exception>
        /// <param name="isSilent">Log messages?</param>
        public static T Retry<T>(int numberOfTries, Func<T> function, int waitDurationMilliseconds = 100, int increaseRetryByMilliseconds = 0, bool isSilent = false)
        {
            if (function == null)
            {
                return default(T);
            }

            if (numberOfTries == 0)
            {
                throw new ApplicationException("You must specify a number of tries greater than zero.");
            }

            var returnValue = default(T);

            var debugging = InDebugMode();
            var logLocation = ChocolateyLoggers.Normal;
            if (isSilent)
            {
                logLocation = ChocolateyLoggers.LogFileOnly;
            }

            for (var i = 1; i <= numberOfTries; i++)
            {
                try
                {
                    returnValue = function.Invoke();
                    break;
                }
                catch (Exception ex)
                {
                    if (i == numberOfTries)
                    {
                        "chocolatey".Log().Error(logLocation, "Maximum tries of {0} reached. Throwing error.".FormatWith(numberOfTries));
                        throw;
                    }

                    var retryWait = waitDurationMilliseconds + (i * increaseRetryByMilliseconds);

                    var exceptionMessage = debugging ? ex.ToString() : ex.Message;

                    "chocolatey".Log().Warn(logLocation, "This is try {3}/{4}. Retrying after {2} milliseconds.{0} Error converted to warning:{0} {1}".FormatWith(
                        Environment.NewLine,
                        exceptionMessage,
                        retryWait,
                        i,
                        numberOfTries));
                    Thread.Sleep(retryWait);
                }
            }

            return returnValue;
        }

        /// <summary>
        /// Wraps an action with a try/catch, logging an error when it fails.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="throwError">if set to <c>true</c> [throw error].</param>
        /// <param name="logWarningInsteadOfError">if set to <c>true</c> log as warning instead of error.</param>
        /// <param name="logDebugInsteadOfError">Log to debug</param>
        /// <param name="isSilent">Log messages?</param>
        public static void TryCatchWithLoggingException(Action action, string errorMessage, bool throwError = false, bool logWarningInsteadOfError = false, bool logDebugInsteadOfError = false, bool isSilent = false)
        {
            if (action == null)
            {
                return;
            }

            var success = TryCatchWithLoggingException(
                () =>
                    {
                        action.Invoke();
                        return true;
                    },
                errorMessage,
                throwError,
                logWarningInsteadOfError,
                logDebugInsteadOfError,
                isSilent
                );
        }

        /// <summary>
        /// Wraps a function with a try/catch, logging an error when it fails.
        /// </summary>
        /// <typeparam name="T">The type of the return value from the function.</typeparam>
        /// <param name="function">The function.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="throwError">if set to <c>true</c> [throw error].</param>
        /// <param name="logWarningInsteadOfError">if set to <c>true</c> log as warning instead of error.</param>
        /// <param name="logDebugInsteadOfError">Log to debug</param>
        /// <param name="isSilent">Log messages?</param>
        /// <returns>The return value from the function</returns>
        public static T TryCatchWithLoggingException<T>(Func<T> function, string errorMessage, bool throwError = false, bool logWarningInsteadOfError = false, bool logDebugInsteadOfError = false, bool isSilent = false)
        {
            if (function == null)
            {
                return default(T);
            }

            var returnValue = default(T);

            var logLocation = ChocolateyLoggers.Normal;
            if (isSilent)
            {
                logLocation = ChocolateyLoggers.LogFileOnly;
            }

            try
            {
                returnValue = function.Invoke();
            }
            catch (Exception ex)
            {
                var exceptionMessage = InDebugMode() ? ex.ToString() : ex.Message;

                if (logDebugInsteadOfError)
                {
                    "chocolatey".Log().Debug(logLocation, "{0}:{1} {2}".FormatWith(errorMessage, Environment.NewLine, exceptionMessage));
                }
                else if (logWarningInsteadOfError)
                {
                    "chocolatey".Log().Warn(logLocation, "{0}:{1} {2}".FormatWith(errorMessage, Environment.NewLine, exceptionMessage));
                }
                else
                {
                    "chocolatey".Log().Error(logLocation, "{0}:{1} {2}".FormatWith(errorMessage, Environment.NewLine, exceptionMessage));
                }

                if (throwError)
                {
                    throw;
                }
            }

            return returnValue;
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void retry(int numberOfTries, Action action, int waitDurationMilliseconds = 100, int increaseRetryByMilliseconds = 0, bool isSilent = false)
            => Retry(numberOfTries, action, waitDurationMilliseconds, increaseRetryByMilliseconds, isSilent);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static T retry<T>(int numberOfTries, Func<T> function, int waitDurationMilliseconds = 100, int increaseRetryByMilliseconds = 0, bool isSilent = false)
            => Retry(numberOfTries, function, waitDurationMilliseconds, increaseRetryByMilliseconds, isSilent);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void try_catch_with_logging_exception(Action action, string errorMessage, bool throwError = false, bool logWarningInsteadOfError = false, bool logDebugInsteadOfError = false, bool isSilent = false)
            => TryCatchWithLoggingException(action, errorMessage, throwError, logWarningInsteadOfError, logDebugInsteadOfError, isSilent);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static T try_catch_with_logging_exception<T>(Func<T> function, string errorMessage, bool throwError = false, bool logWarningInsteadOfError = false, bool logDebugInsteadOfError = false, bool isSilent = false)
            => TryCatchWithLoggingException(function, errorMessage, throwError, logWarningInsteadOfError, logDebugInsteadOfError, isSilent);
#pragma warning restore IDE0022, IDE1006
    }
}
