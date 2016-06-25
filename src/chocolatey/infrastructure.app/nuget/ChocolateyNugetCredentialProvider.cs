﻿// Copyright © 2011 - Present RealDimensions Software, LLC
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
            try
            {
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

            if (_config.Sources.TrimEnd('/').is_equal_to(uri.OriginalString.TrimEnd('/')) || configSourceUri.Host.is_equal_to(uri.Host))
            {
                if (!string.IsNullOrWhiteSpace(_config.SourceCommand.Username) && !string.IsNullOrWhiteSpace(_config.SourceCommand.Password))
                {
                    this.Log().Debug("Using passed in credentials");

                    return new NetworkCredential(_config.SourceCommand.Username, _config.SourceCommand.Password);
                }
            }

            var source = _config.MachineSources.FirstOrDefault(s =>
                {
                    var sourceUrl = s.Key.TrimEnd('/');

                    var equalAtFullUri = sourceUrl.is_equal_to(uri.OriginalString.TrimEnd('/'))
                       && !string.IsNullOrWhiteSpace(s.Username)
                       && !string.IsNullOrWhiteSpace(s.EncryptedPassword);

                    if (equalAtFullUri) return true;

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
                });

            if (source == null)
            {
                return get_credentials_from_user(uri, proxy, credentialType);
            }
           
            this.Log().Debug("Using saved credentials");

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