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

namespace chocolatey.infrastructure.app.nuget
{
    using configuration;
    using NuGet;
    using System;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;

    class ChocolateyClientCertificateProvider : IClientCertificateProvider
    {
        ChocolateyConfiguration _configuration;

        public ChocolateyClientCertificateProvider(ChocolateyConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");
            _configuration = configuration;
        }

        public X509Certificate GetCertificate(Uri uri)
        {
            if (uri.OriginalString.StartsWith(_configuration.Sources.TrimEnd('/').ToLower(),StringComparison.InvariantCultureIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(_configuration.SourceCommand.Certificate))
                {
                    this.Log().Debug("Using passed in certificate");

                    return new X509Certificate2(_configuration.SourceCommand.Certificate, _configuration.SourceCommand.CertificatePassword);
                }
            }

            return _configuration.MachineSources.Where(s =>
            {
                var sourceUri = s.Key.TrimEnd('/').ToLower();
                return uri.OriginalString.ToLower().StartsWith(sourceUri)
                    && !string.IsNullOrWhiteSpace(s.Certificate);
            })
            .Select(s =>
            {
                this.Log().Debug("Using machine source certificate");
                try {
                    var decrypted = string.IsNullOrEmpty(s.EncryptedCertificatePassword)
                        ? string.Empty
                        : NugetEncryptionUtility.DecryptString(s.EncryptedCertificatePassword);
                    return new X509Certificate2(s.Certificate, decrypted);
                } catch(Exception x)
                {
                    this.Log().Error("Unable to load the certificate: {0}", x);
                    return null;
                }
            })
            .FirstOrDefault();
        }
    }
}
