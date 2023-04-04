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

using log4net.Config;

[assembly: XmlConfigurator(Watch = true)]

namespace chocolatey.infrastructure.logging
{
    using System;
    using System.Runtime;
    using log4net;
    using log4net.Core;

    /// <summary>
    ///   Log4net logger implementing special ILog class
    /// </summary>
    public sealed class Log4NetLog : ILog, ILog<Log4NetLog>
    {
        private log4net.ILog _logger;
        // ignore Log4NetLog in the call stack
        private static readonly Type _declaringType = typeof(Log4NetLog);

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void InitializeFor(string loggerName)
        {
            _logger = LogManager.GetLogger(loggerName);
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void Debug(string message, params object[] formatting)
        {
            if (_logger.IsDebugEnabled) Log(Level.Debug, DecorateMessageWithAuditInformation(message), formatting);
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void Debug(Func<string> message)
        {
            if (_logger.IsDebugEnabled) Log(Level.Debug, DecorateMessageWithAuditInformation(message.Invoke()).EscapeCurlyBraces());
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void Info(string message, params object[] formatting)
        {
            if (_logger.IsInfoEnabled) Log(Level.Info, DecorateMessageWithAuditInformation(message), formatting);
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void Info(Func<string> message)
        {
            if (_logger.IsInfoEnabled) Log(Level.Info, DecorateMessageWithAuditInformation(message.Invoke()).EscapeCurlyBraces());
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void Warn(string message, params object[] formatting)
        {
            if (_logger.IsWarnEnabled) Log(Level.Warn, DecorateMessageWithAuditInformation(message), formatting);
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void Warn(Func<string> message)
        {
            if (_logger.IsWarnEnabled) Log(Level.Warn, DecorateMessageWithAuditInformation(message.Invoke()).EscapeCurlyBraces());
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void Error(string message, params object[] formatting)
        {
            // don't need to check for enabled at this level
            Log(Level.Error, DecorateMessageWithAuditInformation(message), formatting);
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void Error(Func<string> message)
        {
            // don't need to check for enabled at this level
            Log(Level.Error, DecorateMessageWithAuditInformation(message.Invoke()).EscapeCurlyBraces());
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void Fatal(string message, params object[] formatting)
        {
            // don't need to check for enabled at this level
            Log(Level.Fatal, DecorateMessageWithAuditInformation(message), formatting);
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void Fatal(Func<string> message)
        {
            // don't need to check for enabled at this level
            Log(Level.Fatal, DecorateMessageWithAuditInformation(message.Invoke()).EscapeCurlyBraces());
        }

        public string DecorateMessageWithAuditInformation(string message)
        {
            return message;
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        private void Log(Level level, string message, params object[] formatting)
        {
            // SystemStringFormat is used to evaluate the message as late as possible. A filter may discard this message.
            _logger.Logger.Log(_declaringType, level, message.FormatWith(formatting), null);
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public string decorate_message_with_audit_information(string message)
            => DecorateMessageWithAuditInformation(message);
#pragma warning restore IDE1006
    }
}
