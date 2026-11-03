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
using System.Collections.Generic;
using System.Management.Automation;
using Chocolatey.PowerShell.Helpers;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    /// <summary>
    /// A helper that records the environment changes it is asked to make instead of making them, and that
    /// joins paths with a backslash on every platform.
    /// </summary>
    public class TestToolsLocationHelper : ToolsLocationHelper
    {
        public TestToolsLocationHelper(PSCmdlet cmdlet)
            : base(cmdlet)
        {
        }

        /// <summary>
        /// Whether the process is treated as having administrative rights.
        /// </summary>
        public bool Elevated { get; set; }

        /// <summary>
        /// Gets the exception that setting a variable fails with, or <c>null</c> if it succeeds.
        /// </summary>
        public Func<string, EnvironmentVariableTarget, Exception> Failure { get; set; } = (name, scope) => null;

        /// <summary>
        /// What was asked of the helper, in order, such as <c>User ChocolateyToolsLocation=C:\tools</c> or <c>IsElevated</c>.
        /// </summary>
        public List<string> Calls { get; } = new List<string>();

        protected override string CombinePaths(string parent, string child)
        {
            return parent.TrimEnd('\\', '/') + "\\" + child.TrimStart('\\', '/');
        }

        protected override void SetVariable(string name, string value, EnvironmentVariableTarget scope)
        {
            Calls.Add($"{scope} {name}={value}");

            var failure = Failure(name, scope);
            if (!(failure is null))
            {
                throw failure;
            }
        }

        protected override bool IsElevated()
        {
            Calls.Add("IsElevated");

            return Elevated;
        }
    }
}
