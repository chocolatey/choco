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

    public sealed class AggregateLog : ILog, ILog<AggregateLog>
    {
        public IEnumerable<ILog> Loggers { get; private set; }

        public AggregateLog()
        {
            Loggers = new List<ILog>();
        }

        public AggregateLog(IEnumerable<ILog> loggers)
        {
            Loggers = loggers;
        }

        public void InitializeFor(string loggerName)
        {
            foreach (var logger in Loggers.or_empty_list_if_null())
            {
                logger.InitializeFor(loggerName);
            }
        }

        public void Debug(string message, params object[] formatting)
        {
            foreach (var logger in Loggers.or_empty_list_if_null())
            {
                logger.Debug(message, formatting);
            }
        }

        public void Debug(Func<string> message)
        {
            foreach (var logger in Loggers.or_empty_list_if_null())
            {
                logger.Debug(message);
            }
        }

        public void Info(string message, params object[] formatting)
        {
            foreach (var logger in Loggers.or_empty_list_if_null())
            {
                logger.Info(message, formatting);
            }
        }

        public void Info(Func<string> message)
        {
            foreach (var logger in Loggers.or_empty_list_if_null())
            {
                logger.Info(message);
            }
        }

        public void Warn(string message, params object[] formatting)
        {
            foreach (var logger in Loggers.or_empty_list_if_null())
            {
                logger.Warn(message, formatting);
            }
        }

        public void Warn(Func<string> message)
        {
            foreach (var logger in Loggers.or_empty_list_if_null())
            {
                logger.Warn(message);
            }
        }

        public void Error(string message, params object[] formatting)
        {
            foreach (var logger in Loggers.or_empty_list_if_null())
            {
                logger.Error(message, formatting);
            }
        }

        public void Error(Func<string> message)
        {
            foreach (var logger in Loggers.or_empty_list_if_null())
            {
                logger.Error(message);
            }
        }

        public void Fatal(string message, params object[] formatting)
        {
            foreach (var logger in Loggers.or_empty_list_if_null())
            {
                logger.Fatal(message, formatting);
            }
        }

        public void Fatal(Func<string> message)
        {
            foreach (var logger in Loggers.or_empty_list_if_null())
            {
                logger.Fatal(message);
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
