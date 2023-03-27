﻿// Copyright © 2017 - 2022 Chocolatey Software, Inc
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

namespace Chocolatey.Infrastructure.App.Services
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation.Runspaces;
    using Configuration;
    using global::NuGet.Packaging;
    using global::NuGet.Protocol.Core.Types;
    using Results;

    public interface IPowershellService
    {
        /// <summary>
        ///   Noops the specified package install.
        /// </summary>
        /// <param name="packageResult">The package result.</param>
        void InstallDryRun(PackageResult packageResult);

        /// <summary>
        ///   Installs the specified package.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="packageResult">The package result.</param>
        /// <returns>true if the chocolateyInstall.ps1 was found, even if it has failures</returns>
        bool Install(ChocolateyConfiguration configuration, PackageResult packageResult);

        /// <summary>
        ///   Noops the specified package uninstall.
        /// </summary>
        /// <param name="packageResult">The package result.</param>
        void UninstallDryRun(PackageResult packageResult);

        /// <summary>
        ///   Uninstalls the specified package.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="packageResult">The package result.</param>
        /// <returns>true if the chocolateyUninstall.ps1 was found, even if it has failures</returns>
        bool Uninstall(ChocolateyConfiguration configuration, PackageResult packageResult);

        /// <summary>
        ///   Noops the specified package before modify operation.
        /// </summary>
        /// <param name="packageResult">The package result.</param>
        void BeforeModifyDryRun(PackageResult packageResult);

        /// <summary>
        ///   Runs any before modification script on the specified package.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="packageResult">The package result.</param>
        /// <returns>true if the chocolateyBeforeModify.ps1 was found, even if it has failures</returns>
        bool BeforeModify(ChocolateyConfiguration configuration, PackageResult packageResult);

        void PreparePowerShellEnvironment(IPackageSearchMetadata package, ChocolateyConfiguration configuration, string packageDirectory);

        PowerShellExecutionResults RunHost(ChocolateyConfiguration config, string chocoPowerShellScript, Action<Pipeline> additionalActionsBeforeScript, IEnumerable<string> hookPreScriptPathList, IEnumerable<string> hookPostScriptPathList);
    }
}
