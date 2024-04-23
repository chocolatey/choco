using System;
using System.Collections.Generic;
using System.Linq;
using chocolatey.infrastructure.app;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.nuget;
using chocolatey.infrastructure.app.services;
using chocolatey.infrastructure.services;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;

namespace chocolatey.tests.integration.infrastructure.app.services
{
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
                MockLogger.Messages["Warn"].Should().NotContain("Feature '{0}' is not supported. Any change have no effect on running Chocolatey.".FormatWith(ApplicationParameters.Features.ChecksumFiles));
            }

            [Fact]
            public void Should_report_feature_being_disabled()
            {
                MockLogger.Messages.Keys.Should().Contain("Warn");
                MockLogger.Messages["Warn"].Should().Contain("Disabled {0}".FormatWith(ApplicationParameters.Features.ChecksumFiles));
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
                MockLogger.Messages.Keys.Should().NotContain("Warn");
            }

            [Fact]
            public void Should_throw_exception_on_unknown_feature()
            {
                Action action = () =>
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

                action.Should().Throw<ApplicationException>()
                    .WithMessage("Feature 'unknown' not found");
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
                Action action = () =>
                {
                    var config = new ChocolateyConfiguration()
                    {
                        FeatureCommand = new FeatureCommandConfiguration()
                        {
                            Name = FeatureName
                        }
                    };

                    Service.DisableFeature(config);
                };
                action.Should().Throw<ApplicationException>()
                    .WithMessage("Feature '{0}' is not supported.".FormatWith(FeatureName));
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
                MockLogger.Messages["Warn"].Should().NotContain("Feature '{0}' is not supported. Any change have no effect on running Chocolatey.".FormatWith(ApplicationParameters.Features.ChecksumFiles));
            }

            [Fact]
            public void Should_report_feature_being_enabled()
            {
                MockLogger.Messages.Keys.Should().Contain("Warn");
                MockLogger.Messages["Warn"].Should().Contain("Enabled {0}".FormatWith(ApplicationParameters.Features.ChecksumFiles));
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
                MockLogger.Messages.Keys.Should().NotContain("Warn");
            }

            [Fact]
            public void Should_throw_exception_on_unknown_feature()
            {
                Action action = () =>
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

                action.Should().Throw<ApplicationException>()
                    .WithMessage("Feature 'unknown' not found");
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
                Action action = () =>
                {
                    var config = new ChocolateyConfiguration()
                    {
                        FeatureCommand = new FeatureCommandConfiguration()
                        {
                            Name = FeatureName
                        }
                    };

                    Service.EnableFeature(config);
                }
                ;
                action.Should().Throw<ApplicationException>()
                    .WithMessage("Feature '{0}' is not supported.".FormatWith(FeatureName));
            }
        }

        public class When_ChocolateyConfigSettingsService_list_feature : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true
                };

                Service.ListFeatures(config);
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
                                Name = ApplicationParameters.Features.VirusCheck,
                            },
                            new ConfigFileFeatureSetting()
                            {
                                Name = ApplicationParameters.Features.AllowEmptyChecksums
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_output_features_in_alphabetical_order()
            {
                MockLogger.Messages.Keys.Should().Contain("Info");

                var infoMessages = MockLogger.Messages["Info"];
                infoMessages.Should().HaveCount(2);
                infoMessages[0].Should().Contain("allowEmptyChecksums");
                infoMessages[1].Should().Contain("virusCheck");
            }
        }

        public class When_ChocolateyConfigSettingsService_list_config : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true
                };

                Service.ListConfig(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        ConfigSettings = new HashSet<ConfigFileConfigSetting>()
                        {
                            new ConfigFileConfigSetting()
                            {
                                Key = ApplicationParameters.ConfigSettings.WebRequestTimeoutSeconds
                            },
                            new ConfigFileConfigSetting()
                            {
                                Key = ApplicationParameters.ConfigSettings.CacheLocation
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_output_config_in_alphabetical_order()
            {
                MockLogger.Messages.Keys.Should().Contain("Info");

                var infoMessages = MockLogger.Messages["Info"];
                infoMessages.Should().HaveCount(2);
                infoMessages[0].Should().Contain("cacheLocation");
                infoMessages[1].Should().Contain("webRequestTimeoutSeconds");
            }
        }

        public class When_ChocolateyConfigSettingsService_list_source : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true
                };

                Service.ListSources(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Sources = new HashSet<ConfigFileSourceSetting>()
                        {
                            new ConfigFileSourceSetting()
                            {
                                Id = "beta"
                            },
                            new ConfigFileSourceSetting()
                            {
                                Id = "alpha"
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_output_sources_in_alphabetical_order()
            {
                MockLogger.Messages.Keys.Should().Contain("Info");

                var infoMessages = MockLogger.Messages["Info"];
                infoMessages.Should().HaveCount(2);
                infoMessages[0].Should().Contain("alpha");
                infoMessages[1].Should().Contain("beta");
            }
        }

        public class When_ChocolateyConfigSettingsService_get_unknown_feature : ChocolateyConfigSettingsServiceSpecsBase
        {
            private Exception _error = null;
            private Action _because;

            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    FeatureCommand = new FeatureCommandConfiguration()
                    {
                        Name = "unknown",
                        Command = chocolatey.infrastructure.app.domain.FeatureCommandType.Get
                    }
                };

                _because = () => Service.GetFeature(config);
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
                                Name = ApplicationParameters.Features.VirusCheck,
                                Enabled = true
                            },
                            new ConfigFileFeatureSetting()
                            {
                                Name = ApplicationParameters.Features.ChecksumFiles,
                                Enabled = false
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_throw_when_unknown_feature_name()
            {
                try
                {
                    _because();
                }
                catch (Exception ex)
                {
                    _error = ex;
                }

                _error.Should().NotBeNull();
                _error.Should().BeOfType<ApplicationException>();
                _error.Message.Should().Contain("No feature value by the name 'unknown'");
            }
        }

        public class When_ChocolateyConfigSettingsService_get_existing_feature : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    FeatureCommand = new FeatureCommandConfiguration()
                    {
                        Name = ApplicationParameters.Features.VirusCheck,
                        Command = chocolatey.infrastructure.app.domain.FeatureCommandType.Get
                    }
                };

                Service.GetFeature(config);
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
                                Name = ApplicationParameters.Features.VirusCheck,
                                Enabled = true
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_feature_status()
            {
                MockLogger.Messages.Keys.Should().Contain("Info");
                var infoMessages = MockLogger.Messages["Info"];
                infoMessages.Should().ContainSingle();
                infoMessages[0].Should().Contain("Enabled");
            }
        }

        public class When_ChocolateyConfigSettingsService_AddSource_When_Source_Already_Exists : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    Sources = "https://test.org/api/v2",
                    SourceCommand = new SourcesCommandConfiguration()
                    {
                        Name = "testSource",
                        Command = chocolatey.infrastructure.app.domain.SourceCommandType.Add
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = false
                    }
                };

                Service.AddSource(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Sources = new HashSet<ConfigFileSourceSetting>
                        {
                            new ConfigFileSourceSetting()
                            {
                                Id = "testSource",
                                Value = "https://test.org/api/v2"
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_0()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(0);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_AddSource_When_Source_Already_Exists_With_Enhanced_Exit_Codes : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    Sources = "https://test.org/api/v2",
                    SourceCommand = new SourcesCommandConfiguration()
                    {
                        Name = "testSource",
                        Command = chocolatey.infrastructure.app.domain.SourceCommandType.Add
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = true
                    }
                };

                Service.AddSource(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Sources = new HashSet<ConfigFileSourceSetting>
                        {
                            new ConfigFileSourceSetting()
                            {
                                Id = "testSource",
                                Value = "https://test.org/api/v2"
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_2()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(2);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_RemoveSource_When_Source_Doesnt_Exist : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    Sources = "https://test.org/api/v2",
                    SourceCommand = new SourcesCommandConfiguration()
                    {
                        Name = "testSource",
                        Command = chocolatey.infrastructure.app.domain.SourceCommandType.Remove
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = false
                    }
                };

                Service.RemoveSource(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Sources = new HashSet<ConfigFileSourceSetting>()
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_0()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(0);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_RemoveSource_When_Source_Doesnt_Exist_With_Enhanced_Exit_Codes : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    Sources = "https://test.org/api/v2",
                    SourceCommand = new SourcesCommandConfiguration()
                    {
                        Name = "testSource",
                        Command = chocolatey.infrastructure.app.domain.SourceCommandType.Remove
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = true
                    }
                };

                Service.RemoveSource(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Sources = new HashSet<ConfigFileSourceSetting>()
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_2()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(2);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_DisableSource_When_Source_Doesnt_Exist : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    Sources = "https://test.org/api/v2",
                    SourceCommand = new SourcesCommandConfiguration()
                    {
                        Name = "testSource",
                        Command = chocolatey.infrastructure.app.domain.SourceCommandType.Disable
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = false
                    }
                };

                Service.DisableSource(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Sources = new HashSet<ConfigFileSourceSetting>()
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_0()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(0);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_DisableSource_When_Source_Doesnt_Exist_With_Enhanced_Exit_Codes : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    Sources = "https://test.org/api/v2",
                    SourceCommand = new SourcesCommandConfiguration()
                    {
                        Name = "testSource",
                        Command = chocolatey.infrastructure.app.domain.SourceCommandType.Disable
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = true
                    }
                };

                Service.DisableSource(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Sources = new HashSet<ConfigFileSourceSetting>()
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_2()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(2);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_EnableSource_When_Source_Doesnt_Exist : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    Sources = "https://test.org/api/v2",
                    SourceCommand = new SourcesCommandConfiguration()
                    {
                        Name = "testSource",
                        Command = chocolatey.infrastructure.app.domain.SourceCommandType.Enable
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = false
                    }
                };

                Service.EnableSource(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Sources = new HashSet<ConfigFileSourceSetting>()
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_0()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(0);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_EnableSource_When_Source_Doesnt_Exist_With_Enhanced_Exit_Codes : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    Sources = "https://test.org/api/v2",
                    SourceCommand = new SourcesCommandConfiguration()
                    {
                        Name = "testSource",
                        Command = chocolatey.infrastructure.app.domain.SourceCommandType.Enable
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = true
                    }
                };

                Service.EnableSource(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        Sources = new HashSet<ConfigFileSourceSetting>()
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_2()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(2);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_DisableFeature_When_Feature_Already_Disabled : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    FeatureCommand = new FeatureCommandConfiguration()
                    {
                        Name = ApplicationParameters.Features.ChecksumFiles,
                        Command = chocolatey.infrastructure.app.domain.FeatureCommandType.Disable
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = false
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
                        Features = new HashSet<ConfigFileFeatureSetting>
                        {
                            new ConfigFileFeatureSetting
                            {
                                Name = ApplicationParameters.Features.ChecksumFiles,
                                Enabled = false,
                                SetExplicitly = true
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_0()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(0);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_DisableFeature_When_Feature_Already_Disabled_With_Enhanced_Exit_Codes : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    FeatureCommand = new FeatureCommandConfiguration()
                    {
                        Name = ApplicationParameters.Features.ChecksumFiles,
                        Command = chocolatey.infrastructure.app.domain.FeatureCommandType.Disable
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = true
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
                        Features = new HashSet<ConfigFileFeatureSetting>
                        {
                            new ConfigFileFeatureSetting
                            {
                                Name = ApplicationParameters.Features.ChecksumFiles,
                                Enabled = false,
                                SetExplicitly = true
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_2()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(2);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_EnableFeature_When_Feature_Already_Enabled: ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    FeatureCommand = new FeatureCommandConfiguration()
                    {
                        Name = ApplicationParameters.Features.ChecksumFiles,
                        Command = chocolatey.infrastructure.app.domain.FeatureCommandType.Enable
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = false
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
                        Features = new HashSet<ConfigFileFeatureSetting>
                        {
                            new ConfigFileFeatureSetting
                            {
                                Name = ApplicationParameters.Features.ChecksumFiles,
                                Enabled = true,
                                SetExplicitly = true
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_0()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(0);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_EnableFeature_When_Feature_Already_Enabled_With_Enhanced_Exit_Codes : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    FeatureCommand = new FeatureCommandConfiguration()
                    {
                        Name = ApplicationParameters.Features.ChecksumFiles,
                        Command = chocolatey.infrastructure.app.domain.FeatureCommandType.Enable
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = true
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
                        Features = new HashSet<ConfigFileFeatureSetting>
                        {
                            new ConfigFileFeatureSetting
                            {
                                Name = ApplicationParameters.Features.ChecksumFiles,
                                Enabled = true,
                                SetExplicitly = true
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_2()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(2);
                }
            }
        }

        [WindowsOnly]
        public class When_ChocolateyConfigSettingsService_SetApiKey_When_ApiKey_Already_Exists : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    Sources = "https://test.org/api/v2",
                    ApiKeyCommand = new ApiKeyCommandConfiguration()
                    {
                        Key = "bob",
                        Command = chocolatey.infrastructure.app.domain.ApiKeyCommandType.Add
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = false
                    }
                };

                Service.SetApiKey(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        ApiKeys = new HashSet<ConfigFileApiKeySetting>
                        {
                            new ConfigFileApiKeySetting
                            {
                                Key = NugetEncryptionUtility.EncryptString("bob"),
                                Source = "https://test.org/api/v2"
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_0()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(0);
                }
            }
        }

        [WindowsOnly]
        public class When_ChocolateyConfigSettingsService_SetApiKey_When_ApiKey_Already_Exists_With_Enhanced_Exit_Codes : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    Sources = "https://test.org/api/v2",
                    ApiKeyCommand = new ApiKeyCommandConfiguration()
                    {
                        Key = "bob",
                        Command = chocolatey.infrastructure.app.domain.ApiKeyCommandType.Add
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = true
                    }
                };

                Service.SetApiKey(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        ApiKeys = new HashSet<ConfigFileApiKeySetting>
                        {
                            new ConfigFileApiKeySetting
                            {
                                Key = NugetEncryptionUtility.EncryptString("bob"),
                                Source = "https://test.org/api/v2"
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_2()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(2);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_SetConfig_When_Config_Already_Set : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    ConfigCommand = new ConfigCommandConfiguration()
                    {
                        Name = ApplicationParameters.ConfigSettings.CacheLocation,
                        Command = chocolatey.infrastructure.app.domain.ConfigCommandType.Set,
                        ConfigValue = @"C:\temp"
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = false
                    }
                };

                Service.SetConfig(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        ConfigSettings = new HashSet<ConfigFileConfigSetting>()
                        {
                            new ConfigFileConfigSetting()
                            {
                                Key = ApplicationParameters.ConfigSettings.CacheLocation,
                                Value = @"C:\temp"
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_0()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(0);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_SetConfig_When_Config_Already_Set_Enhanced_Exit_Codes : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    ConfigCommand = new ConfigCommandConfiguration()
                    {
                        Name = ApplicationParameters.ConfigSettings.CacheLocation,
                        Command = chocolatey.infrastructure.app.domain.ConfigCommandType.Set,
                        ConfigValue = @"C:\temp"

                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = true
                    }
                };

                Service.SetConfig(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        ConfigSettings = new HashSet<ConfigFileConfigSetting>()
                        {
                            new ConfigFileConfigSetting()
                            {
                                Key = ApplicationParameters.ConfigSettings.CacheLocation,
                                Value = @"C:\temp"
                            }
                        }
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_2()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(2);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_UnSetConfig_When_Config_Already_UnSet : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    ConfigCommand = new ConfigCommandConfiguration()
                    {
                        Name = ApplicationParameters.ConfigSettings.CacheLocation,
                        Command = chocolatey.infrastructure.app.domain.ConfigCommandType.Unset
                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = false
                    }
                };

                Service.UnsetConfig(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        ConfigSettings = new HashSet<ConfigFileConfigSetting>()
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_0()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(0);
                }
            }
        }

        public class When_ChocolateyConfigSettingsService_UnSetConfig_When_Config_Already_UnSet_Enhanced_Exit_Codes : ChocolateyConfigSettingsServiceSpecsBase
        {
            public override void Because()
            {
                var config = new ChocolateyConfiguration()
                {
                    RegularOutput = true,
                    ConfigCommand = new ConfigCommandConfiguration()
                    {
                        Name = ApplicationParameters.ConfigSettings.CacheLocation,
                        Command = chocolatey.infrastructure.app.domain.ConfigCommandType.Unset

                    },
                    Features = new FeaturesConfiguration()
                    {
                        UseEnhancedExitCodes = true
                    }
                };

                Service.UnsetConfig(config);
            }

            public override void Context()
            {
                base.Context();

                XmlService.Setup(x => x.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation))
                    .Returns(new ConfigFileSettings
                    {
                        ConfigSettings = new HashSet<ConfigFileConfigSetting>()
                    });

                Service = new ChocolateyConfigSettingsService(XmlService.Object);
            }

            [Fact]
            public void Should_return_exit_code_2()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey("Warn").WhoseValue.Should().ContainSingle().And.Contain("Nothing to change. Config already set.");
                    Environment.ExitCode.Should().Be(2);
                }
            }
        }
    }
}
