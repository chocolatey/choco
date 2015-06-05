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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Reflection;
    using System.Text;
    using configuration;
    using filesystem;
    using logging;
    using templates;
    using tokens;

    public class TemplateService : ITemplateService
    {
        private readonly IFileSystem _fileSystem;

        public TemplateService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            var templateLocation = _fileSystem.combine_paths(_fileSystem.get_current_directory(), configuration.NewCommand.Name);
            this.Log().Info(() => "Would have generated a new package specification at {0}".format_with(templateLocation));
        }

        public void generate(ChocolateyConfiguration configuration)
        {
            var packageLocation = _fileSystem.combine_paths(_fileSystem.get_current_directory(), configuration.NewCommand.Name);
            if (_fileSystem.directory_exists(packageLocation) && !configuration.Force)
            {
                throw new ApplicationException("The location for the template already exists. You can:{0} 1. Remove '{1}'{0} 2. Use --force{0} 3. Specify a different name".format_with(Environment.NewLine, packageLocation));
            }

            if (configuration.RegularOutput) this.Log().Info(() => "Creating a new package specification at {0}".format_with(packageLocation));
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
            if (configuration.NewCommand.AutomaticPackage)
            {
                tokens.set_auto();
            }

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
                    if (configuration.RegularOutput) this.Log().Warn("Property {0} was not found for replacement in the Template Values.".format_with(property.Key));
                }
            }

            this.Log().Debug(() => "Token Values after merge:");
            foreach (var propertyInfo in tokens.GetType().GetProperties())
            {
                this.Log().Debug(() => " {0}={1}".format_with(propertyInfo.Name, propertyInfo.GetValue(tokens, null)));
            }

            generate_file_from_template(configuration, tokens, NuspecTemplate.Template, _fileSystem.combine_paths(packageLocation, "{0}.nuspec".format_with(tokens.PackageNameLower)), Encoding.UTF8);
            generate_file_from_template(configuration, tokens, ChocolateyInstallTemplate.Template, _fileSystem.combine_paths(packageToolsLocation, "chocolateyinstall.ps1"), Encoding.UTF8);
            generate_file_from_template(configuration, tokens, ChocolateyUninstallTemplate.Template, _fileSystem.combine_paths(packageToolsLocation, "chocolateyuninstall.ps1"), Encoding.UTF8);
            generate_file_from_template(configuration, tokens, ChocolateyReadMeTemplate.Template, _fileSystem.combine_paths(packageToolsLocation, "ReadMe.md"), Encoding.UTF8);

            this.Log().Info(ChocolateyLoggers.Important, "Successfully generated {0}{1} package specification files{2} at '{3}'".format_with(configuration.NewCommand.Name, configuration.NewCommand.AutomaticPackage ? " (automatic)" : string.Empty, Environment.NewLine, packageLocation));
        }

        public void generate_file_from_template(ChocolateyConfiguration configuration, TemplateValues tokens, string template, string fileLocation, Encoding encoding)
        {
            template = TokenReplacer.replace_tokens(tokens, template);

            if (configuration.RegularOutput) this.Log().Info(() => "Generating template to a file{0} at '{1}'".format_with(Environment.NewLine, fileLocation));
            this.Log().Debug(() => "{0}".format_with(template));
            _fileSystem.write_file(fileLocation, template, encoding);
        }
    }
}