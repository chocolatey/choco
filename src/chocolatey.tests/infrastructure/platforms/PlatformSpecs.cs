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

namespace chocolatey.tests.infrastructure.platforms
{
    using System;
    using chocolatey.infrastructure.adapters;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.platforms;
    using Moq;
    using Should;
    using Environment = System.Environment;

    public class PlatformSpecs
    {
        public abstract class PlatformSpecsBase : TinySpec
        {
            protected Mock<IEnvironment> MockEnvironment = new Mock<IEnvironment>();
            protected Mock<IFileSystem> MockFileSystem = new Mock<IFileSystem>();

            public override void Context()
            {
                Platform.initialize_with(new Lazy<IEnvironment>(() => MockEnvironment.Object), new Lazy<IFileSystem>(() => MockFileSystem.Object));
                MockEnvironment.Setup(e => e.OSVersion).Returns(Environment.OSVersion);
            }
        }

        public class when_calling_Platform_get_platform : PlatformSpecsBase
        {
            private PlatformType result;

            public override void Because()
            {
                result = Platform.get_platform();
            }

            [Fact]
            public void should_not_be_Unknown()
            {
                result.ShouldNotEqual(PlatformType.Unknown);
            }
        }

        public class when_calling_Platform_get_platform_on_Windows : PlatformSpecsBase
        {
            private PlatformType result;

            public override void Context()
            {
                base.Context();
                MockEnvironment.Setup(e => e.OSVersion).Returns(new OperatingSystem(PlatformID.Win32Windows, new Version(2, 1, 0, 0)));
            }

            public override void Because()
            {
                result = Platform.get_platform();
            }

            [Fact]
            public void should_return_Windows()
            {
                result.ShouldEqual(PlatformType.Windows);
            }
        }

        public class when_calling_Platform_get_platform_on_MacOSX : PlatformSpecsBase
        {
            private PlatformType result;

            public override void Context()
            {
                base.Context();
                MockEnvironment.Setup(e => e.OSVersion).Returns(new OperatingSystem(PlatformID.MacOSX, new Version(2, 1, 0, 0)));
            }

            public override void Because()
            {
                result = Platform.get_platform();
            }

            [Fact]
            public void should_return_Mac()
            {
                result.ShouldEqual(PlatformType.Mac);
            }
        }

        public class when_calling_Platform_get_platform_on_Linux : PlatformSpecsBase
        {
            private PlatformType result;

            public override void Context()
            {
                base.Context();
                MockEnvironment.Setup(e => e.OSVersion).Returns(new OperatingSystem(PlatformID.Unix, new Version(2, 1, 0, 0)));
                MockFileSystem.Setup(f => f.directory_exists(It.IsAny<string>())).Returns(false);
            }

            public override void Because()
            {
                result = Platform.get_platform();
            }

            [Fact]
            public void should_return_Linux()
            {
                result.ShouldEqual(PlatformType.Linux);
            }
        }

        public class when_calling_Platform_get_platform_on_PlatformId_Linux_with_MacOSX_folder_structure : PlatformSpecsBase
        {
            private PlatformType result;

            public override void Context()
            {
                base.Context();
                MockEnvironment.Setup(e => e.OSVersion).Returns(new OperatingSystem(PlatformID.Unix, new Version(2, 1, 0, 0)));
                MockFileSystem.Setup(f => f.directory_exists(It.IsAny<string>())).Returns(true);
            }

            public override void Because()
            {
                result = Platform.get_platform();
            }

            [Fact]
            public void should_return_Mac()
            {
                result.ShouldEqual(PlatformType.Mac);
            }
        }
    }
}
