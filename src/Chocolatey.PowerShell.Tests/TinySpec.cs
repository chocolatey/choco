// Copyright © 2017 - 2026 Chocolatey Software, Inc
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

using System;
using NUnit.Framework;

namespace Chocolatey.PowerShell.Tests
{
    // ReSharper disable InconsistentNaming

    [TestFixture]
    public abstract class TinySpec
    {
        [OneTimeSetUp]
        public void Setup()
        {
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

    // ReSharper restore InconsistentNaming
}
