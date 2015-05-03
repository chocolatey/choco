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

namespace chocolatey.infrastructure.app.services
{
    using domain;
    using filesystem;
    using infrastructure.services;

    public sealed class FilesService : IFilesService
    {
        private readonly IXmlService _xmlService;
        private readonly IFileSystem _fileSystem;

        public FilesService(IXmlService xmlService, IFileSystem fileSystem)
        {
            _xmlService = xmlService;
            _fileSystem = fileSystem;
        }

        public PackageFiles read_from_file(string filePath)
        {
            if (!_fileSystem.file_exists(filePath))
            {
                return null;
            }

            return _xmlService.deserialize<PackageFiles>(filePath);
        }

        public void save_to_file(PackageFiles snapshot, string filePath)
        {
            _xmlService.serialize(snapshot, filePath);
        }
    }
}