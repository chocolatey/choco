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

namespace chocolatey.tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using chocolatey.infrastructure.logging;
    using Moq;

    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    public class MockLogger : Mock<ILog>, ILog, ILog<MockLogger>
    {
        public MockLogger()
        {
            LoggerNames = new HashSet<string>();
        }

        public void Reset()
        {
            Messages.Clear();
            this.ResetCalls();
            LogMessagesToConsole = false;
            LoggerNames.Clear();
        }

        public bool ContainsMessage(string expectedMessage)
        {
            return ContainsMessageCount(expectedMessage) != 0;
        }

        public bool ContainsMessage(string expectedMessage, LogLevel level)
        {
            return ContainsMessageCount(expectedMessage, level) != 0;
        }

        public int ContainsMessageCount(string expectedMessage)
        {
            int messageCount = 0;
            foreach (var messageLevel in Messages)
            {
                foreach (var message in messageLevel.Value.OrEmpty())
                {
                    if (message.Contains(expectedMessage)) messageCount++;
                }
            }

            return messageCount;
        }

        public int ContainsMessageCount(string expectedMessage, LogLevel level)
        {
            int messageCount = 0;
            foreach (var message in MessagesFor(level).OrEmpty())
            {
                if (message.Contains(expectedMessage)) messageCount++;
            }

            return messageCount;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to log messages to console.
        /// This is purely used in debugging purposes when it is not clear why a spec is failing.
        /// This should not have any usages in specs in committed code.
        /// </summary>
        /// <value>
        /// <c>true</c> if logging messages to console; otherwise, <c>false</c>.
        /// </value>
        public bool LogMessagesToConsole { get; set; }

        private readonly Lazy<ConcurrentDictionary<string, IList<string>>> _messages = new Lazy<ConcurrentDictionary<string, IList<string>>>();

        public HashSet<string> LoggerNames { get; private set; }

        public ConcurrentDictionary<string, IList<string>> Messages
        {
            get { return _messages.Value; }
        }

        public IList<string> MessagesFor(LogLevel logLevel)
        {
            return _messages.Value.GetOrAdd(logLevel.ToString(), new List<string>());
        }

        public void InitializeFor(string loggerName)
        {
            LoggerNames.Add(loggerName);
        }

        public void LogMessage(LogLevel logLevel, string message)
        {
            var list = _messages.Value.GetOrAdd(logLevel.ToString(), new List<string>());
            list.Add(message);
            if (LogMessagesToConsole)
            {
                Console.WriteLine("[{0}] {1}".FormatWith(logLevel.ToStringSafe(), message));
            }
        }

        public void Debug(string message, params object[] formatting)
        {
            Object.Debug(message.FormatWith(formatting));
            LogMessage(LogLevel.Debug, message.FormatWith(formatting));
        }

        public void Debug(Func<string> message)
        {
            Object.Debug(message());
            LogMessage(LogLevel.Debug, message());
        }

        public void Info(string message, params object[] formatting)
        {
            Object.Info(message.FormatWith(formatting));
            LogMessage(LogLevel.Info, message.FormatWith(formatting));
        }

        public void Info(Func<string> message)
        {
            Object.Info(message());
            LogMessage(LogLevel.Info, message());
        }

        public void Warn(string message, params object[] formatting)
        {
            Object.Warn(message.FormatWith(formatting));
            LogMessage(LogLevel.Warn, message.FormatWith(formatting));
        }

        public void Warn(Func<string> message)
        {
            Object.Warn(message());
            LogMessage(LogLevel.Warn, message());
        }

        public void Error(string message, params object[] formatting)
        {
            Object.Error(message.FormatWith(formatting));
            LogMessage(LogLevel.Error, message.FormatWith(formatting));
        }

        public void Error(Func<string> message)
        {
            Object.Error(message());
            LogMessage(LogLevel.Error, message());
        }

        public void Fatal(string message, params object[] formatting)
        {
            Object.Fatal(message.FormatWith(formatting));
            LogMessage(LogLevel.Fatal, message.FormatWith(formatting));
        }

        public void Fatal(Func<string> message)
        {
            Object.Fatal(message());
            LogMessage(LogLevel.Fatal, message());
        }
    }
}
