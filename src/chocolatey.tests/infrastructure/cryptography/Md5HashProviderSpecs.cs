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

namespace chocolatey.tests.infrastructure.cryptography
{
    using System;
    using System.Security.Cryptography;
    using Moq;
    using Should;
    using chocolatey.infrastructure.cryptography;
    using chocolatey.infrastructure.filesystem;

    public class Md5HashProviderSpecs
    {
        public abstract class Md5HashProviderSpecsBase : TinySpec
        {
            protected Md5HashProvider Provider;
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();

            public override void Context()
            {
                Provider = new Md5HashProvider(FileSystem.Object);
            }
        }

        public class when_Md5HashProvider_provides_a_hash : Md5HashProviderSpecsBase
        {
            private string result;
            private string filePath = "c:\\path\\does\\not\\matter.txt";
            private readonly byte[] byteArray = new byte[] {23, 25, 27};

            public override void Context()
            {
                base.Context();
                FileSystem.Setup(x => x.file_exists(It.IsAny<string>())).Returns(true);
                FileSystem.Setup(x => x.read_file_bytes(filePath)).Returns(byteArray);
            }

            public override void Because()
            {
                result = Provider.hash_file(filePath);
            }

            [Fact]
            public void should_provide_the_correct_hash_based_on_a_checksum()
            {
                var expected = BitConverter.ToString(MD5.Create().ComputeHash(byteArray)).Replace("-", string.Empty);

                result.ShouldEqual(expected);
            }
        }
    }
}