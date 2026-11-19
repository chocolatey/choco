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
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using FluentAssertions;
using Moq;
using Chocolatey.PowerShell.Helpers;
using static Chocolatey.PowerShell.Tests.Helpers.FakeUninstallRegistry;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public class RegistryHelperSpecs
    {
        private const string BadKeysWarning = "Found 10 or more bad registry keys. Run command again with '--verbose --debug' for more info.";

        /// <summary>
        /// Gives each spec a cmdlet that records the verbose, debug, and warning messages written through it, in
        /// order, as <c>VERBOSE: text</c>, <c>DEBUG: text</c>, and <c>WARNING: text</c>. Specs derived from this
        /// create the registry for the keys they are about.
        /// </summary>
        public abstract class RegistryHelperSpecsBase : TinySpec
        {
            protected Mock<ICommandRuntime> CommandRuntime;
            protected TestCmdlet Cmdlet;
            protected List<string> Messages;

            public override void Context()
            {
                Messages = new List<string>();

                CommandRuntime = new Mock<ICommandRuntime>();
                CommandRuntime
                    .Setup(r => r.WriteVerbose(It.IsAny<string>()))
                    .Callback<string>(message => Messages.Add("VERBOSE: " + message));
                CommandRuntime
                    .Setup(r => r.WriteDebug(It.IsAny<string>()))
                    .Callback<string>(message => Messages.Add("DEBUG: " + message));
                CommandRuntime
                    .Setup(r => r.WriteWarning(It.IsAny<string>()))
                    .Callback<string>(message => Messages.Add("WARNING: " + message));

                Cmdlet = new TestCmdlet { CommandRuntime = CommandRuntime.Object };
            }

            public override void Because()
            {
            }

            protected IEnumerable<string> Warnings
            {
                get
                {
                    return Messages
                        .Where(message => message.StartsWith("WARNING: ", StringComparison.Ordinal))
                        .Select(message => message.Substring("WARNING: ".Length));
                }
            }

            /// <summary>
            /// Finds <paramref name="softwareName"/> in <paramref name="registry"/>, writing messages through the cmdlet.
            /// </summary>
            protected List<PSObject> FindUninstallKeys(FakeUninstallRegistry registry, string softwareName)
            {
                return RegistryHelper.FindUninstallKeys(Cmdlet, softwareName, registry.GetKeyPaths, registry.GetProperties);
            }
        }

        /// <summary>
        /// Base for specs that add keys to the <see cref="Registry"/> in <c>Context()</c>, and look at the
        /// <see cref="Result"/> of finding <see cref="SoftwareName"/>, or the <see cref="Error"/> it fails with.
        /// </summary>
        public abstract class FindSpecsBase : RegistryHelperSpecsBase
        {
            protected FakeUninstallRegistry Registry;
            protected string SoftwareName;
            protected List<PSObject> Result;
            protected Exception Error;

            public override void Context()
            {
                base.Context();

                Registry = new FakeUninstallRegistry();
            }

            public override void Because()
            {
                try
                {
                    Result = FindUninstallKeys(Registry, SoftwareName);
                }
                catch (Exception error)
                {
                    Error = error;
                }
            }

            protected IEnumerable<object> ResultNames
            {
                get
                {
                    return Result.Select(key => key.Properties["PSChildName"].Value);
                }
            }
        }

        public class When_one_key_matches : FindSpecsBase
        {
            public override void Context()
            {
                base.Context();

                SoftwareName = "VLC media player";
                Registry.AddKey(Wow6432NodeLocation, "Other", "Other software");
                Registry.AddKey(MachineLocation, "VLC", "VLC media player");
                Registry.AddKey(UserLocation, "Unnamed", null);
            }

            [Fact]
            public void Should_not_fail()
            {
                Error.Should().BeNull();
            }

            [Fact]
            public void Should_return_that_key()
            {
                ResultNames.Should().Equal("VLC");
            }

            [Fact]
            public void Should_write_the_messages_in_order()
            {
                Messages.Should().Equal(
                    "VERBOSE: Retrieving all uninstall registry keys",
                    "DEBUG: Registry uninstall keys on system: 3",
                    "DEBUG: Found 1 uninstall registry key(s) with SoftwareName: 'VLC media player'");
            }
        }

        public class When_keys_in_several_locations_match : FindSpecsBase
        {
            public override void Context()
            {
                base.Context();

                SoftwareName = "Launchy*";
                Registry.AddKey(UserLocation, "User", "Launchy 2.6");
                Registry.AddKey(MachineLocation, "Machine", "Launchy 2.5");
                Registry.AddKey(MachineLocation, "Other", "Other software");
                Registry.AddKey(Wow6432NodeLocation, "Wow6432Node", "Launchy 2.4");
            }

            [Fact]
            public void Should_return_them_in_the_order_the_locations_are_searched()
            {
                ResultNames.Should().Equal("Wow6432Node", "Machine", "User");
            }

            [Fact]
            public void Should_count_them()
            {
                Messages.Should().Contain("DEBUG: Found 3 uninstall registry key(s) with SoftwareName: 'Launchy*'");
            }

            [Fact]
            public void Should_not_warn()
            {
                Warnings.Should().BeEmpty();
            }
        }

        public class When_matching_a_display_name : RegistryHelperSpecsBase
        {
            [InlineData("VLC media player", "VLC media player", true)]
            [InlineData("vlc MEDIA player", "VLC media player", true)]
            [InlineData("Launchy*", "Launchy 2.5", true)]
            [InlineData("Launchy", "Launchy 2.5", false)]
            [InlineData("Gpg4win (*)", "Gpg4Win (2.3.0)", true)]
            [InlineData("?ython", "Python", true)]
            [InlineData("SketchUp [0-9]*", "SketchUp 2016", true)]
            [InlineData("SketchUp [0-9]*", "SketchUp Viewer", false)]
            [InlineData("*", "", true)]
            [InlineData("*", null, true)]
            [InlineData("a*", null, false)]
            public void Should_find_the_key_only_when_the_name_matches(string softwareName, string displayName, bool found)
            {
                var registry = new FakeUninstallRegistry();
                registry.AddKey(MachineLocation, "Key", displayName);

                FindUninstallKeys(registry, softwareName).Should().HaveCount(found ? 1 : 0);
            }
        }

        public class When_nothing_matches : FindSpecsBase
        {
            public override void Context()
            {
                base.Context();

                SoftwareName = "Missing*";
                Registry.AddKey(MachineLocation, "Other", "Other software");
            }

            [Fact]
            public void Should_not_fail()
            {
                Error.Should().BeNull();
            }

            [Fact]
            public void Should_return_nothing()
            {
                Result.Should().BeEmpty();
            }

            [Fact]
            public void Should_warn_that_no_key_was_found()
            {
                Warnings.Should().Equal("No registry key found based on 'Missing*'");
            }

            [Fact]
            public void Should_count_none()
            {
                Messages.Should().Contain("DEBUG: Found 0 uninstall registry key(s) with SoftwareName: 'Missing*'");
            }
        }

        public class When_a_key_has_no_values : FindSpecsBase
        {
            public override void Context()
            {
                base.Context();

                SoftwareName = "*";
                Registry.AddKeyWithoutValues(MachineLocation, "Empty");
                Registry.AddKey(MachineLocation, "Named", "Some software");
            }

            [Fact]
            public void Should_not_return_it()
            {
                ResultNames.Should().Equal("Named");
            }

            [Fact]
            public void Should_count_it_as_a_key_on_the_system()
            {
                Messages.Should().Contain("DEBUG: Registry uninstall keys on system: 2");
            }
        }

        public class When_a_location_cannot_be_listed : FindSpecsBase
        {
            public override void Context()
            {
                base.Context();

                SoftwareName = "Some software";
                Registry.Locations.Remove(Wow6432NodeLocation);
                Registry.AddKey(MachineLocation, "Machine", "Some software");
                Registry.AddKey(UserLocation, "User", "Some software");
            }

            [Fact]
            public void Should_not_fail()
            {
                Error.Should().BeNull();
            }

            [Fact]
            public void Should_search_the_other_locations()
            {
                ResultNames.Should().Equal("Machine", "User");
            }

            [Fact]
            public void Should_not_report_it()
            {
                Warnings.Should().BeEmpty();
                CommandRuntime.Verify(r => r.WriteError(It.IsAny<ErrorRecord>()), Times.Never);
            }
        }

        public class When_a_key_cannot_be_read : FindSpecsBase
        {
            private string _badKey;

            public override void Context()
            {
                base.Context();

                SoftwareName = "Some software";
                Registry.AddKey(MachineLocation, "First", "Some software");
                _badKey = Registry.AddKey(MachineLocation, "Bad", "Some software");
                Registry.AddKey(MachineLocation, "Last", "Some software");
                Registry.BadKeys[_badKey] = new InvalidOperationException("Bad key");
            }

            [Fact]
            public void Should_not_fail()
            {
                Error.Should().BeNull();
            }

            [Fact]
            public void Should_return_the_other_keys()
            {
                ResultNames.Should().Equal("First", "Last");
            }

            [Fact]
            public void Should_say_which_key_was_skipped()
            {
                Messages.Should().Contain("VERBOSE: Skipping bad key: " + _badKey);
            }

            [Fact]
            public void Should_not_warn()
            {
                Warnings.Should().BeEmpty();
            }
        }

        public class When_several_keys_cannot_be_read : RegistryHelperSpecsBase
        {
            [InlineData(9, 0)]
            [InlineData(10, 1)]
            [InlineData(25, 1)]
            public void Should_warn_once_when_there_are_ten_or_more(int badKeys, int warnings)
            {
                // Clear previous test case's messages.
                Messages.Clear();

                var registry = new FakeUninstallRegistry();
                registry.AddKey(MachineLocation, "Good", "Some software");
                for (var i = 0; i < badKeys; i++)
                {
                    registry.BadKeys[registry.AddKey(MachineLocation, "Bad" + i, "Some software")] = new InvalidOperationException("Bad key");
                }

                FindUninstallKeys(registry, "Some software").Should().ContainSingle();

                Warnings.Count(warning => warning == BadKeysWarning).Should().Be(warnings);
            }
        }

        public class When_the_pipeline_is_stopped_while_reading_a_key : FindSpecsBase
        {
            public override void Context()
            {
                base.Context();

                SoftwareName = "Some software";
                var key = Registry.AddKey(MachineLocation, "Key", "Some software");
                Registry.BadKeys[key] = new PipelineStoppedException();
            }

            [Fact]
            public void Should_stop()
            {
                Error.Should().BeOfType<PipelineStoppedException>();
            }

            [Fact]
            public void Should_not_report_the_key_as_bad()
            {
                Messages.Should().NotContain(message => message.StartsWith("VERBOSE: Skipping bad key:", StringComparison.Ordinal));
            }
        }

        public class When_the_software_name_is_not_a_valid_pattern : FindSpecsBase
        {
            public override void Context()
            {
                base.Context();

                SoftwareName = "Foo[";
                Registry.AddKey(MachineLocation, "Key", "Foo");
            }

            [Fact]
            public void Should_fail()
            {
                Error.Should().BeOfType<WildcardPatternException>();
            }

            [Fact]
            public void Should_not_report_the_key_as_bad()
            {
                Messages.Should().NotContain(message => message.StartsWith("VERBOSE: Skipping bad key:", StringComparison.Ordinal));
                Warnings.Should().BeEmpty();
            }
        }
    }
}
