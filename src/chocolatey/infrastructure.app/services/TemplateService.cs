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
    using IFileSystem = filesystem.IFileSystem;

    public class TemplateService : ITemplateService
    {
        private readonly UTF8Encoding _utf8WithoutBOM = new UTF8Encoding(false);
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

        public void GenerateDryRun(ChocolateyConfiguration configuration)
        {
            var templateLocation = _fileSystem.CombinePaths(configuration.OutputDirectory ?? _fileSystem.GetCurrentDirectory(), configuration.NewCommand.Name);
            this.Log().Info(() => "Would have generated a new package specification at {0}".FormatWith(templateLocation));
        }

        public void Generate(ChocolateyConfiguration configuration)
        {
            var logger = ChocolateyLoggers.Normal;
            if (configuration.QuietOutput) logger = ChocolateyLoggers.LogFileOnly;

            var packageLocation = _fileSystem.CombinePaths(configuration.OutputDirectory ?? _fileSystem.GetCurrentDirectory(), configuration.NewCommand.Name);
            if (_fileSystem.DirectoryExists(packageLocation) && !configuration.Force)
            {
                throw new ApplicationException(
                    "The location for the template already exists. You can:{0} 1. Remove '{1}'{0} 2. Use --force{0} 3. Specify a different name".FormatWith(Environment.NewLine, packageLocation));
            }

            if (configuration.RegularOutput) this.Log().Info(logger, () => "Creating a new package specification at {0}".FormatWith(packageLocation));
            try
            {
                _fileSystem.DeleteDirectoryChecked(packageLocation, recursive: true);
            }
            catch (Exception ex)
            {
                if (configuration.RegularOutput) this.Log().Warn(() => "{0}".FormatWith(ex.Message));
            }
            _fileSystem.EnsureDirectoryExists(packageLocation);
            var packageToolsLocation = _fileSystem.CombinePaths(packageLocation, "tools");
            _fileSystem.EnsureDirectoryExists(packageToolsLocation);

            var tokens = new TemplateValues();
            if (configuration.NewCommand.AutomaticPackage) tokens.SetAutomatic();

            // now override those values
            foreach (var property in configuration.NewCommand.TemplateProperties)
            {
                try
                {
                    tokens.GetType().GetProperty(property.Key, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase).SetValue(tokens, property.Value, null);
                    this.Log().Debug(() => "Set token for '{0}' to '{1}'".FormatWith(property.Key, property.Value));
                }
                catch (Exception)
                {
                    if (configuration.RegularOutput) this.Log().Debug("Property {0} will be added to additional properties.".FormatWith(property.Key));
                    tokens.AdditionalProperties.Add(property.Key, property.Value);
                }
            }

            this.Log().Debug(() => "Token Values after merge:");
            foreach (var propertyInfo in tokens.GetType().GetProperties())
            {
                this.Log().Debug(() => " {0}={1}".FormatWith(propertyInfo.Name, propertyInfo.GetValue(tokens, null)));
            }
            foreach (var additionalProperty in tokens.AdditionalProperties.OrEmpty())
            {
                this.Log().Debug(() => " {0}={1}".FormatWith(additionalProperty.Key, additionalProperty.Value));
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
                var defaultTemplateNameLocation = _fileSystem.CombinePaths(ApplicationParameters.TemplatesLocation, defaultTemplateName);
                if (!_fileSystem.DirectoryExists(defaultTemplateNameLocation))
                {
                    this.Log().Warn(() => "defaultTemplateName configuration value has been set to '{0}', but no template with that name exists in '{1}'. Reverting to default template.".FormatWith(defaultTemplateName, ApplicationParameters.TemplatesLocation));
                }
                else
                {
                    this.Log().Debug(() => "Setting TemplateName to '{0}'".FormatWith(defaultTemplateName));
                    configuration.NewCommand.TemplateName = defaultTemplateName;
                }
            }

            var defaultTemplateOverride = _fileSystem.CombinePaths(ApplicationParameters.TemplatesLocation, "default");
            if (string.IsNullOrWhiteSpace(configuration.NewCommand.TemplateName) && (!_fileSystem.DirectoryExists(defaultTemplateOverride) || configuration.NewCommand.UseOriginalTemplate))
            {
                GenerateFileFromTemplate(configuration, tokens, NuspecTemplate.Template, _fileSystem.CombinePaths(packageLocation, "{0}.nuspec".FormatWith(tokens.PackageNameLower)), _utf8WithoutBOM);
                GenerateFileFromTemplate(configuration, tokens, ChocolateyInstallTemplate.Template, _fileSystem.CombinePaths(packageToolsLocation, "chocolateyinstall.ps1"), Encoding.UTF8);
                GenerateFileFromTemplate(configuration, tokens, ChocolateyBeforeModifyTemplate.Template, _fileSystem.CombinePaths(packageToolsLocation, "chocolateybeforemodify.ps1"), Encoding.UTF8);
                GenerateFileFromTemplate(configuration, tokens, ChocolateyUninstallTemplate.Template, _fileSystem.CombinePaths(packageToolsLocation, "chocolateyuninstall.ps1"), Encoding.UTF8);
                GenerateFileFromTemplate(configuration, tokens, ChocolateyLicenseFileTemplate.Template, _fileSystem.CombinePaths(packageToolsLocation, "LICENSE.txt"), Encoding.UTF8);
                GenerateFileFromTemplate(configuration, tokens, ChocolateyVerificationFileTemplate.Template, _fileSystem.CombinePaths(packageToolsLocation, "VERIFICATION.txt"), Encoding.UTF8);
                GenerateFileFromTemplate(configuration, tokens, ChocolateyReadMeTemplate.Template, _fileSystem.CombinePaths(packageLocation, "ReadMe.md"), Encoding.UTF8);
                GenerateFileFromTemplate(configuration, tokens, ChocolateyTodoTemplate.Template, _fileSystem.CombinePaths(packageLocation, "_TODO.txt"), Encoding.UTF8);
            }
            else
            {
                configuration.NewCommand.TemplateName = string.IsNullOrWhiteSpace(configuration.NewCommand.TemplateName) ? "default" : configuration.NewCommand.TemplateName;

                var templatePath = _fileSystem.CombinePaths(ApplicationParameters.TemplatesLocation, configuration.NewCommand.TemplateName);
                var templateParameterCachePath = _fileSystem.CombinePaths(templatePath, _templateParameterCacheFilename);
                if (!_fileSystem.DirectoryExists(templatePath)) throw new ApplicationException("Unable to find path to requested template '{0}'. Path should be '{1}'".FormatWith(configuration.NewCommand.TemplateName, templatePath));

                this.Log().Info(configuration.QuietOutput ? logger : ChocolateyLoggers.Important, "Generating package from custom template at '{0}'.".FormatWith(templatePath));

                // Create directory structure from template so as to include empty directories
                foreach (var directory in _fileSystem.GetDirectories(templatePath, "*.*", SearchOption.AllDirectories))
                {
                    var packageDirectoryLocation = directory.Replace(templatePath, packageLocation);
                    this.Log().Debug("Creating directory {0}".FormatWith(packageDirectoryLocation));
                    _fileSystem.EnsureDirectoryExists(packageDirectoryLocation);
                }
                foreach (var file in _fileSystem.GetFiles(templatePath, "*.*", SearchOption.AllDirectories))
                {
                    var packageFileLocation = file.Replace(templatePath, packageLocation);
                    var fileExtension = _fileSystem.GetFileExtension(packageFileLocation);

                    if (fileExtension.IsEqualTo(".nuspec"))
                    {
                        packageFileLocation = _fileSystem.CombinePaths(packageLocation, "{0}.nuspec".FormatWith(tokens.PackageNameLower));
                        GenerateFileFromTemplate(configuration, tokens, _fileSystem.ReadFile(file), packageFileLocation, _utf8WithoutBOM);
                    }
                    else if (_templateBinaryExtensions.Contains(fileExtension))
                    {
                        this.Log().Debug(" Treating template file ('{0}') as binary instead of replacing templated values.".FormatWith(_fileSystem.GetFileName(file)));
                        _fileSystem.CopyFile(file, packageFileLocation, overwriteExisting:true);
                    }
                    else if (templateParameterCachePath.IsEqualTo(file))
                    {
                        this.Log().Debug("{0} is the parameter cache file, ignoring".FormatWith(file));
                    }
                    else
                    {
                        GenerateFileFromTemplate(configuration, tokens, _fileSystem.ReadFile(file), packageFileLocation, Encoding.UTF8);
                    }
                }
            }

            this.Log().Info(configuration.QuietOutput ? logger : ChocolateyLoggers.Important,
                "Successfully generated {0}{1} package specification files{2} at '{3}'".FormatWith(
                    configuration.NewCommand.Name, configuration.NewCommand.AutomaticPackage ? " (automatic)" : string.Empty, Environment.NewLine, packageLocation));
        }

        public void GenerateFileFromTemplate(ChocolateyConfiguration configuration, TemplateValues tokens, string template, string fileLocation, Encoding encoding)
        {
            template = TokenReplacer.ReplaceTokens(tokens, template);
            template = TokenReplacer.ReplaceTokens(tokens.AdditionalProperties, template);

            if (configuration.RegularOutput) this.Log().Info(() => "Generating template to a file{0} at '{1}'".FormatWith(Environment.NewLine, fileLocation));
            this.Log().Debug(() => "{0}".FormatWith(template));
            _fileSystem.EnsureDirectoryExists(_fileSystem.GetDirectoryName(fileLocation));
            _fileSystem.WriteFile(fileLocation, template, encoding);
        }

        public void ListDryRun(ChocolateyConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.TemplateCommand.Name))
            {
                this.Log().Info(() => "Would have listed templates in {0}".FormatWith(ApplicationParameters.TemplatesLocation));
            }
            else
            {
                this.Log().Info(() => "Would have listed information about {0}".FormatWith(configuration.TemplateCommand.Name));
            }
        }

        public void List(ChocolateyConfiguration configuration)
        {
            var templateDirList = _fileSystem.GetDirectories(ApplicationParameters.TemplatesLocation).ToList();
            var isBuiltInTemplateOverridden = templateDirList.Contains(_fileSystem.CombinePaths(ApplicationParameters.TemplatesLocation, _builtInTemplateOverrideName));
            var isBuiltInOrDefaultTemplateDefault = string.IsNullOrWhiteSpace(configuration.DefaultTemplateName) || !templateDirList.Contains(_fileSystem.CombinePaths(ApplicationParameters.TemplatesLocation, configuration.DefaultTemplateName));

            if (string.IsNullOrWhiteSpace(configuration.TemplateCommand.Name))
            {
                if (templateDirList.Any())
                {
                    foreach (var templateDir in templateDirList)
                    {
                        configuration.TemplateCommand.Name = _fileSystem.GetFileName(templateDir);
                        ListCustomTemplateInformation(configuration);
                    }

                    this.Log().Info(configuration.RegularOutput ? "{0} Custom templates found at {1}{2}".FormatWith(templateDirList.Count(), ApplicationParameters.TemplatesLocation, Environment.NewLine) : string.Empty);
                }
                else
                {
                    this.Log().Info(configuration.RegularOutput ? "No custom templates installed in {0}{1}".FormatWith(ApplicationParameters.TemplatesLocation, Environment.NewLine) : string.Empty);
                }

                ListBuiltinTemplateInformation(configuration, isBuiltInTemplateOverridden, isBuiltInOrDefaultTemplateDefault);
            }
            else
            {
                if (templateDirList.Contains(_fileSystem.CombinePaths(ApplicationParameters.TemplatesLocation, configuration.TemplateCommand.Name)))
                {
                    ListCustomTemplateInformation(configuration);
                    if (configuration.TemplateCommand.Name == _builtInTemplateName || configuration.TemplateCommand.Name == _builtInTemplateOverrideName)
                    {
                        ListBuiltinTemplateInformation(configuration, isBuiltInTemplateOverridden, isBuiltInOrDefaultTemplateDefault);
                    }
                }
                else
                {
                    if (configuration.TemplateCommand.Name.ToLowerInvariant() == _builtInTemplateName || configuration.TemplateCommand.Name.ToLowerInvariant() == _builtInTemplateOverrideName)
                    {
                        // We know that the template is not overridden since the template directory was checked
                        ListBuiltinTemplateInformation(configuration, isBuiltInTemplateOverridden, isBuiltInOrDefaultTemplateDefault);
                    }
                    else
                    {
                        throw new ApplicationException("Unable to find requested template '{0}'".FormatWith(configuration.TemplateCommand.Name));
                    }
                }
            }
        }

        protected void ListCustomTemplateInformation(ChocolateyConfiguration configuration)
        {
            var sourceCacheContext = new ChocolateySourceCacheContext(configuration);
            var packageResources = NugetCommon.GetRepositoryResources(configuration, _nugetLogger, _fileSystem, sourceCacheContext);
            var pkg = NugetList.FindPackage(
                    "{0}.template".FormatWith(configuration.TemplateCommand.Name),
                    configuration,
                    _nugetLogger,
                    sourceCacheContext,
                    packageResources);

            var templateInstalledViaPackage = (pkg != null);

            var pkgVersion = templateInstalledViaPackage ? pkg.Identity.Version.ToNormalizedStringChecked() : "0.0.0";
            var pkgTitle = templateInstalledViaPackage ? pkg.Title : "{0} (Unmanaged)".FormatWith(configuration.TemplateCommand.Name);
            var pkgSummary = templateInstalledViaPackage ?
                (pkg.Summary != null && !string.IsNullOrWhiteSpace(pkg.Summary.ToStringSafe()) ? "{0}".FormatWith(pkg.Summary.EscapeCurlyBraces().ToStringSafe()) : string.Empty) : string.Empty;
            var pkgDescription = templateInstalledViaPackage ? pkg.Description.EscapeCurlyBraces().Replace("\n    ", "\n").Replace("\n", "\n  ") : string.Empty;
            var pkgFiles = "  {0}".FormatWith(string.Join("{0}  "
                .FormatWith(Environment.NewLine), _fileSystem.GetFiles(_fileSystem
                    .CombinePaths(ApplicationParameters.TemplatesLocation, configuration.TemplateCommand.Name), "*", SearchOption.AllDirectories)));
            var isOverridingBuiltIn = configuration.TemplateCommand.Name == _builtInTemplateOverrideName;
            var isDefault = string.IsNullOrWhiteSpace(configuration.DefaultTemplateName) ? isOverridingBuiltIn : (configuration.DefaultTemplateName == configuration.TemplateCommand.Name);
            var templateParams = "  {0}".FormatWith(string.Join("{0}  ".FormatWith(Environment.NewLine), GetTemplateParameters(configuration, templateInstalledViaPackage)));

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
".FormatWith(configuration.TemplateCommand.Name,
                        pkgVersion,
                        isDefault,
                        isOverridingBuiltIn ? "This template is overriding the built in template{0}".FormatWith(Environment.NewLine) : string.Empty,
                        pkgTitle,
                        string.IsNullOrEmpty(pkgSummary) ? "Template not installed as a package" : "Summary: {0}".FormatWith(pkgSummary),
                        string.IsNullOrEmpty(pkgDescription) ? string.Empty : "{0}Description:{0}  {1}".FormatWith(Environment.NewLine, pkgDescription),
                        pkgFiles,
                        templateParams));
                }
                else
                {
                    this.Log().Info("{0} {1} {2}".FormatWith((isDefault ? '*' : ' '), configuration.TemplateCommand.Name, pkgVersion));
                }
            }
            else
            {
                this.Log().Info("{0}|{1}".FormatWith(configuration.TemplateCommand.Name, pkgVersion));
            }
        }

        protected void ListBuiltinTemplateInformation(ChocolateyConfiguration configuration, bool isOverridden, bool isDefault)
        {
            if (configuration.RegularOutput)
            {
                if (isOverridden)
                {
                    this.Log().Info("Built-in template overridden by 'default' template.{0}".FormatWith(Environment.NewLine));
                }
                else
                {
                    if (isDefault)
                    {
                        this.Log().Info("Built-in template is default.{0}".FormatWith(Environment.NewLine));
                    }
                    else
                    {
                        this.Log().Info("Built-in template is not default, it can be specified if the --built-in parameter is used{0}".FormatWith(Environment.NewLine));
                    }
                }
                if (configuration.Verbose)
                {
                    this.Log().Info("Help about the built-in template can be found with 'choco new --help'{0}".FormatWith(Environment.NewLine));
                }
            }
            else
            {
                //If reduced output, only print out the built in template if it is not overridden
                if (!isOverridden)
                {
                    this.Log().Info("built-in|0.0.0");
                }
            }
        }

        protected IEnumerable<string> GetTemplateParameters(ChocolateyConfiguration configuration, bool templateInstalledViaPackage)
        {
            // If the template was installed via package, the cache file gets removed on upgrade, so the cache file would be up to date if it exists
            if (templateInstalledViaPackage)
            {
                var templateDirectory = _fileSystem.CombinePaths(ApplicationParameters.TemplatesLocation, configuration.TemplateCommand.Name);
                var cacheFilePath = _fileSystem.CombinePaths(templateDirectory, _templateParameterCacheFilename);

                if (!_fileSystem.FileExists(cacheFilePath))
                {
                    _xmlService.Serialize(GetTemplateParametersFromFiles(configuration).ToList(), cacheFilePath);
                }

                return _xmlService.Deserialize<List<string>>(cacheFilePath);
            }
            // If the template is not installed via a package, always read the parameters directly as the template may have been updated manually

            return GetTemplateParametersFromFiles(configuration).ToList();
        }

        protected HashSet<string> GetTemplateParametersFromFiles(ChocolateyConfiguration configuration)
        {
            var filesList = _fileSystem.GetFiles(_fileSystem.CombinePaths(ApplicationParameters.TemplatesLocation, configuration.TemplateCommand.Name), "*", SearchOption.AllDirectories);
            var parametersList = new HashSet<string>();

            foreach (var filePath in filesList)
            {
                if (_templateBinaryExtensions.Contains(_fileSystem.GetFileExtension(filePath)))
                {
                    this.Log().Debug("{0} is a binary file, not reading parameters".FormatWith(filePath));
                    continue;
                }

                if (_fileSystem.GetFileName(filePath) == _templateParameterCacheFilename)
                {
                    this.Log().Debug("{0} is the parameter cache file, not reading parameters".FormatWith(filePath));
                    continue;
                }

                var fileContents = _fileSystem.ReadFile(filePath);
                parametersList.UnionWith(TokenReplacer.GetTokens(fileContents, "[[", "]]"));
            }

            return parametersList;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void generate_noop(ChocolateyConfiguration configuration)
            => GenerateDryRun(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void generate(ChocolateyConfiguration configuration)
            => Generate(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void generate_file_from_template(ChocolateyConfiguration configuration, TemplateValues tokens, string template, string fileLocation, Encoding encoding)
            => GenerateFileFromTemplate(configuration, tokens, template, fileLocation, encoding);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void list_noop(ChocolateyConfiguration configuration)
            => ListDryRun(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void list(ChocolateyConfiguration configuration)
            => List(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected void list_custom_template_info(ChocolateyConfiguration configuration)
            => ListCustomTemplateInformation(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected void list_built_in_template_info(ChocolateyConfiguration configuration, bool isOverridden, bool isDefault)
            => ListBuiltinTemplateInformation(configuration, isOverridden, isDefault);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected IEnumerable<string> get_template_parameters(ChocolateyConfiguration configuration, bool templateInstalledViaPackage)
            => GetTemplateParameters(configuration, templateInstalledViaPackage);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected HashSet<string> get_template_parameters_from_files(ChocolateyConfiguration configuration)
            => GetTemplateParametersFromFiles(configuration);
#pragma warning restore IDE1006
    }
}
