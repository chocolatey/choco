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
    using configuration;
    using logging;
    using NuGet.Credentials;
    using System.Threading.Tasks;
    using NuGet.Configuration;
    using System.Threading;

    // ReSharper disable InconsistentNaming

    public sealed class ChocolateyNugetCredentialProvider : ICredentialProvider
    {
        private readonly ChocolateyConfiguration _config;

        private const string INVALID_URL = "http://somewhere123zzaafasd.invalid";

        /// <summary>
        /// Unique identifier of this credential provider
        /// </summary>
        public string Id { get; }

        public ChocolateyNugetCredentialProvider(ChocolateyConfiguration config)
        {
            _config = config;
            Id = $"{nameof(ChocolateyNugetCredentialProvider)}_{Guid.NewGuid()}";
        }

        public Task<CredentialResponse> GetAsync(Uri uri, IWebProxy proxy, CredentialRequestType credentialType, string message, bool isRetry, bool nonInteractive, CancellationToken cancellationToken)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (isRetry)
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

                    return Task.FromResult(new CredentialResponse(new NetworkCredential(_config.SourceCommand.Username, _config.SourceCommand.Password)));
                }
            }

            // credentials were not explicit
            // discover based on closest match in sources
            var candidateSources = _config.MachineSources.Where(
                s =>
                {
                    var sourceUrl = s.Key.TrimEnd('/');

                    try
                    {
                        var sourceUri = new Uri(sourceUrl);
                        return sourceUri.Host.is_equal_to(uri.Host)
                            && !string.IsNullOrWhiteSpace(s.Username)
                            && !string.IsNullOrWhiteSpace(s.EncryptedPassword);
                    }
                    catch (Exception)
                    {
                        this.Log().Error("Source '{0}' is not a valid Uri".format_with(sourceUrl));
                    }

                    return false;
                }).ToList();

            MachineSourceConfiguration source = null;


            if (candidateSources.Count == 1)
            {
                // only one match, use it
                source = candidateSources.FirstOrDefault();
            }
            else if (candidateSources.Count > 1)
            {
                // find the source that is the closest match
                foreach (var candidateSource in candidateSources.or_empty_list_if_null())
                {
                    var candidateRegEx = new Regex(Regex.Escape(candidateSource.Key.TrimEnd('/')),RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                    if (candidateRegEx.IsMatch(uri.OriginalString.TrimEnd('/')))
                    {
                        this.Log().Debug("Source selected will be '{0}'".format_with(candidateSource.Key.TrimEnd('/')));
                        source = candidateSource;
                        break;
                    }
                }

                if (source == null && !isRetry)
                {
                    // use the first source. If it fails, fall back to grabbing credentials from the user
                    var candidateSource = candidateSources.First();
                    this.Log().Debug("Evaluated {0} candidate sources but was unable to find a match, using {1}".format_with(candidateSources.Count, candidateSource.Key.TrimEnd('/')));
                    source = candidateSource;
                }
            }

            if (source == null)
            {
                this.Log().Debug("Asking user for credentials for '{0}'".format_with(uri.OriginalString));
                return Task.FromResult(new CredentialResponse(get_credentials_from_user(uri, proxy, credentialType)));
            }
            else
            {
                this.Log().Debug("Using saved credentials");
            }

            return Task.FromResult(new CredentialResponse(new NetworkCredential(source.Username, NugetEncryptionUtility.DecryptString(source.EncryptedPassword))));
        }


        public ICredentials get_credentials_from_user(Uri uri, IWebProxy proxy, CredentialRequestType credentialType)
        {
            if (!_config.Information.IsInteractive)
            {
                // https://blogs.msdn.microsoft.com/buckh/2004/07/28/authentication-in-web-services-with-httpwebrequest/
                //return credentialType == CredentialType.ProxyCredentials ? CredentialCache.DefaultCredentials : CredentialCache.DefaultNetworkCredentials;
                return CredentialCache.DefaultCredentials;
            }

            string message = credentialType == CredentialRequestType.Proxy ?
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
                return CredentialCache.DefaultNetworkCredentials;
            }

            var credentials = new NetworkCredential
                {
                    UserName = username,
                    Password = password
                };

            return credentials;
        }
    }

    // ReSharper restore InconsistentNaming
}
