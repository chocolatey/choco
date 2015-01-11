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
    using NuGet;

    // ReSharper disable InconsistentNaming

    public class ChocolateyLocalPackageRepository : LocalPackageRepository
    {
        public ChocolateyLocalPackageRepository(string physicalPath)
            : base(physicalPath)
        {
        }

        public ChocolateyLocalPackageRepository(string physicalPath, bool enableCaching)
            : base(physicalPath, enableCaching)
        {
        }

        public ChocolateyLocalPackageRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem)
            : base(pathResolver, fileSystem)
        {
        }

        public ChocolateyLocalPackageRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem, bool enableCaching)
            : base(pathResolver, fileSystem, enableCaching)
        {
        }

        public override void AddPackage(IPackage package)
        {
            string packageFilePath = GetPackageFilePath(package);
            FileSystem.AddFileWithCheck(packageFilePath, package.GetStream);
        }
    }

    // ReSharper restore InconsistentNaming
}