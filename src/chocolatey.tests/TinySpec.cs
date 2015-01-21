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
    using chocolatey.infrastructure.logging;

    // ReSharper disable InconsistentNaming

    [TestFixture]
    public abstract class TinySpec
    {
        private MockLogger _mockLogger;

        public MockLogger MockLogger
        {
            get { return _mockLogger; }
        }

        [TestFixtureSetUp]
        public void Setup()
        {
            _mockLogger = new MockLogger();
            Log.InitializeWith(_mockLogger);
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
            _mockLogger = null;
            Log.InitializeWith(new NullLog());
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

    public class IntegrationAttribute : CategoryAttribute
    {
        public IntegrationAttribute()
            : base("Integration")
        {
        }
    }

    // ReSharper restore InconsistentNaming
}