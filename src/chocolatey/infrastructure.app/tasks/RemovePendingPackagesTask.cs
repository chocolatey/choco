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

namespace chocolatey.infrastructure.app.tasks
{
    using System;
    using System.IO;
    using System.Linq;
    using events;
    using filesystem;
    using infrastructure.events;
    using infrastructure.services;
    using infrastructure.tasks;
    using logging;
    using tolerance;

    public class RemovePendingPackagesTask : ITask
    {
        private readonly IFileSystem _fileSystem;
        private readonly IDateTimeService _dateTimeService;
        private IDisposable _subscription;
        private const int PendingFileAgeSeconds = 10;
        private const string PendingSkipFile = ".chocolateyPendingSkip";

        public RemovePendingPackagesTask(IFileSystem fileSystem) : this(fileSystem, new SystemDateTimeUtcService())
        {
        }

        public RemovePendingPackagesTask(IFileSystem fileSystem, IDateTimeService dateTimeService)
        {
            _fileSystem = fileSystem;
            _dateTimeService = dateTimeService;
        }

        public void Initialize()
        {
            _subscription = EventManager.Subscribe<PreRunMessage>(HandleMessage, null, null);
            this.Log().Debug(ChocolateyLoggers.Verbose, () => "{0} is now ready and waiting for {1}.".FormatWith(GetType().Name, typeof(PreRunMessage).Name));
        }

        public void Shutdown()
        {
            if (_subscription != null) _subscription.Dispose();
        }

        private void HandleMessage(PreRunMessage message)
        {
            this.Log().Debug(ChocolateyLoggers.Verbose, "[Pending] Removing all pending packages that should not be considered installed...");

            var pendingFiles = _fileSystem.GetFiles(ApplicationParameters.PackagesLocation, ApplicationParameters.PackagePendingFileName).ToList();
            foreach (var pendingFile in pendingFiles.OrEmpty())
            {
                var packageFolder = _fileSystem.GetDirectoryName(pendingFile);
                string packageFolderName = _fileSystem.GetDirectoryInfo(packageFolder).Name;

                var pendingSkipFiles = _fileSystem.GetFiles(packageFolder, PendingSkipFile).ToList();
                if (pendingSkipFiles.Count != 0)
                {
                    this.Log().Warn("Pending file found for {0}, but a {1} file was also found. Skipping removal".FormatWith(packageFolderName, PendingSkipFile));
                    continue;
                }

                try
                {
                    //attempt to open the pending file. If it is locked, continue
                    var file = _fileSystem.OpenFileExclusive(pendingFile);
                    file.Close();
                    file.Dispose();
                }
                catch (Exception)
                {
                    this.Log().Debug("Pending file found for {0}, but the file is locked by another process.".FormatWith(packageFolderName));
                    continue;
                }

                // wait for the file to be at least x seconds old
                // this allows commands running from the package for configuring sources, etc
                var fileInfo = _fileSystem.GetFileInfoFor(pendingFile);
                if (fileInfo.CreationTimeUtc.AddSeconds(PendingFileAgeSeconds) > _dateTimeService.GetCurrentDateTime())
                {
                    this.Log().Debug("Pending file found for {0}, but the file is not {1} seconds old yet.".FormatWith(packageFolderName, PendingFileAgeSeconds));
                    continue;
                }

                this.Log().Warn("[Pending] Removing incomplete install for '{0}'".FormatWith(packageFolderName));
                FaultTolerance.Retry(2, () => _fileSystem.DeleteDirectoryChecked(packageFolder, recursive: true, overrideAttributes: true, isSilent: true), 500, isSilent: true);
            }
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void initialize()
            => Initialize();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void shutdown()
            => Shutdown();
#pragma warning restore IDE1006
    }
}
