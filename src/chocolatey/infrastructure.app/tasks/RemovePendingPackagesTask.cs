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
        private const int PENDING_FILE_AGE_SECONDS = 10;
        private const string PENDING_SKIP_FILE = ".chocolateyPendingSkip";

        public RemovePendingPackagesTask(IFileSystem fileSystem) : this(fileSystem, new SystemDateTimeUtcService())
        {
        }

        public RemovePendingPackagesTask(IFileSystem fileSystem, IDateTimeService dateTimeService)
        {
            _fileSystem = fileSystem;
            _dateTimeService = dateTimeService;
        }

        public void initialize()
        {
            _subscription = EventManager.subscribe<PreRunMessage>(handle_message, null, null);
            this.Log().Debug(ChocolateyLoggers.Verbose, () => "{0} is now ready and waiting for {1}.".format_with(GetType().Name, typeof(PreRunMessage).Name));
        }

        public void shutdown()
        {
            if (_subscription != null) _subscription.Dispose();
        }

        private void handle_message(PreRunMessage message)
        {
            this.Log().Debug(ChocolateyLoggers.Verbose, "[Pending] Removing all pending packages that should not be considered installed...");

            var pendingFiles = _fileSystem.get_files(ApplicationParameters.PackagesLocation, ApplicationParameters.PackagePendingFileName, SearchOption.AllDirectories).ToList();
            foreach (var pendingFile in pendingFiles.or_empty_list_if_null())
            {
                var packageFolder = _fileSystem.get_directory_name(pendingFile);
                string packageFolderName = _fileSystem.get_directory_info_for(packageFolder).Name;

                var pendingSkipFiles = _fileSystem.get_files(packageFolder, PENDING_SKIP_FILE, SearchOption.AllDirectories).ToList();
                if (pendingSkipFiles.Count != 0)
                {
                    this.Log().Warn("Pending file found for {0}, but a {1} file was also found. Skipping removal".format_with(packageFolderName, PENDING_SKIP_FILE));
                    continue;
                }

                try
                {
                    //attempt to open the pending file. If it is locked, continue
                    var file = _fileSystem.open_file_exclusive(pendingFile);
                    file.Close();
                    file.Dispose();
                }
                catch (Exception)
                {
                    this.Log().Debug("Pending file found for {0}, but the file is locked by another process.".format_with(packageFolderName));
                    continue;
                }

                // wait for the file to be at least x seconds old
                // this allows commands running from the package for configuring sources, etc
                var fileInfo = _fileSystem.get_file_info_for(pendingFile);
                if (fileInfo.CreationTimeUtc.AddSeconds(PENDING_FILE_AGE_SECONDS) > _dateTimeService.get_current_date_time())
                {
                    this.Log().Debug("Pending file found for {0}, but the file is not {1} seconds old yet.".format_with(packageFolderName, PENDING_FILE_AGE_SECONDS));
                    continue;
                }

                this.Log().Warn("[Pending] Removing incomplete install for '{0}'".format_with(packageFolderName));
                FaultTolerance.retry(2, () => _fileSystem.delete_directory_if_exists(packageFolder, recursive: true, overrideAttributes: true, isSilent: true), 500, isSilent: true);
            }
        }
    }
}
