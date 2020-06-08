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
    using System.Collections.Generic;

    // ReSharper disable InconsistentNaming

    public sealed class LogSinkLog : ILog, ILog<LogSinkLog>
    {
        private readonly IList<LogMessage> _messages = new List<LogMessage>();

        public ICollection<LogMessage> Messages { get { return _messages; } }

        public void InitializeFor(string loggerName)
        {
            //noop
        }

        public void Debug(string message, params object[] formatting)
        {
            _messages.Add(new LogMessage(LogLevelType.Debug, message.format_with(formatting)));
        }

        public void Debug(Func<string> message)
        {
            _messages.Add(new LogMessage(LogLevelType.Debug, message()));
        }

        public void Info(string message, params object[] formatting)
        {
            _messages.Add(new LogMessage(LogLevelType.Information, message.format_with(formatting)));
        }

        public void Info(Func<string> message)
        {
            _messages.Add(new LogMessage(LogLevelType.Information, message()));
        }

        public void Warn(string message, params object[] formatting)
        {
            _messages.Add(new LogMessage(LogLevelType.Warning, message.format_with(formatting)));
        }

        public void Warn(Func<string> message)
        {
            _messages.Add(new LogMessage(LogLevelType.Warning, message()));
        }

        public void Error(string message, params object[] formatting)
        {
            _messages.Add(new LogMessage(LogLevelType.Error, message.format_with(formatting)));
        }

        public void Error(Func<string> message)
        {
            _messages.Add(new LogMessage(LogLevelType.Error, message()));
        }

        public void Fatal(string message, params object[] formatting)
        {
            _messages.Add(new LogMessage(LogLevelType.Fatal, message.format_with(formatting)));
        }

        public void Fatal(Func<string> message)
        {
            _messages.Add(new LogMessage(LogLevelType.Fatal, message()));
        }
    }

    // ReSharper restore InconsistentNaming
}
