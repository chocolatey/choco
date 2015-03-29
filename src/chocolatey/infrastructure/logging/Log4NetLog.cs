// Copyright © 2011 - Present RealDimensions Software, LLC
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

using log4net.Config;

[assembly: XmlConfigurator(Watch = true)]

namespace chocolatey.infrastructure.logging
{
    using System;
    using log4net;

    // ReSharper disable InconsistentNaming

    /// <summary>
    ///   Log4net logger implementing special ILog class
    /// </summary>
    public sealed class Log4NetLog : ILog, ILog<Log4NetLog>
    {
        private log4net.ILog _logger;

        public void InitializeFor(string loggerName)
        {
            _logger = LogManager.GetLogger(loggerName);
        }

        public void Debug(string message, params object[] formatting)
        {
            if (_logger.IsDebugEnabled) _logger.DebugFormat(decorate_message_with_audit_information(message), formatting);
        }

        public void Debug(Func<string> message)
        {
            if (_logger.IsDebugEnabled) _logger.DebugFormat(decorate_message_with_audit_information(message.Invoke()).escape_curly_braces());
        }

        public void Info(string message, params object[] formatting)
        {
            if (_logger.IsInfoEnabled) _logger.InfoFormat(decorate_message_with_audit_information(message), formatting);
        }

        public void Info(Func<string> message)
        {
            if (_logger.IsInfoEnabled) _logger.InfoFormat(decorate_message_with_audit_information(message.Invoke()).escape_curly_braces());
        }

        public void Warn(string message, params object[] formatting)
        {
            if (_logger.IsWarnEnabled) _logger.WarnFormat(decorate_message_with_audit_information(message), formatting);
        }

        public void Warn(Func<string> message)
        {
            if (_logger.IsWarnEnabled) _logger.WarnFormat(decorate_message_with_audit_information(message.Invoke()).escape_curly_braces());
        }

        public void Error(string message, params object[] formatting)
        {
            // don't need to check for enabled at this level
            _logger.ErrorFormat(decorate_message_with_audit_information(message), formatting);
        }

        public void Error(Func<string> message)
        {
            // don't need to check for enabled at this level
            _logger.ErrorFormat(decorate_message_with_audit_information(message.Invoke()).escape_curly_braces());
        }

        public void Fatal(string message, params object[] formatting)
        {
            // don't need to check for enabled at this level
            _logger.FatalFormat(decorate_message_with_audit_information(message), formatting);
        }

        public void Fatal(Func<string> message)
        {
            // don't need to check for enabled at this level
            _logger.FatalFormat(decorate_message_with_audit_information(message.Invoke()).escape_curly_braces());
        }

        public string decorate_message_with_audit_information(string message)
        {
            return message;
        }
    }

    // ReSharper restore InconsistentNaming
}