﻿// Copyright © 2017 - 2025 Chocolatey Software, Inc
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
using chocolatey.infrastructure.app;
using log4net;
using chocolatey.infrastructure.logging;
using ILog = log4net.ILog;

namespace chocolatey.infrastructure.registration
{
    /// <summary>
    ///   Application bootstrapping - sets up logging and errors for the app domain
    /// </summary>
    public sealed class Bootstrap
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Bootstrap));

        /// <summary>
        ///   Initializes this instance.
        /// </summary>
        public static void Initialize()
        {
            Log.InitializeWith<Log4NetLog>();
            _logger.Debug("XmlConfiguration is now operational");
        }

        /// <summary>
        ///   Startups this instance.
        /// </summary>
        public static void Startup()
        {
            AppDomain.CurrentDomain.UnhandledException += DomainUnhandledException;
        }

        /// <summary>
        ///   Handles unhandled exception for the application domain.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">
        ///   The <see cref="System.UnhandledExceptionEventArgs" /> instance containing the event data.
        /// </param>
// ReSharper disable InconsistentNaming
        private static void DomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        // ReSharper restore InconsistentNaming
        {
            var ex = e.ExceptionObject as Exception;
            var exceptionMessage = string.Empty;
            if (ex != null)
            {
                exceptionMessage = ex.ToString();
            }
            _logger.ErrorFormat("{0} had an error on {1} (with user {2}):{3}{4}",
                                ApplicationParameters.Name,
                                Environment.MachineName,
                                Environment.UserName,
                                Environment.NewLine,
                                exceptionMessage
                );
        }

        /// <summary>
        ///   Shutdowns this instance.
        /// </summary>
        public static void Shutdown()
        {
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void initialize()
            => Initialize();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void startup()
            => Startup();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void shutdown()
            => Shutdown();
#pragma warning restore IDE0022, IDE1006
    }
}
