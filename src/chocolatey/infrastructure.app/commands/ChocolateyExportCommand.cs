// Copyright © 2017 - 2025 Chocolatey Software, Inc
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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using chocolatey.infrastructure.app.attributes;
using chocolatey.infrastructure.commandline;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.filesystem;
using chocolatey.infrastructure.commands;
using chocolatey.infrastructure.logging;
using chocolatey.infrastructure.app.services;
using chocolatey.infrastructure.tolerance;

namespace chocolatey.infrastructure.app.commands
{
    [CommandFor("export", "exports list of currently installed packages")]
    public class ChocolateyExportCommand : ICommand
    {
        private readonly INugetService _nugetService;
        private readonly IFileSystem _fileSystem;
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly IChocolateyPackageService _packageService;

        public ChocolateyExportCommand(
            INugetService nugetService,
            IFileSystem fileSystem,
            IChocolateyPackageInformationService packageInfoService,
            IChocolateyPackageService packageService)
        {
            _nugetService = nugetService;
            _fileSystem = fileSystem;
            _packageInfoService = packageInfoService;
            _packageService = packageService;
        }

        public void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("o=|output-file-path=",
                     "Output File Path - the path to where the list of currently installed packages should be saved. Defaults to packages.config.",
                     option => configuration.ExportCommand.OutputFilePath = option.UnquoteSafe())
                .Add("include-version-numbers|include-version",
                     "Include Version Numbers - controls whether or not version numbers for each package appear in generated file.  Defaults to false.",
                     option => configuration.ExportCommand.IncludeVersionNumbers = option != null)
                .Add("include-arguments|include-remembered-arguments",
                    "Include Remembered Arguments - controls whether or not remembered arguments for each package appear in generated file.  Defaults to false. Available in 2.3.0+",
                    option => configuration.ExportCommand.IncludeRememberedPackageArguments = option != null)
                .Add("exclude-pins",
                    "Exclude Pins - controls whether or not pins are included. Only applies if remembered arguments are exported. Defaults to false. Available in 2.4.0+",
                    option => configuration.ExportCommand.ExcludePins = option != null)
                ;
        }

        public void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);

            if (string.IsNullOrWhiteSpace(configuration.ExportCommand.OutputFilePath) && unparsedArguments.Count >= 1)
            {
                configuration.ExportCommand.OutputFilePath = unparsedArguments[0];
            }

            // If no value has been provided for the OutputFilePath, default to packages.config
            if (string.IsNullOrWhiteSpace(configuration.ExportCommand.OutputFilePath))
            {
                configuration.ExportCommand.OutputFilePath = "packages.config";
            }
        }

        public void Validate(ChocolateyConfiguration configuration)
        {
            // Currently, no additional validation is required.
        }

        public void HelpMessage(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Export Command");
            this.Log().Info(@"
Export all currently installed packages to a file.

This is especially helpful when re-building a machine that was created
using Chocolatey.  Export all packages to a file, and then re-install
those packages onto new machine using `choco install packages.config`.
");
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco export [<options/switches>]
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco export
    choco export --include-version-numbers
    choco export --include-version-numbers --include-remembered-arguments
    choco export --include-remembered-arguments --exclude-pins
    choco export ""'c:\temp\packages.config'""
    choco export ""'c:\temp\packages.config'"" --include-version-numbers
    choco export -o=""'c:\temp\packages.config'""
    choco export -o=""'c:\temp\packages.config'"" --include-version-numbers
    choco export --output-file-path=""'c:\temp\packages.config'""
    choco export --output-file-path=""'c:\temp\packages.config'"" --include-version-numbers
    choco export --output-file-path=""""'c:\temp\packages.config'"""" --include-remembered-arguments

NOTE: See scripting in the command reference (`choco -?`) for how to
 write proper scripts and integrations.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Exit Codes");
            "chocolatey".Log().Info(@"
Exit codes that normally result from running this command.

Normal:
 - 0: operation was successful, no issues detected
 - -1 or 1: an error has occurred

If you find other exit codes that we have not yet documented, please
 file a ticket so we can document it at
 https://github.com/chocolatey/choco/issues/new/choose.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public bool MayRequireAdminAccess()
        {
            return false;
        }

        public void DryRun(ChocolateyConfiguration configuration)
        {
            this.Log().Info("Export would have been with options: {0} Output File Path={1}{0} Include Version Numbers:{2}{0} Include Remembered Arguments: {3}{0}  Exclude Pins: {4}".FormatWith(
                Environment.NewLine,
                configuration.ExportCommand.OutputFilePath,
                configuration.ExportCommand.IncludeVersionNumbers,
                configuration.ExportCommand.IncludeRememberedPackageArguments,
                configuration.ExportCommand.ExcludePins));
        }

        public void Run(ChocolateyConfiguration configuration)
        {
            var installedPackages = _nugetService.GetInstalledPackages(configuration);
            var xmlWriterSettings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };

            configuration.CreateBackup();

            FaultTolerance.TryCatchWithLoggingException(
                () =>
                {
                    var packagesConfig = new PackagesConfigFileSettings();
                    packagesConfig.Packages = new HashSet<PackagesConfigFilePackageSetting>();

                    using (var stringWriter = new StringWriter())
                    {
                        using (var xw = XmlWriter.Create(stringWriter, xmlWriterSettings))
                        {
                            foreach (var packageResult in installedPackages)
                            {
                                var packageElement = new PackagesConfigFilePackageSetting
                                {
                                    Id = packageResult.PackageMetadata.Id
                                };

                                if (configuration.ExportCommand.IncludeVersionNumbers)
                                {
                                    packageElement.Version = packageResult.PackageMetadata.Version.ToString();
                                }

                                if (configuration.ExportCommand.IncludeRememberedPackageArguments)
                                {
                                    var pkgInfo = _packageInfoService.Get(packageResult.PackageMetadata);
                                    configuration.Features.UseRememberedArgumentsForUpgrades = true;
                                    var rememberedConfig =  _nugetService.GetPackageConfigFromRememberedArguments(configuration, pkgInfo);

                                    // Mirrors the arguments captured in ChocolateyPackageService.CaptureArguments()
                                    if (configuration.Prerelease) packageElement.Prerelease = true;
                                    if (configuration.IgnoreDependencies) packageElement.IgnoreDependencies = true;
                                    if (configuration.ForceX86) packageElement.ForceX86 = true;
                                    if (!string.IsNullOrWhiteSpace(configuration.InstallArguments)) packageElement.InstallArguments = configuration.InstallArguments;
                                    if (configuration.OverrideArguments) packageElement.OverrideArguments = true;
                                    if (configuration.ApplyInstallArgumentsToDependencies) packageElement.ApplyInstallArgumentsToDependencies = true;
                                    if (!string.IsNullOrWhiteSpace(configuration.PackageParameters)) packageElement.PackageParameters = configuration.PackageParameters;
                                    if (configuration.ApplyPackageParametersToDependencies) packageElement.ApplyPackageParametersToDependencies = true;
                                    if (configuration.AllowDowngrade) packageElement.AllowDowngrade = true;
                                    if (!string.IsNullOrWhiteSpace(configuration.SourceCommand.Username)) packageElement.User = configuration.SourceCommand.Username;
                                    if (!string.IsNullOrWhiteSpace(configuration.SourceCommand.Password)) packageElement.Password = configuration.SourceCommand.Password;
                                    if (!string.IsNullOrWhiteSpace(configuration.SourceCommand.Certificate)) packageElement.Cert = configuration.SourceCommand.Certificate;
                                    if (!string.IsNullOrWhiteSpace(configuration.SourceCommand.CertificatePassword)) packageElement.CertPassword = configuration.SourceCommand.CertificatePassword;
                                    // Arguments from the global options set
                                    if (configuration.CommandExecutionTimeoutSeconds != ApplicationParameters.DefaultWaitForExitInSeconds)
                                    {
                                        packageElement.ExecutionTimeout = configuration.CommandExecutionTimeoutSeconds;
                                    }
                                    // This was discussed in the PR, and because it is potentially system specific, it should not be included in the exported file
                                    // if (!string.IsNullOrWhiteSpace(configuration.CacheLocation)) packageElement.CacheLocation = configuration.CacheLocation;
                                    // if (configuration.Features.FailOnStandardError) packageElement.FailOnStderr = true;
                                    // if (!configuration.Features.UsePowerShellHost) packageElement.UseSystemPowershell = true;

                                    if (!configuration.ExportCommand.ExcludePins && pkgInfo.IsPinned)
                                    {
                                        xw.WriteAttributeString("pinPackage", "true");
                                    }

                                    // Make sure to reset the configuration so as to be able to parse the next set of remembered arguments
                                    configuration.RevertChanges();
                                }

                                packagesConfig.Packages.Add(packageElement);
                            }

                            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                            ns.Add("", "");

                            var packagesConfigSerializer = new XmlSerializer(typeof(PackagesConfigFileSettings));
                            packagesConfigSerializer.Serialize(xw, packagesConfig, ns);
                        }

                        var fullOutputFilePath = _fileSystem.GetFullPath(configuration.ExportCommand.OutputFilePath);
                        var fileExists = _fileSystem.FileExists(fullOutputFilePath);

                        // If the file doesn't already exist, just write the new one out directly
                        if (!fileExists)
                        {
                            _fileSystem.WriteFile(
                                fullOutputFilePath,
                                stringWriter.GetStringBuilder().ToString(),
                                new UTF8Encoding(false));

                            return;
                        }


                        // Otherwise, create an update file, and resiliently move it into place.
                        var tempUpdateFile = fullOutputFilePath + "." + Process.GetCurrentProcess().Id + ".update";
                        _fileSystem.WriteFile(tempUpdateFile,
                            stringWriter.GetStringBuilder().ToString(),
                            new UTF8Encoding(false));

                        _fileSystem.ReplaceFile(tempUpdateFile, fullOutputFilePath, fullOutputFilePath + ".backup");
                    }
                },
                errorMessage: "Error exporting currently installed packages",
                throwError: true
            );
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
            => ConfigureArgumentParser(optionSet, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
            => ParseAdditionalArguments(unparsedArguments, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_validation(ChocolateyConfiguration configuration)
            => Validate(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void help_message(ChocolateyConfiguration configuration)
            => HelpMessage(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void noop(ChocolateyConfiguration configuration)
            => DryRun(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void run(ChocolateyConfiguration configuration)
            => Run(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual bool may_require_admin_access()
            => MayRequireAdminAccess();
#pragma warning restore IDE0022, IDE1006
    }
}
