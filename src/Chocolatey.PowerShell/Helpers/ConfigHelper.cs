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
using System.IO;
using System.Management.Automation;
using System.Xml;

namespace Chocolatey.PowerShell.Helpers
{
    /// <summary>
    /// Helper for retrieving configuration settings and feature flags from the Chocolatey configuration file.
    /// </summary>
    public class ConfigHelper
    {
        private static string _configFileLocation;

        /// <summary>
        /// Returns the file path to the chocolatey.config file.
        /// </summary>
        /// <param name="cmdlet">The calling cmdlet.</param>
        /// <returns>The file path to the config file as a string.</returns>
        public static string GetConfigFileLocation(PSCmdlet cmdlet)
        {
            if (_configFileLocation is null)
            {
                _configFileLocation = PSHelper.CombinePaths(cmdlet, PSHelper.GetInstallLocation(cmdlet), "config", "chocolatey.config");
            }

            return _configFileLocation;
        }

        /// <summary>
        /// Retrieves a specific configuration value from the configuration file.
        /// </summary>
        /// <param name="cmdlet">The calling cmldet.</param>
        /// <param name="key">The name of the configuration value.</param>
        /// <returns>The value of the configuration entry as a string.</returns>
        public static string GetConfigValue(PSCmdlet cmdlet, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return default;
            }

            return GetXmlValue(cmdlet, "chocolatey/config/add", "key", key, "value");
        }

        private static string GetXmlValue(PSCmdlet cmdlet, string xpath, string filterAttribute, string filterValue, string valueAttribute)
        {
            cmdlet.WriteDebug($"Retrieving configuration data from chocolatey.config; xpath: {xpath}, where {filterAttribute} = {filterValue}, returning {valueAttribute}");
            var xmlConfig = LoadConfig(cmdlet);

            foreach (XmlNode node in xmlConfig.SelectNodes(xpath))
            {
                var attributeValue = node.Attributes?[filterAttribute]?.Value;
                if (attributeValue != null && attributeValue.Equals(filterValue, StringComparison.OrdinalIgnoreCase))
                {
                    return node.Attributes[valueAttribute]?.Value;
                }
            }

            cmdlet.WriteDebug($"Configuration file entry with {filterAttribute}='{filterValue}' matching {xpath} was not found.");
            return null;
        }

        /// <summary>
        /// Retrieves the feature state from the configuration file.
        /// </summary>
        /// <param name="cmdlet">The calling cmdlet.</param>
        /// <param name="name">The name of the feature to check.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        public static bool IsFeatureEnabled(PSCmdlet cmdlet, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var featureValue = GetXmlValue(cmdlet, "chocolatey/features/feature", "name", name, "enabled");

            return bool.TryParse(featureValue, out var result) && result;
        }

        /// <summary>
        /// Returns the XmlDocument representation of the configuration file, loading it from disk if
        /// it has not already been loaded into memory.
        /// </summary>
        /// <param name="cmdlet">The calling cmdlet.</param>
        /// <returns>The parsed XmlDocument object representing the configuraiton file values.</returns>
        /// <exception cref="FileNotFoundException">Unable to find or open the configuration file.</exception>
        /// <exception cref="InvalidOperationException">The configuration file path exists but could not be loaded.</exception>
        private static XmlDocument LoadConfig(PSCmdlet cmdlet)
        {
            var configPath = PSHelper.GetUnresolvedPath(cmdlet, GetConfigFileLocation(cmdlet));
            cmdlet.WriteDebug($"Chocolatey config file location resolved to '{configPath}'");

            if (!PSHelper.ItemExists(cmdlet, configPath))
            {
                throw new FileNotFoundException("Chocolatey config file not found", configPath);
            }

            var config = new XmlDocument();
            try
            {
                config.Load(configPath);
            }
            catch (SystemException error)
            {
                throw new InvalidOperationException($"Unable to load the Chocolatey config file: {error.Message}", error);
            }
            catch (Exception error) when (error is ArgumentException || error is IOException)
            {
                throw new FileNotFoundException($"Chocolatey config file could not be loaded: {error.Message}", configPath, error);
            }

            return config;
        }
    }
}
