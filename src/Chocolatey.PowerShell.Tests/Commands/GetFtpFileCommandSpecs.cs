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
using System.Management.Automation;
using Chocolatey.PowerShell.Commands;
using FluentAssertions;
using Moq;

namespace Chocolatey.PowerShell.Tests.Commands
{
    // ReSharper disable InconsistentNaming

    public class GetFtpFileCommandSpecs
    {
        public class TestGetFtpFileCommand : GetFtpFileCommand
        {
            public List<KeyValuePair<string, string>> Copies { get; } = new List<KeyValuePair<string, string>>();

            public void RunEnd()
            {
                End();
            }

            protected override void CopyLocalFile(string source, string destination)
            {
                Copies.Add(new KeyValuePair<string, string>(source, destination));
            }
        }

        public abstract class FtpFileCommandSpecsBase : TinySpec
        {
            protected const string SourcePath = @"C:\downloads\installer.msi";

            protected Mock<ICommandRuntime> CommandRuntime;
            protected TestGetFtpFileCommand Command;
            protected Exception Error;

            protected abstract string Address { get; }

            protected virtual string Destination
            {
                get { return @"C:\temp\copied.msi"; }
            }

            public override void Context()
            {
                CommandRuntime = new Mock<ICommandRuntime>();
                Command = new TestGetFtpFileCommand
                {
                    CommandRuntime = CommandRuntime.Object,
                    Url = Address,
                    Path = Destination,
                };
            }

            public override void Because()
            {
                try
                {
                    Command.RunEnd();
                }
                catch (Exception error)
                {
                    Error = error;
                }
            }
        }


        public class When_the_address_is_a_local_file : FtpFileCommandSpecsBase
        {
            protected override string Address
            {
                get { return new Uri(SourcePath).AbsoluteUri; }
            }

            [Fact]
            public void Should_copy_the_file_from_the_local_path_to_the_destination()
            {
                Command.Copies.Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, string>(SourcePath, @"C:\temp\copied.msi"));
            }

            [Fact]
            public void Should_not_make_a_request()
            {
                Error.Should().BeNull();
            }
        }

        public class When_the_address_is_the_local_file_that_is_the_destination : FtpFileCommandSpecsBase
        {
            protected override string Address
            {
                get { return new Uri(SourcePath).AbsoluteUri; }
            }

            protected override string Destination
            {
                get { return SourcePath; }
            }

            [Fact]
            public void Should_not_copy_the_file()
            {
                Command.Copies.Should().BeEmpty();
            }

            [Fact]
            public void Should_not_make_a_request()
            {
                Error.Should().BeNull();
            }
        }
    }

    // ReSharper restore InconsistentNaming
}