// Copyright © 2021 - 2021 Chocolatey Software, Inc
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

namespace chocolatey.tests.infrastructure.app.utility
{
    using chocolatey.infrastructure.app.utility;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.platforms;
    using NUnit.Framework;
    using Should;

    public class PackageUtilitySpecs
    {
        public abstract class PackageUtilitySpecsBase : TinySpec
        {
            public override void Context()
            {
            }
        }


        [TestFixture("bob", "", true)]
        [TestFixture("", "bob", true)]
        [TestFixture("bob", "bob", false)]
        [TestFixture("bob", "bob;separatedPackage", false)]
        [TestFixture("bob", "C:\\chocolatey-packages\\bob\\bob.1.0.0.nupkg", false)]
        [TestFixture("dependency", "C:\\chocolatey-packages\\bob\\bob.1.0.0.nupkg", true)]
        [TestFixture("bob", "C:\\chocolatey-packages\\bob\\bob.nuspec", false)]
        [TestFixture("dependency", "C:\\chocolatey-packages\\bob\\bob.nuspec", true)]
        [TestFixture("bob", "\\bob", false)]
        [TestFixture("dependency", "\\bob", true)]
        [TestFixture("dependency", "bob", true)]
        [TestFixture("dependency", "bob;separatedPackage", true)]
        public class when_PackageUtility_is_checking_if_package_is_dependency : PackageUtilitySpecsBase
        {
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();
            private bool _result;
            private bool _expectedResult;
            private string _packageName;

            public when_PackageUtility_is_checking_if_package_is_dependency(string packageName, string configNames, bool expectedResult)
            {
                if (Platform.get_platform() != PlatformType.Windows) configNames = configNames.Replace("\\", "/");

                _packageName = packageName;
                _config.PackageNames = configNames;
                _expectedResult = expectedResult;
            }

            public override void Because()
            {
                _result = PackageUtility.package_is_a_dependency(_config, _packageName);
            }

            [Fact]
            public void should_return_expected_result()
            {
                _result.ShouldEqual(_expectedResult);
            }
        }
    }
}
