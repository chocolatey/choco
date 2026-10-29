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
    /// <summary>
    /// Base for specs that read or write process environment variables. The variables named in
    /// <see cref="ManagedVariables"/> are cleared before the spec runs and restored afterwards.
    /// </summary>
    [NonParallelizable]
    public abstract class EnvironmentSpecsBase : TinySpec
    {
        private string[] _originalValues;

        protected abstract string[] ManagedVariables { get; }

        public override void Context()
        {
            _originalValues = Array.ConvertAll(ManagedVariables, Environment.GetEnvironmentVariable);
            foreach (var name in ManagedVariables)
            {
                Environment.SetEnvironmentVariable(name, null);
            }
        }

        public override void AfterObservations()
        {
            for (var i = 0; i < ManagedVariables.Length; i++)
            {
                Environment.SetEnvironmentVariable(ManagedVariables[i], _originalValues[i]);
            }
        }
    }
}
