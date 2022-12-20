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

namespace chocolatey.tests
{
    using System;
    using NUnit.Framework;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.logging;
    using System.IO;

    // ReSharper disable InconsistentNaming

    [SetUpFixture]
    public class NUnitSetup
    {
        public static MockLogger MockLogger { get; set; }

        private static readonly string InstallLocationVariable = Environment.GetEnvironmentVariable(ApplicationParameters.ChocolateyInstallEnvironmentVariableName);

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

        public virtual void before_everything()
        {
        }

        [OneTimeTearDown]
        public void AfterEverything()
        {
            Environment.SetEnvironmentVariable(ApplicationParameters.ChocolateyInstallEnvironmentVariableName, InstallLocationVariable);
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
            if (MockLogger != null) MockLogger.reset();
            //Log.InitializeWith(MockLogger);
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

    public class ExplicitAttribute : NUnit.Framework.ExplicitAttribute
    {
    }


    public class ConcernForAttribute : CategoryAttribute
    {
        public ConcernForAttribute(string name)
            : base("ConcernFor - {0}".format_with(name))
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
            : base("Pending test - {0}".format_with(reason))
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

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
        public sealed class SideBySideAttribute : CategoryAttribute
        {
            public SideBySideAttribute()
                : base("Side-by-Side")
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
    }

    // ReSharper restore InconsistentNaming
}
