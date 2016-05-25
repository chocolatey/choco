// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.infrastructure.powershell
{
    using System;
    using System.Globalization;
    using System.Management.Automation.Host;
    using app;
    using app.configuration;

    public class PoshHost : PSHost
    {
        private readonly ChocolateyConfiguration _configuration;
        private readonly Guid _hostId = Guid.NewGuid();
        private readonly PoshHostUserInterface _psUI;
        private readonly CultureInfo _cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
        private readonly CultureInfo _cultureUiInfo = System.Threading.Thread.CurrentThread.CurrentUICulture;

        private bool _isClosing;

        // http://blogs.msdn.com/b/kebab/archive/2014/04/28/executing-powershell-scripts-from-c.aspx
        // https://msdn.microsoft.com/en-us/library/ee706594(v=vs.85).aspx
        // http://powershellstation.com/2009/11/10/writing-your-own-powershell-hosting-app-part-4/

        // others
        // http://stackoverflow.com/questions/16329448/hosting-powershell-powershell-vs-runspace-vs-runspacepool-vs-pipeline
        // 

        public int ExitCode { get; set; }
        public Exception HostException { get; set; }
        public bool StandardErrorWritten { get { return _psUI.StandardErrorWritten; } }

        public PoshHost(ChocolateyConfiguration configuration)
        {
            ExitCode = -1;
            _configuration = configuration;
            _psUI = new PoshHostUserInterface(configuration);
        }

        public override void SetShouldExit(int exitCode)
        {
            if (!_isClosing)
            {
                _isClosing = true;
            }

            ExitCode = exitCode;
        }

        public override CultureInfo CurrentCulture
        {
            get { return _cultureInfo; }
        }

        public override CultureInfo CurrentUICulture
        {
            get { return _cultureUiInfo; }
        }

        public override Guid InstanceId
        {
            get { return _hostId; }
        }

        public override string Name
        {
            get { return ApplicationParameters.Name + "_PSHost"; }
        }

        public override PSHostUserInterface UI
        {
            get { return _psUI; }
        }

        public override Version Version
        {
            get { return new Version(_configuration.Information.ChocolateyVersion); }
        }

        #region Not Implemented / Empty

        public override void NotifyBeginApplication()
        {
            // no state to hold
        }

        public override void NotifyEndApplication()
        {
            // no state to restore
        }

        public override void EnterNestedPrompt()
        {
            throw new NotImplementedException("Nested prompt not implemented");
        }

        public override void ExitNestedPrompt()
        {
            throw new NotImplementedException("Nested prompt not implemented");
        }

        #endregion
    }
}