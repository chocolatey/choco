// Copyright © 2017 - 2018 Chocolatey Software, Inc
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

namespace chocolatey.tests.integration.infrastructure.app.shimtarget
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using chocolatey.infrastructure.app.domain;
    using NUnit.Framework;
    using Should;

    public class ShimTargetSpecs
    {
        public abstract class ShimTargetSpecsBase : TinySpec
        {
            protected string PackagePath;
            protected string RootPath;
            protected string IncludeFile;
            protected string ErrorMessage;
            protected IEnumerable<string> Results;

            public override void Context()
            {
                PackagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "infrastructure.app", "shimtarget", "pkg");
                RootPath = Path.Combine(PackagePath, "tools");
                IncludeFile = Path.Combine(RootPath, ".shiminclude");
                ErrorMessage = string.Empty;
            }

            public override void Because()
            {
                var shimManager = new ShimTargetManager(PackagePath);
                Results = shimManager.get_shim_targets();
            }

            protected bool check_results(string[] expected)
            {
                var targetsExist = true;
                ErrorMessage = string.Empty;

                foreach (var path in expected)
                {
                    var target = resolve_from_root(path);
                    if (Results.Contains(target, StringComparer.OrdinalIgnoreCase)) continue;

                    targetsExist = false;
                    ErrorMessage = "Expected file missing: {0}".format_with(target);
                    break;
                }

                return targetsExist;
            }

            protected string resolve_from_root(string relativePath)
            {
                return Path.GetFullPath(Path.Combine(RootPath, relativePath));
            }

            protected void write_shim_include(string[] content)
            {
                File.WriteAllLines(IncludeFile, content, Encoding.UTF8);
            }
        }

        [Category("Integration")]
        public class when_shiminclude_is_empty : ShimTargetSpecsBase
        {
            public override void Context()
            {
                base.Context();
                string[] lines = { };
                write_shim_include(lines);
            }

            [Fact]
            public void no_targets_should_be_returned()
            {
                Results.ShouldBeEmpty();
            }
        }

        [Category("Integration")]
        public class when_shiminclude_contains_blank_and_comment_lines : ShimTargetSpecsBase
        {
            public override void Context()
            {
                base.Context();
                string[] lines = { "# comment", "", "", "#*.exe" };
                write_shim_include(lines);
            }

            [Fact]
            public void no_targets_should_be_returned()
            {
                Results.ShouldBeEmpty();
            }
        }

        [Category("Integration")]
        public class when_including_a_target_outside_the_package_directory : ShimTargetSpecsBase
        {
            private string _target;

            public override void Context()
            {
                base.Context();
                _target = @"..\..\outside.exe";
                string[] lines = { _target };
                write_shim_include(lines);
            }

            [Fact]
            public void the_target_should_exist()
            {
                File.Exists(resolve_from_root(_target)).ShouldBeTrue();
            }

            [Fact]
            public void the_target_should_not_be_returned()
            {
                Results.ShouldBeEmpty();
            }
        }

        [Category("Integration")]
        public class when_including_a_target_in_the_package_directory : ShimTargetSpecsBase
        {
            private string _target;

            public override void Context()
            {
                base.Context();
                _target = @"..\pkg.exe";
                string[] lines = { _target };
                write_shim_include(lines);
            }

            [Fact]
            public void there_should_be_one_target_returned()
            {
                Results.Count().ShouldEqual(1);
            }

            [Fact]
            public void the_target_should_be_returned()
            {
                string[] expected = { _target };
                check_results(expected).ShouldBeTrue(ErrorMessage);
            }
        }

        [Category("Integration")]
        public class when_including_a_target_with_an_absolute_path : ShimTargetSpecsBase
        {
            private string _target;

            public override void Context()
            {
                base.Context();
                _target = resolve_from_root(@"prog\sbin\prog.exe");
                string[] lines = { _target };
                write_shim_include(lines);
            }

            [Fact]
            public void the_target_should_exist()
            {
                File.Exists(_target).ShouldBeTrue();
            }

            [Fact]
            public void the_target_should_not_be_returned()
            {
                Results.ShouldBeEmpty();
            }
        }

        [Category("Integration")]
        public class when_including_a_target_with_an_absolute_path_from_the_current_drive : ShimTargetSpecsBase
        {
            private string _target;

            public override void Context()
            {
                base.Context();
                _target = resolve_from_root(@"prog\sbin\prog.exe");
                string[] lines = { _target.Substring(3) };
                write_shim_include(lines);
            }

            [Fact]
            public void the_target_should_exist()
            {
                File.Exists(_target).ShouldBeTrue();
            }

            [Fact]
            public void the_target_should_not_be_returned()
            {
                Results.ShouldBeEmpty();
            }
        }

        [Category("Integration")]
        public class when_including_a_target_with_an_unsupported_file_extension : ShimTargetSpecsBase
        {
            private string _target;

            public override void Context()
            {
                base.Context();
                _target = @"prog\opt\opt3.vbs";
                string[] lines = { _target };
                write_shim_include(lines);
            }

            [Fact]
            public void the_target_should_exist()
            {
                File.Exists(resolve_from_root(_target)).ShouldBeTrue();
            }

            [Fact]
            public void the_target_should_not_be_returned()
            {
                Results.ShouldBeEmpty();
            }
        }

        [Category("Integration")]
        public class when_including_exe_targets_in_the_root_directory : ShimTargetSpecsBase
        {
            public override void Context()
            {
                base.Context();
                string[] lines = { "." };
                write_shim_include(lines);
            }

            [Fact]
            public void there_should_be_two_targets_returned()
            {
                Results.Count().ShouldEqual(2);
            }

            [Fact]
            public void the_exe_targets_should_be_returned()
            {
                string[] expected = { "!tools.exe", "#tools.exe" };
                check_results(expected).ShouldBeTrue(ErrorMessage);
            }
        }

        [Category("Integration")]
        public class when_including_targets_starting_with_hash_and_exclamation_mark : ShimTargetSpecsBase
        {
            public override void Context()
            {
                base.Context();
                // also test using an empty directory
                string[] lines = { @"\#tools.exe", @"\!tools.exe" };
                write_shim_include(lines);
            }

            [Fact]
            public void there_should_be_two_targets_returned()
            {
                Results.Count().ShouldEqual(2);
            }

            [Fact]
            public void the_targets_should_be_returned()
            {
                string[] expected = { "!tools.exe", "#tools.exe" };
                check_results(expected).ShouldBeTrue(ErrorMessage);
            }
        }

        [Category("Integration")]
        public class when_excluding_a_target_starting_with_exclamation_mark : ShimTargetSpecsBase
        {
            public override void Context()
            {
                base.Context();
                // also test using an empty directory
                string[] lines = { "*.exe", "!!tools.exe" };
                write_shim_include(lines);
            }

            [Fact]
            public void there_should_be_one_target_returned()
            {
                Results.Count().ShouldEqual(1);
            }

            [Fact]
            public void the_non_excluded_targets_should_be_returned()
            {
                string[] expected = { "#tools.exe" };
                check_results(expected).ShouldBeTrue(ErrorMessage);
            }
        }

        [Category("Integration")]
        public class when_including_wildcard_folders : ShimTargetSpecsBase
        {
            public override void Context()
            {
                base.Context();
                // also test forward slashes
                string[] lines = { "prog/*" };
                write_shim_include(lines);
            }

            [Fact]
            public void there_should_be_three_targets_returned()
            {
                Results.Count().ShouldEqual(3);
            }

            [Fact]
            public void the_targets_should_be_returned()
            {
                string[] expected = { @"prog\opt\opt.exe", @"prog\sbin\prog.exe", @"prog\sbin\prog2.exe" };
                check_results(expected).ShouldBeTrue(ErrorMessage);
            }
        }

        [Category("Integration")]
        public class when_including_wildcard_folders_with_other_extensions : ShimTargetSpecsBase
        {
            public override void Context()
            {
                base.Context();
                // also test forward slashes
                string[] lines = { "prog/*/*.bat", "prog/*/*.cmd" };
                write_shim_include(lines);
            }

            [Fact]
            public void there_should_be_two_targets_returned()
            {
                Results.Count().ShouldEqual(2);
            }

            [Fact]
            public void the_targets_should_be_returned()
            {
                string[] expected = { @"prog\opt\opt2.cmd", @"prog\sbin\prog3.bat" };
                check_results(expected).ShouldBeTrue(ErrorMessage);
            }
        }

        [Category("Integration")]
        public class when_including_a_specifically_ignored_target : ShimTargetSpecsBase
        {
            private string _resolvedTarget;

            public override void Context()
            {
                base.Context();
                var target = @"prog\ignore-me.exe";
                _resolvedTarget = resolve_from_root(target);
                string[] lines = { target };
                write_shim_include(lines);
            }

            [Fact]
            public void the_target_should_exist()
            {
                File.Exists(_resolvedTarget).ShouldBeTrue();
            }

            [Fact]
            public void the_target_ignore_file_should_exist()
            {
                File.Exists(_resolvedTarget + ".ignore").ShouldBeTrue();
            }

            [Fact]
            public void the_target_should_not_be_returned()
            {
                Results.ShouldBeEmpty();
            }
        }

        [Category("Integration")]
        public class when_no_shiminclude_is_found : ShimTargetSpecsBase
        {
            public override void Context()
            {
                base.Context();
                File.Delete(IncludeFile);
            }

            [Fact]
            public void the_shiminclude_file_should_not_exist()
            {
                File.Exists(IncludeFile).ShouldBeFalse();
            }

            [Fact]
            public void there_should_be_six_targets_returned()
            {
                Results.Count().ShouldEqual(6);
            }

            [Fact]
            public void the_exe_targets_should_be_returned()
            {
                string[] expected =
                {
                    @"..\pkg.exe",
                    @".\!tools.exe",
                    @".\#tools.exe",
                    @"prog\opt\opt.exe",
                    @"prog\sbin\prog.exe",
                    @"prog\sbin\prog2.exe"
                };

                check_results(expected).ShouldBeTrue(ErrorMessage);
            }
        }
    }
}
