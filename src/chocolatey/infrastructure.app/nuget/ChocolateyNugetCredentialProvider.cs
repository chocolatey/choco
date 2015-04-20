// Copyright © 2011 - Present RealDimensions Software, LLC
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
    using NuGet;
    using configuration;
    using logging;

    // ReSharper disable InconsistentNaming

    public sealed class ChocolateyNugetCredentialProvider : ICredentialProvider
    {
        private readonly ChocolateyConfiguration _config;

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
            
            if (_config.Sources.TrimEnd('/').is_equal_to(uri.OriginalString.TrimEnd('/')))
            {
                if (!string.IsNullOrWhiteSpace(_config.SourceCommand.Username) && !string.IsNullOrWhiteSpace(_config.SourceCommand.Password))
                {
                    this.Log().Debug("Using passed in credentials");

                    return new NetworkCredential(_config.SourceCommand.Username, _config.SourceCommand.Password);
                }
            }

            var source = _config.MachineSources.FirstOrDefault(s =>
                {
                    var sourceUri = s.Key.TrimEnd('/');
                    return sourceUri.is_equal_to(uri.OriginalString.TrimEnd('/')) 
                        && !string.IsNullOrWhiteSpace(s.Username)
                        && !string.IsNullOrWhiteSpace(s.EncryptedPassword);
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
                return CredentialCache.DefaultCredentials;
            }

            string message = credentialType == CredentialType.ProxyCredentials ?
                                 "Please provide proxy credentials:" :
                                 "Please provide credentials for: {0}".format_with(uri.OriginalString);
            this.Log().Info(ChocolateyLoggers.Important, message);

            Console.Write("User name: ");
            string username = Console.ReadLine();
            Console.Write("Password: ");
            var password = Console.ReadLine();

            //todo: set this up as secure
            //using (var securePassword = new SecureString())
            //{
            //    foreach (var letter in password.to_string())
            //    {
            //        securePassword.AppendChar(letter);
            //    }

            var credentials = new NetworkCredential
                {
                    UserName = username,
                    Password = password,
                    //SecurePassword = securePassword
                };
            return credentials;
            // }
        }
    }


    // ReSharper restore InconsistentNaming
}