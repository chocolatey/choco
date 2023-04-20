namespace chocolatey.infrastructure.registration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    public static class HttpsSecurity
    {
        /// <summary>
        /// Resets the <see cref="ServicePointManager.SecurityProtocol"/> and <see cref="ServicePointManager.ServerCertificateValidationCallback"/> for HTTPS connections
        /// in order to ensure that PowerShell scripts we run can't have a persistent effect on these settings across the whole process after they're done running.
        /// </summary>
        public static void Reset()
        {
            // SystemDefault is used because:
            // 1. Most supported operating systems default to / require TLS 1.2 anyway.
            // 2. Explicitly *enabling* any less secure protocols leaves us open to TLS downgrade attacks.
            //    https://en.wikipedia.org/wiki/Downgrade_attack
            // 3. Always *requiring* TLS 1.2 or higher on any OS version may cause issues with some folks' internal
            //    networks which may have older infrastructure that doesn't support it, and they won't have a way
            //    to make Chocolately CLI work with that infrastructure if we're forcing a TLS version they can't
            //    support.
            // 4. Thus, the only sensible solution (since we don't want to add a configuration value for this) is
            //    to take the OS-level setting and use that, because if folks set their system up to use certain
            //    TLS versions, we should probably follow suit, regardless of what their OS version may be.
            //
            // See https://learn.microsoft.com/en-us/dotnet/framework/network-programming/tls for more information
            // and best practices recommendation from the .NET team.
            if (ServicePointManager.SecurityProtocol != SecurityProtocolType.SystemDefault)
            {
                "chocolatey".Log().Warn(
                    "{0} was set to {1}, resetting to SystemDefault.",
                    nameof(ServicePointManager.SecurityProtocol),
                    ServicePointManager.SecurityProtocol);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
            }

            try
            {
                // We reset this to ensure that if a package overrides this callback during its script runs, it doesn't affect the rest of
                // the actions we need to take for the duration of this process (other packages we're installing, etc.) Setting this to
                // null restores the default behaviour of certificate validation instead of whatever custom behaviour has been injected.
                if (ServicePointManager.ServerCertificateValidationCallback != null)
                {
                    "chocolatey".Log().Warn(
                        "{0} was set to '{1}' Removing.",
                        nameof(ServicePointManager.ServerCertificateValidationCallback),
                        ServicePointManager.ServerCertificateValidationCallback);

                    ServicePointManager.ServerCertificateValidationCallback = null;
                }
            }
            catch (Exception ex)
            {
                "chocolatey".Log().Warn("Error resetting ServerCertificateValidationCallback: {0}".FormatWith(ex.Message));
            }
        }
    }
}
