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

namespace chocolatey.infrastructure.app.nuget
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using commandline;
    using NuGet;
    using configuration;
    using logging;

    // ReSharper disable InconsistentNaming

    public sealed class ChocolateyNugetCredentialProvider : ICredentialProvider
    {
        private readonly ChocolateyConfiguration _config;

        private const string INVALID_URL = "http://somewhere123zzaafasd.invalid";

        public ChocolateyNugetCredentialProvider(ChocolateyConfiguration config)
        {
            _config = config;
        }

        public ICredentials GetCredentials(Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (retrying)
            {
                this.Log().Warn("Invalid credentials specified.");
            }

            var configSourceUri = new Uri(INVALID_URL);

            this.Log().Debug(ChocolateyLoggers.Verbose, "Attempting to gather credentials for '{0}'".format_with(uri.OriginalString));
            try
            {
                // the source to validate against is typically passed in
                var firstSpecifiedSource = _config.Sources.to_string().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault().to_string();
                if (!string.IsNullOrWhiteSpace(firstSpecifiedSource))
                {
                    configSourceUri = new Uri(firstSpecifiedSource);
                }
            }
            catch (Exception ex)
            {
                this.Log().Warn("Cannot determine uri from specified source:{0} {1}".format_with(Environment.NewLine, ex.Message));
            }

            // did the user pass credentials and a source?
            if (_config.Sources.TrimEnd('/').is_equal_to(uri.OriginalString.TrimEnd('/')) || configSourceUri.Host.is_equal_to(uri.Host))
            {
                if (!string.IsNullOrWhiteSpace(_config.SourceCommand.Username) && !string.IsNullOrWhiteSpace(_config.SourceCommand.Password))
                {
                    this.Log().Debug("Using passed in credentials");

                    return new NetworkCredential(_config.SourceCommand.Username, _config.SourceCommand.Password);
                }
            }

            // credentials were not explicit
            // find matching source(s) in sources list
            var trimmedTargetUri = new Uri(uri.AbsoluteUri.TrimEnd('/'));
            MachineSourceConfiguration source = null;

            // If the user has specified --source with a *named* source and not a URL, try to find the matching one
            // with the correct URL for this credential request.
            // Lower case all of the explicitly named sources so that we can use .Contains to compare them.
            var namedExplicitSources = _config.ExplicitSources?.ToLower().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => !Uri.IsWellFormedUriString(s, UriKind.Absolute))
                .ToList();

            if (namedExplicitSources?.Count > 0)
            {
                // Instead of using Uri.Equals(), we're using Uri.Compare() on the HttpRequestUrl components as this allows
                // us to ignore the case of everything.
                source = _config.MachineSources
                    .Where(s => namedExplicitSources.Contains(s.Name.ToLower())
                    && Uri.TryCreate(s.Key.TrimEnd('/'), UriKind.Absolute, out var trimmedSourceUri)
                    && Uri.Compare(trimmedSourceUri, trimmedTargetUri, UriComponents.HttpRequestUrl, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
                    .FirstOrDefault();
            }
            
            if (source is null)
            {
                // Could not find a valid source by name, or the source(s) specified were all URLs.
                // Try to look up the target URL in the saved machine sources to attempt to match credentials.
                //
                // Note: This behaviour remains as removing it would be a breaking change.
                var candidateSources = _config.MachineSources
                    .Where(s => !string.IsNullOrWhiteSpace(s.Username)
                        && !string.IsNullOrWhiteSpace(s.EncryptedPassword)
                        && Uri.TryCreate(s.Key.TrimEnd('/'), UriKind.Absolute, out var trimmedSourceUri)
                        && (Uri.Compare(trimmedSourceUri, trimmedTargetUri, UriComponents.HttpRequestUrl, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0
                            // If the target starts with a machine source, we're in a scenario where NuGet is now trying to download the package.
                            // For whatever reason NuGet sometimes forgets the credentials between discovering the package and downloading the package.
                            || trimmedTargetUri.ToString().StartsWith(trimmedSourceUri.ToString(), StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (candidateSources.Count == 1)
                {
                    // only one match, use it
                    source = candidateSources.First();
                }
                else if (candidateSources.Count > 1 && !retrying)
                {
                    // Use the credentials from the first found source, unless it's a retry (creds already tried and failed)
                    // use the first source. If it fails, fall back to grabbing credentials from the user.
                    var candidateSource = candidateSources.First();
                    this.Log().Debug("Evaluated {0} candidate sources but was unable to find a match, using {1}".format_with(candidateSources.Count, candidateSource.Key.TrimEnd('/')));
                    source = candidateSource;
                }
            }

            if (source is null)
            {
                this.Log().Debug("Asking user for credentials for '{0}'".format_with(uri.OriginalString));
                return get_credentials_from_user(uri, proxy, credentialType);
            }
            else
            {
                this.Log().Debug("Using saved credentials");
            }

            return new NetworkCredential(source.Username, NugetEncryptionUtility.DecryptString(source.EncryptedPassword));
        }

        public ICredentials get_credentials_from_user(Uri uri, IWebProxy proxy, CredentialType credentialType)
        {
            if (!_config.Information.IsInteractive)
            {
                // https://blogs.msdn.microsoft.com/buckh/2004/07/28/authentication-in-web-services-with-httpwebrequest/
                //return credentialType == CredentialType.ProxyCredentials ? CredentialCache.DefaultCredentials : CredentialCache.DefaultNetworkCredentials;
                return CredentialCache.DefaultCredentials;
            }

            string message = credentialType == CredentialType.ProxyCredentials ?
                                 "Please provide proxy credentials:" :
                                 "Please provide credentials for: {0}".format_with(uri.OriginalString);
            this.Log().Info(ChocolateyLoggers.Important, message);

            Console.Write("User name: ");
            string username = Console.ReadLine();
            Console.Write("Password: ");
            var password = InteractivePrompt.get_password(_config.PromptForConfirmation);

            if (string.IsNullOrWhiteSpace(password))
            {
                this.Log().Warn("No password specified, this will probably error.");
                //return CredentialCache.DefaultNetworkCredentials;
            }

            var credentials = new NetworkCredential
                {
                    UserName = username,
                    Password = password,
                    //SecurePassword = password.to_secure_string(),
                };

            return credentials;
        }
    }

    // ReSharper restore InconsistentNaming
}
