namespace chocolatey.tests.integration.infrastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.services;
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

        public class when_ChocolateyConfigSettingsService_disables_available_feature : ChocolateyConfigSettingsServiceSpecsBase
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

                Service.feature_disable(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
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
            public void should_not_report_feature_being_unsupported()
            {
                MockLogger.Messages["Warn"].ShouldNotContain("Feature '{0}' is not supported. Any change have no effect on running Chocolatey.".format_with(ApplicationParameters.Features.ChecksumFiles));
            }

            [Fact]
            public void should_report_feature_being_disabled()
            {
                MockLogger.Messages.Keys.ShouldContain("Warn");
                MockLogger.Messages["Warn"].ShouldContain("Disabled {0}".format_with(ApplicationParameters.Features.ChecksumFiles));
            }

            [Fact]
            public void should_serialize_feature_correctly()
            {
                XmlService.Verify(x => x.serialize(It.Is<ConfigFileSettings>(config =>
                    config.Features.Any(f =>
                        f.Name == ApplicationParameters.Features.ChecksumFiles && f.SetExplicitly && !f.Enabled)), ApplicationParameters.GlobalConfigFileLocation), Times.Once);
            }
        }

        public class when_ChocolateyConfigSettingsService_disables_unknown_feature : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Features = new HashSet<ConfigFileFeatureSetting>()
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void should_not_contain_any_warnings()
            {
                MockLogger.Messages.Keys.ShouldNotContain("Warn");
            }

            [Fact]
            public void should_throw_exception_on_unknown_feature()
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

                    Service.feature_disable(config);
                };

                Assert.Throws<ApplicationException>(action)
                    .Message.ShouldEqual("Feature 'unknown' not found");
            }
        }

        public class when_ChocolateyConfigSettingsService_disables_unsupported_feature : ChocolateyConfigSettingsServiceSpecsBase
        {
            private const string FEATURE_NAME = "scriptsCheckLastExitCode";

            public override void Because()
            {
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Features = new HashSet<ConfigFileFeatureSetting>()
                        {
                            new ConfigFileFeatureSetting()
                            {
                                Name = FEATURE_NAME
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void should_throw_exception_on_unsupported_feature()
            {
                Assert.Throws<ApplicationException>(() =>
                {
                    var config = new ChocolateyConfiguration()
                    {
                        FeatureCommand = new FeatureCommandConfiguration()
                        {
                            Name = FEATURE_NAME
                        }
                    };

                    Service.feature_disable(config);
                }).Message.ShouldEqual("Feature '{0}' is not supported.".format_with(FEATURE_NAME));
            }
        }

        public class when_ChocolateyConfigSettingsService_enables_available_feature : ChocolateyConfigSettingsServiceSpecsBase
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

                Service.feature_enable(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
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
            public void should_not_report_feature_being_unsupported()
            {
                MockLogger.Messages["Warn"].ShouldNotContain("Feature '{0}' is not supported. Any change have no effect on running Chocolatey.".format_with(ApplicationParameters.Features.ChecksumFiles));
            }

            [Fact]
            public void should_report_feature_being_enabled()
            {
                MockLogger.Messages.Keys.ShouldContain("Warn");
                MockLogger.Messages["Warn"].ShouldContain("Enabled {0}".format_with(ApplicationParameters.Features.ChecksumFiles));
            }

            [Fact]
            public void should_serialize_feature_correctly()
            {
                XmlService.Verify(x => x.serialize(It.Is<ConfigFileSettings>(config =>
                    config.Features.Any(f =>
                        f.Name == ApplicationParameters.Features.ChecksumFiles && f.SetExplicitly && f.Enabled)), ApplicationParameters.GlobalConfigFileLocation), Times.Once);
            }
        }

        public class when_ChocolateyConfigSettingsService_enables_unknown_feature : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Features = new HashSet<ConfigFileFeatureSetting>()
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void should_not_contain_any_warnings()
            {
                MockLogger.Messages.Keys.ShouldNotContain("Warn");
            }

            [Fact]
            public void should_throw_exception_on_unknown_feature()
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

                    Service.feature_enable(config);
                };

                Assert.Throws<ApplicationException>(action)
                    .Message.ShouldEqual("Feature 'unknown' not found");
            }
        }

        public class when_ChocolateyConfigSettingsService_enables_unsupported_feature : ChocolateyConfigSettingsServiceSpecsBase
        {
            private const string FEATURE_NAME = "scriptsCheckLastExitCode";

            public override void Because()
            {
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Features = new HashSet<ConfigFileFeatureSetting>()
                        {
                            new ConfigFileFeatureSetting()
                            {
                                Name = FEATURE_NAME
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void should_throw_exception_on_unsupported_feature()
            {
                Assert.Throws<ApplicationException>(() =>
                {
                    var config = new ChocolateyConfiguration()
                    {
                        FeatureCommand = new FeatureCommandConfiguration()
                        {
                            Name = FEATURE_NAME
                        }
                    };

                    Service.feature_enable(config);
                }).Message.ShouldEqual("Feature '{0}' is not supported.".format_with(FEATURE_NAME));
            }
        }
    }
}