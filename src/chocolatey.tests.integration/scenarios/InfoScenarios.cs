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
    using chocolatey.infrastructure.platforms;
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
                MockLogger.Reset();
                // There is no info run. It is purely listing with verbose and exact set to true
                Results = Service.List(Configuration).ToList();
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
                MockLogger.Reset();

                Command.Run(Configuration);
            }
        }

        public class When_searching_for_exact_package_through_command : CommandScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";

                Configuration.Sources = "PackageOutput";
                Scenario.add_packages_to_source_location(Configuration, "installpackage.*" + NuGetConstants.PackageExtension);
            }

            [Fact]
            public void Should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.ToStringSafe());
                MockLogger.Messages[LogLevel.Info.ToStringSafe()].ShouldContain("installpackage 1.0.0");
            }

            [Fact]
            public void Should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.1.0.0" + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.ToStringSafe());
                MockLogger.Messages[LogLevel.Info.ToStringSafe()].ShouldContain(" Title: installpackage | Published: {0}\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".FormatWith(lastWriteDate));
            }

            [Fact]
            public void Should_log_package_count_as_warning()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Warn.ToStringSafe());
                MockLogger.Messages[LogLevel.Warn.ToStringSafe()].ShouldContain("1 packages found.");
            }
        }

        public class When_searching_for_exact_package_with_version_specified : CommandScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";

                Configuration.Sources = "PackageOutput";
                Scenario.add_packages_to_source_location(Configuration, "installpackage.*" + NuGetConstants.PackageExtension);

                Configuration.Version = "1.0.0";
            }

            [Fact]
            public void Should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.ToStringSafe());
                MockLogger.Messages[LogLevel.Info.ToStringSafe()].ShouldContain("installpackage 1.0.0");
            }

            [Fact]
            public void Should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.1.0.0" + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.ToStringSafe());
                MockLogger.Messages[LogLevel.Info.ToStringSafe()].ShouldContain(" Title: installpackage | Published: {0}\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".FormatWith(lastWriteDate));
            }

            [Fact]
            public void Should_log_package_count_as_warning()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Warn.ToStringSafe());
                MockLogger.Messages[LogLevel.Warn.ToStringSafe()].ShouldContain("1 packages found.");
            }
        }

        public class When_searching_for_exact_package_with_non_normalized_version_specified : CommandScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";

                Configuration.Sources = "PackageOutput";
                Scenario.add_packages_to_source_location(Configuration, "installpackage.*" + NuGetConstants.PackageExtension);

                Configuration.Version = "01.0.0.0";
            }

            [Fact]
            public void Should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.ToStringSafe());
                MockLogger.Messages[LogLevel.Info.ToStringSafe()].ShouldContain("installpackage 1.0.0");
            }

            [Fact]
            public void Should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.1.0.0" + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.ToStringSafe());
                MockLogger.Messages[LogLevel.Info.ToStringSafe()].ShouldContain(" Title: installpackage | Published: {0}\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".FormatWith(lastWriteDate));
            }

            [Fact]
            public void Should_log_package_count_as_warning()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Warn.ToStringSafe());
                MockLogger.Messages[LogLevel.Warn.ToStringSafe()].ShouldContain("1 packages found.");
            }
        }

        public class When_searching_for_non_normalized_exact_package : CommandScenariosBase
        {
            private string NonNormalizedVersion = "004.0.01.0";
            private string NormalizedVersion = "4.0.1";

            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";

                Configuration.Sources = "PackageOutput";

                Scenario.add_changed_version_package_to_source_location(Configuration, "installpackage.1.0.0" + NuGetConstants.PackageExtension, NonNormalizedVersion);
            }

            [Fact]
            public void Should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.ToStringSafe());
                MockLogger.Messages[LogLevel.Info.ToStringSafe()].ShouldContain("installpackage {0}".FormatWith(NormalizedVersion));
            }

            [Fact]
            public void Should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.{0}".FormatWith(NonNormalizedVersion) + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.ToStringSafe());
                MockLogger.Messages[LogLevel.Info.ToStringSafe()].ShouldContain(" Title: installpackage | Published: {0}\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".FormatWith(lastWriteDate));
            }

            [Fact]
            public void Should_log_package_count_as_warning()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Warn.ToStringSafe());
                MockLogger.Messages[LogLevel.Warn.ToStringSafe()].ShouldContain("1 packages found.");
            }
        }

        public class When_searching_for_non_normalized_exact_package_with_version_specified : CommandScenariosBase
        {
            private string NonNormalizedVersion = "004.0.01.0";
            private string NormalizedVersion = "4.0.1";

            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";

                Configuration.Sources = "PackageOutput";

                Scenario.add_changed_version_package_to_source_location(Configuration, "installpackage.1.0.0" + NuGetConstants.PackageExtension, NonNormalizedVersion);

                Configuration.Version = "4.0.1";
            }

            [Fact]
            public void Should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.ToStringSafe());
                MockLogger.Messages[LogLevel.Info.ToStringSafe()].ShouldContain("installpackage {0}".FormatWith(NormalizedVersion));
            }

            [Fact]
            public void Should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.{0}".FormatWith(NonNormalizedVersion) + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.ToStringSafe());
                MockLogger.Messages[LogLevel.Info.ToStringSafe()].ShouldContain(" Title: installpackage | Published: {0}\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".FormatWith(lastWriteDate));
            }

            [Fact]
            public void Should_log_package_count_as_warning()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Warn.ToStringSafe());
                MockLogger.Messages[LogLevel.Warn.ToStringSafe()].ShouldContain("1 packages found.");
            }
        }

        public class When_searching_for_non_normalized_exact_package_with_non_normalized_version_specified : CommandScenariosBase
        {
            private string NonNormalizedVersion = "004.0.01.0";
            private string NormalizedVersion = "4.0.1";

            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";

                Configuration.Sources = "PackageOutput";

                Scenario.add_changed_version_package_to_source_location(Configuration, "installpackage.1.0.0" + NuGetConstants.PackageExtension, NonNormalizedVersion);

                Configuration.Version = NonNormalizedVersion;
            }

            [Fact]
            public void Should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.ToStringSafe());
                MockLogger.Messages[LogLevel.Info.ToStringSafe()].ShouldContain("installpackage {0}".FormatWith(NormalizedVersion));
            }

            [Fact]
            public void Should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.{0}".FormatWith(NonNormalizedVersion) + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.ToStringSafe());
                MockLogger.Messages[LogLevel.Info.ToStringSafe()].ShouldContain(" Title: installpackage | Published: {0}\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".FormatWith(lastWriteDate));
            }

            [Fact]
            public void Should_log_package_count_as_warning()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Warn.ToStringSafe());
                MockLogger.Messages[LogLevel.Warn.ToStringSafe()].ShouldContain("1 packages found.");
            }
        }

        public class When_searching_for_exact_package_with_dot_relative_path_source : When_searching_for_exact_package_through_command
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
            public new void Should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.ToStringSafe());
                MockLogger.Messages[LogLevel.Info.ToStringSafe()].ShouldContain("installpackage 1.0.0");
            }

            [Fact]
            public new void Should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.1.0.0" + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Keys.ShouldContain(LogLevel.Info.ToStringSafe());
                MockLogger.Messages[LogLevel.Info.ToStringSafe()].ShouldContain(" Title: installpackage | Published: {0}\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".FormatWith(lastWriteDate));
            }

            [Fact]
            public new void Should_log_package_count_as_warning()
            {
                MockLogger.Messages.Keys.ShouldContain(LogLevel.Warn.ToStringSafe());
                MockLogger.Messages[LogLevel.Warn.ToStringSafe()].ShouldContain("1 packages found.");
            }
        }

        public class When_searching_for_exact_package_with_verbose_output : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";
                Configuration.Sources = "PackageOutput";
                Scenario.add_packages_to_source_location(Configuration, "installpackage.*" + NuGetConstants.PackageExtension);
            }

            [Fact]
            public void Should_show_only_one_result()
            {
                Results.Count.ShouldEqual(1, "Expected 1 package to be returned!");
            }

            [Fact]
            public void Should_set_exit_code_to_zero()
            {
                Results[0].ExitCode.ShouldEqual(0);
            }

            [Fact]
            public void Should_not_be_reported_as_inconclusive()
            {
                Results[0].Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_report_expected_name()
            {
                Results[0].Name.ShouldEqual("installpackage");
            }

            [Fact]
            public void Should_set_source_to_expected_value()
            {
                Results[0].Source.ShouldEqual(
                    ((Platform.GetPlatform() == PlatformType.Windows ? "file:///" : "file://") + Path.Combine(Environment.CurrentDirectory, "PackageOutput"))
                    .Replace("\\","/"));
            }

            [Fact]
            public void Should_set_expected_version()
            {
                Results[0].Version.ShouldEqual("1.0.0");
            }
        }

        [Categories.SourcePriority]
        public class When_searching_for_a_package_in_a_priority_source : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "test-package";
                Configuration.Sources = Scenario.add_packages_to_priority_source_location(Configuration, "test-package.*" + NuGetConstants.PackageExtension, priority: 1);
            }

            [Fact]
            public void Should_show_only_one_result()
            {
                Results.Count.ShouldEqual(1, "Expected 1 package to be returned!");
            }

            [Fact]
            public void Should_set_exit_code_to_zero()
            {
                Results[0].ExitCode.ShouldEqual(0);
            }

            [Fact]
            public void Should_not_be_reported_as_inconclusive()
            {
                Results[0].Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_report_expected_name()
            {
                Results[0].Name.ShouldEqual("test-package");
            }

            [Fact]
            public void Should_set_source_to_expected_value()
            {
                var expectedSource = "file:///" + Path.Combine(
                    Scenario.get_top_level(),
                    "PrioritySources",
                    "Priority1").Replace('\\', '/');

                Results[0].Source.ShouldEqual(expectedSource);
            }

            [Fact]
            public void Should_set_expected_version()
            {
                Results[0].Version.ShouldEqual("0.1.0");
            }
        }

        [Categories.SourcePriority]
        public class When_searching_for_a_package_in_and_prioritised_source_has_lower_version : ScenariosBase
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
            public void Should_show_only_one_result()
            {
                Results.Count.ShouldEqual(1, "Expected 1 package to be returned!");
            }

            [Fact]
            public void Should_set_exit_code_to_zero()
            {
                Results[0].ExitCode.ShouldEqual(0);
            }

            [Fact]
            public void Should_not_be_reported_as_inconclusive()
            {
                Results[0].Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_report_expected_name()
            {
                Results[0].Name.ShouldEqual("upgradepackage");
            }

            [Fact]
            public void Should_set_source_to_expected_value()
            {
                var expectedSource = "file:///" + Path.Combine(
                    Scenario.get_top_level(),
                    "PrioritySources",
                    "Priority1").Replace('\\', '/');

                Results[0].Source.ShouldEqual(expectedSource);
            }

            [Fact]
            public void Should_set_expected_version()
            {
                Results[0].Version.ShouldEqual("1.0.0");
            }
        }
    }
}
