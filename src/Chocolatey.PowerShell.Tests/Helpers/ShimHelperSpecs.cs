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
using System.IO;
using System.Management.Automation;
using System.Text;
using Chocolatey.PowerShell.Helpers;
using FluentAssertions;
using Moq;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    public class ShimHelperSpecs
    {
        /// <summary>
        /// Gives <see cref="ShimHelper"/> a temporary install folder with a bin folder and a placeholder
        /// shimgen, a fake that stands in for running shimgen, and a cmdlet whose ShouldProcess calls answer
        /// <see cref="ShouldProcess"/>.
        /// </summary>
        public abstract class ShimHelperSpecsBase : TinySpec
        {
            protected string Root;
            protected string BinFolder;
            protected string ShimGenPath;
            protected Mock<ICommandRuntime> CommandRuntime;
            protected TestCmdlet Cmdlet;

            protected bool ShouldProcess = true;
            protected int ShimGenExitCode;
            protected bool ShimGenCreatesShim = true;
            protected IList<string> ShimGenOutput = new List<string>();
            protected string ShimGenCalledWith;
            protected string ShimGenArguments;

            protected string Name = "tool";
            protected string BatchFile => Path.Combine(BinFolder, $"{Name}.bat");
            protected string BashFile => Path.Combine(BinFolder, Name);
            protected string ShimFile => Path.Combine(BinFolder, $"{Name}.exe");

            public override void Context()
            {
                Root = Path.Combine(Path.GetTempPath(), "ChocolateyTests", Guid.NewGuid().ToString("N")) + @"\";
                BinFolder = Path.Combine(Root, "bin");
                ShimGenPath = Path.Combine(Root, "tools", "shimgen.exe");

                Directory.CreateDirectory(BinFolder);
                Directory.CreateDirectory(Path.GetDirectoryName(ShimGenPath));
                File.WriteAllText(ShimGenPath, string.Empty);

                CommandRuntime = new Mock<ICommandRuntime>();
                CommandRuntime
                    .Setup(r => r.ShouldProcess(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(() => ShouldProcess);
                Cmdlet = new TestCmdlet { CommandRuntime = CommandRuntime.Object };
            }

            public override void AfterObservations()
            {
                Directory.Delete(Root, recursive: true);
            }

            protected void Create(string path, bool useStart = false, string command = "", bool debug = false)
            {
                ShimHelper.Create(Cmdlet, Name, path, useStart, command, GetEnvironment(debug));
            }

            protected void Remove(string path)
            {
                ShimHelper.Remove(Cmdlet, Name, path, Root);
            }

            private ShimGenConfig GetEnvironment(bool debug)
            {
                return new ShimGenConfig
                {
                    InstallRoot = Root,
                    Path = ShimGenPath,
                    Debug = debug,
                    RunAction = RunShimGen,
                };
            }

            protected void VerifyHost(string message)
            {
                // With no host, PSHelper.WriteHost writes to the verbose stream instead.
                CommandRuntime.Verify(r => r.WriteVerbose(message), Times.Once);
            }

            protected void VerifyDebug(string message)
            {
                CommandRuntime.Verify(r => r.WriteDebug(message), Times.Once);
            }

            private (int ExitCode, IList<string> Output) RunShimGen(string shimGenPath, string arguments)
            {
                ShimGenCalledWith = shimGenPath;
                ShimGenArguments = arguments;

                if (ShimGenCreatesShim)
                {
                    File.WriteAllText(ShimFile, "shim");
                }

                return (ShimGenExitCode, ShimGenOutput);
            }
        }

        public class When_getting_the_relative_target : TinySpec
        {
            public override void Context()
            {
            }

            public override void Because()
            {
            }

            [InlineData(@"C:\Choco\", @"C:\Choco\lib\pkg\tool.exe", @"..\lib\pkg\tool.exe")]
            [InlineData(@"C:\Choco", @"C:\Choco\lib\pkg\tool.exe", @"..\lib\pkg\tool.exe")]
            [InlineData(@"C:\Choco\", @"c:\CHOCO\Lib\Pkg\Tool.EXE", @"..\Lib\Pkg\Tool.EXE")]
            [InlineData(@"C:\Choco\", @"C:\Choco\lib\\pkg\tool.exe", @"..\lib\pkg\tool.exe")]
            [InlineData(@"C:\Choco\", @"C:\Other\Tool.exe", @"C:\Other\Tool.exe")]
            [InlineData(@"C:\Choco\", @"C:\Chocolatey\tool.exe", @"C:\Chocolatey\tool.exe")]
            [InlineData(@"C:\Choco\", @"D:\Backup\C:\Choco\tool.exe", @"D:\Backup\C:\Choco\tool.exe")]
            [InlineData(@"C:\Choco\", @"..\lib\pkg\tool.exe", @"..\lib\pkg\tool.exe")]
            public void Should_make_paths_in_the_install_folder_relative_to_the_bin_folder(string installRoot, string path, string expected)
            {
                ShimHelper.GetRelativeTarget(installRoot, path).Should().Be(expected);
            }
        }

        public class When_getting_the_shimgen_arguments : TinySpec
        {
            private const string Expected = @"-o ""C:\Choco\bin\tool.exe"" -p ""..\lib\tool.exe"" -i ""C:\Choco\lib\tool.exe""";

            public override void Context()
            {
            }

            public override void Because()
            {
            }

            [InlineData(false, "", false, Expected)]
            [InlineData(false, null, false, Expected)]
            [InlineData(true, "", false, Expected + " -gui")]
            [InlineData(false, "-jar app.jar", false, Expected + " -c -jar app.jar")]
            [InlineData(false, "", true, Expected + " -debug")]
            [InlineData(true, "-jar app.jar", true, Expected + " -c -jar app.jar -gui -debug")]
            public void Should_include_each_option(bool useStart, string command, bool debug, string expected)
            {
                ShimHelper.GetShimGenArguments(@"C:\Choco\bin\tool.exe", @"..\lib\tool.exe", @"C:\Choco\lib\tool.exe", useStart, command, debug)
                    .Should().Be(expected);
            }
        }

        public class When_shimgen_creates_the_shim : ShimHelperSpecsBase
        {
            private string _path;

            public override void Context()
            {
                base.Context();
                _path = Path.Combine(Root, @"Lib\Pkg\Tools\Tool.EXE");
                ShimGenOutput = new List<string> { "first line", "ShimGen has successfully created it" };

                File.WriteAllText(BatchFile, "old batch");
                File.WriteAllText(BashFile, "old bash");
                File.SetAttributes(BashFile, FileAttributes.ReadOnly);
            }

            public override void Because()
            {
                Create(_path, debug: true);
            }

            [Fact]
            public void Should_run_shimgen()
            {
                ShimGenCalledWith.Should().Be(ShimGenPath);
            }

            [Fact]
            public void Should_pass_the_shim_the_target_relative_to_the_bin_folder()
            {
                ShimGenArguments.Should().Be($@"-o ""{ShimFile}"" -p ""..\Lib\Pkg\Tools\Tool.EXE"" -i ""{_path}"" -debug");
            }

            [Fact]
            public void Should_write_where_shimgen_was_found()
            {
                VerifyDebug($"ShimGen found at '{ShimGenPath}'");
            }

            [Fact]
            public void Should_write_the_shimgen_call()
            {
                VerifyDebug($"Calling {ShimGenPath} {ShimGenArguments}");
            }

            [Fact]
            public void Should_write_the_shimgen_output()
            {
                VerifyDebug(" [ShimGen] first line");
                VerifyDebug(" [ShimGen] ShimGen has successfully created it");
            }

            [Fact]
            public void Should_say_the_shim_was_added()
            {
                VerifyHost($@"Added {ShimFile} shim pointed to '..\Lib\Pkg\Tools\Tool.EXE'.");
            }

            [Fact]
            public void Should_remove_existing_redirect_files()
            {
                File.Exists(BatchFile).Should().BeFalse();
                File.Exists(BashFile).Should().BeFalse();
            }

            [Fact]
            public void Should_not_warn()
            {
                CommandRuntime.Verify(r => r.WriteWarning(It.IsAny<string>()), Times.Never);
            }
        }

        public class When_shimgen_fails : ShimHelperSpecsBase
        {
            private string _path;

            public override void Context()
            {
                base.Context();
                _path = Path.Combine(Root, @"Lib\Pkg\Tools\Tool.EXE");
                ShimGenExitCode = 1;
                ShimGenCreatesShim = false;
            }

            public override void Because()
            {
                Create(_path);
            }

            [Fact]
            public void Should_write_the_exit_code()
            {
                VerifyDebug("ShimGen exited with code 1.");
            }

            [Fact]
            public void Should_warn_that_it_is_using_the_old_method()
            {
                CommandRuntime.Verify(r => r.WriteWarning("An error occurred generating shim, using old method."), Times.Once);
            }

            [Fact]
            public void Should_say_which_redirect_files_it_adds()
            {
                VerifyHost($@"Adding {BatchFile} and pointing to '%DIR%..\Lib\Pkg\Tools\Tool.EXE'.");
                VerifyHost($@"Adding {BashFile} and pointing to '%DIR%..\Lib\Pkg\Tools\Tool.EXE'.");
            }

            [Fact]
            public void Should_write_a_batch_file_that_waits_for_the_target()
            {
                File.ReadAllBytes(BatchFile).Should().Equal(Encoding.ASCII.GetBytes(
                    "@echo off\r\nSET DIR=%~dp0%\r\ncmd /c \"\"%DIR%..\\Lib\\Pkg\\Tools\\Tool.EXE\" %*\"\r\nexit /b %ERRORLEVEL%\r\n"));
            }

            [Fact]
            public void Should_write_a_bash_file_that_waits_for_the_target()
            {
                File.ReadAllBytes(BashFile).Should().Equal(Encoding.ASCII.GetBytes(
                    "#!/bin/sh\nDIR=${0%/*}\n\"$DIR/../Lib/Pkg/Tools/Tool.EXE\" \"$@\"\nexit $?\n"));
            }

            [Fact]
            public void Should_not_say_the_shim_was_added()
            {
                CommandRuntime.Verify(r => r.WriteVerbose(It.Is<string>(s => s.StartsWith("Added "))), Times.Never);
            }
        }

        public class When_shimgen_fails_and_an_old_shim_exists : ShimHelperSpecsBase
        {
            public override void Context()
            {
                base.Context();
                File.WriteAllText(ShimFile, "old shim");
                ShimGenExitCode = 1;
                ShimGenCreatesShim = false;
            }

            public override void Because()
            {
                Create(Path.Combine(Root, @"lib\tool.exe"));
            }

            [Fact]
            public void Should_warn_that_it_is_using_the_old_method()
            {
                CommandRuntime.Verify(r => r.WriteWarning("An error occurred generating shim, using old method."), Times.Once);
            }

            [Fact]
            public void Should_write_the_redirect_files()
            {
                File.Exists(BatchFile).Should().BeTrue();
                File.Exists(BashFile).Should().BeTrue();
            }
        }

        public class When_shimgen_is_missing : ShimHelperSpecsBase
        {
            public override void Context()
            {
                base.Context();
                File.Delete(ShimGenPath);
            }

            public override void Because()
            {
                Create(Path.Combine(Root, @"lib\tool.exe"));
            }

            [Fact]
            public void Should_not_run_shimgen()
            {
                ShimGenCalledWith.Should().BeNull();
            }

            [Fact]
            public void Should_not_write_an_exit_code()
            {
                CommandRuntime.Verify(r => r.WriteDebug(It.Is<string>(s => s.StartsWith("ShimGen exited"))), Times.Never);
            }

            [Fact]
            public void Should_warn_that_it_is_using_the_old_method()
            {
                CommandRuntime.Verify(r => r.WriteWarning("An error occurred generating shim, using old method."), Times.Once);
            }

            [Fact]
            public void Should_write_the_redirect_files()
            {
                File.Exists(BatchFile).Should().BeTrue();
                File.Exists(BashFile).Should().BeTrue();
            }
        }

        public class When_falling_back_for_a_gui_application : ShimHelperSpecsBase
        {
            public override void Context()
            {
                base.Context();
                ShimGenExitCode = 1;
                ShimGenCreatesShim = false;
            }

            public override void Because()
            {
                Create(Path.Combine(Root, @"lib\tool.exe"), useStart: true);
            }

            [Fact]
            public void Should_say_it_is_not_a_command_line_application()
            {
                VerifyHost($"Setting up {Name} as a non-command line application.");
            }

            [Fact]
            public void Should_write_a_batch_file_that_does_not_wait_for_the_target()
            {
                File.ReadAllBytes(BatchFile).Should().Equal(Encoding.ASCII.GetBytes(
                    "@echo off\r\nSET DIR=%~dp0%\r\nstart \"\" \"%DIR%..\\lib\\tool.exe\" %*\r\n"));
            }

            [Fact]
            public void Should_write_a_bash_file_that_does_not_wait_for_the_target()
            {
                File.ReadAllBytes(BashFile).Should().Equal(Encoding.ASCII.GetBytes(
                    "#!/bin/sh\nDIR=${0%/*}\n\"$DIR/../lib/tool.exe\" \"$@\" &\n"));
            }
        }

        public class When_falling_back_for_a_file_outside_the_install_folder : ShimHelperSpecsBase
        {
            public override void Context()
            {
                base.Context();
                ShimGenExitCode = 1;
                ShimGenCreatesShim = false;
            }

            public override void Because()
            {
                Create(@"C:\Other\Ext.exe");
            }

            [Fact]
            public void Should_say_which_redirect_files_it_adds()
            {
                VerifyHost($@"Adding {BatchFile} and pointing to 'C:\Other\Ext.exe'.");
            }

            [Fact]
            public void Should_point_the_batch_file_at_the_absolute_path()
            {
                File.ReadAllText(BatchFile).Should().Contain("cmd /c \"\"C:\\Other\\Ext.exe\" %*\"");
            }

            [Fact]
            public void Should_point_the_bash_file_at_the_absolute_path()
            {
                File.ReadAllText(BashFile).Should().Contain("\"C:/Other/Ext.exe\" \"$@\"");
            }
        }

        public class When_should_process_declines_to_install : ShimHelperSpecsBase
        {
            public override void Context()
            {
                base.Context();
                ShouldProcess = false;
                File.WriteAllText(BatchFile, "old batch");
            }

            public override void Because()
            {
                Create(Path.Combine(Root, @"lib\tool.exe"));
            }

            [Fact]
            public void Should_ask_to_create_the_shim()
            {
                CommandRuntime.Verify(r => r.ShouldProcess(ShimFile, "Create shim"), Times.Once);
            }

            [Fact]
            public void Should_not_run_shimgen()
            {
                ShimGenCalledWith.Should().BeNull();
            }

            [Fact]
            public void Should_not_remove_existing_redirect_files()
            {
                File.Exists(BatchFile).Should().BeTrue();
            }
        }

        public class When_uninstalling_a_bin_file : ShimHelperSpecsBase
        {
            private const string Target = @"%DIR%..\Lib\Pkg\Tools\Tool.EXE";

            public override void Context()
            {
                base.Context();
                File.WriteAllText(BatchFile, "batch");
                File.WriteAllText(BashFile, "bash");
                File.WriteAllText(ShimFile, "shim");
            }

            public override void Because()
            {
                Remove(Path.Combine(Root, @"Lib\Pkg\Tools\Tool.EXE"));
            }

            [Fact]
            public void Should_remove_all_three_files()
            {
                File.Exists(BatchFile).Should().BeFalse();
                File.Exists(BashFile).Should().BeFalse();
                File.Exists(ShimFile).Should().BeFalse();
            }

            [Fact]
            public void Should_say_what_it_removes()
            {
                VerifyHost($"Removing batch file {BatchFile} which pointed to '{Target}'.");
                VerifyHost($"Removing bash file {BashFile} which pointed to '{Target}'.");
                VerifyHost($"Removing shim {ShimFile} which pointed to '{Target}'.");
            }

            [Fact]
            public void Should_write_what_it_attempts()
            {
                VerifyDebug($"Attempting to remove the batch and bash shortcuts: {BatchFile} and {BashFile}");
                VerifyDebug($"Attempting to remove the shim: {ShimFile}");
            }
        }

        public class When_uninstalling_a_bin_file_that_does_not_exist : ShimHelperSpecsBase
        {
            public override void Because()
            {
                Remove(string.Empty);
            }

            [Fact]
            public void Should_write_that_each_file_was_already_removed()
            {
                VerifyDebug($"Tried to remove batch file {BatchFile} but it was already removed.");
                VerifyDebug($"Tried to remove bash file {BashFile} but it was already removed.");
                VerifyDebug($"Tried to remove shim {ShimFile} but it was already removed.");
            }

            [Fact]
            public void Should_not_write_to_the_host()
            {
                CommandRuntime.Verify(r => r.WriteVerbose(It.IsAny<string>()), Times.Never);
            }
        }

        public class When_uninstalling_with_a_path_outside_the_install_folder : ShimHelperSpecsBase
        {
            public override void Context()
            {
                base.Context();
                File.WriteAllText(ShimFile, "shim");
            }

            public override void Because()
            {
                Remove(@"C:\Other\Ext.exe");
            }

            [Fact]
            public void Should_show_the_path_unchanged()
            {
                VerifyHost($@"Removing shim {ShimFile} which pointed to 'C:\Other\Ext.exe'.");
            }
        }

        public class When_uninstalling_without_a_path : ShimHelperSpecsBase
        {
            public override void Context()
            {
                base.Context();
                File.WriteAllText(ShimFile, "shim");
            }

            public override void Because()
            {
                Remove(null);
            }

            [Fact]
            public void Should_show_an_empty_path()
            {
                VerifyHost($"Removing shim {ShimFile} which pointed to ''.");
            }
        }

        public class When_should_process_declines_to_uninstall : ShimHelperSpecsBase
        {
            public override void Context()
            {
                base.Context();
                ShouldProcess = false;
                File.WriteAllText(BatchFile, "batch");
                File.WriteAllText(ShimFile, "shim");
            }

            public override void Because()
            {
                Remove(string.Empty);
            }

            [Fact]
            public void Should_ask_to_remove_each_existing_file()
            {
                CommandRuntime.Verify(r => r.ShouldProcess(BatchFile, "Remove batch file"), Times.Once);
                CommandRuntime.Verify(r => r.ShouldProcess(ShimFile, "Remove shim"), Times.Once);
            }

            [Fact]
            public void Should_not_remove_the_files()
            {
                File.Exists(BatchFile).Should().BeTrue();
                File.Exists(ShimFile).Should().BeTrue();
            }

            [Fact]
            public void Should_not_write_to_the_host()
            {
                CommandRuntime.Verify(r => r.WriteVerbose(It.IsAny<string>()), Times.Never);
            }
        }
    }
}
