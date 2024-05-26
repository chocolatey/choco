// ==============================================================================
//
// Fervent Coder Copyright © 2011 - Released under the Apache 2.0 License
//
// Copyright 2007-2008 The Apache Software Foundation.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the
// License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, either express or implied. See the License for the
// specific language governing permissions and limitations under the License.
// ==============================================================================

using System;
using NUnit.Framework;
using chocolatey.infrastructure.app;
using chocolatey.infrastructure.logging;
using System.IO;
using chocolatey.infrastructure.app.nuget;

namespace chocolatey.tests
{
    // ReSharper disable InconsistentNaming

    [SetUpFixture]
    public class NUnitSetup
    {
        public static MockLogger MockLogger { get; set; }

        private static readonly string _installLocationVariable = Environment.GetEnvironmentVariable(ApplicationParameters.ChocolateyInstallEnvironmentVariableName);

        [OneTimeSetUp]
        public virtual void BeforeEverything()
        {
            Environment.SetEnvironmentVariable(ApplicationParameters.ChocolateyInstallEnvironmentVariableName, string.Empty);
            MockLogger = new MockLogger();
            Log.InitializeWith(MockLogger);
            // do not log trace messages
            ILogExtensions.LogTraceMessages = false;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        }

        [OneTimeTearDown]
        public void AfterEverything()
        {
            Environment.SetEnvironmentVariable(ApplicationParameters.ChocolateyInstallEnvironmentVariableName, _installLocationVariable);
        }
    }

    [TestFixture]
    public abstract class TinySpec
    {
        public MockLogger MockLogger
        {
            get { return NUnitSetup.MockLogger; }
        }

        [OneTimeSetUp]
        public void Setup()
        {
            if (MockLogger != null)
            {
                MockLogger.Reset();
            }

            // Chocolatey CLI by default will exit with Code 0, when everything work as expected, even if it doesn't
            // set this explicitly.
            // However, in some tests, we are testing for the setting of an explicit exit code, and when we do this,
            // it can have an impact on other tests, since it may not have been reset.  Let's explicitly set it to
            // 0 before running each test, so that everything starts off at the right place.
            Environment.ExitCode = default;

            //Log.InitializeWith(MockLogger);
            NugetCommon.ClearRepositoriesCache();
            Context();
            Because();
        }

        public abstract void Context();

        public abstract void Because();

        [SetUp]
        public void EachSpecSetup()
        {
            BeforeEachSpec();
        }

        public virtual void BeforeEachSpec()
        {
        }

        [TearDown]
        public void EachSpecTearDown()
        {
            AfterEachSpec();
        }

        public virtual void AfterEachSpec()
        {
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            AfterObservations();
        }

        public virtual void AfterObservations()
        {
        }
    }

    public class ObservationAttribute : TestAttribute
    {
    }

    public class FactAttribute : ObservationAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class InlineDataAttribute : TestCaseAttribute
    {
        public InlineDataAttribute(params object[] data)
            : base(data)
        {
        }
    }

    public class ExplicitAttribute : NUnit.Framework.ExplicitAttribute
    {
    }


    public class ConcernForAttribute : CategoryAttribute
    {
        public ConcernForAttribute(string name)
            : base("ConcernFor - {0}".FormatWith(name))
        {
        }
    }

    public class NotWorkingAttribute : CategoryAttribute
    {
        public string Reason { get; set; }

        public NotWorkingAttribute(string reason)
            : base("NotWorking")
        {
            Reason = reason;
        }
    }

    public class PendingAttribute : IgnoreAttribute
    {
        public PendingAttribute(string reason)
            : base("Pending test - {0}".FormatWith(reason))
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class BrokenAttribute : CategoryAttribute
    {
        public BrokenAttribute()
            : base("Broken")
        {
        }
    }

    public class WindowsOnlyAttribute : PlatformAttribute
    {
        public WindowsOnlyAttribute()
        {
            Exclude = "Mono, Linux, MacOsX, Linux";
        }

        public WindowsOnlyAttribute(string platforms) : base(platforms)
        {
        }
    }

    public static class Categories
    {
        [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
        public sealed class IntegrationAttribute : CategoryAttribute
        {
            public IntegrationAttribute()
                : base("Integration")
            {
            }
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
        public sealed class SemVer20Attribute : CategoryAttribute
        {
            public SemVer20Attribute()
                : base("SemVer 2.0")
            {
            }
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
        public sealed class RuleEngine : CategoryAttribute
        {
            public RuleEngine()
                : base("Rule Engine")
            {
            }
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
        public sealed class LegacySemVerAttribute : CategoryAttribute
        {
            public LegacySemVerAttribute()
                : base("Legacy SemVer")
            {
            }
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
        public sealed class ExceptionHandlingAttribute : CategoryAttribute
        {
            public ExceptionHandlingAttribute()
                : base("Exception Handling")
            {
            }
        }

        /// <summary>
        /// Attribute used to define a test class or method as belonging to source priorities.
        /// </summary>
        /// <remarks>This need to be changed to inherit from <see cref="CategoryAttribute"/> once we have a working implementation of source priorities.</remarks>
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
        public sealed class SourcePriorityAttribute : IgnoreAttribute
        {
            public SourcePriorityAttribute()
                : base("Source priority is not implemented")
            {
            }
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
        public sealed class LoggingAttribute : CategoryAttribute
        {
            public LoggingAttribute()
                : base("Logging")
            {
            }
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
        public sealed class UncAttribute : PlatformAttribute
        {
            public UncAttribute()
                : base("Win")
            {
                Reason = "UNC Test paths are only available when running on Windows";
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
