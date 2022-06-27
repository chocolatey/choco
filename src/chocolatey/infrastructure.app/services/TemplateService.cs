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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using configuration;
    using infrastructure.services;
    using logging;
    using templates;
    using tokens;
    using nuget;
    using NuGet.Common;
    using NuGet.PackageManagement;
    using NuGet.Protocol.Core.Types;
    using NuGet.Versioning;
    using IFileSystem = filesystem.IFileSystem;

    public class TemplateService : ITemplateService
    {
        private readonly UTF8Encoding utf8WithoutBOM = new UTF8Encoding(false);
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _nugetLogger;
        private readonly IXmlService _xmlService;

        private readonly IList<string> _templateBinaryExtensions = new List<string> {
            ".exe", ".msi", ".msu", ".msp", ".mst",
            ".7z", ".zip", ".rar", ".gz", ".iso", ".tar", ".sfx",
            ".dmg",
            ".cer", ".crt", ".der", ".p7b", ".pfx", ".p12", ".pem"
            };

        private readonly string _builtInTemplateOverrideName = "default";
        private readonly string _builtInTemplateName = "built-in";
        private readonly string _templateParameterCacheFilename = ".parameters";

        public TemplateService(IFileSystem fileSystem, IXmlService xmlService, ILogger logger)
        {
            _fileSystem = fileSystem;
            _xmlService = xmlService;
            _nugetLogger = logger;
        }

        public void generate_noop(ChocolateyConfiguration configuration)
        {
            var templateLocation = _fileSystem.combine_paths(configuration.OutputDirectory ?? _fileSystem.get_current_directory(), configuration.NewCommand.Name);
            this.Log().Info(() => "Would have generated a new package specification at {0}".format_with(templateLocation));
        }

        public void generate(ChocolateyConfiguration configuration)
        {
            var logger = ChocolateyLoggers.Normal;
            if (configuration.QuietOutput) logger = ChocolateyLoggers.LogFileOnly;

            var packageLocation = _fileSystem.combine_paths(configuration.OutputDirectory ?? _fileSystem.get_current_directory(), configuration.NewCommand.Name);
            if (_fileSystem.directory_exists(packageLocation) && !configuration.Force)
            {
                throw new ApplicationException(
                    "The location for the template already exists. You can:{0} 1. Remove '{1}'{0} 2. Use --force{0} 3. Specify a different name".format_with(Environment.NewLine, packageLocation));
            }

            if (configuration.RegularOutput) this.Log().Info(logger, () => "Creating a new package specification at {0}".format_with(packageLocation));
            try
            {
                _fileSystem.delete_directory_if_exists(packageLocation, recursive: true);
            }
            catch (Exception ex)
            {
                if (configuration.RegularOutput) this.Log().Warn(() => "{0}".format_with(ex.Message));
            }
            _fileSystem.create_directory_if_not_exists(packageLocation);
            var packageToolsLocation = _fileSystem.combine_paths(packageLocation, "tools");
            _fileSystem.create_directory_if_not_exists(packageToolsLocation);

            var tokens = new TemplateValues();
            if (configuration.NewCommand.AutomaticPackage) tokens.set_auto();

            // now override those values
            foreach (var property in configuration.NewCommand.TemplateProperties)
            {
                try
                {
                    tokens.GetType().GetProperty(property.Key, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase).SetValue(tokens, property.Value, null);
                    this.Log().Debug(() => "Set token for '{0}' to '{1}'".format_with(property.Key, property.Value));
                }
                catch (Exception)
                {
                    if (configuration.RegularOutput) this.Log().Debug("Property {0} will be added to additional properties.".format_with(property.Key));
                    tokens.AdditionalProperties.Add(property.Key, property.Value);
                }
            }

            this.Log().Debug(() => "Token Values after merge:");
            foreach (var propertyInfo in tokens.GetType().GetProperties())
            {
                this.Log().Debug(() => " {0}={1}".format_with(propertyInfo.Name, propertyInfo.GetValue(tokens, null)));
            }
            foreach (var additionalProperty in tokens.AdditionalProperties.or_empty_list_if_null())
            {
                this.Log().Debug(() => " {0}={1}".format_with(additionalProperty.Key, additionalProperty.Value));
            }

            // Attempt to set the name of the template that will be used to generate the new package
            // If no template name has been passed at the command line, check to see if there is a defaultTemplateName set in the
            // chocolatey.config file. If there is, and this template exists on disk, use it.
            // Otherwise, revert to the built in default template.
            // In addition, if the command line option to use the built-in template has been set, respect that
            // and use the built in template.
            var defaultTemplateName = configuration.DefaultTemplateName;
            if (string.IsNullOrWhiteSpace(configuration.NewCommand.TemplateName) && !string.IsNullOrWhiteSpace(defaultTemplateName) && !configuration.NewCommand.UseOriginalTemplate)
            {
                var defaultTemplateNameLocation = _fileSystem.combine_paths(ApplicationParameters.TemplatesLocation, defaultTemplateName);
                if (!_fileSystem.directory_exists(defaultTemplateNameLocation))
                {
                    this.Log().Warn(() => "defaultTemplateName configuration value has been set to '{0}', but no template with that name exists in '{1}'. Reverting to default template.".format_with(defaultTemplateName, ApplicationParameters.TemplatesLocation));
                }
                else
                {
                    this.Log().Debug(() => "Setting TemplateName to '{0}'".format_with(defaultTemplateName));
                    configuration.NewCommand.TemplateName = defaultTemplateName;
                }
            }

            var defaultTemplateOverride = _fileSystem.combine_paths(ApplicationParameters.TemplatesLocation, "default");
            if (string.IsNullOrWhiteSpace(configuration.NewCommand.TemplateName) && (!_fileSystem.directory_exists(defaultTemplateOverride) || configuration.NewCommand.UseOriginalTemplate))
            {
                generate_file_from_template(configuration, tokens, NuspecTemplate.Template, _fileSystem.combine_paths(packageLocation, "{0}.nuspec".format_with(tokens.PackageNameLower)), utf8WithoutBOM);
                generate_file_from_template(configuration, tokens, ChocolateyInstallTemplate.Template, _fileSystem.combine_paths(packageToolsLocation, "chocolateyinstall.ps1"), Encoding.UTF8);
                generate_file_from_template(configuration, tokens, ChocolateyBeforeModifyTemplate.Template, _fileSystem.combine_paths(packageToolsLocation, "chocolateybeforemodify.ps1"), Encoding.UTF8);
                generate_file_from_template(configuration, tokens, ChocolateyUninstallTemplate.Template, _fileSystem.combine_paths(packageToolsLocation, "chocolateyuninstall.ps1"), Encoding.UTF8);
                generate_file_from_template(configuration, tokens, ChocolateyLicenseFileTemplate.Template, _fileSystem.combine_paths(packageToolsLocation, "LICENSE.txt"), Encoding.UTF8);
                generate_file_from_template(configuration, tokens, ChocolateyVerificationFileTemplate.Template, _fileSystem.combine_paths(packageToolsLocation, "VERIFICATION.txt"), Encoding.UTF8);
                generate_file_from_template(configuration, tokens, ChocolateyReadMeTemplate.Template, _fileSystem.combine_paths(packageLocation, "ReadMe.md"), Encoding.UTF8);
                generate_file_from_template(configuration, tokens, ChocolateyTodoTemplate.Template, _fileSystem.combine_paths(packageLocation, "_TODO.txt"), Encoding.UTF8);
            }
            else
            {
                configuration.NewCommand.TemplateName = string.IsNullOrWhiteSpace(configuration.NewCommand.TemplateName) ? "default" : configuration.NewCommand.TemplateName;

                var templatePath = _fileSystem.combine_paths(ApplicationParameters.TemplatesLocation, configuration.NewCommand.TemplateName);
                var templateParameterCachePath = _fileSystem.combine_paths(templatePath, _templateParameterCacheFilename);
                if (!_fileSystem.directory_exists(templatePath)) throw new ApplicationException("Unable to find path to requested template '{0}'. Path should be '{1}'".format_with(configuration.NewCommand.TemplateName, templatePath));

                this.Log().Info(configuration.QuietOutput ? logger : ChocolateyLoggers.Important, "Generating package from custom template at '{0}'.".format_with(templatePath));

                // Create directory structure from template so as to include empty directories
                foreach (var directory in _fileSystem.get_directories(templatePath, "*.*", SearchOption.AllDirectories))
                {
                    var packageDirectoryLocation = directory.Replace(templatePath, packageLocation);
                    this.Log().Debug("Creating directory {0}".format_with(packageDirectoryLocation));
                    _fileSystem.create_directory_if_not_exists(packageDirectoryLocation);
                }
                foreach (var file in _fileSystem.get_files(templatePath, "*.*", SearchOption.AllDirectories))
                {
                    var packageFileLocation = file.Replace(templatePath, packageLocation);
                    var fileExtension = _fileSystem.get_file_extension(packageFileLocation);

                    if (fileExtension.is_equal_to(".nuspec"))
                    {
                        packageFileLocation = _fileSystem.combine_paths(packageLocation, "{0}.nuspec".format_with(tokens.PackageNameLower));
                        generate_file_from_template(configuration, tokens, _fileSystem.read_file(file), packageFileLocation, utf8WithoutBOM);
                    }
                    else if (_templateBinaryExtensions.Contains(fileExtension))
                    {
                        this.Log().Debug(" Treating template file ('{0}') as binary instead of replacing templated values.".format_with(_fileSystem.get_file_name(file)));
                        _fileSystem.copy_file(file, packageFileLocation, overwriteExisting:true);
                    }
                    else if (templateParameterCachePath.is_equal_to(file))
                    {
                        this.Log().Debug("{0} is the parameter cache file, ignoring".format_with(file));
                    }
                    else
                    {
                        generate_file_from_template(configuration, tokens, _fileSystem.read_file(file), packageFileLocation, Encoding.UTF8);
                    }
                }
            }

            this.Log().Info(configuration.QuietOutput ? logger : ChocolateyLoggers.Important,
                "Successfully generated {0}{1} package specification files{2} at '{3}'".format_with(
                    configuration.NewCommand.Name, configuration.NewCommand.AutomaticPackage ? " (automatic)" : string.Empty, Environment.NewLine, packageLocation));
        }

        public void generate_file_from_template(ChocolateyConfiguration configuration, TemplateValues tokens, string template, string fileLocation, Encoding encoding)
        {
            template = TokenReplacer.replace_tokens(tokens, template);
            template = TokenReplacer.replace_tokens(tokens.AdditionalProperties, template);

            if (configuration.RegularOutput) this.Log().Info(() => "Generating template to a file{0} at '{1}'".format_with(Environment.NewLine, fileLocation));
            this.Log().Debug(() => "{0}".format_with(template));
            _fileSystem.create_directory_if_not_exists(_fileSystem.get_directory_name(fileLocation));
            _fileSystem.write_file(fileLocation, template, encoding);
        }

        public void list_noop(ChocolateyConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.TemplateCommand.Name))
            {
                this.Log().Info(() => "Would have listed templates in {0}".format_with(ApplicationParameters.TemplatesLocation));
            }
            else
            {
                this.Log().Info(() => "Would have listed information about {0}".format_with(configuration.TemplateCommand.Name));
            }
        }

        public void list(ChocolateyConfiguration configuration)
        {
            var templateDirList = _fileSystem.get_directories(ApplicationParameters.TemplatesLocation).ToList();
            var isBuiltInTemplateOverriden = templateDirList.Contains(_fileSystem.combine_paths(ApplicationParameters.TemplatesLocation, _builtInTemplateOverrideName));
            var isBuiltInOrDefaultTemplateDefault = string.IsNullOrWhiteSpace(configuration.DefaultTemplateName) || !templateDirList.Contains(_fileSystem.combine_paths(ApplicationParameters.TemplatesLocation, configuration.DefaultTemplateName));

            if (string.IsNullOrWhiteSpace(configuration.TemplateCommand.Name))
            {
                if (templateDirList.Any())
                {
                    foreach (var templateDir in templateDirList)
                    {
                        configuration.TemplateCommand.Name = _fileSystem.get_file_name(templateDir);
                        list_custom_template_info(configuration);
                    }

                    this.Log().Info(configuration.RegularOutput ? "{0} Custom templates found at {1}{2}".format_with(templateDirList.Count(), ApplicationParameters.TemplatesLocation, Environment.NewLine) : string.Empty);
                }
                else
                {
                    this.Log().Info(configuration.RegularOutput ? "No custom templates installed in {0}{1}".format_with(ApplicationParameters.TemplatesLocation, Environment.NewLine) : string.Empty);
                }

                list_built_in_template_info(configuration, isBuiltInTemplateOverriden, isBuiltInOrDefaultTemplateDefault);
            }
            else
            {
                if (templateDirList.Contains(_fileSystem.combine_paths(ApplicationParameters.TemplatesLocation, configuration.TemplateCommand.Name)))
                {
                    list_custom_template_info(configuration);
                    if (configuration.TemplateCommand.Name == _builtInTemplateName || configuration.TemplateCommand.Name == _builtInTemplateOverrideName)
                    {
                        list_built_in_template_info(configuration, isBuiltInTemplateOverriden, isBuiltInOrDefaultTemplateDefault);
                    }
                }
                else
                {
                    if (configuration.TemplateCommand.Name.ToLowerInvariant() == _builtInTemplateName || configuration.TemplateCommand.Name.ToLowerInvariant() == _builtInTemplateOverrideName)
                    {
                        // We know that the template is not overriden since the template directory was checked
                        list_built_in_template_info(configuration, isBuiltInTemplateOverriden, isBuiltInOrDefaultTemplateDefault);
                    }
                    else
                    {
                        throw new ApplicationException("Unable to find requested template '{0}'".format_with(configuration.TemplateCommand.Name));
                    }
                }
            }
        }

        protected void list_custom_template_info(ChocolateyConfiguration configuration)
        {
            var packageRepositories = NugetCommon.GetRemoteRepositories(configuration, _nugetLogger);
            var sourceCacheContext = new ChocolateySourceCacheContext(configuration);
            var pkg = NugetList.find_package(
                    "{0}.template".format_with(configuration.TemplateCommand.Name),
                    configuration,
                    _nugetLogger,
                    sourceCacheContext,
                    NugetCommon.GetRepositoryResource<PackageMetadataResource>(packageRepositories));

            var templateInstalledViaPackage = (pkg != null);

            var pkgVersion = templateInstalledViaPackage ? pkg.Identity.Version.to_string() : "0.0.0";
            var pkgTitle = templateInstalledViaPackage ? pkg.Title : "{0} (Unmanaged)".format_with(configuration.TemplateCommand.Name);
            var pkgSummary = templateInstalledViaPackage ?
                (pkg.Summary != null && !string.IsNullOrWhiteSpace(pkg.Summary.to_string()) ? "{0}".format_with(pkg.Summary.escape_curly_braces().to_string()) : string.Empty) : string.Empty;
            var pkgDescription = templateInstalledViaPackage ? pkg.Description.escape_curly_braces().Replace("\n    ", "\n").Replace("\n", "\n  ") : string.Empty;
            var pkgFiles = "  {0}".format_with(string.Join("{0}  "
                .format_with(Environment.NewLine), _fileSystem.get_files(_fileSystem
                    .combine_paths(ApplicationParameters.TemplatesLocation, configuration.TemplateCommand.Name), "*", SearchOption.AllDirectories)));
            var isOverridingBuiltIn = configuration.TemplateCommand.Name == _builtInTemplateOverrideName;
            var isDefault = string.IsNullOrWhiteSpace(configuration.DefaultTemplateName) ? isOverridingBuiltIn : (configuration.DefaultTemplateName == configuration.TemplateCommand.Name);
            var templateParams = "  {0}".format_with(string.Join("{0}  ".format_with(Environment.NewLine), get_template_parameters(configuration, templateInstalledViaPackage)));

            if (configuration.RegularOutput)
            {
                if (configuration.Verbose)
                {
                    this.Log().Info(@"Template name: {0}
Version: {1}
Default template: {2}
{3}Title: {4}
{5}{6}
List of files:
{7}
List of Parameters:
{8}
".format_with(configuration.TemplateCommand.Name,
                        pkgVersion,
                        isDefault,
                        isOverridingBuiltIn ? "This template is overriding the built in template{0}".format_with(Environment.NewLine) : string.Empty,
                        pkgTitle,
                        string.IsNullOrEmpty(pkgSummary) ? "Template not installed as a package" : "Summary: {0}".format_with(pkgSummary),
                        string.IsNullOrEmpty(pkgDescription) ? string.Empty : "{0}Description:{0}  {1}".format_with(Environment.NewLine, pkgDescription),
                        pkgFiles,
                        templateParams));
                }
                else
                {
                    this.Log().Info("{0} {1} {2}".format_with((isDefault ? '*' : ' '), configuration.TemplateCommand.Name, pkgVersion));
                }
            }
            else
            {
                this.Log().Info("{0}|{1}".format_with(configuration.TemplateCommand.Name, pkgVersion));
            }
        }

        protected void list_built_in_template_info(ChocolateyConfiguration configuration, bool isOverridden, bool isDefault)
        {
            if (configuration.RegularOutput)
            {
                if (isOverridden)
                {
                    this.Log().Info("Built-in template overriden by 'default' template.{0}".format_with(Environment.NewLine));
                }
                else
                {
                    if (isDefault)
                    {
                        this.Log().Info("Built-in template is default.{0}".format_with(Environment.NewLine));
                    }
                    else
                    {
                        this.Log().Info("Built-in template is not default, it can be specified if the --built-in parameter is used{0}".format_with(Environment.NewLine));
                    }
                }
                if (configuration.Verbose)
                {
                    this.Log().Info("Help about the built-in template can be found with 'choco new --help'{0}".format_with(Environment.NewLine));
                }
            }
            else
            {
                //If reduced output, only print out the built in template if it is not overriden
                if (!isOverridden)
                {
                    this.Log().Info("built-in|0.0.0");
                }
            }
        }

        protected IEnumerable<string> get_template_parameters(ChocolateyConfiguration configuration, bool templateInstalledViaPackage)
        {
            // If the template was installed via package, the cache file gets removed on upgrade, so the cache file would be up to date if it exists
            if (templateInstalledViaPackage)
            {
                var templateDirectory = _fileSystem.combine_paths(ApplicationParameters.TemplatesLocation, configuration.TemplateCommand.Name);
                var cacheFilePath = _fileSystem.combine_paths(templateDirectory, _templateParameterCacheFilename);

                if (!_fileSystem.file_exists(cacheFilePath))
                {
                    _xmlService.serialize(get_template_parameters_from_files(configuration).ToList(), cacheFilePath);
                }

                return _xmlService.deserialize<List<string>>(cacheFilePath);
            }
            // If the template is not installed via a package, always read the parameters directly as the template may have been updated manually

            return get_template_parameters_from_files(configuration).ToList();
        }

        protected HashSet<string> get_template_parameters_from_files(ChocolateyConfiguration configuration)
        {
            var filesList = _fileSystem.get_files(_fileSystem.combine_paths(ApplicationParameters.TemplatesLocation, configuration.TemplateCommand.Name), "*", SearchOption.AllDirectories);
            var parametersList = new HashSet<string>();

            foreach (var filePath in filesList)
            {
                if (_templateBinaryExtensions.Contains(_fileSystem.get_file_extension(filePath)))
                {
                    this.Log().Debug("{0} is a binary file, not reading parameters".format_with(filePath));
                    continue;
                }

                if (_fileSystem.get_file_name(filePath) == _templateParameterCacheFilename)
                {
                    this.Log().Debug("{0} is the parameter cache file, not reading parameters".format_with(filePath));
                    continue;
                }

                var fileContents = _fileSystem.read_file(filePath);
                parametersList.UnionWith(TokenReplacer.get_tokens(fileContents, "[[", "]]"));
            }

            return parametersList;
        }
    }
}
