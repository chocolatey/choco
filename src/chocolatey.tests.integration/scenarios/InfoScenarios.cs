namespace chocolatey.tests.integration.scenarios
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.results;

    using NuGet;
    using NuGet.Configuration;

    using NUnit.Framework;

    using Should;

    public class InfoScenarios
    {
        [ConcernFor("info")]
        public abstract class ScenariosBase : TinySpec
        {
            protected IList<PackageResult> Results;
            protected ChocolateyConfiguration Configuration;
            protected IChocolateyPackageService Service;

            public override void Context()
            {
                Configuration = Scenario.info();
                Scenario.reset(Configuration);

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
            }

            public override void Because()
            {
                MockLogger.reset();
                // There is no info run. It is purely listing with verbose and exact set to true
                Results = Service.list_run(Configuration).ToList();
            }
        }

        [ConcernFor("info")]
        public abstract class CommandScenariosBase : TinySpec
        {
            protected ChocolateyConfiguration Configuration;
            protected ICommand Command;

            public override void Context()
            {
                Configuration = Scenario.info();
                Scenario.reset(Configuration);

                Command = NUnitSetup.Container.GetAllInstances<ICommand>()
                    .Where(c => c.GetType() == typeof(ChocolateyInfoCommand)).First();
            }

            public override void Because()
            {
                MockLogger.reset();

                Command.run(Configuration);
            }
        }

        [Broken, Pending("Need to be fixed in either NuGet.Client or before calling code in NuGet.Client")]
        public class when_searching_for_exact_package_through_command : CommandScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";

                Configuration.Sources = "PackageOutput";
                Scenario.add_packages_to_source_location(Configuration, "installpackage.*" + NuGetConstants.PackageExtension);
            }

            [Fact]
            public void should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.to_string());
                MockLogger.Messages[LogLevel.Info.to_string()].ShouldContain("installpackage 1.0.0");
            }

            [Fact]
            public void should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.1.0.0" + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.to_string());
                MockLogger.Messages[LogLevel.Info.to_string()].ShouldContain(" Title: installpackage | Published: 14.12.2022\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".format_with(lastWriteDate));
            }

            [Fact]
            public void should_log_package_count_as_warning()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Warn.to_string());
                MockLogger.Messages[LogLevel.Warn.to_string()].ShouldContain("1 packages found.");
            }
        }

        [Broken, Pending("Need to be fixed in either NuGet.Client or before calling code in NuGet.Client")]
        public class when_searching_for_exact_package_with_dot_relative_path_source : when_searching_for_exact_package_through_command
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = ".";
            }

            public override void Because()
            {
                var currentDirectory = Environment.CurrentDirectory;
                Environment.CurrentDirectory = Path.Combine(Environment.CurrentDirectory, "PackageOutput");

                try
                {
                    base.Because();
                }
                finally
                {
                    Environment.CurrentDirectory = currentDirectory;
                }
            }

            [Fact]
            public new void should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.to_string());
                MockLogger.Messages[LogLevel.Info.to_string()].ShouldContain("installpackage 1.0.0");
            }

            [Fact]
            public new void should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.1.0.0" + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.to_string());
                MockLogger.Messages[LogLevel.Info.to_string()].ShouldContain(" Title: installpackage | Published: 14.12.2022\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".format_with(lastWriteDate));
            }

            [Fact]
            public new void should_log_package_count_as_warning()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Warn.to_string());
                MockLogger.Messages[LogLevel.Warn.to_string()].ShouldContain("1 packages found.");
            }
        }

        [Broken, Pending("Need to be fixed in either NuGet.Client or before calling code in NuGet.Client")]
        public class when_searching_for_exact_package_with_verbose_output : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";
                Configuration.Sources = "PackageOutput";
                Scenario.add_packages_to_source_location(Configuration, "installpackage.*" + NuGetConstants.PackageExtension);
            }

            [Fact]
            public void should_show_only_one_result()
            {
                Results.Count.ShouldEqual(1, "Expected 1 package to be returned!");
            }

            [Fact]
            public void should_set_exit_code_to_zero()
            {
                Results[0].ExitCode.ShouldEqual(0);
            }

            [Fact]
            public void should_not_be_reported_as_inconclusive()
            {
                Results[0].Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_report_expected_name()
            {
                Results[0].Name.ShouldEqual("installpackage");
            }

            [Fact]
            public void should_set_source_to_expected_value()
            {
                Results[0].Source.ShouldEqual("PackageOutput");
            }

            [Fact]
            public void should_set_expected_version()
            {
                Results[0].Version.ShouldEqual("1.0.0");
            }
        }

        [Categories.SourcePriority]
        public class when_searching_for_a_package_in_a_priority_source : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "test-package";
                Configuration.Sources = Scenario.add_packages_to_priority_source_location(Configuration, "test-package.*" + NuGetConstants.PackageExtension, priority: 1);
            }

            [Fact]
            public void should_show_only_one_result()
            {
                Results.Count.ShouldEqual(1, "Expected 1 package to be returned!");
            }

            [Fact]
            public void should_set_exit_code_to_zero()
            {
                Results[0].ExitCode.ShouldEqual(0);
            }

            [Fact]
            public void should_not_be_reported_as_inconclusive()
            {
                Results[0].Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_report_expected_name()
            {
                Results[0].Name.ShouldEqual("test-package");
            }

            [Fact]
            public void should_set_source_to_expected_value()
            {
                var expectedSource = "file:///" + Path.Combine(
                    Scenario.get_top_level(),
                    "PrioritySources",
                    "Priority1").Replace('\\', '/');

                Results[0].Source.ShouldEqual(expectedSource);
            }

            [Fact]
            public void should_set_expected_version()
            {
                Results[0].Version.ShouldEqual("0.1.0");
            }
        }

        [Categories.SourcePriority]
        public class when_searching_for_a_package_in_and_prioritised_source_has_lower_version : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "upgradepackage";
                Configuration.Sources = string.Join(",",
                    Scenario.add_packages_to_priority_source_location(Configuration, "upgradepackage.1.0.0" + NuGetConstants.PackageExtension, priority: 1),
                    Scenario.add_packages_to_priority_source_location(Configuration,
                      "upgradepackage.1.1.0" + NuGetConstants.PackageExtension, priority: 0));
            }

            [Fact]
            public void should_show_only_one_result()
            {
                Results.Count.ShouldEqual(1, "Expected 1 package to be returned!");
            }

            [Fact]
            public void should_set_exit_code_to_zero()
            {
                Results[0].ExitCode.ShouldEqual(0);
            }

            [Fact]
            public void should_not_be_reported_as_inconclusive()
            {
                Results[0].Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_report_expected_name()
            {
                Results[0].Name.ShouldEqual("upgradepackage");
            }

            [Fact]
            public void should_set_source_to_expected_value()
            {
                var expectedSource = "file:///" + Path.Combine(
                    Scenario.get_top_level(),
                    "PrioritySources",
                    "Priority1").Replace('\\', '/');

                Results[0].Source.ShouldEqual(expectedSource);
            }

            [Fact]
            public void should_set_expected_version()
            {
                Results[0].Version.ShouldEqual("1.0.0");
            }
        }
    }
}
