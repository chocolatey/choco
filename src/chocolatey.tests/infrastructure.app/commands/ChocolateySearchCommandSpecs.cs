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
using System.Collections.Generic;
using System.Linq;
using chocolatey.infrastructure.app.attributes;
using chocolatey.infrastructure.app.commands;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.services;
using chocolatey.infrastructure.commandline;
using Moq;
using FluentAssertions;
using chocolatey.infrastructure.app.domain;

namespace chocolatey.tests.infrastructure.app.commands
{
    public class ChocolateySearchCommandSpecs
    {
        [ConcernFor("search")]
        public abstract class ChocolateySearchCommandSpecsBase : TinySpec
        {
            protected ChocolateySearchCommand Command;
            protected Mock<IChocolateyPackageService> PackageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                MockLogger.Reset();

                Configuration.Sources = "bob";
                Command = new ChocolateySearchCommand(PackageService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateySearchCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_not_implement_list()
            {
                _results.Should().NotContain("list");
            }

            [Fact]
            public void Should_implement_search()
            {
                _results.Should().Contain("search");
            }

            [Fact]
            public void Should_implement_find()
            {
                _results.Should().Contain("find");
            }
        }

        public class When_configurating_the_argument_parser_for_search_command : ChocolateySearchCommandSpecsBase
        {
            private OptionSet _optionSet;

            public override void Context()
            {
                base.Context();
                _optionSet = new OptionSet();
                Configuration.CommandName = "search";
            }

            public override void Because()
            {
                Command.ConfigureArgumentParser(_optionSet, Configuration);
            }

            [Fact]
            public void Should_add_source_to_the_option_set()
            {
                _optionSet.Contains("source").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_source_to_the_option_set()
            {
                _optionSet.Contains("s").Should().BeTrue();
            }

            [Fact]
            public void Should_add_prerelease_to_the_option_set()
            {
                _optionSet.Contains("prerelease").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_prerelease_to_the_option_set()
            {
                _optionSet.Contains("pre").Should().BeTrue();
            }

            [Fact]
            public void Should_add_includeprograms_to_the_option_set()
            {
                _optionSet.Contains("includeprograms").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_includeprograms_to_the_option_set()
            {
                _optionSet.Contains("i").Should().BeTrue();
            }

            [Fact]
            public void Should_add_allversions_to_the_option_set()
            {
                _optionSet.Contains("allversions").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_allversions_to_the_option_set()
            {
                _optionSet.Contains("a").Should().BeTrue();
            }

            [Fact]
            public void Should_add_user_to_the_option_set()
            {
                _optionSet.Contains("user").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_user_to_the_option_set()
            {
                _optionSet.Contains("u").Should().BeTrue();
            }

            [Fact]
            public void Should_add_password_to_the_option_set()
            {
                _optionSet.Contains("password").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_password_to_the_option_set()
            {
                _optionSet.Contains("p").Should().BeTrue();
            }

            [Fact]
            public void Should_add_include_configured_sources_to_the_option_set()
            {
                _optionSet.Contains("include-configured-sources").Should().BeTrue();
            }
        }

        [NUnit.Framework.TestFixture("search")]
        [NUnit.Framework.TestFixture("find")]
        public class When_configurating_the_argument_parser : ChocolateySearchCommandSpecsBase
        {
            private OptionSet _optionSet;

            public When_configurating_the_argument_parser(string commandName)
            {
                Configuration.CommandName = commandName;
            }

            public override void Context()
            {
                base.Context();
                _optionSet = new OptionSet();
            }

            public override void Because()
            {
                Command.ConfigureArgumentParser(_optionSet, Configuration);
            }

            [Fact]
            public void Should_add_source_to_the_option_set()
            {
                _optionSet.Contains("source").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_source_to_the_option_set()
            {
                _optionSet.Contains("s").Should().BeTrue();
            }

            [Fact]
            public void Should_add_prerelease_to_the_option_set()
            {
                _optionSet.Contains("prerelease").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_prerelease_to_the_option_set()
            {
                _optionSet.Contains("pre").Should().BeTrue();
            }

            [Fact]
            public void Should_add_includeprograms_to_the_option_set()
            {
                _optionSet.Contains("includeprograms").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_includeprograms_to_the_option_set()
            {
                _optionSet.Contains("i").Should().BeTrue();
            }

            [Fact]
            public void Should_add_allversions_to_the_option_set()
            {
                _optionSet.Contains("allversions").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_allversions_to_the_option_set()
            {
                _optionSet.Contains("a").Should().BeTrue();
            }

            [Fact]
            public void Should_add_user_to_the_option_set()
            {
                _optionSet.Contains("user").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_user_to_the_option_set()
            {
                _optionSet.Contains("u").Should().BeTrue();
            }

            [Fact]
            public void Should_add_password_to_the_option_set()
            {
                _optionSet.Contains("password").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_password_to_the_option_set()
            {
                _optionSet.Contains("p").Should().BeTrue();
            }

            [Fact]
            public void Should_add_order_by_to_the_option_set()
            {
                _optionSet.Contains("order-by").Should().BeTrue();
            }

            [Fact]
            public void Should_add_order_by_popularity_to_the_option_set()
            {
                _optionSet.Contains("order-by-popularity").Should().BeTrue();
            }

            [Fact]
            public void Should_have_marked_order_by_popularity_as_deprecated()
            {
                _optionSet["order-by-popularity"].Description.Should().Contain("Deprecated");
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateySearchCommandSpecsBase
        {
            private readonly IList<string> _unparsedArgs = new List<string>();
            private readonly string _source = "https://somewhereoutthere";
            private Action _because;

            public override void Context()
            {
                base.Context();
                _unparsedArgs.Add("pkg1");
                _unparsedArgs.Add("pkg2");
                Configuration.Sources = _source;
            }

            public override void Because()
            {
                _because = () => Command.ParseAdditionalArguments(_unparsedArgs, Configuration);
            }

            [Fact]
            public void Should_set_unparsed_arguments_to_configuration_input()
            {
                _because();
                Configuration.Input.Should().Be("pkg1 pkg2");
            }

            [Fact]
            public void Should_leave_source_as_set()
            {
                Configuration.ListCommand.LocalOnly = false;
                _because();
                Configuration.Sources.Should().Be(_source);
            }
        }

        public class When_noop_is_called_with_search_command : ChocolateySearchCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.CommandName = "search";
            }

            public override void Because()
            {
                Command.DryRun(Configuration);
            }

            [Fact]
            public void Should_call_service_list_noop()
            {
                PackageService.Verify(c => c.ListDryRun(Configuration), Times.Once);
            }
        }

        [NUnit.Framework.TestFixtureSource(nameof(TestOrders))]
        public class When_handling_order_by_parameter_correctly_parses_argument : ChocolateySearchCommandSpecsBase
        {
            private OptionSet _optionSet;
            private OptionContext _optionContext;
            private PackageOrder _order;
            private string _orderString;

            public When_handling_order_by_parameter_correctly_parses_argument(string orderString)
            {
                _orderString = orderString;
                _order = (PackageOrder)Enum.Parse(typeof(PackageOrder), _orderString);
            }

            private static object[] TestOrders
            {
                get
                {
                    var values = Enum.GetNames(typeof(PackageOrder));
                    var result = new List<object>();

                    foreach (var value in values)
                    {
                        result.Add(new object[] { value });
                    }

                    return result.ToArray();
                }
            }

            public override void Context()
            {
                base.Context();

                _optionSet = new OptionSet();
                Command.ConfigureArgumentParser(_optionSet, Configuration);
                _optionContext = new OptionContext(_optionSet)
                {
                    Option = _optionSet["order-by"]
                };
                _optionContext.OptionName = _optionContext.Option.Names.First();
                _optionContext.OptionValues.Add(_orderString);
            }

            public override void Because()
            {
                _optionContext.Option.Invoke(_optionContext);
            }

            [Fact]
            public void Should_have_set_expected_package_sort_on_config()
            {
                Configuration.ListCommand.OrderBy.Should().Be(_order);
            }
        }

        [NUnit.Framework.TestFixture(null)]
        [NUnit.Framework.TestFixture("")]
        public class When_handling_order_by_with_empty_values : ChocolateySearchCommandSpecsBase
        {
            private OptionSet _optionSet;
            private OptionContext _optionContext;
            private Exception _ex = null;
            private string _testValue;

            public When_handling_order_by_with_empty_values(string testValue)
            {
                _testValue = testValue;
            }

            public override void Context()
            {
                base.Context();

                _optionSet = new OptionSet();
                Command.ConfigureArgumentParser(_optionSet, Configuration);
                _optionContext = new OptionContext(_optionSet)
                {
                    Option = _optionSet["order-by"]
                };
                _optionContext.OptionName = _optionContext.Option.Names.First();
                _optionContext.OptionValues.Add(_testValue);
            }

            public override void Because()
            {
                try
                {
                    _optionContext.Option.Invoke(_optionContext);
                }
                catch (Exception ex)
                {
                    _ex = ex;
                }
            }

            [Fact]
            public void Should_have_thrown_expected_exception()
            {
                _ex.Should().NotBeNull()
                    .And.BeOfType<ApplicationException>()
                    .Which.Message.Should().StartWith("No '--order-by' clause was provided. Specify one of the supported clauses:");
            }
        }

        public class When_handling_order_by_with_unsupported_value : ChocolateySearchCommandSpecsBase
        {
            private OptionSet _optionSet;
            private OptionContext _optionContext;
            private Exception _ex = null;

            public override void Context()
            {
                base.Context();

                _optionSet = new OptionSet();
                Command.ConfigureArgumentParser(_optionSet, Configuration);
                _optionContext = new OptionContext(_optionSet)
                {
                    Option = _optionSet["order-by"]
                };
                _optionContext.OptionName = _optionContext.Option.Names.First();
                _optionContext.OptionValues.Add("NotExisting");
            }

            public override void Because()
            {
                try
                {
                    _optionContext.Option.Invoke(_optionContext);
                }
                catch (Exception ex)
                {
                    _ex = ex;
                }
            }

            [Fact]
            public void Should_have_thrown_expected_exception()
            {
                _ex.Should().NotBeNull()
                    .And.BeOfType<ApplicationException>()
                    .Which.Message.Should().StartWith("The '--order-by' clause 'NotExisting' is not recognized. Use one of the supported clauses:");
            }
        }

        public class When_handling_order_by_popularity : ChocolateySearchCommandSpecsBase
        {
            private OptionSet _optionSet;
            private OptionContext _optionContext;

            public override void Context()
            {
                base.Context();

                _optionSet = new OptionSet();
                Command.ConfigureArgumentParser(_optionSet, Configuration);
                _optionContext = new OptionContext(_optionSet)
                {
                    Option = _optionSet["order-by-popularity"]
                };
                _optionContext.OptionName = _optionContext.Option.Names.First();
                _optionContext.OptionValues.Add(bool.TrueString);
            }

            public override void Because()
            {
                _optionContext.Option.Invoke(_optionContext);
            }

            [Fact]
            public void Should_have_set_expected_order_by_property_value()
            {
                Configuration.ListCommand.OrderBy.Should().Be(PackageOrder.Popularity);
            }

            [Fact]
            public void Should_have_outputted_warning_message()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Warn.ToString())
                    .WhoseValue.Should().Contain(@"'--order-by-popularity' is deprecated and will be removed in a future release.
 Use '--order-by='Popularity'' instead.");
            }
        }

        public class When_noop_is_called : ChocolateySearchCommandSpecsBase
        {
            public override void Because()
            {
                Command.DryRun(Configuration);
            }

            [Fact]
            public void Should_call_service_list_noop()
            {
                PackageService.Verify(c => c.ListDryRun(Configuration), Times.Once);
            }

            [Fact]
            public void Should_not_report_any_warning_messages()
            {
                MockLogger.Messages.Keys.Should().NotContain("Warn");
            }
        }

        public class When_run_is_called_with_search_command : ChocolateySearchCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.CommandName = "search";
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_call_service_list_run()
            {
                PackageService.Verify(c => c.List(Configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateySearchCommandSpecsBase
        {
            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_call_service_list_run()
            {
                PackageService.Verify(c => c.List(Configuration), Times.Once);
            }

            [Fact]
            public void Should_not_report_any_warning_messages()
            {
                MockLogger.Messages.Keys.Should().NotContain("Warn");
            }
        }

        [NUnit.Framework.TestFixture("search")]
        [NUnit.Framework.TestFixture("find")]
        public class When_outputting_help_message : ChocolateySearchCommandSpecsBase
        {
            public When_outputting_help_message(string commandName)
            {
                Configuration.CommandName = commandName;
            }

            public override void Because()
            {
                Command.HelpMessage(Configuration);
            }
        }
    }
}
