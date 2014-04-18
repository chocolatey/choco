using log4net.Config;

[assembly: XmlConfigurator(Watch = true)]

namespace chocolatey.infrastructure.logging
{
    using System;
    using log4net;

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
            if (_logger.IsDebugEnabled) _logger.Debug(decorate_message_with_audit_information(message.Invoke()));
        }

        public void Info(string message, params object[] formatting)
        {
            if (_logger.IsInfoEnabled) _logger.InfoFormat(decorate_message_with_audit_information(message), formatting);
        }

        public void Info(Func<string> message)
        {
            if (_logger.IsInfoEnabled) _logger.Info(decorate_message_with_audit_information(message.Invoke()));
        }

        public void Warn(string message, params object[] formatting)
        {
            if (_logger.IsWarnEnabled) _logger.WarnFormat(decorate_message_with_audit_information(message), formatting);
        }

        public void Warn(Func<string> message)
        {
            if (_logger.IsWarnEnabled) _logger.Warn(decorate_message_with_audit_information(message.Invoke()));
        }

        public void Error(string message, params object[] formatting)
        {
            // don't need to check for enabled at this level
            _logger.ErrorFormat(decorate_message_with_audit_information(message), formatting);
        }

        public void Error(Func<string> message)
        {
            // don't need to check for enabled at this level
            _logger.Error(decorate_message_with_audit_information(message.Invoke()));
        }

        public void Fatal(string message, params object[] formatting)
        {
            // don't need to check for enabled at this level
            _logger.FatalFormat(decorate_message_with_audit_information(message), formatting);
        }

        public void Fatal(Func<string> message)
        {
            // don't need to check for enabled at this level
            _logger.Fatal(decorate_message_with_audit_information(message.Invoke()));
        }

        public string decorate_message_with_audit_information(string message)
        {
            return message;
        }
    }
}