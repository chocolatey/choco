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

namespace chocolatey.tests.infrastructure.cryptography
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using chocolatey.infrastructure.adapters;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.cryptography;
    using chocolatey.infrastructure.filesystem;
    using Moq;
    using FluentAssertions;

    public class CryptoHashProviderSpecs
    {
        public abstract class CryptoHashProviderSpecsBase : TinySpec
        {
            protected CryptoHashProvider Provider;
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();

            public override void Context()
            {
                Provider = Provider = new CryptoHashProvider(FileSystem.Object);
            }
        }

        public class When_HashProvider_provides_a_hash : CryptoHashProviderSpecsBase
        {
            private string _result;
            private readonly string _filePath = "c:\\path\\does\\not\\matter.txt";
            private readonly byte[] _byteArray = new byte[] { 23, 25, 27 };

            public override void Context()
            {
                base.Context();
                FileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
                FileSystem.Setup(x => x.ReadFileBytes(_filePath)).Returns(_byteArray);
            }

            public override void Because()
            {
                _result = Provider.ComputeFileHash(_filePath);
            }

            [Fact]
            public void Should_provide_the_correct_hash_based_on_a_checksum()
            {
                var expected = BitConverter.ToString(SHA256.Create().ComputeHash(_byteArray)).Replace("-", string.Empty);

                _result.Should().Be(expected);
            }
        }

        public class When_HashProvider_attempts_to_provide_a_hash_for_a_file_over_2GB : CryptoHashProviderSpecsBase
        {
            private string _result;
            private readonly string _filePath = "c:\\path\\does\\not\\matter.txt";
            private readonly byte[] _byteArray = new byte[] { 23, 25, 27 };
            private readonly Mock<IHashAlgorithm> _hashAlgorithm = new Mock<IHashAlgorithm>();

            public override void Context()
            {
                base.Context();
                Provider = new CryptoHashProvider(FileSystem.Object, _hashAlgorithm.Object);

                FileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
                FileSystem.Setup(x => x.ReadFileBytes(_filePath)).Returns(_byteArray);
                _hashAlgorithm.Setup(x => x.ComputeHash(_byteArray)).Throws<IOException>(); //IO.IO_FileTooLong2GB (over Int32.MaxValue)
            }

            public override void Because()
            {
                _result = Provider.ComputeFileHash(_filePath);
            }

            [Fact]
            public void Should_log_a_warning()
            {
                MockLogger.MessagesFor(LogLevel.Warn).Should().HaveCount(1);
            }

            [Fact]
            public void Should_not_throw_an_error_itself()
            {
                //this handles itself
            }

            [Fact]
            public void Should_provide_an_unchanging_hash_for_a_file_too_big_to_hash()
            {
                _result.Should().Be(ApplicationParameters.HashProviderFileTooBig);
            }
        }
    }
}
