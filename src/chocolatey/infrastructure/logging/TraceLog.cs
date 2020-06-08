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
    using System.Diagnostics;
    using System.Net;
    using System.Reflection;
    using System.Threading;
    using log4net.Util;

    public class TraceLog : TraceListener
    {
        public TraceLog()
        {
            try
            {
                enable_system_net_logging();
            }
            catch (Exception e)
            {
                this.Log().Warn(ChocolateyLoggers.Verbose, "Unable to set trace logging:{0} {1}".format_with(Environment.NewLine, e.Message));
            }
        }

        public TraceLog(string name)
            : base(name)
        {
            try
            {
                enable_system_net_logging();
            }
            catch (Exception e)
            {
                this.Log().Warn(ChocolateyLoggers.Verbose, "Unable to set trace logging:{0} {1}".format_with(Environment.NewLine, e.Message));
            }
        }

        /// <summary>
        /// Enable logging for network requests and responses
        /// </summary>
        /// <remarks>Based on http://stackoverflow.com/a/27467753/18475 </remarks>
        private void enable_system_net_logging()
        {
            var logging = typeof(WebRequest).Assembly.GetType("System.Net.Logging");
            var isInitialized = logging.GetField("s_LoggingInitialized", BindingFlags.NonPublic | BindingFlags.Static);
            if (isInitialized != null)
            {
                if (!(bool)isInitialized.GetValue(null))
                {
                    //// force initialization
                    HttpWebRequest.Create("http://localhost");
                    Thread waitForInitializationThread = new Thread(() =>
                    {
                        while (!(bool)isInitialized.GetValue(null))
                        {
                            Thread.Sleep(100);
                        }
                    });

                    waitForInitializationThread.Start();
                    waitForInitializationThread.Join();
                }
            }
           
            enable_trace_source("s_WebTraceSource", logging, this); //System.Net
            enable_trace_source("s_HttpListenerTraceSource", logging, this); //System.Net.HttpListener
            enable_trace_source("s_SocketsTraceSource", logging, this); //System.Net.Sockets
            enable_trace_source("s_CacheTraceSource", logging, this);  //System.Net.Cache

            var isEnabled = logging.GetField("s_LoggingEnabled", BindingFlags.NonPublic | BindingFlags.Static);
            if (isEnabled !=null) isEnabled.SetValue(null, true);
        }

        private static void enable_trace_source(string fieldName, Type logging, TraceListener listener)
        {
            var traceSource = (TraceSource)logging.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            if (traceSource != null)
            {
                traceSource.Attributes["tracemode"] = "protocolonly";
                traceSource.Listeners.Add(listener);
                traceSource.Switch.Level = SourceLevels.Information;
            }
        }

        public override void Write(string message)
        {
            // this causes issues with log4net so just log to console for now
            //this.Log().Debug(ChocolateyLoggers.Trace, message);
            Console.Write(message);
        }

        public override void WriteLine(string message)
        {
            // this causes issues with log4net so just log to console for now
            //this.Log().Debug(ChocolateyLoggers.Trace, message);
            Console.WriteLine(message);
        }

        public override void Close()
        {
            base.Close();
        }

        public override void Fail(string message)
        {
            this.Log().Error(ChocolateyLoggers.Trace, message);
        }

        public override void Flush()
        {
        }
    }
}