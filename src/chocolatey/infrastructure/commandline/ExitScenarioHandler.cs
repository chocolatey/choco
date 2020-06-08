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
    using System.Runtime.InteropServices;
    using logging;
    using platforms;

    /// <summary>
    ///   Detect abnormal exit signals and log them
    /// </summary>
    /// <remarks>
    ///   http://geekswithblogs.net/mrnat/archive/2004/09/23/11594.aspx
    ///   http://stackoverflow.com/a/22996552/18475
    /// </remarks>
    public class ExitScenarioHandler
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(SignalControlType sig);

        private static EventHandler _handler;

        private enum SignalControlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        public static void SetHandler()
        {
            if (Platform.get_platform() != PlatformType.Windows) return;

            _handler += Handler;
            SetConsoleCtrlHandler(_handler, true);
        }

        private static bool Handler(SignalControlType signal)
        {
            const string errorMessage = @"Exiting chocolatey abnormally. Please manually clean up anything that 
 was not finished.";
            switch (signal)
            {
                case SignalControlType.CTRL_SHUTDOWN_EVENT:
                    break;
                case SignalControlType.CTRL_C_EVENT:
                case SignalControlType.CTRL_CLOSE_EVENT:
                    "chocolatey".Log().Error(ChocolateyLoggers.Important, errorMessage);
                    "chocolatey".Log().Error("Please do not ever use Control+C or close the window to exit.");
                    break;
                default:
                    "chocolatey".Log().Error(ChocolateyLoggers.Important, errorMessage);
                    break;
            }

            Console.ResetColor();
            Environment.Exit(-1);

            return true;
        }
    }
}