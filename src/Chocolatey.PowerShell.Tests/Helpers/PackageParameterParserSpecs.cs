// Copyright © 2017 - 2026 Chocolatey Software, Inc
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Chocolatey.PowerShell.Helpers;
using FluentAssertions;
using Moq;
using static chocolatey.StringResources.EnvironmentVariables;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public class PackageParameterParserSpecs
    {
        /// <summary>
        /// Clears the environment variables that the parser reads, and gives each spec a cmdlet to parse with.
        /// Specs derived from this call <see cref="Parse"/> for the text they are about.
        /// </summary>
        public abstract class PackageParameterParserSpecsBase : EnvironmentSpecsBase
        {
            protected Mock<ICommandRuntime> CommandRuntime;
            protected TestCmdlet Cmdlet;

            protected override string[] ManagedVariables { get; } =
            {
                Package.ChocolateyPackageParameters,
                Package.ChocolateyPackageParametersSensitive,
            };

            public override void Context()
            {
                base.Context();

                CommandRuntime = new Mock<ICommandRuntime>();
                Cmdlet = new TestCmdlet { CommandRuntime = CommandRuntime.Object };
            }

            public override void Because()
            {
            }

            protected Hashtable Parse(string parameters)
            {
                return PackageParameterParser.Parse(Cmdlet, parameters);
            }

            protected static Dictionary<string, object> Entries(Hashtable table)
            {
                return table.Cast<DictionaryEntry>().ToDictionary(entry => (string)entry.Key, entry => entry.Value);
            }

            protected static Dictionary<string, object> Expected(params (string Name, object Value)[] parameters)
            {
                return parameters.ToDictionary(parameter => parameter.Name, parameter => parameter.Value);
            }

            protected void VerifyDebug(string message, Func<Times> times)
            {
                CommandRuntime.Verify(r => r.WriteDebug(message), times);
            }
        }

        /// <summary>
        /// Base for specs that arrange <see cref="Parameters"/> and the environment variables in <c>Context()</c>,
        /// and look at the <see cref="Result"/> of parsing them.
        /// </summary>
        public abstract class ParsedParametersSpecsBase : PackageParameterParserSpecsBase
        {
            protected string Parameters;
            protected Hashtable Result;

            public override void Because()
            {
                Result = Parse(Parameters);
            }
        }

        public class When_there_is_nothing_to_parse : PackageParameterParserSpecsBase
        {
            [InlineData(null)]
            [InlineData("")]
            [InlineData("   ")]
            [InlineData("no slash here")]
            [InlineData("/")]
            [InlineData("/ /")]
            public void Should_find_no_parameters(string text)
            {
                Parse(text).Count.Should().Be(0);
            }
        }

        public class When_a_parameter_has_no_value : PackageParameterParserSpecsBase
        {
            [InlineData("/A")]
            [InlineData("/A ")]
            [InlineData("/A:")]
            [InlineData("/A=")]
            [InlineData("/A:  ")]
            [InlineData("/A:''")]
            [InlineData("/A:\"\"")]
            public void Should_give_the_boolean_true(string text)
            {
                var result = Parse(text);

                Entries(result).Should().BeEquivalentTo(Expected(("A", true)));
                result["A"].Should().BeOfType<bool>();
            }
        }

        public class When_a_parameter_has_a_value : PackageParameterParserSpecsBase
        {
            [InlineData("/A:value", "value")]
            [InlineData("/A=value", "value")]
            [InlineData("/A:'value'", "value")]
            [InlineData("/A:\"value\"", "value")]
            [InlineData("/A='value'", "value")]
            [InlineData("/A:value   ", "value")]
            [InlineData("/A:  value", "value")]
            [InlineData("/A:0", "0")]
            [InlineData("/A:false", "false")]
            [InlineData("/A:1/B:2", "1/B:2")]
            [InlineData("/A:Server=x;Db=y", "Server=x;Db=y")]
            [InlineData("/A:one two", "one two")]
            [InlineData("/A:'x y'", "x y")]
            [InlineData("/A:it's fine", "it's fine")]
            [InlineData(@"/A:C:\x\", @"C:\x\")]
            [InlineData("/A:'it''s'", "it''s")]
            public void Should_give_the_value_as_a_string_without_the_quotes_or_surrounding_space(string text, string expected)
            {
                var result = Parse(text);

                Entries(result).Should().BeEquivalentTo(Expected(("A", expected)));
                result["A"].Should().BeOfType<string>();
            }
        }

        public class When_there_are_several_parameters : PackageParameterParserSpecsBase
        {
            [Fact]
            public void Should_find_each_of_them()
            {
                Entries(Parse("/Shortcut /InstallDir:'c:\\program files\\xyz' /NoStartup")).Should().BeEquivalentTo(Expected(
                    ("Shortcut", true),
                    ("InstallDir", @"c:\program files\xyz"),
                    ("NoStartup", true)));
            }

            [Fact]
            public void Should_find_a_value_with_spaces_that_is_not_quoted()
            {
                Entries(Parse("/A:one two /B")).Should().BeEquivalentTo(Expected(("A", "one two"), ("B", true)));
            }

            [Fact]
            public void Should_find_a_quoted_value_and_the_parameter_after_it()
            {
                Entries(Parse("/A:'x y' /B:z")).Should().BeEquivalentTo(Expected(("A", "x y"), ("B", "z")));
            }

            [Fact]
            public void Should_ignore_text_before_the_first_parameter()
            {
                Entries(Parse("foo /A:1")).Should().BeEquivalentTo(Expected(("A", "1")));
            }

            [InlineData("/A:1\n/B:2")]
            [InlineData("/A:1\r\n/B:2")]
            [InlineData("/A:1\t/B:2")]
            public void Should_separate_parameters_with_any_whitespace(string text)
            {
                Entries(Parse(text)).Should().BeEquivalentTo(Expected(("A", "1"), ("B", "2")));
            }

            [Fact]
            public void Should_find_the_parameter_without_its_value_when_the_value_has_a_line_break()
            {
                Entries(Parse("/A:one\ntwo /B:3")).Should().BeEquivalentTo(Expected(("A", true), ("B", "3")));
            }
        }

        public class When_naming_a_parameter : PackageParameterParserSpecsBase
        {
            [InlineData("install-dir.x")]
            [InlineData("Name_1")]
            [InlineData("Zoë")]
            [InlineData("Ключ")]
            public void Should_take_the_name_up_to_the_separator(string name)
            {
                Entries(Parse($"/{name}:v")).Should().BeEquivalentTo(Expected((name, "v")));
            }

            [Fact]
            public void Should_end_the_name_at_a_close_parenthesis()
            {
                Entries(Parse("/Key)x:v")).Should().BeEquivalentTo(Expected(("Key", true)));
            }

            [Fact]
            public void Should_not_be_case_sensitive_when_looking_a_name_up()
            {
                var result = Parse("/InstallDir:x");

                result.ContainsKey("installdir").Should().BeTrue();
                result["INSTALLDIR"].Should().Be("x");
            }

            [Fact]
            public void Should_keep_one_entry_with_the_last_value_when_names_differ_only_in_case()
            {
                var result = Parse("/Key:first /KEY:second");

                result.Count.Should().Be(1);
                result["key"].Should().Be("second");
            }
        }

        public class When_parameters_are_given : ParsedParametersSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Environment.SetEnvironmentVariable(Package.ChocolateyPackageParameters, "/FromEnvironment:1");
                Parameters = "/A:1 /B /Path:'c:\\program files\\x'";
            }

            [Fact]
            public void Should_contain_each_parameter()
            {
                Result.Count.Should().Be(3);
                Result["A"].Should().Be("1");
                Result["B"].Should().Be(true);
                Result["Path"].Should().Be(@"c:\program files\x");
            }

            [Fact]
            public void Should_not_use_the_environment_variables()
            {
                Result.ContainsKey("FromEnvironment").Should().BeFalse();
                VerifyDebug("Parsing $env:ChocolateyPackageParameters and $env:ChocolateyPackageParametersSensitive for parameters", Times.Never);
            }

            [Fact]
            public void Should_log_each_parameter()
            {
                VerifyDebug("Adding package param 'A'='1'", Times.Once);
                VerifyDebug("Adding package param 'B'='True'", Times.Once);
                VerifyDebug(@"Adding package param 'Path'='c:\program files\x'", Times.Once);
            }
        }

        public class When_the_parameters_are_in_the_environment : ParsedParametersSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Environment.SetEnvironmentVariable(Package.ChocolateyPackageParameters, "/A:1 /B:2");
            }

            [Fact]
            public void Should_contain_the_parameters()
            {
                Result.Count.Should().Be(2);
                Result["A"].Should().Be("1");
                Result["B"].Should().Be("2");
            }

            [Fact]
            public void Should_say_where_it_is_looking_and_log_each_parameter()
            {
                VerifyDebug("Parsing $env:ChocolateyPackageParameters and $env:ChocolateyPackageParametersSensitive for parameters", Times.Once);
                VerifyDebug("Adding package param 'A'='1'", Times.Once);
                VerifyDebug("Adding package param 'B'='2'", Times.Once);
            }
        }

        public class When_the_parameters_given_are_empty : ParsedParametersSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Environment.SetEnvironmentVariable(Package.ChocolateyPackageParameters, "/A:1");
                Parameters = string.Empty;
            }

            [Fact]
            public void Should_use_the_environment_variables()
            {
                Result["A"].Should().Be("1");
            }
        }

        public class When_the_parameters_given_are_only_whitespace : ParsedParametersSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Environment.SetEnvironmentVariable(Package.ChocolateyPackageParameters, "/A:1");
                Parameters = "  ";
            }

            [Fact]
            public void Should_find_no_parameters_and_not_use_the_environment_variables()
            {
                Result.Count.Should().Be(0);
                VerifyDebug("Parsing $env:ChocolateyPackageParameters and $env:ChocolateyPackageParametersSensitive for parameters", Times.Never);
            }
        }

        public class When_sensitive_parameters_are_in_the_environment : ParsedParametersSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Environment.SetEnvironmentVariable(Package.ChocolateyPackageParameters, "/A:1 /B:2");
                Environment.SetEnvironmentVariable(Package.ChocolateyPackageParametersSensitive, "/B:secret /C");
            }

            [Fact]
            public void Should_merge_the_two_with_the_sensitive_value_winning()
            {
                Result.Count.Should().Be(3);
                Result["A"].Should().Be("1");
                Result["B"].Should().Be("secret");
                Result["C"].Should().Be(true);
            }

            [Fact]
            public void Should_say_that_it_will_not_log_the_parameters_and_not_log_them()
            {
                VerifyDebug("Sensitive parameters detected, no logging of parameters.", Times.Once);
                CommandRuntime.Verify(r => r.WriteDebug(It.Is<string>(m => m.StartsWith("Adding package param"))), Times.Never);
            }
        }

        public class When_the_sensitive_variable_is_empty : ParsedParametersSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Environment.SetEnvironmentVariable(Package.ChocolateyPackageParameters, "/A:1");
                Environment.SetEnvironmentVariable(Package.ChocolateyPackageParametersSensitive, string.Empty);
            }

            [Fact]
            public void Should_log_the_parameters()
            {
                VerifyDebug("Adding package param 'A'='1'", Times.Once);
                VerifyDebug("Sensitive parameters detected, no logging of parameters.", Times.Never);
            }
        }

        public class When_parameters_are_given_and_there_are_sensitive_parameters_in_the_environment : ParsedParametersSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Environment.SetEnvironmentVariable(Package.ChocolateyPackageParametersSensitive, "/Password:s3cret");
                Parameters = "/A:1";
            }

            [Fact]
            public void Should_contain_only_the_given_parameters()
            {
                Result.Count.Should().Be(1);
                Result["A"].Should().Be("1");
            }

            [Fact]
            public void Should_still_log_the_given_parameters()
            {
                VerifyDebug("Adding package param 'A'='1'", Times.Once);
                VerifyDebug("Sensitive parameters detected, no logging of parameters.", Times.Never);
            }
        }

        public class When_neither_variable_is_set : ParsedParametersSpecsBase
        {
            [Fact]
            public void Should_find_no_parameters_and_say_where_it_looked()
            {
                Result.Count.Should().Be(0);
                VerifyDebug("Parsing $env:ChocolateyPackageParameters and $env:ChocolateyPackageParametersSensitive for parameters", Times.Once);
            }
        }
    }
}
