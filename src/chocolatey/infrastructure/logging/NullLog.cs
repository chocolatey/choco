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

namespace chocolatey.infrastructure.logging
{
    using System;

    // ReSharper disable InconsistentNaming

    /// <summary>
    ///   The default logger until one is set.
    /// </summary>
    public sealed class NullLog : ILog, ILog<NullLog>
    {
        public void InitializeFor(string loggerName)
        {
        }

        public void Debug(string message, params object[] formatting)
        {
        }

        public void Debug(Func<string> message)
        {
        }

        public void Info(string message, params object[] formatting)
        {
        }

        public void Info(Func<string> message)
        {
        }

        public void Warn(string message, params object[] formatting)
        {
        }

        public void Warn(Func<string> message)
        {
        }

        public void Error(string message, params object[] formatting)
        {
        }

        public void Error(Func<string> message)
        {
        }

        public void Fatal(string message, params object[] formatting)
        {
        }

        public void Fatal(Func<string> message)
        {
        }
    }

    // ReSharper restore InconsistentNaming
}
