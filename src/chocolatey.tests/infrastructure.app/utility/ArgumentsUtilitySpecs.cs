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

using System.IO;
using System.Linq;
using chocolatey.infrastructure.app;
using chocolatey.infrastructure.app.nuget;
using chocolatey.infrastructure.app.utility;
using chocolatey.infrastructure.filesystem;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace chocolatey.tests.infrastructure.app.utility
{
    public class ArgumentsUtilitySpecs
    {
        public abstract class ArgumentsUtilitySpecsBase : TinySpec
        {
            public override void Context()
            {
            }
        }

        public abstract class ArgumentsUtilityDecryptSpecsBase : TinySpec
        {
            protected string[] Result;
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();

            public override void Context()
            {
                FileSystem.Setup(f => f.CombinePaths(It.IsAny<string>(), It.IsAny<string[]>()))
                    .Returns<string, string[]>((arg1, args) => Path.Combine(arg1, Path.Combine(args)));
            }

            public override void Because()
            {
                Result = ArgumentsUtility.DecryptPackageArgumentsFile(FileSystem.Object, "test-package", "0.1.0").ToArray();
            }

            protected void AddArgumentsMock(string arguments)
            {
                AddArgumentsMock(arguments, "test-package", "0.1.0");
            }

            protected void AddArgumentsMock(string arguments, string id, string version)
            {
                var expectedPathEnds = Path.Combine(
                    ApplicationParameters.InstallLocation,
                    ".chocolatey",
                    id + "." + version,
                    ".arguments");

                var exists = true;

                if (arguments is null)
                {
                    exists = false;
                    FileSystem.Setup(f => f.ReadFile(expectedPathEnds)).Throws(new FileNotFoundException());
                }
                else
                {
                    FileSystem.Setup(f => f.ReadFile(expectedPathEnds)).Returns(NugetEncryptionUtility.EncryptString(arguments));
                }

                FileSystem.Setup(f => f.FileExists(expectedPathEnds)).Returns(exists);
            }
        }

        public class When_ArgumentsUtility_is_parsing_remembered_arguments_and_file_does_not_exist : ArgumentsUtilityDecryptSpecsBase
        {
            public override void Context()
            {
                base.Context();

                AddArgumentsMock(null);
            }

            [Fact]
            public void Should_Have_Returned_An_Empty_Set()
            {
                Result.Should().BeEmpty();
            }
        }

        public class When_ArgumentsUtility_is_parsing_remembered_arguments_with_single_quote_in_variable : ArgumentsUtilityDecryptSpecsBase
        {
            public override void Context()
            {
                base.Context();

                AddArgumentsMock(" --prerelease  --ignore-dependencies --package-parameters=\"'--quiet --install-path=\"C:\\User\\My' User\\Install\"'\"");
            }

            [Fact]
            public void Should_Only_Contain_Three_Arguments()
            {
                Result.Should().HaveCount(3);
            }

            [InlineData("--prerelease")]
            [InlineData("--ignore-dependencies")]
            [InlineData("--package-parameters='--quiet --install-path=\"C:\\User\\My' User\\Install\"'")]
            public void Should_Contain_Expected_Arguments(string argument)
            {
                Result.Should().Contain(argument);
            }
        }

        public class When_ArgumentsUtility_is_parsing_remembered_arguments_with_quotes : ArgumentsUtilityDecryptSpecsBase
        {
            public override void Context()
            {
                base.Context();

                AddArgumentsMock(" --override-arguments --package-parameters=\"'--quiet --norestart --wait --includeRecommended --includeOptional'\" --cache-location=\"'C:\\Users\\Test\\AppData\\Local\\Temp\\chocolatey'\"");
            }

            [Fact]
            public void Should_Only_Contain_Three_Arguments()
            {
                Result.Should().HaveCount(3);
            }

            [InlineData("--override-arguments")]
            [InlineData("--package-parameters='--quiet --norestart --wait --includeRecommended --includeOptional'")]
            [InlineData("--cache-location='C:\\Users\\Test\\AppData\\Local\\Temp\\chocolatey'")]
            public void Should_Contain_Expected_Arguments(string argument)
            {
                Result.Should().Contain(argument);
            }
        }

        public class When_ArgumentsUtility_is_parsing_arguments_with_large_junk_string : ArgumentsUtilityDecryptSpecsBase
        {
            public override void Context()
            {
                base.Context();

                // Create a large, unmatched string that never contains the expected quote sequence
                var junk = new string('x', 10_000);

                AddArgumentsMock($"--package-parameters=\"'{junk}'\"");
            }

            [Fact]
            public void Should_Contain_A_Single_Argument()
            {
                Result.Should().ContainSingle();
            }

            [Fact]
            public void Should_Not_Throw_Or_Overflow()
            {
                // If we reach this point, no exception or stack overflow occurred
                Result.First().Should().StartWith("--package-parameters=");
            }
        }

        public class When_ArgumentsUtility_encounters_many_partial_quote_matches : ArgumentsUtilityDecryptSpecsBase
        {
            public override void Context()
            {
                base.Context();

                // This will trigger hundreds of failed partial matches, forcing recursion
                var misleading = string.Concat(Enumerable.Repeat("'junk ", 200));
                var args = $"--package-parameters=\"{misleading}\"";

                AddArgumentsMock(args);
            }

            [Fact]
            public void Should_Not_Throw_Or_Overflow()
            {
                // If this completes, we didn't blow the stack
                Result.Should().NotBeEmpty();
            }
        }

        public class When_ArgumentsUtility_is_parsing_remembered_arguments_with_quotes_and_pkg_parameters_in_quotes : ArgumentsUtilityDecryptSpecsBase
        {
            public override void Context()
            {
                base.Context();

                AddArgumentsMock("--package-parameters='\"/User:\\\"kim\\\" /Env:\\\"dev\\\"\"'");
            }

            [Fact]
            public void Should_Only_Contain_A_Single_Argument_Arguments()
            {
                Result.Should().ContainSingle();
            }

            [InlineData("--package-parameters=\"/User:\\\"kim\\\" /Env:\\\"dev\\\"\"")]
            public void Should_Contain_Expected_Arguments(string argument)
            {
                Result.Should().Contain(argument);
            }
        }

        public class When_ArgumentsUtility_is_parsing_remembered_arguments_with_quotes_and_pkg_parameters_in_quotes_and_spaces : ArgumentsUtilityDecryptSpecsBase
        {
            public override void Context()
            {
                base.Context();

                AddArgumentsMock("--package-parameters=\"'/Path:\"\"C:\\Program Files\\Dummy\"\" /LicenseKey:ABC-123-XYZ'\"");
            }

            [Fact]
            public void Should_Only_Contain_A_Single_Argument()
            {
                Result.Should().ContainSingle();
            }

            [InlineData("--package-parameters='/Path:\"\"C:\\Program Files\\Dummy\"\" /LicenseKey:ABC-123-XYZ'")]
            public void Should_Contain_Expected_Arguments(string argument)
            {
                Result.Should().Contain(argument);
            }
        }

        public class When_ArgumentsUtility_is_parsing_remembered_arguments_with_parameter_using_single_quote : ArgumentsUtilityDecryptSpecsBase
        {
            public override void Context()
            {
                base.Context();

                AddArgumentsMock("--package-parameters=\"/Author:O'Reilly /Title:Beginner's Guide\"");
            }

            [Fact]
            public void Should_Contain_A_Single_Argument()
            {
                Result.Should().ContainSingle();
            }

            [InlineData("--package-parameters=/Author:O'Reilly /Title:Beginner's Guide")]
            public void Should_Contain_Expected_Arguments(string argument)
            {
                Result.Should().Contain(argument);
            }
        }
        public class When_ArgumentsUtility_encounters_unterminated_quotes : ArgumentsUtilityDecryptSpecsBase
        {
            public override void Context()
            {
                base.Context();

                AddArgumentsMock("--package-parameters='--opt1 --opt2");
            }

            [Fact]
            public void Should_Preserve_Unterminated_Argument()
            {
                // We don't ensure that the argument is closing the quote,
                // as such the same argument that was passed in is returned.
                Result.Should().Equal(new[]
                {
                    "--package-parameters='--opt1 --opt2"
                });
            }
        }
        public class When_ArgumentsUtility_encounters_empty_quoted_argument : ArgumentsUtilityDecryptSpecsBase
        {
            public override void Context()
            {
                base.Context();

                AddArgumentsMock("--option=\"\"");
            }

            [Fact]
            public void Should_Treat_Empty_Quoted_Value_As_Empty_String()
            {
                Result.Should().Equal(new[]
                {
                    "--option"
                });
            }
        }
        public class When_ArgumentsUtility_encounters_mixed_quote_styles : ArgumentsUtilityDecryptSpecsBase
        {
            public override void Context()
            {
                base.Context();

                AddArgumentsMock("--first='value one' --second=\"value two\"");
            }

            [Fact]
            public void Should_Parse_Mixed_Quoted_Arguments_Correctly()
            {
                Result.Should().Equal(new[]
                {
                    "--first=value one",
                    "--second=value two"
                });
            }
        }

        [TestFixture("choco install bob --package-parameters-sensitive=\"/test=bill\"", true)]
        [TestFixture("choco install bob -package-parameters-sensitive=\"/test=bill\"", true)]
        [TestFixture("choco install bob --install-arguments-sensitive=\"/test=bill\"", true)]
        [TestFixture("choco install bob -install-arguments-sensitive=\"/test=bill\"", true)]
        [TestFixture("choco apikey -k secretKey -s secretSource", true)]
        [TestFixture("choco config set --name=proxyPassword --value=secretPassword", true)]
        [TestFixture("choco push package.nupkg -k=secretKey", true)]
        [TestFixture("choco source add -n=test -u=bob -p bill", true)]
        [TestFixture("choco source add -n=test -u=bob -p=bill", true)]
        [TestFixture("choco source add -n=test -u=bob -password=bill", true)]
        [TestFixture("choco source add -n=test -cert=text.pfx -cp secretPassword", true)]
        [TestFixture("choco source add -n=test -cert=text.pfx -cp=secretPassword", true)]
        [TestFixture("choco source add -n=test -cert=text.pfx -certpassword=secretPassword", true)]
        [TestFixture("choco push package.nupkg -k secretKey", true)]
        [TestFixture("choco push package.nupkg -k=secretKey", true)]
        [TestFixture("choco push package.nupkg -key secretKey", true)]
        [TestFixture("choco push package.nupkg -key=secretKey", true)]
        [TestFixture("choco install bob -apikey=secretKey", true)]
        [TestFixture("choco install bob -apikey secretKey", true)]
        [TestFixture("choco install bob -api-key=secretKey", true)]
        [TestFixture("choco install bob -api-key secretKey", true)]
        [TestFixture("choco install bob", false)]
        public class When_ArgumentsUtility_is_testing_for_sensitive_parameters : ArgumentsUtilitySpecsBase
        {
            private bool _result;
            private bool _expectedResult;
            private string _commandArguments;

            public When_ArgumentsUtility_is_testing_for_sensitive_parameters(string commandArguments, bool expectedResult)
            {
                _commandArguments = commandArguments;
                _expectedResult = expectedResult;
            }

            public override void Because()
            {
                _result = ArgumentsUtility.SensitiveArgumentsProvided(_commandArguments);
            }

            [Fact]
            public void Should_return_expected_result()
            {
                _result.Should().Be(_expectedResult);
            }
        }
    }
}
