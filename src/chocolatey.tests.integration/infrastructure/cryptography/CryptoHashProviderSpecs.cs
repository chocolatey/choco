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
using System.IO;
using System.Security.Cryptography;
using chocolatey.infrastructure.cryptography;
using chocolatey.infrastructure.filesystem;
using FluentAssertions;

namespace chocolatey.tests.integration.infrastructure.cryptography
{
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

        public class When_HashProvider_is_given_a_large_file_to_hash : CryptoHashProviderSpecsBase
        {
            private string _result;
            private string _filePath;

            public override void Context()
            {
                base.Context();

                // We need to create a large, 2.5GB file on disk, so that we can run
                // the ComputeFileHash method against that file, and then test the
                // generated hash of the file to make sure that the method is working
                // as expected. It takes around 15-20 seconds to create the file on
                // disk, which is "fine", and is better than adding a large 2.5 GB
                // file directly into the repository.
                var sizeInMb = 2500;
                const int blockSize = 1024 * 8;
                const int blocksPerMb = (1024 * 1024) / blockSize;
                var data = new byte[blockSize];
                var rng = new Random(13);
                _filePath = FileSystem.CombinePaths(ContextDirectory, "large-file.txt");

                using (FileStream stream = File.Create(_filePath))
                {
                    for (var i = 0; i < sizeInMb * blocksPerMb; i++)
                    {
                        rng.NextBytes(data);
                        stream.Write(data, 0, data.Length);
                    }
                }
            }

            public override void Because()
            {
                _result = Provider.ComputeFileHash(_filePath);
            }

            [Fact]
            public void Should_provide_the_correct_hash_based_on_a_checksum()
            {
                // This is the pre-calculated file of for the large 2.5GB file.
                // Since we have used a fixed seed value for the Random above, the
                // generated file should have the same hash each time, so we can
                // pin this value here, and assert against it.
                var expected = "15884E3178F05AF1F25343D528B72E59A93ECACA039E549B2B67207FAD18B01E";

                _result.Should().Be(expected);
            }

            public override void AfterObservations()
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }
            }
        }
    }
}
