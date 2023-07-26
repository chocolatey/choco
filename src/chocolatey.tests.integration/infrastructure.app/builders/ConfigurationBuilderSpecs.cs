// Copyright © 2017 - 2023 Chocolatey Software, Inc
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

namespace chocolatey.tests.integration.infrastructure.app.builders
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using chocolatey.infrastructure.adapters;
    using chocolatey.infrastructure.app;
    using ConfigurationBuilder = chocolatey.infrastructure.app.builders.ConfigurationBuilder;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.licensing;
    using Moq;
    using NUnit.Framework;
    using Container = SimpleInjector.Container;
    using License = System.ComponentModel.License;
    using chocolatey.infrastructure.registration;
    using Microsoft.Win32;
    using scenarios;
    using FluentAssertions;

    public class ConfigurationBuilderSpecs
    {
        private const string IgnoreSystemProxyReason = "System Proxy not mockable";

        public abstract class ProxyConfigurationBase : TinySpec
        {
            protected bool SystemSet = false;
            protected bool EnvironmentVariableSet = false;
            protected bool ConfigSet = false;
            protected bool ArgumentSet = false;
            protected const string ConfigurationFileProxySettingName = "proxy";
            protected const string ConfigurationFileProxyBypassSettingName = "proxyBypassList";
            protected const string EnvironmentVariableProxyValue = "EnvironmentVariableSet";
            protected const string CommandArgumentProxyValue = "CommandArgumentSet";
            protected const string ConfigurationFileProxyValue = "ConfigurationFileSet";
            protected const string SystemLevelProxyValue = "SystemLevelSet";
            protected ChocolateyConfiguration Configuration;
            protected Container Container;
            protected ChocolateyLicense License;
            protected Mock<IEnvironment> Environment;
            protected List<string> ArgumentsList = new List<string>();

            public ProxyConfigurationBase(bool systemSet, bool environmentVariableSet, bool configSet, bool argumentSet)
            {
                SystemSet = systemSet;
                EnvironmentVariableSet = environmentVariableSet;
                ConfigSet = configSet;
                ArgumentSet = argumentSet;
            }

            public override void Context()
            {
                Configuration = Scenario.Proxy();
                Scenario.SetConfigurationFileSetting(ConfigurationFileProxySettingName, string.Empty);
                Scenario.SetConfigurationFileSetting(ConfigurationFileProxyBypassSettingName, string.Empty);
                Scenario.Reset(Configuration);
                Container = NUnitSetup.Container;
                License = new ChocolateyLicense();
                Environment = new Mock<IEnvironment>();
                ConfigurationBuilder.InitializeWith(new Lazy<IEnvironment>(() => Environment.Object));
                Environment.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>())).Returns(string.Empty);
                ArgumentsList.Clear();
            }

            public override void Because()
            {
                ConfigurationBuilder.SetupConfiguration(ArgumentsList, Configuration, Container, License, null);
            }
        }

        // System and Configuration File and Environment Variable and CLI Argument
        [TestFixture(false, false, false, false, TestName = "No Proxy Set")]
        [TestFixture(true, false, false, false, TestName = "System Set", IgnoreReason = IgnoreSystemProxyReason)]
        [TestFixture(true, true, false, false, TestName = "System and Environment Set", IgnoreReason = IgnoreSystemProxyReason)]
        [TestFixture(true, true, true, false, TestName = "System and Environment and Configuration Set", IgnoreReason = IgnoreSystemProxyReason)]
        [TestFixture(true, true, true, true, TestName = "System and Environment and Configuration and Argument Set", IgnoreReason = IgnoreSystemProxyReason)]
        [TestFixture(true, false, true, false, TestName = "System and Configuration Set", IgnoreReason = IgnoreSystemProxyReason)]
        [TestFixture(true, false, true, true, TestName = "System and Configuration and Argument Set", IgnoreReason = IgnoreSystemProxyReason)]
        [TestFixture(true, false, false, true, TestName = "System and Argument Set", IgnoreReason = IgnoreSystemProxyReason)]
        [TestFixture(true, true, false, true, TestName = "System and Environment and Argument Set", IgnoreReason = IgnoreSystemProxyReason)]
        [TestFixture(false, true, false, false, TestName = "Environment Set")]
        [TestFixture(false, true, true, false, TestName = "Environment and Configuration Set")]
        [TestFixture(false, true, false, true, TestName = "Environment and Argument Set")]
        [TestFixture(false, true, true, true, TestName = "Environment and Configuration and Argument Set")]
        [TestFixture(false, false, true, false, TestName = "Configuration Set")]
        [TestFixture(false, false, true, true, TestName = "Configuration and Argument Set")]
        [TestFixture(false, false, false, true, TestName = "Argument Set")]
        public class WhenProxyConfigurationTests : ProxyConfigurationBase
        {
            public WhenProxyConfigurationTests(bool system, bool environment, bool config, bool argument) : base(system, environment, config, argument) {}
            public override void Context()
            {
                base.Context();

                if (SystemSet)
                {
                    // Do System Level things
                }

                if (EnvironmentVariableSet)
                {
                    Environment.Setup(e => e.GetEnvironmentVariable(It.IsIn("http_proxy", "https_proxy"))).Returns(EnvironmentVariableProxyValue);
                }
                else
                {
                    Environment.Setup(e => e.GetEnvironmentVariable(It.IsIn("http_proxy", "https_proxy"))).Returns(string.Empty);
                }

                if (ConfigSet)
                {
                    Scenario.SetConfigurationFileSetting(ConfigurationFileProxySettingName, ConfigurationFileProxyValue);
                }

                if (ArgumentSet)
                {
                    ArgumentsList.Add("--proxy='{0}'".FormatWith(CommandArgumentProxyValue));
                }
            }

            [Fact]
            public void ShouldHaveProxyConfiguration()
            {
                if (!SystemSet && !ArgumentSet && !ConfigSet &&
                    !EnvironmentVariableSet)
                {
                    Configuration.Proxy.Location.Should().BeEmpty();
                    return;
                }

                if (ArgumentSet)
                {
                    Configuration.Proxy.Location.Should().Be(CommandArgumentProxyValue);
                    return;
                }

                if (ConfigSet)
                {
                    Configuration.Proxy.Location.Should().Be(ConfigurationFileProxyValue);
                    return;
                }

                if (EnvironmentVariableSet)
                {
                    Configuration.Proxy.Location.Should().Be(EnvironmentVariableProxyValue);
                    return;
                }

                if (SystemSet)
                {
                    Configuration.Proxy.Location.Should().Be(SystemLevelProxyValue);
                    return;
                }
            }
        }

        [TestFixture(false, false, false, false, TestName = "No Bypass Set")]
        [TestFixture(false, true, false, false, TestName = "Config Bypass Set")]
        [TestFixture(false, true, true, false, TestName = "Config and Environment Variable Bypass Set")]
        [TestFixture(false, true, true, true, TestName = "Config and Environment Variable and Argument Bypass Set")]
        [TestFixture(false, true, false, true, TestName = "Config and Argument Bypass Set")]
        [TestFixture(false, false, true, false, TestName = "Environment Variable Bypass Set")]
        [TestFixture(false, false, true, true, TestName = "Environment Variable and Argument Bypass Set")]
        public class WhenProxyBypassConfigurationTests : ProxyConfigurationBase
        {
            public WhenProxyBypassConfigurationTests(bool system, bool environment, bool config, bool argument) : base(system, environment, config, argument) { }

            public override void Context()
            {
                base.Context();

                if (ConfigSet)
                {
                    Scenario.SetConfigurationFileSetting(ConfigurationFileProxyBypassSettingName, ConfigurationFileProxyValue);
                }

                if (EnvironmentVariableSet)
                {
                    Environment.Setup(e => e.GetEnvironmentVariable("no_proxy")).Returns(EnvironmentVariableProxyValue);
                }
                else
                {
                    Environment.Setup(e => e.GetEnvironmentVariable("no_proxy")).Returns(string.Empty);
                }

                if (ArgumentSet)
                {
                    ArgumentsList.Add("--proxy-bypass-list='{0}'".FormatWith(CommandArgumentProxyValue));
                }
            }

            [Fact]
            public void ShouldBypassProxy()
            {
                if (!ArgumentSet && !ConfigSet &&
                    !EnvironmentVariableSet)
                {
                    Configuration.Proxy.BypassList.Should().BeEmpty();
                    return;
                }

                if (ArgumentSet)
                {
                    Configuration.Proxy.BypassList.Should().Be(CommandArgumentProxyValue);
                    return;
                }

                if (ConfigSet)
                {
                    Configuration.Proxy.BypassList.Should().Be(ConfigurationFileProxyValue);
                    return;
                }

                if (EnvironmentVariableSet)
                {
                    Configuration.Proxy.BypassList.Should().Be(EnvironmentVariableProxyValue);
                    return;
                }
            }
        }
    }
}
