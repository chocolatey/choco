// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Xml;
    using attributes;
    using commandline;
    using configuration;
    using filesystem;
    using infrastructure.commands;
    using logging;
    using services;
    using tolerance;

    [CommandFor("export", "exports list of currently installed packages")]
    public class ChocolateyExportCommand : ICommand
    {
        private readonly INugetService _nugetService;
        private readonly IFileSystem _fileSystem;

        public ChocolateyExportCommand(INugetService nugetService, IFileSystem fileSystem)
        {
            _nugetService = nugetService;
            _fileSystem = fileSystem;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("o=|output-file-path=",
                     "Output File Path - the path to where the list of currently installed packages should be saved. Defaults to packages.config.",
                     option => configuration.ExportCommand.OutputFilePath = option.remove_surrounding_quotes())
                .Add("include-version-numbers|include-version",
                     "Include Version Numbers - controls whether or not version numbers for each package appear in generated file.  Defaults to false.",
                     option => configuration.ExportCommand.IncludeVersionNumbers = option != null)
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);

            if (string.IsNullOrWhiteSpace(configuration.ExportCommand.OutputFilePath) && unparsedArguments.Count >=1)
            {
                configuration.ExportCommand.OutputFilePath = unparsedArguments[0];
            }

            // If no value has been provided for the OutputFilePath, default to packages.config
            if (string.IsNullOrWhiteSpace(configuration.ExportCommand.OutputFilePath))
            {
                configuration.ExportCommand.OutputFilePath = "packages.config";
            }
        }

        public void handle_validation(ChocolateyConfiguration configuration)
        {
            // Currently, no additional validation is required.
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Export Command");
            this.Log().Info(@"
Export all currently installed packages to a file.

This is especially helpful when re-building a machine that was created
using Chocolatey.  Export all packages to a file, and then re-install
those packages onto new machine using `choco install packages.config`.

NOTE: Available with 0.11.0+.
");
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco export [<options/switches>]
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco export
    choco export --include-version-numbers
    choco export ""'c:\temp\packages.config'""
    choco export ""'c:\temp\packages.config'"" --include-version-numbers
    choco export -o=""'c:\temp\packages.config'""
    choco export -o=""'c:\temp\packages.config'"" --include-version-numbers
    choco export --output-file-path=""'c:\temp\packages.config'""
    choco export --output-file-path=""'c:\temp\packages.config'"" --include-version-numbers

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

        public bool may_require_admin_access()
        {
            return false;
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            this.Log().Info("Export would have been with options: {0} Output File Path={1}{0} Include Version Numbers:{2}".format_with(Environment.NewLine, configuration.ExportCommand.OutputFilePath, configuration.ExportCommand.IncludeVersionNumbers));
        }

        public void run(ChocolateyConfiguration configuration)
        {
            var packageResults = _nugetService.get_all_installed_packages(configuration);
            var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };

            FaultTolerance.try_catch_with_logging_exception(
                () =>
                {
                    using (var stringWriter = new StringWriter())
                    {
                        using (var xw = XmlWriter.Create(stringWriter, settings))
                        {
                            xw.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");
                            xw.WriteStartElement("packages");

                            foreach (var packageResult in packageResults)
                            {
                                xw.WriteStartElement("package");
                                xw.WriteAttributeString("id", packageResult.PackageMetadata.Id);

                                if (configuration.ExportCommand.IncludeVersionNumbers)
                                {
                                    xw.WriteAttributeString("version", packageResult.PackageMetadata.Version.ToString());
                                }

                                xw.WriteEndElement();
                            }

                            xw.WriteEndElement();
                            xw.Flush();
                        }

                        var fullOutputFilePath = _fileSystem.get_full_path(configuration.ExportCommand.OutputFilePath);
                        var fileExists = _fileSystem.file_exists(fullOutputFilePath);

                        // If the file doesn't already exist, just write the new one out directly
                        if (!fileExists)
                        {
                            _fileSystem.write_file(
                                fullOutputFilePath,
                                stringWriter.GetStringBuilder().ToString(),
                                new UTF8Encoding(false));

                            return;
                        }


                        // Otherwise, create an update file, and resiliently move it into place.
                        var tempUpdateFile = fullOutputFilePath + "." + Process.GetCurrentProcess().Id + ".update";
                        _fileSystem.write_file(tempUpdateFile,
                            stringWriter.GetStringBuilder().ToString(),
                            new UTF8Encoding(false));

                        _fileSystem.replace_file(tempUpdateFile, fullOutputFilePath, fullOutputFilePath + ".backup");
                    }
                },
                errorMessage: "Error exporting currently installed packages",
                throwError: true
            );
        }
    }
}
