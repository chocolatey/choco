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

    using FluentAssertions;

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
                Configuration = Scenario.Info();
                Scenario.Reset(Configuration);

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
                Configuration = Scenario.Info();
                Scenario.Reset(Configuration);

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
                Scenario.AddPackagesToSourceLocation(Configuration, "installpackage.*" + NuGetConstants.PackageExtension);
            }

            [Fact]
            public void Should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage 1.0.0"));
            }

            [Fact]
            public void Should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.1.0.0" + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains(" Title: installpackage | Published: {0}\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".FormatWith(lastWriteDate)));
            }

            [Fact]
            public void Should_log_package_count_as_warning()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1 packages found."));
            }
        }

        public class When_searching_for_exact_package_with_version_specified : CommandScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";

                Configuration.Sources = "PackageOutput";
                Scenario.AddPackagesToSourceLocation(Configuration, "installpackage.*" + NuGetConstants.PackageExtension);

                Configuration.Version = "1.0.0";
            }

            [Fact]
            public void Should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage 1.0.0"));
            }

            [Fact]
            public void Should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.1.0.0" + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains(" Title: installpackage | Published: {0}\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".FormatWith(lastWriteDate)));
            }

            [Fact]
            public void Should_log_package_count_as_warning()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1 packages found."));
            }
        }

        public class When_searching_for_exact_package_with_non_normalized_version_specified : CommandScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";

                Configuration.Sources = "PackageOutput";
                Scenario.AddPackagesToSourceLocation(Configuration, "installpackage.*" + NuGetConstants.PackageExtension);

                Configuration.Version = "01.0.0.0";
            }

            [Fact]
            public void Should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage 1.0.0"));
            }

            [Fact]
            public void Should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.1.0.0" + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains(" Title: installpackage | Published: {0}\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".FormatWith(lastWriteDate)));
            }

            [Fact]
            public void Should_log_package_count_as_warning()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1 packages found."));
            }
        }

        public class When_searching_for_non_normalized_exact_package : CommandScenariosBase
        {
            private const string NonNormalizedVersion = "004.0.01.0";
            private const string NormalizedVersion = "4.0.1";

            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";

                Configuration.Sources = "PackageOutput";

                Scenario.AddChangedVersionPackageToSourceLocation(Configuration, "installpackage.1.0.0" + NuGetConstants.PackageExtension, NonNormalizedVersion);
            }

            [Fact]
            public void Should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage {0}".FormatWith(NormalizedVersion)));
            }

            [Fact]
            public void Should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.{0}".FormatWith(NonNormalizedVersion) + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains(" Title: installpackage | Published: {0}\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".FormatWith(lastWriteDate)));
            }

            [Fact]
            public void Should_log_package_count_as_warning()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1 packages found."));
            }
        }

        public class When_searching_for_non_normalized_exact_package_with_version_specified : CommandScenariosBase
        {
            private const string NonNormalizedVersion = "004.0.01.0";
            private const string NormalizedVersion = "4.0.1";

            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";

                Configuration.Sources = "PackageOutput";

                Scenario.AddChangedVersionPackageToSourceLocation(Configuration, "installpackage.1.0.0" + NuGetConstants.PackageExtension, NonNormalizedVersion);

                Configuration.Version = "4.0.1";
            }

            [Fact]
            public void Should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage {0}".FormatWith(NormalizedVersion)));
            }

            [Fact]
            public void Should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.{0}".FormatWith(NonNormalizedVersion) + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains(" Title: installpackage | Published: {0}\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".FormatWith(lastWriteDate)));
            }

            [Fact]
            public void Should_log_package_count_as_warning()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1 packages found."));
            }
        }

        public class When_searching_for_non_normalized_exact_package_with_non_normalized_version_specified : CommandScenariosBase
        {
            private const string NonNormalizedVersion = "004.0.01.0";
            private const string NormalizedVersion = "4.0.1";

            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";

                Configuration.Sources = "PackageOutput";

                Scenario.AddChangedVersionPackageToSourceLocation(Configuration, "installpackage.1.0.0" + NuGetConstants.PackageExtension, NonNormalizedVersion);

                Configuration.Version = NonNormalizedVersion;
            }

            [Fact]
            public void Should_log_standalone_header_with_package_name_and_version()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage {0}".FormatWith(NormalizedVersion)));
            }

            [Fact]
            public void Should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.{0}".FormatWith(NonNormalizedVersion) + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains(" Title: installpackage | Published: {0}\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".FormatWith(lastWriteDate)));
            }

            [Fact]
            public void Should_log_package_count_as_warning()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1 packages found."));
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
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage 1.0.0"));
            }

            [Fact]
            public new void Should_log_package_information()
            {
                var lastWriteDate = File.GetLastWriteTimeUtc(Path.Combine("PackageOutput", "installpackage.1.0.0" + NuGetConstants.PackageExtension))
                    .ToShortDateString();

                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains(" Title: installpackage | Published: {0}\r\n Number of Downloads: n/a | Downloads for this version: n/a\r\n Package url\r\n Chocolatey Package Source: n/a\r\n Tags: installpackage admin\r\n Software Site: n/a\r\n Software License: n/a\r\n Summary: __REPLACE__\r\n Description: __REPLACE__\r\n".FormatWith(lastWriteDate)));
            }

            [Fact]
            public new void Should_log_package_count_as_warning()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1 packages found."));
            }
        }

        public class When_searching_for_exact_package_with_verbose_output : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "installpackage";
                Configuration.Sources = "PackageOutput";
                Scenario.AddPackagesToSourceLocation(Configuration, "installpackage.*" + NuGetConstants.PackageExtension);
            }

            [Fact]
            public void Should_show_only_one_result()
            {
                Results.Should().ContainSingle("Expected 1 package to be returned!");
            }

            [Fact]
            public void Should_set_exit_code_to_zero()
            {
                Results[0].ExitCode.Should().Be(0);
            }

            [Fact]
            public void Should_not_be_reported_as_inconclusive()
            {
                Results[0].Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_report_expected_name()
            {
                Results[0].Name.Should().Be("installpackage");
            }

            [Fact]
            public void Should_set_source_to_expected_value()
            {
                Results[0].Source.Should().Be(
                    ((Platform.GetPlatform() == PlatformType.Windows ? "file:///" : "file://") + Path.Combine(Environment.CurrentDirectory, "PackageOutput"))
                    .Replace("\\","/"));
            }

            [Fact]
            public void Should_set_expected_version()
            {
                Results[0].Version.Should().Be("1.0.0");
            }
        }

        [Categories.SourcePriority]
        public class When_searching_for_a_package_in_a_priority_source : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "test-package";
                Configuration.Sources = Scenario.AddPackagesToPrioritySourceLocation(Configuration, "test-package.*" + NuGetConstants.PackageExtension, priority: 1);
            }

            [Fact]
            public void Should_show_only_one_result()
            {
                Results.Should().ContainSingle( "Expected 1 package to be returned!");
            }

            [Fact]
            public void Should_set_exit_code_to_zero()
            {
                Results[0].ExitCode.Should().Be(0);
            }

            [Fact]
            public void Should_not_be_reported_as_inconclusive()
            {
                Results[0].Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_report_expected_name()
            {
                Results[0].Name.Should().Be("test-package");
            }

            [Fact]
            public void Should_set_source_to_expected_value()
            {
                var expectedSource = "file:///" + Path.Combine(
                    Scenario.GetTopLevel(),
                    "PrioritySources",
                    "Priority1").Replace('\\', '/');

                Results[0].Source.Should().Be(expectedSource);
            }

            [Fact]
            public void Should_set_expected_version()
            {
                Results[0].Version.Should().Be("0.1.0");
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
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "upgradepackage.1.0.0" + NuGetConstants.PackageExtension, priority: 1),
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration,
                      "upgradepackage.1.1.0" + NuGetConstants.PackageExtension, priority: 0));
            }

            [Fact]
            public void Should_show_only_one_result()
            {
                Results.Should().ContainSingle( "Expected 1 package to be returned!");
            }

            [Fact]
            public void Should_set_exit_code_to_zero()
            {
                Results[0].ExitCode.Should().Be(0);
            }

            [Fact]
            public void Should_not_be_reported_as_inconclusive()
            {
                Results[0].Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_report_expected_name()
            {
                Results[0].Name.Should().Be("upgradepackage");
            }

            [Fact]
            public void Should_set_source_to_expected_value()
            {
                var expectedSource = "file:///" + Path.Combine(
                    Scenario.GetTopLevel(),
                    "PrioritySources",
                    "Priority1").Replace('\\', '/');

                Results[0].Source.Should().Be(expectedSource);
            }

            [Fact]
            public void Should_set_expected_version()
            {
                Results[0].Version.Should().Be("1.0.0");
            }
        }
    }
}
