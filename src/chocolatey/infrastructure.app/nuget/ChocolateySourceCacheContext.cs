// Copyright © 2017 - 2025 Chocolatey Software, Inc
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
using System.Threading;
using Alphaleonis.Win32.Filesystem;
using chocolatey.infrastructure.app.configuration;
using NuGet.Protocol.Core.Types;

namespace chocolatey.infrastructure.app.nuget
{
    public class ChocolateySourceCacheContext : SourceCacheContext
    {
        /// <summary>
        /// Path of temp folder if requested by GeneratedTempFolder
        /// </summary>
        private string _generatedChocolateyTempFolder = null;
        private readonly string _chocolateyCacheLocation;

        public ChocolateySourceCacheContext(ChocolateyConfiguration config)
        {
            _chocolateyCacheLocation = config.CacheLocation;

            if (config.CacheExpirationInMinutes <= 0)
            {
                MaxAge = DateTime.UtcNow;
                RefreshMemoryCache = true;
            }
            else
            {
                MaxAge = DateTime.UtcNow.AddMinutes(-config.CacheExpirationInMinutes);
            }
        }

        public override string GeneratedTempFolder
        {
            get
            {
                if (_generatedChocolateyTempFolder == null)
                {

                    var newTempFolder = Path.Combine(
                        _chocolateyCacheLocation,
                        "SourceTempCache",
                        Guid.NewGuid().ToString());

                    Interlocked.CompareExchange(ref _generatedChocolateyTempFolder, newTempFolder, comparand: null);
                }

                return _generatedChocolateyTempFolder;
            }

            set
            {
                Interlocked.CompareExchange(ref _generatedChocolateyTempFolder, value, comparand: null);
            }
        }

        /// <summary>
        /// Clones the current SourceCacheContext.
        /// </summary>
        public override SourceCacheContext Clone()
        {
            return new SourceCacheContext()
            {
                DirectDownload = DirectDownload,
                IgnoreFailedSources = IgnoreFailedSources,
                MaxAge = MaxAge,
                NoCache = NoCache,
                GeneratedTempFolder = _generatedChocolateyTempFolder,
                RefreshMemoryCache = RefreshMemoryCache,
                SessionId = SessionId
            };
        }

        protected override void Dispose(bool disposing)
        {
            var currentTempFolder = Interlocked.CompareExchange(ref _generatedChocolateyTempFolder, value: null, comparand: null);

            if (currentTempFolder != null)
            {
                try
                {
                    Directory.Delete(_generatedChocolateyTempFolder, recursive: true);
                }
                catch
                {
                    // Ignore failures when cleaning up.
                }
            }

            base.Dispose();
        }
    }
}
