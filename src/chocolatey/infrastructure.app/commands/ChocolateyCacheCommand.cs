// Copyright © 2023 Chocolatey Software, Inc
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
    using System.IO;
    using System.Linq;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.nuget;
    using chocolatey.infrastructure.commandline;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.logging;

    [CommandFor("cache", "Manage the local HTTP caches used to store queries", Version = "2.1.0")]
    public class ChocolateyCacheCommand : ChocolateyCommandBase, ICommand
    {
        private readonly IFileSystem _fileSystem;
        private const string LockDirectoryName = ".locks";

        public ChocolateyCacheCommand(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public virtual void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("expired",
                    "Expired - Remove cached items that have expired.",
                    option => configuration.CacheCommand.RemoveExpiredItemsOnly = option != null);
        }

        public virtual void DryRun(ChocolateyConfiguration configuration)
        {
            Run(configuration);
        }

        public virtual bool MayRequireAdminAccess()
        {
            // We will support cleaning the user cache directory without cleaning the system directory.
            // As such it can be run without admin access.
            return false;
        }

        public virtual void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            CacheCommandType command;

            if (unparsedArguments.Count == 0)
            {
                command = CacheCommandType.List;
            }
            else if (!Enum.TryParse(unparsedArguments[0], true, out command) || command == CacheCommandType.Unknown)
            {
                this.Log().Warn("Unknown command '{0}'. Setting to list.", unparsedArguments[0]);
                command = CacheCommandType.List;
            }

            configuration.CacheCommand.Command = command;
        }

        public virtual void Run(ChocolateyConfiguration config)
        {
            switch (config.CacheCommand.Command)
            {
                case CacheCommandType.List:
                    ListCacheStatistics(config);
                    break;

                case CacheCommandType.Remove:
                    RemoveCachedItems(config);
                    break;
            }
        }

        public virtual void Validate(ChocolateyConfiguration configuration)
        {
            // Nothing to validate
        }

        protected void CleanCachedItemsInPath(ChocolateyConfiguration configuration, string cacheLocation)
        {
            if (configuration.Noop && configuration.CacheCommand.RemoveExpiredItemsOnly)
            {
                this.Log().Info("Would remove all files with the .dat extension older than 30 minutes in '{0}'.", cacheLocation);

                return;
            }
            else if (configuration.Noop)
            {
                this.Log().Info("Would remove all files with the .dat in '{0}'.", cacheLocation);

                return;
            }

            var expirationTimer = GetCacheExpiration(configuration);

            var filesBeforeClean = _fileSystem.GetFiles(cacheLocation, "*.dat", SearchOption.AllDirectories);

            if (configuration.CacheCommand.RemoveExpiredItemsOnly)
            {
                filesBeforeClean = filesBeforeClean.Where(f => _fileSystem.GetFileModifiedDate(f) < expirationTimer);
            }

            var beforeFilesCount = filesBeforeClean.Count();

            if (beforeFilesCount == 0)
            {
                this.Log().Info("No cached items available to be removed in '{0}'.", cacheLocation);
                return;
            }

            if (configuration.CacheCommand.RemoveExpiredItemsOnly)
            {
                // We need to remove each individual file when the user only request
                // deleting expired items. This takes a bit longer.
                foreach (var fileToRemove in filesBeforeClean)
                {
                    _fileSystem.DeleteFile(fileToRemove);
                }

                foreach (var directoryToRemove in _fileSystem.GetDirectories(cacheLocation).Where(d => !_fileSystem.GetFileName(d).IsEqualTo(LockDirectoryName)))
                {
                    if (!_fileSystem.GetFiles(directoryToRemove, "*", SearchOption.AllDirectories).Any())
                    {
                        _fileSystem.DeleteDirectoryChecked(directoryToRemove, recursive: false, overrideAttributes: false, isSilent: true);
                    }
                }
            }
            else
            {
                foreach (var directoryToRemove in _fileSystem.GetDirectories(cacheLocation).Where(d => !_fileSystem.GetFileName(d).IsEqualTo(LockDirectoryName)))
                {
                    _fileSystem.DeleteDirectoryChecked(directoryToRemove, recursive: true);
                }
            }

            var filesAfterClean = _fileSystem.GetFiles(cacheLocation, "*.dat", SearchOption.AllDirectories);

            if (configuration.CacheCommand.RemoveExpiredItemsOnly)
            {
                filesAfterClean = filesAfterClean.Where(f => _fileSystem.GetFileModifiedDate(f) < expirationTimer);

                this.Log().Info("Removed {0} expired cached items in '{1}'.", beforeFilesCount - filesAfterClean.Count(), cacheLocation);
            }
            else
            {
                this.Log().Info("Removed {0} cached items in '{1}'.", beforeFilesCount - filesAfterClean.Count(), cacheLocation);
            }
        }

        protected override string GetCommandDescription(CommandForAttribute attribute, ChocolateyConfiguration configuration)
        {
            return @"Get the statistics of what Chocolatey CLI has cached or clear any cached
items in the current context.

The behavior of this command will vary depending on whether it is running in an elevated context or not.
Statistics and removing cached items will always happen on the User HTTP Cache.
However, the System HTTP Cache will only be considered if running in an elevated context.";
        }

        protected override IEnumerable<string> GetCommandExamples(CommandForAttribute[] attributes, ChocolateyConfiguration configuration)
        {
            return new[]
            {
                "choco cache",
                "choco cache list",
                "choco cache remove",
                "choco cache remove --expired"
            };
        }

        protected override IEnumerable<string> GetCommandUsage(CommandForAttribute[] attributes, ChocolateyConfiguration configuration)
        {
            return new[] { "choco cache [list]|remove [options/switches]" };
        }

        protected virtual void ListCacheStatistics(ChocolateyConfiguration configuration)
        {
            var userCacheLocation = ApplicationParameters.HttpCacheUserLocation;
            var systemCacheLocation = ApplicationParameters.HttpCacheLocation;

            if (userCacheLocation != systemCacheLocation)
            {
                this.Log().Info(ChocolateyLoggers.Important, "System HTTP Cache");
                ListCachedItems(configuration, systemCacheLocation);

                this.Log().Info(string.Empty);
                this.Log().Info(ChocolateyLoggers.Important, "User HTTP Cache");
                ListCachedItems(configuration, userCacheLocation);
            }
            else
            {
                this.Log().Info(ChocolateyLoggers.Important, "User HTTP Cache");
                ListCachedItems(configuration, userCacheLocation);

                this.Log().Info(string.Empty);
                this.Log().Warn("Run the same command as an Administrator to list information about the System HTTP cache.");
            }
        }

        protected virtual void RemoveCachedItems(ChocolateyConfiguration configuration)
        {
            var systemCacheLocation = ApplicationParameters.HttpCacheLocation;
            var userCacheLocation = ApplicationParameters.HttpCacheUserLocation;

            this.Log().Info(ChocolateyLoggers.Important, "Cache cleanup");

            if (userCacheLocation != systemCacheLocation)
            {
                CleanCachedItemsInPath(configuration, systemCacheLocation);
                CleanCachedItemsInPath(configuration, userCacheLocation);
            }
            else
            {
                CleanCachedItemsInPath(configuration, userCacheLocation);

                this.Log().Info(string.Empty);
                this.Log().Warn("Run the same command as an Administrator to remove the System HTTP cache.");
            }
        }

        private static DateTime GetCacheExpiration(ChocolateyConfiguration configuration)
        {
            DateTime? expirationTimer;
            var cacheContext = new ChocolateySourceCacheContext(configuration);

            if (cacheContext.MaxAge.HasValue)
            {
                expirationTimer = cacheContext.MaxAge.Value.DateTime;
            }
            else
            {
                expirationTimer = DateTime.Now.Subtract(cacheContext.MaxAgeTimeSpan);
            }

            return expirationTimer.Value;
        }

        private void ListCachedItems(ChocolateyConfiguration configuration, string cacheLocation)
        {
            var cachedFiles = _fileSystem.GetFiles(cacheLocation, "*.dat", SearchOption.AllDirectories);
            var cachedDirectories = _fileSystem.GetDirectories(cacheLocation).Where(d => !_fileSystem.GetFileName(d).IsEqualTo(LockDirectoryName));
            var expirationTimer = GetCacheExpiration(configuration);
            
            var expiredFiles = cachedFiles.Where(f => _fileSystem.GetFileModifiedDate(f) < expirationTimer);

            this.Log().Info("We found {0} cached sources.", cachedDirectories.Count());
            this.Log().Info("We found {0} cached items for all sources, where {1} items have expired.", cachedFiles.Count(), expiredFiles.Count());
        }
        
        #region Obsoleted methods

        [Obsolete("Will be removed in v3. Use ConfigureArgumentParser instead!")]
        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            ConfigureArgumentParser(optionSet, configuration);
        }

        [Obsolete("Will be removed in v3. Use ParseAdditionalArguments instead!")]
        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            ParseAdditionalArguments(unparsedArguments, configuration);
        }

        [Obsolete("Will be removed in v3. Use Validate instead!")]
        public void handle_validation(ChocolateyConfiguration configuration)
        {
            Validate(configuration);
        }

        [Obsolete("Will be removed in v3. Use HelpMessage instead!")]
        public void help_message(ChocolateyConfiguration configuration)
        {
            HelpMessage(configuration);
        }

        [Obsolete("Will be removed in v3. Use MayRequireAdminAccess instead!")]
        public bool may_require_admin_access()
        {
            return MayRequireAdminAccess();
        }

        [Obsolete("Will be removed in v3. Use DryRun instead!")]
        public void noop(ChocolateyConfiguration configuration)
        {
            DryRun(configuration);
        }

        [Obsolete("Will be removed in v3. Use Run instead!")]
        public void run(ChocolateyConfiguration config)
        {
            Run(config);
        }

        #endregion Obsoleted methods
    }
}