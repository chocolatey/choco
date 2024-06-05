using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using chocolatey.infrastructure.app.attributes;
using chocolatey.infrastructure.app.commands;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.commandline;
using chocolatey.infrastructure.rules;
using chocolatey.infrastructure.services;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;

namespace chocolatey.tests.infrastructure.app.commands
{
    public class ChocolateyRuleCommandSpecs
    {
        [ConcernFor("rule")]
        public abstract class ChocolateyRuleCommandSpecsBase : TinySpec
        {
            protected ChocolateyRuleCommand Command;
            protected Mock<IRuleService> RuleService = new Mock<IRuleService>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Command = new ChocolateyRuleCommand(RuleService.Object);
            }
        }

        public class When_Implementing_Command_For : ChocolateyRuleCommandSpecsBase
        {
            private List<CommandForAttribute> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes<CommandForAttribute>().ToList();
            }

            [Fact]
            public void Should_Have_Expected_Number_Of_Commands()
            {
                _results.Should().HaveCount(1);
            }

            [InlineData("rule")]
            public void Should_Implement_Expected_Command(string name)
            {
                _results.Should().ContainSingle(r => r.CommandName == name);
            }

            [Fact]
            public void Should_Specify_Expected_Version_For_All_Commands()
            {
                _results.Should().AllSatisfy(r => r.Version.Should().Be("2.3.0"));
            }
        }

        public class When_Configuring_The_Argument_Parser : ChocolateyRuleCommandSpecsBase
        {
            private OptionSet _optionSet;

            public override void Context()
            {
                base.Context();
                _optionSet = new OptionSet();
            }

            public override void Because()
            {
                Command.ConfigureArgumentParser(_optionSet, Configuration);
            }

            [InlineData("n")]
            [InlineData("name")]
            public void Should_Set_Expected_Arguments_On_Option_Set(string name)
            {
                _optionSet.Contains(name).Should().BeTrue();
            }
        }

        public class When_Handling_Additional_Argument_Parsing : ChocolateyRuleCommandSpecsBase
        {
            private readonly IList<string> _unparsedArgs = new List<string>();
            private Action _because;

            public override void Because()
            {
                _because = () => Command.ParseAdditionalArguments(_unparsedArgs, Configuration);
            }

            public void Reset()
            {
                _unparsedArgs.Clear();
                Configuration = new ChocolateyConfiguration();
            }

            [Fact]
            public void Should_Set_Default_Sub_Command_On_Empty_Arguments()
            {
                Reset();
                _because();

                Configuration.RuleCommand.Command.Should().BeLowerCased().And.Be("list");
            }

            [Fact]
            public void Should_Set_Command_to_List_when_Specified()
            {
                Reset();
                _unparsedArgs.Add("LIST");
                _because();

                Configuration.RuleCommand.Command.Should().BeLowerCased().And.Be("list");
            }

            [Fact]
            public void Should_Set_Command_To_Get_When_Specified()
            {
                Reset();
                _unparsedArgs.Add("Get");
                _because();

                Configuration.RuleCommand.Command.Should().BeLowerCased().And.Be("get");
            }

            [Fact]
            public void Should_Set_Name_When_Command_When_Not_Already_Set()
            {
                Reset();
                _unparsedArgs.Add("Get");
                _unparsedArgs.Add("test-name");
                _because();

                Configuration.RuleCommand.Name.Should().Be("test-name");
            }

            [Fact]
            public void Should_Not_Set_Name_When_Already_Set()
            {
                Reset();
                _unparsedArgs.Add("Get");
                _unparsedArgs.Add("test-name");
                Configuration.RuleCommand.Name = "old-name";
                _because();

                Configuration.RuleCommand.Name.Should().Be("old-name");
            }

            [Fact]
            public void Should_Not_Set_Name_When_Only_Sub_Command_Specified()
            {
                Reset();
                _unparsedArgs.Add("something");
                _because();

                Configuration.RuleCommand.Name.Should().BeNullOrEmpty();
            }
        }

        public class When_Validating : ChocolateyRuleCommandSpecsBase
        {
            private Action _because;

            public override void Because()
            {
                _because = () => Command.Validate(Configuration);
            }

            [Fact]
            public void Should_Run_Successfully_When_Only_List_Command_Specified()
            {
                Configuration = new ChocolateyConfiguration();
                Configuration.RuleCommand.Command = "list";

                _because.Should().NotThrow();
            }

            [Fact]
            public void Throws_Expected_Exception_When_Name_Specified_For_List_Command()
            {
                Configuration = new ChocolateyConfiguration();
                Configuration.RuleCommand.Command = "list";
                Configuration.RuleCommand.Name = "some-name";

                _because.Should().Throw<ApplicationException>().WithMessage("A Rule Name (-n|--name) should not be specified when listing all validation rules.");
            }

            [Fact]
            public void Throws_Expected_Exception_When_Name_Is_Not_Specified_For_Get_Command()
            {
                Configuration = new ChocolateyConfiguration();
                Configuration.RuleCommand.Command = "get";

                _because.Should().Throw<ApplicationException>().WithMessage("A Rule Name (-n|--name) is required when getting information for a specific rule.");
            }

            [Fact]
            public void Should_Run_Successfully_When_Get_Command_With_Name_Is_Specified()
            {
                Configuration = new ChocolateyConfiguration();
                Configuration.RuleCommand.Command = "get";
                Configuration.RuleCommand.Name = "some-name";

                _because.Should().NotThrow();
            }

            [Fact]
            public void Should_Log_Warning_When_Unknown_Sub_Command_Is_Used()
            {
                Configuration = new ChocolateyConfiguration();
                Configuration.RuleCommand.Command = "something";

                using (new AssertionScope())
                {
                    _because.Should().NotThrow();
                    MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToString())
                        .WhoseValue.Should().Contain("Unknown command 'something'. Setting to list.");
                    Configuration.RuleCommand.Command.Should().Be("list");
                }
            }

            [Fact]
            public void Throws_Expected_Exception_When_Name_Supplied_For_Unknown_Command()
            {
                Configuration = new ChocolateyConfiguration();
                Configuration.RuleCommand.Command = "something";
                Configuration.RuleCommand.Name = "MyName";

                using (new AssertionScope())
                {
                    _because.Should().Throw<ApplicationException>().WithMessage("A Rule Name (-n|--name) should not be specified when listing all validation rules.");
                    Configuration.RuleCommand.Command.Should().Be("list");
                    MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToString())
                        .WhoseValue.Should().Contain("Unknown command 'something'. Setting to list.");
                }
            }
        }

        public class When_Run_Is_Called_Using_List_Command_And_Has_Rules : ChocolateyRuleCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();

                RuleService.Setup(r => r.GetAllAvailableRules())
                    .Returns(new[]
                    {
                        new ImmutableRule(RuleType.Error, "Error ID", "Some Error Summary", "Help URL"),
                        new ImmutableRule(RuleType.Warning, "Warning ID", "Some Warning Summary", "Help URL"),
                        new ImmutableRule(RuleType.Information, "Information ID", "Some Information Summary", "Help URL"),
                        new ImmutableRule(RuleType.Note, "Note ID", "Some Note Summary", "Help URL"),
                        new ImmutableRule(RuleType.None, "Disabled ID", "Some Disabled Summary", "Help URL"),
                    });
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_Output_Headers_In_Correct_Order()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().ContainInOrder(
                        "Implemented Package Rules",
                        "Error/Required Rules",
                        "Warning/Guideline Rules",
                        "Information/Suggestion Rules",
                        "Note Rules",
                        "Disabled Rules");
            }

            [Fact]
            public void Should_Output_Error_Rules_With_Expected_Header()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().ContainInConsecutiveOrder(
                        "Error/Required Rules",
                        string.Empty,
                        "Error ID: Some Error Summary");
            }

            [Fact]
            public void Should_Output_Warning_Rules_With_Expected_Header()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().ContainInConsecutiveOrder(
                        "Warning/Guideline Rules",
                        string.Empty,
                        "Warning ID: Some Warning Summary");
            }

            [Fact]
            public void Should_Output_Information_Rules_With_Expected_Header()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().ContainInConsecutiveOrder(
                        "Information/Suggestion Rules",
                        string.Empty,
                        "Information ID: Some Information Summary");
            }

            [Fact]
            public void Should_Output_Note_Rules_With_Expected_Header()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().ContainInConsecutiveOrder(
                        "Note Rules",
                        string.Empty,
                        "Note ID: Some Note Summary");
            }

            [Fact]
            public void Should_Output_Disabled_Rules_With_Expected_Header()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().ContainInConsecutiveOrder(
                        "Disabled Rules",
                        string.Empty,
                        "Disabled ID: Some Disabled Summary");
            }
        }

        public class When_Run_Is_Called_Using_List_Command_And_Has_Rules_With_Limited_Output : ChocolateyRuleCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.RegularOutput = false;

                RuleService.Setup(r => r.GetAllAvailableRules())
                    .Returns(new[]
                    {
                        new ImmutableRule(RuleType.Error, "Error ID", "Some Error Summary", "Help URL"),
                        new ImmutableRule(RuleType.Warning, "Warning ID", "Some Warning Summary", "Help URL"),
                        new ImmutableRule(RuleType.Information, "Information ID", "Some Information Summary", "Help URL"),
                        new ImmutableRule(RuleType.Note, "Note ID", "Some Note Summary", "Help URL"),
                        new ImmutableRule(RuleType.None, "Disabled ID", "Some Disabled Summary", "Help URL"),
                    });
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_Output_Rules_With_In_Order_With_Limited_Output()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().ContainInConsecutiveOrder(
                        "Error|Error ID|Some Error Summary|Help URL",
                        "Warning|Warning ID|Some Warning Summary|Help URL",
                        "Information|Information ID|Some Information Summary|Help URL",
                        "Note|Note ID|Some Note Summary|Help URL",
                        "None|Disabled ID|Some Disabled Summary|Help URL");
            }
        }

        public class When_Run_Is_Called_Using_List_Command_Without_Any_Rules_Available : ChocolateyRuleCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();

                RuleService.Setup(r => r.GetAllAvailableRules())
                    .Returns(Array.Empty<ImmutableRule>());
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_Output_Rules_With_In_Order_With_Limited_Output()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().ContainInOrder(
                        "Implemented Package Rules",
                        "Error/Required Rules",
                        "No implemented Error/Required rules available.",
                        "Warning/Guideline Rules",
                        "No implemented Warning/Guideline rules available.",
                        "Information/Suggestion Rules",
                        "No implemented Information/Suggestion rules available.",
                        "Note Rules",
                        "No implemented Note rules available.",
                        "Disabled Rules",
                        "No implemented Disabled rules available.");
            }
        }

        public class When_Run_Is_Called_Using_List_Command_Without_Any_Rules_Available_With_Limited_Output : ChocolateyRuleCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.RegularOutput = false;

                RuleService.Setup(r => r.GetAllAvailableRules())
                    .Returns(Array.Empty<ImmutableRule>());
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_Not_Output_Any_Rules()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Info.ToString());
            }
        }

        public class When_Run_Is_Called_With_Rule_Name_And_Error_Severity : ChocolateyRuleCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.RuleCommand.Name = "ErrorID";

                RuleService.Setup(r => r.GetAllAvailableRules())
                    .Returns(new[]
                    {
                        new ImmutableRule(RuleType.Error, "ErrorID", "My Rule Summary", "https://rules.info/rule/ErrorID"),
                        new ImmutableRule(RuleType.Error, "OtherID", "Some Other Rule Summary")
                    });
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_Output_Information_On_Found_Rule()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().ContainInConsecutiveOrder(
                        "Name: ErrorID | Severity: Error",
                        "Summary: My Rule Summary",
                        "Help URL: https://rules.info/rule/ErrorID");
            }
        }

        public class When_Run_Is_Called_With_Rule_Name_And_Warning_Severity : ChocolateyRuleCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.RuleCommand.Name = "WarningID";

                RuleService.Setup(r => r.GetAllAvailableRules())
                    .Returns(new[]
                    {
                        new ImmutableRule(RuleType.Warning, "WarningID", "My Rule Summary", "https://rules.info/rule/WarningID"),
                        new ImmutableRule(RuleType.Error, "OtherID", "Some Other Rule Summary")
                    });
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_Output_Information_On_Found_Rule()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().ContainInConsecutiveOrder(
                        "Name: WarningID | Severity: Warning",
                        "Summary: My Rule Summary",
                        "Help URL: https://rules.info/rule/WarningID");
            }
        }

        public class When_Run_Is_Called_With_Rule_Name_And_Information_Severity : ChocolateyRuleCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.RuleCommand.Name = "InformationID";

                RuleService.Setup(r => r.GetAllAvailableRules())
                    .Returns(new[]
                    {
                        new ImmutableRule(RuleType.Information, "InformationID", "My Rule Summary", "https://rules.info/rule/InformationID"),
                        new ImmutableRule(RuleType.Error, "OtherID", "Some Other Rule Summary")
                    });
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_Output_Information_On_Found_Rule()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().ContainInConsecutiveOrder(
                        "Name: InformationID | Severity: Information",
                        "Summary: My Rule Summary",
                        "Help URL: https://rules.info/rule/InformationID");
            }
        }

        public class When_Run_Is_Called_With_Rule_Name_And_Note_Severity : ChocolateyRuleCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.RuleCommand.Name = "NoteID";

                RuleService.Setup(r => r.GetAllAvailableRules())
                    .Returns(new[]
                    {
                        new ImmutableRule(RuleType.Note, "NoteID", "My Rule Summary", "https://rules.info/rule/NoteID"),
                        new ImmutableRule(RuleType.Error, "OtherID", "Some Other Rule Summary")
                    });
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_Output_Information_On_Found_Rule()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().ContainInConsecutiveOrder(
                        "Name: NoteID | Severity: Note",
                        "Summary: My Rule Summary",
                        "Help URL: https://rules.info/rule/NoteID");
            }
        }

        public class When_Run_Is_Called_With_Rule_Name_And_Disabled_Severity : ChocolateyRuleCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.RuleCommand.Name = "DisabledID";

                RuleService.Setup(r => r.GetAllAvailableRules())
                    .Returns(new[]
                    {
                        new ImmutableRule(RuleType.None, "DisabledID", "My Rule Summary", "https://rules.info/rule/DisabledID"),
                        new ImmutableRule(RuleType.Error, "OtherID", "Some Other Rule Summary")
                    });
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_Output_Information_On_Found_Rule()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().ContainInConsecutiveOrder(
                        "Name: DisabledID | Severity: None",
                        "Summary: My Rule Summary",
                        "Help URL: https://rules.info/rule/DisabledID");
            }
        }

        public class When_Run_Is_Called_With_Rule_Name_And_Does_Not_Exist : ChocolateyRuleCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.RuleCommand.Name = "NoneExistingID";

                RuleService.Setup(r => r.GetAllAvailableRules())
                    .Returns(new[]
                    {
                        new ImmutableRule(RuleType.Error, "OtherID", "I should not be found")
                    });
            }

            public override void Because()
            {
            }

            [Fact]
            public void Throws_Expected_Argument_Exception()
            {
                Action action = () => Command.Run(Configuration);
                action.Should().Throw<ApplicationException>()
                    .WithMessage("No rule with the name NoneExistingID could be found.");
            }
        }

        public class When_Run_Is_Called_With_Rule_Name_And_Limited_Output : ChocolateyRuleCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.RuleCommand.Name = "LimitedRule";
                Configuration.RegularOutput = false;

                RuleService.Setup(r => r.GetAllAvailableRules())
                    .Returns(new[]
                    {
                        new ImmutableRule(RuleType.Warning, "LimitedRule", "I am the summary of a warning.", "https://limited.info/rules/CCCC"),
                        new ImmutableRule(RuleType.Warning, "OtherID", "I Should Not be Outputted")
                    });
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_Only_Output_Single_Rule_In_Parsable_Output()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().OnlyContain(v => v == "Warning|LimitedRule|I am the summary of a warning.|https://limited.info/rules/CCCC");
            }
        }

        public class When_Run_Is_Called_With_Not_Found_Rule_Name_And_Limited_Output : ChocolateyRuleCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.RuleCommand.Name = "LimitedRule";
                Configuration.RegularOutput = false;

                RuleService.Setup(r => r.GetAllAvailableRules())
                    .Returns(new[]
                    {
                        new ImmutableRule(RuleType.Warning, "OtherID", "I Should Not be Outputted")
                    });
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_Not_Output_Any_Rules()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Info.ToString());
            }
        }
    }
}
