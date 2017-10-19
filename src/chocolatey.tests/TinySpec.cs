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

    // ReSharper disable InconsistentNaming

    [SetUpFixture]
    public class NUnitSetup
    {
        public static MockLogger MockLogger { get; set; }

        private static readonly string InstallLocationVariable = Environment.GetEnvironmentVariable(ApplicationParameters.ChocolateyInstallEnvironmentVariableName);

        [SetUp]
        public virtual void BeforeEverything()
        {
            Environment.SetEnvironmentVariable(ApplicationParameters.ChocolateyInstallEnvironmentVariableName, string.Empty);
            MockLogger = new MockLogger();
            Log.InitializeWith(MockLogger);
            // do not log trace messages
            ILogExtensions.LogTraceMessages = false;
        }

        public virtual void before_everything()
        {
        }

        [TearDown]
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

        [TestFixtureSetUp]
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

        [TestFixtureTearDown]
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


    public class ConcernForAttribute : Attribute
    {
        public string Name { get; set; }

        public ConcernForAttribute(string name)
        {
            Name = name;
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

#if __MonoCS__
     public class WindowsOnlyAttribute : IgnoreAttribute
    {
        public WindowsOnlyAttribute() : base("This is a Windows only test")
        {
        }
    }
#else
    public class WindowsOnlyAttribute : Attribute
    {
    }
#endif

    public class IntegrationAttribute : CategoryAttribute
    {
        public IntegrationAttribute()
            : base("Integration")
        {
        }
    }

    public class ExpectedExceptionAttribute : NUnit.Framework.ExpectedExceptionAttribute
    {
        public ExpectedExceptionAttribute(Type exceptionType) : base(exceptionType)
        {}

        public ExpectedExceptionAttribute(string exceptionName) : base(exceptionName)
        {}
    }

    // ReSharper restore InconsistentNaming
}