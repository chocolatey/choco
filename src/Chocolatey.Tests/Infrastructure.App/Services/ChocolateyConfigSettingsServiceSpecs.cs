namespace Chocolatey.Tests.Integration.Infrastructure.App.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Chocolatey.Infrastructure.App;
    using Chocolatey.Infrastructure.App.Configuration;
    using Chocolatey.Infrastructure.App.Services;
    using Chocolatey.Infrastructure.Services;
    using Moq;
    using Should;
    using Assert = Should.Core.Assertions.Assert;

    public class ChocolateyConfigSettingsServiceSpecs
    {
        public abstract class ChocolateyConfigSettingsServiceSpecsBase : TinySpec
        {
            protected ChocolateyConfigSettingsService Service;
            protected readonly Mock<IXmlService> XmlService = new Mock<IXmlService>();

            public override void Context()
            {
                XmlService.ResetCalls();
            }
        }

        public class When_ChocolateyConfigSettingsService_disables_available_feature : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    FeatureCommand = new FeatureCommandConfiguration()
                    {
                        Name = ApplicationParameters.Features.ChecksumFiles
                    }
                };

                Service.DisableFeature(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Features = new HashSet<ConfigFileFeatureSetting>()
                        {
                            new ConfigFileFeatureSetting()
                            {
                                Name = ApplicationParameters.Features.ChecksumFiles
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_not_report_feature_being_unsupported()
            {
                MockLogger.Messages["Warn"].ShouldNotContain("Feature '{0}' is not supported. Any change have no effect on running Chocolatey.".FormatWith(ApplicationParameters.Features.ChecksumFiles));
            }

            [Fact]
            public void Should_report_feature_being_disabled()
            {
                MockLogger.Messages.Keys.ShouldContain("Warn");
                MockLogger.Messages["Warn"].ShouldContain("Disabled {0}".FormatWith(ApplicationParameters.Features.ChecksumFiles));
            }

            [Fact]
            public void Should_serialize_feature_correctly()
            {
                XmlService.Verify(x => x.Serialize(It.Is<ConfigFileSettings>(config =>
                    config.Features.Any(f =>
                        f.Name == ApplicationParameters.Features.ChecksumFiles && f.SetExplicitly && !f.Enabled)), ApplicationParameters.GlobalConfigFileLocation), Times.Once);
            }
        }

        public class When_ChocolateyConfigSettingsService_disables_unknown_feature : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Features = new HashSet<ConfigFileFeatureSetting>()
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_not_contain_any_warnings()
            {
                MockLogger.Messages.Keys.ShouldNotContain("Warn");
            }

            [Fact]
            public void Should_throw_exception_on_unknown_feature()
            {
                Assert.ThrowsDelegate action = () =>
                {
                    var config = new ChocolateyConfiguration()
                    {
                        FeatureCommand = new FeatureCommandConfiguration()
                        {
                            Name = "unknown"
                        }
                    };

                    Service.DisableFeature(config);
                };

                Assert.Throws<ApplicationException>(action)
                    .Message.ShouldEqual("Feature 'unknown' not found");
            }
        }

        public class When_ChocolateyConfigSettingsService_disables_unsupported_feature : ChocolateyConfigSettingsServiceSpecsBase
        {
            private const string FeatureName = "scriptsCheckLastExitCode";

            public override void Because()
            {
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Features = new HashSet<ConfigFileFeatureSetting>()
                        {
                            new ConfigFileFeatureSetting()
                            {
                                Name = FeatureName
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_throw_exception_on_unsupported_feature()
            {
                Assert.Throws<ApplicationException>(() =>
                {
                    var config = new ChocolateyConfiguration()
                    {
                        FeatureCommand = new FeatureCommandConfiguration()
                        {
                            Name = FeatureName
                        }
                    };

                    Service.DisableFeature(config);
                }).Message.ShouldEqual("Feature '{0}' is not supported.".FormatWith(FeatureName));
            }
        }

        public class When_ChocolateyConfigSettingsService_enables_available_feature : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    FeatureCommand = new FeatureCommandConfiguration()
                    {
                        Name = ApplicationParameters.Features.ChecksumFiles
                    }
                };

                Service.EnableFeature(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Features = new HashSet<ConfigFileFeatureSetting>()
                        {
                            new ConfigFileFeatureSetting()
                            {
                                Name = ApplicationParameters.Features.ChecksumFiles
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_not_report_feature_being_unsupported()
            {
                MockLogger.Messages["Warn"].ShouldNotContain("Feature '{0}' is not supported. Any change have no effect on running Chocolatey.".FormatWith(ApplicationParameters.Features.ChecksumFiles));
            }

            [Fact]
            public void Should_report_feature_being_enabled()
            {
                MockLogger.Messages.Keys.ShouldContain("Warn");
                MockLogger.Messages["Warn"].ShouldContain("Enabled {0}".FormatWith(ApplicationParameters.Features.ChecksumFiles));
            }

            [Fact]
            public void Should_serialize_feature_correctly()
            {
                XmlService.Verify(x => x.Serialize(It.Is<ConfigFileSettings>(config =>
                    config.Features.Any(f =>
                        f.Name == ApplicationParameters.Features.ChecksumFiles && f.SetExplicitly && f.Enabled)), ApplicationParameters.GlobalConfigFileLocation), Times.Once);
            }
        }

        public class When_ChocolateyConfigSettingsService_enables_unknown_feature : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Features = new HashSet<ConfigFileFeatureSetting>()
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_not_contain_any_warnings()
            {
                MockLogger.Messages.Keys.ShouldNotContain("Warn");
            }

            [Fact]
            public void Should_throw_exception_on_unknown_feature()
            {
                Assert.ThrowsDelegate action = () =>
                {
                    var config = new ChocolateyConfiguration()
                    {
                        FeatureCommand = new FeatureCommandConfiguration()
                        {
                            Name = "unknown"
                        }
                    };

                    Service.EnableFeature(config);
                };

                Assert.Throws<ApplicationException>(action)
                    .Message.ShouldEqual("Feature 'unknown' not found");
            }
        }

        public class When_ChocolateyConfigSettingsService_enables_unsupported_feature : ChocolateyConfigSettingsServiceSpecsBase
        {
            private const string FeatureName = "scriptsCheckLastExitCode";

            public override void Because()
            {
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Features = new HashSet<ConfigFileFeatureSetting>()
                        {
                            new ConfigFileFeatureSetting()
                            {
                                Name = FeatureName
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_throw_exception_on_unsupported_feature()
            {
                Assert.Throws<ApplicationException>(() =>
                {
                    var config = new ChocolateyConfiguration()
                    {
                        FeatureCommand = new FeatureCommandConfiguration()
                        {
                            Name = FeatureName
                        }
                    };

                    Service.EnableFeature(config);
                }).Message.ShouldEqual("Feature '{0}' is not supported.".FormatWith(FeatureName));
            }
        }
    }
}
