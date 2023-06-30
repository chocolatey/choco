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

namespace chocolatey.tests.integration.infrastructure.cryptography
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using chocolatey.infrastructure.cryptography;
    using chocolatey.infrastructure.filesystem;
    using FluentAssertions;

    public class CryptoHashProviderSpecs
    {
        public abstract class CryptoHashProviderSpecsBase : TinySpec
        {
            protected CryptoHashProvider Provider;
            protected DotNetFileSystem FileSystem;
            protected string ContextDirectory;

            public override void Context()
            {
                FileSystem = new DotNetFileSystem();
                Provider = new CryptoHashProvider(FileSystem);
                ContextDirectory = FileSystem.CombinePaths(FileSystem.GetDirectoryName(FileSystem.GetCurrentAssemblyPath()), "context");
            }
        }

        public class When_HashProvider_provides_a_hash : CryptoHashProviderSpecsBase
        {
            private string _result;
            private string _filePath;

            public override void Context()
            {
                base.Context();
                _filePath = FileSystem.CombinePaths(ContextDirectory, "testing.packages.config");
            }

            public override void Because()
            {
                _result = Provider.ComputeFileHash(_filePath);
            }

            [Fact]
            public void Should_provide_the_correct_hash_based_on_a_checksum()
            {
                var expected = BitConverter.ToString(SHA256.Create().ComputeHash(File.ReadAllBytes(_filePath))).Replace("-", string.Empty);

                _result.Should().Be(expected);
            }
        }
    }
}
