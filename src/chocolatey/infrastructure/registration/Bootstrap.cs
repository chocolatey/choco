namespace chocolatey.infrastructure.registration
{
    using System;
    using app;
    using log4net;
    using logging;
    using ILog = log4net.ILog;

    /// <summary>
    ///     Application bootstrapping - sets up logging and errors for the app domain
    /// </summary>
    public class Bootstrap
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof (Bootstrap));

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        public static void initialize()
        {
            Log.InitializeWith<Log4NetLog>();
            //initialization code 
            _logger.Debug("XmlConfiguration is now operational");
        }

        /// <summary>
        ///     Startups this instance.
        /// </summary>
        public static void startup()
        {
            AppDomain.CurrentDomain.UnhandledException += DomainUnhandledException;
            _logger.DebugFormat("Performing bootstrapping operations for '{0}'.", ApplicationParameters.Name);
        }

        /// <summary>
        ///     Handles unhandled exception for the application domain.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">
        ///     The <see cref="System.UnhandledExceptionEventArgs" /> instance containing the event data.
        /// </param>
        private static void DomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            string exceptionMessage = string.Empty;
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
        ///     Shutdowns this instance.
        /// </summary>
        public static void shutdown()
        {
            _logger.DebugFormat("Performing shutdown operations for '{0}'.", ApplicationParameters.Name);
        }
    }
}