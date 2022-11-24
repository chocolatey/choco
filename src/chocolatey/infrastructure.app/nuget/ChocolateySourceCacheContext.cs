using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chocolatey.infrastructure.app.nuget
{
    using System.Threading;
    using Alphaleonis.Win32.Filesystem;
    using configuration;
    using NuGet.Protocol.Core.Types;

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

            set => Interlocked.CompareExchange(ref _generatedChocolateyTempFolder, value, comparand: null);
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
