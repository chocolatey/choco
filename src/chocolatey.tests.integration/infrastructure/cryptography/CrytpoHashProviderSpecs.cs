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

namespace chocolatey.tests.integration.infrastructure.cryptography
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using chocolatey.infrastructure.cryptography;
    using chocolatey.infrastructure.filesystem;
    using Should;

    public class CrytpoHashProviderSpecs
    {
        public abstract class CrytpoHashProviderSpecsBase : TinySpec
        {
            protected CryptoHashProvider Provider;
            protected DotNetFileSystem FileSystem;
            protected string ContextDirectory;

            public override void Context()
            {
                FileSystem = new DotNetFileSystem();
                Provider = new CryptoHashProvider(FileSystem);
                ContextDirectory = FileSystem.combine_paths(FileSystem.get_directory_name(FileSystem.get_current_assembly_path()), "context");
            }
        }

        public class when_HashProvider_provides_a_hash : CrytpoHashProviderSpecsBase
        {
            private string result;
            private string filePath;

            public override void Context()
            {
                base.Context();
                filePath = FileSystem.combine_paths(ContextDirectory, "testing.packages.config");
            }

            public override void Because()
            {
                result = Provider.hash_file(filePath);
            }

            [Fact]
            public void should_provide_the_correct_hash_based_on_a_checksum()
            {
                var expected = BitConverter.ToString(SHA256.Create().ComputeHash(File.ReadAllBytes(filePath))).Replace("-", string.Empty);

                result.ShouldEqual(expected);
            }
        }
    }
}
