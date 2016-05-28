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

namespace chocolatey.infrastructure.app.tasks
{
    using System;
    using System.IO;
    using System.Linq;
    using events;
    using filesystem;
    using infrastructure.events;
    using infrastructure.tasks;
    using logging;
    using tolerance;

    public class RemovePendingPackagesTask : ITask
    {
        private readonly IFileSystem _fileSystem;
        private IDisposable _subscription;

        public RemovePendingPackagesTask(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
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
                this.Log().Warn("[Pending] Removing incomplete install for '{0}'".format_with(_fileSystem.get_directory_info_for(packageFolder).Name));

                FaultTolerance.retry(2, () => _fileSystem.delete_directory_if_exists(packageFolder, recursive: true, overrideAttributes: true, isSilent: true), 500, isSilent: true);
            }
        }
    }
}
