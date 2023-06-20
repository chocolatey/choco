namespace chocolatey.tests.infrastructure.app.commands
{
    using chocolatey.infrastructure.app.attributes;
    using System.Collections.Generic;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.filesystem;
    using Moq;
    using System.Reflection;
    using System.Linq;
    using FluentAssertions;
    using chocolatey.infrastructure.commandline;
    using chocolatey.infrastructure.app.domain;
    using FluentAssertions.Execution;

    public class ChocolateyCacheCommandSpecs
    {
        [ConcernFor("cache")]
        public abstract class ChocolateyCacheCommandSpecsBase : TinySpec
        {
            protected ChocolateyCacheCommand Command;
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();

            public override void Context()
            {
                Configuration.CommandName = "cache";
                Command = new ChocolateyCacheCommand(FileSystem.Object);
            }
        }

        public class WhenImplementingCommandFor : ChocolateyCacheCommandSpecsBase
        {
            private List<CommandForAttribute> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes<CommandForAttribute>().ToList();
            }

            [Fact]
            public void ShouldImplementCache()
            {
                _results.Should().AllSatisfy(c => c.CommandName.Should().Be("cache"));
            }

            [Fact]
            public void ShouldSetADescription()
            {
                _results.Should().AllSatisfy(c => c.Description.Should().NotBeNullOrEmpty());
            }

            [Fact]
            public void ShouldSetVersionProperty()
            {
                _results.Should().AllSatisfy(c => c.Version.Should().Be("2.1.0"));
            }
        }

        public class WhenConfiguringTheArgumentParser : ChocolateyCacheCommandSpecsBase
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

            [Fact]
            [InlineData("expired")]
            public void ShouldAddOptionToOptionSet(string name)
            {
                _optionSet.Contains(name).Should().BeTrue("Option set should include the parameter {0}", name);
            }
        }

        public class WhenParsingAdditionalParameters : ChocolateyCacheCommandSpecsBase
        {
            public override void Because()
            {
                
            }

            public override void BeforeEachSpec()
            {
                Configuration.CacheCommand.Command = CacheCommandType.Unknown;
                MockLogger.Reset();
            }

            [Fact]
            public void ShouldHaveSetCacheCommandTypeToListOnUnusedSubCommand()
            {
                Command.ParseAdditionalArguments(new List<string>(), Configuration);
                Configuration.CacheCommand.Command.Should().Be(CacheCommandType.List);
            }

            [InlineData("list", CacheCommandType.List)]
            [InlineData("remove", CacheCommandType.Remove)]
            [InlineData("unknown", CacheCommandType.List)]
            public void ShouldHaveSetCacheCommandTypeToListOnListSubCommand(string testArg, CacheCommandType expectedType)
            {
                var unparsedArgs = new[] { testArg };
                Command.ParseAdditionalArguments(unparsedArgs, Configuration);
                Configuration.CacheCommand.Command.Should().Be(expectedType);
            }

            [Fact]
            public void ShouldHaveSetCacheCommandTypeToListOnUnknownSubCommand()
            {
                var unparsedArgs = new[] { "some-command" };

                Command.ParseAdditionalArguments(unparsedArgs, Configuration);

                using (new AssertionScope())
                {
                    Configuration.CacheCommand.Command.Should().Be(CacheCommandType.List);
                    MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToString())
                        .WhoseValue.Should().Contain("Unknown command 'some-command'. Setting to list.");
                }
            }
        }
    }
}