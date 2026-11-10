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
using System.ComponentModel;
using chocolatey.infrastructure.information;
using FluentAssertions;

namespace chocolatey.tests.infrastructure.information
{
    public class ProcessInformationSpecs
    {
        public abstract class ProcessInformationSpecsBase : TinySpec
        {
            public override void Context()
            {
            }

            protected class FakeProcess : IProcessInfo
            {
                public int Id { get; set; }

                public string ProcessName { get; set; }
            }

            protected static FakeProcess CreateProcessWithId(int id, string name)
            {
                return new FakeProcess { Id = id, ProcessName = name };
            }
        }

        public class When_populating_process_tree_with_no_cycle : ProcessInformationSpecsBase
        {
            public ProcessTree Result;

            public override void Context()
            {
            }

            public override void Because()
            {
                var processA = CreateProcessWithId(1, "processA");
                var processB = CreateProcessWithId(2, "processB");
                var processC = CreateProcessWithId(3, "processC");

                Func<IProcessInfo, IProcessInfo> getParent = p =>
                {
                    if (p.Id == 1) return processB;
                    if (p.Id == 2) return processC;
                    return null;
                };

                Result = ProcessInformation.PopulateProcessTree(
                    new ProcessTree("processA"), processA, getParent);
            }

            [Fact]
            public void Should_contain_all_parent_processes()
            {
                Result.Processes.Count.Should().Be(2);
            }

            [Fact]
            public void Should_have_processB_as_first_parent()
            {
                Result.Processes.First.Value.Should().Be("processB");
            }

            [Fact]
            public void Should_have_processC_as_last_parent()
            {
                Result.Processes.Last.Value.Should().Be("processC");
            }
        }

        public class When_populating_process_tree_with_a_cycle : ProcessInformationSpecsBase
        {
            public ProcessTree Result;

            public override void Context()
            {
            }

            public override void Because()
            {
                var processA = CreateProcessWithId(1, "processA");
                var processB = CreateProcessWithId(2, "processB");
                var processC = CreateProcessWithId(3, "processC");

                Func<IProcessInfo, IProcessInfo> getParent = p =>
                {
                    if (p.Id == 1) return processB;
                    if (p.Id == 2) return processC;
                    if (p.Id == 3) return processA;
                    return null;
                };

                Result = ProcessInformation.PopulateProcessTree(
                    new ProcessTree("processA"), processA, getParent);
            }

            [Fact]
            public void Should_not_loop_indefinitely()
            {
                Result.Processes.Count.Should().BeLessOrEqualTo(3);
            }

            [Fact]
            public void Should_stop_before_revisiting_a_pid()
            {
                Result.Processes.Count.Should().Be(2);
            }
        }

        public class When_populating_process_tree_with_access_denied : ProcessInformationSpecsBase
        {
            public ProcessTree Result;

            public override void Context()
            {
            }

            public override void Because()
            {
                var processA = CreateProcessWithId(1, "processA");
                var processB = CreateProcessWithId(2, "processB");

                Func<IProcessInfo, IProcessInfo> getParent = p =>
                {
                    if (p.Id == 1) return processB;
                    if (p.Id == 2) throw new Win32Exception(5);
                    return null;
                };

                Result = ProcessInformation.PopulateProcessTree(
                    new ProcessTree("processA"), processA, getParent);
            }

            [Fact]
            public void Should_catch_access_denied_and_continue()
            {
                Result.Processes.Count.Should().Be(1);
            }

            [Fact]
            public void Should_have_added_process_before_exception()
            {
                Result.Processes.First.Value.Should().Be("processB");
            }
        }

        public class When_populating_process_tree_with_win32_exception : ProcessInformationSpecsBase
        {
            public Win32Exception ResultException;

            public override void Context()
            {
            }

            public override void Because()
            {
                var processA = CreateProcessWithId(1, "processA");
                var processB = CreateProcessWithId(2, "processB");

                Func<IProcessInfo, IProcessInfo> getParent = p =>
                {
                    if (p.Id == 1) return processB;
                    if (p.Id == 2) throw new Win32Exception(42);
                    return null;
                };

                Action act = () => ProcessInformation.PopulateProcessTree(
                    new ProcessTree("processA"), processA, getParent);

                ResultException = act.Should().Throw<Win32Exception>().Which;
            }

            [Fact]
            public void Should_rethrow_the_exception()
            {
                ResultException.NativeErrorCode.Should().Be(42);
            }
        }
    }
}
