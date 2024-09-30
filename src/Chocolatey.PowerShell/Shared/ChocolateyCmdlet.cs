// Copyright © 2017 - 2024 Chocolatey Software, Inc
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

using System.Collections;
using System.Management.Automation;
using System.Text;
using Chocolatey.PowerShell.Helpers;

namespace Chocolatey.PowerShell.Shared
{
    /// <summary>
    /// Base class for all Chocolatey cmdlets.
    /// Contains a number of helpers and common code that is used by all cmdlets.
    /// </summary>
    public abstract class ChocolateyCmdlet : PSCmdlet
    {
        /// <summary>
        /// The canonical error ID for the command to assist with traceability.
        /// For more specific error IDs where needed, use <c>"{ErrorId}.EventName"</c>.
        /// </summary>
        protected string ErrorId
        {
            get
            {
                return GetType().Name + "Error";
            }
        }

        /// <summary>
        /// For compatibility reasons, we always add the -IgnoredArguments parameter, so that newly added parameters
        /// won't break things too much if a package is run with an older version of Chocolatey.
        /// </summary>
        [Parameter(ValueFromRemainingArguments = true)]
        public object[] IgnoredArguments { get; set; }

        /// <summary>
        /// Sets whether the cmdlet writes its parameters and name to the debug log when it is called and
        /// when it completes its operation (after End() is called).
        /// This should remain set to true for all commands that are considered part of the public Chocolatey CLI API,
        /// unless there are concerns about potentially sensitive information making it into a log file from the parameters of the command.
        /// </summary>
        protected virtual bool Logging { get; } = true;

        protected sealed override void BeginProcessing()
        {
            WriteCmdletCallDebugMessage();
            Begin();
        }

        /// <summary>
        /// Override this method to define the cmdlet's begin {} block behaviour.
        /// Note that parameters that are defined as ValueFromPipeline or ValueFromPipelineByPropertyName
        /// will not be available for the duration of this method.
        /// </summary>
        protected virtual void Begin()
        {
        }

        protected sealed override void ProcessRecord()
        {
            Process();
        }

        /// <summary>
        /// Override this method to define the cmdlet's process {} block behaviour.
        /// This is called once for every item the cmdlet receives to a pipeline parameter, or only once if the value is supplied directly.
        /// Parameters that are defined as ValueFromPipeline or ValueFromPipelineByPropertyName will be available during this method call.
        /// </summary>
        protected virtual void Process()
        {
        }

        protected sealed override void EndProcessing()
        {
            End();
            WriteCmdletCompletionDebugMessage();
        }

        /// <summary>
        /// Override this method to define the cmdlet's end {} block behaviour.
        /// Note that parameters that are defined as ValueFromPipeline or ValueFromPipelineByPropertyName
        /// may not be available or have complete data during this method call.
        /// </summary>
        protected virtual void End()
        {
        }

        protected void WriteHost(string message)
        {
            PSHelper.WriteHost(this, message);
        }

        protected new void WriteObject(object value)
        {
            PSHelper.WriteObject(this, value);
        }

        protected void WriteCmdletCallDebugMessage()
        {
            if (!Logging)
            {
                return;
            }

            var logMessage = new StringBuilder()
                .Append("Running ")
                .Append(MyInvocation.InvocationName);

            foreach (var param in MyInvocation.BoundParameters)
            {
                var paramNameLower = param.Key.ToLower();

                if (paramNameLower == "ignoredarguments")
                {
                    continue;
                }

                var paramValue = paramNameLower == "sensitivestatements" || paramNameLower == "password"
                    ? "[REDACTED]"
                    : param.Value is IList list
                        ? string.Join(" ", list)
                        : LanguagePrimitives.ConvertTo(param.Value, typeof(string));

                logMessage.Append($" -{param.Key} '{paramValue}'");
            }

            WriteDebug(logMessage.ToString());
        }

        protected void WriteCmdletCompletionDebugMessage()
        {
            if (!Logging)
            {
                return;
            }

            WriteDebug($"Finishing '{MyInvocation.InvocationName}'");
        }
    }
}
