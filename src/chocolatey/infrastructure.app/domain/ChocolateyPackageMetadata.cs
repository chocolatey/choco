// Copyright © 2017 - 2022 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.domain
{
    using System.Collections.Generic;
    using NuGet.Configuration;
    using NuGet.Packaging;
    using NuGet.Packaging.Core;
    using NuGet.Versioning;
    using IFileSystem = filesystem.IFileSystem;

    public  class ChocolateyPackageMetadata : IPackageMetadata
    {
        public ChocolateyPackageMetadata(NuspecReader reader)
        {
            ProjectSourceUrl = GetUriSafe(reader.GetProjectSourceUrl());
            PackageSourceUrl = GetUriSafe(reader.GetPackageSourceUrl());
            DocsUrl = GetUriSafe(reader.GetDocsUrl());
            WikiUrl = GetUriSafe(reader.GetWikiUrl());
            MailingListUrl = GetUriSafe(reader.GetMailingListUrl());
            BugTrackerUrl = GetUriSafe(reader.GetBugTrackerUrl());
            Replaces = reader.GetReplaces().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            Provides = reader.GetProvides().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            Conflicts = reader.GetConflicts().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            SoftwareDisplayName = reader.GetSoftwareDisplayName();
            SoftwareDisplayVersion = reader.GetSoftwareDisplayVersion();
            Id = reader.GetId();
            Version = reader.GetVersion();
            Title = reader.GetTitle();
            Authors = reader.GetAuthors().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            Owners = reader.GetOwners().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            IconUrl = GetUriSafe(reader.GetIconUrl());
            LicenseUrl = GetUriSafe(reader.GetLicenseUrl());
            ProjectUrl = GetUriSafe(reader.GetProjectUrl());
            RequireLicenseAcceptance = reader.GetRequireLicenseAcceptance();
            DevelopmentDependency = reader.GetDevelopmentDependency();
            Description = reader.GetDescription();
            Summary = reader.GetSummary();
            ReleaseNotes = reader.GetReleaseNotes();
            Language = reader.GetLanguage();
            Tags = reader.GetTags();
            Serviceable = reader.IsServiceable();
            Copyright = reader.GetCopyright();
            Icon = reader.GetIcon();
            Readme = reader.GetReadme();
            DependencyGroups = reader.GetDependencyGroups();
            PackageTypes = reader.GetPackageTypes();
            Repository = reader.GetRepositoryMetadata();
            LicenseMetadata = reader.GetLicenseMetadata();
            FrameworkReferenceGroups = reader.GetFrameworkRefGroups();
        }

        public ChocolateyPackageMetadata(string packagePath, IFileSystem filesystem)
        {
            if (filesystem.get_file_extension(packagePath) == NuGetConstants.PackageExtension)
            {
                using (var archiveReader = new PackageArchiveReader(packagePath))
                {
                    var reader = archiveReader.NuspecReader;
                    ProjectSourceUrl = GetUriSafe(reader.GetProjectSourceUrl());
                    PackageSourceUrl = GetUriSafe(reader.GetPackageSourceUrl());
                    DocsUrl = GetUriSafe(reader.GetDocsUrl());
                    WikiUrl = GetUriSafe(reader.GetWikiUrl());
                    MailingListUrl = GetUriSafe(reader.GetMailingListUrl());
                    BugTrackerUrl = GetUriSafe(reader.GetBugTrackerUrl());
                    Replaces = reader.GetReplaces().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    Provides = reader.GetProvides().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    Conflicts = reader.GetConflicts().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    SoftwareDisplayName = reader.GetSoftwareDisplayName();
                    SoftwareDisplayVersion = reader.GetSoftwareDisplayVersion();
                    Id = reader.GetId();
                    Version = reader.GetVersion();
                    Title = reader.GetTitle();
                    Authors = reader.GetAuthors().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    Owners = reader.GetOwners().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    IconUrl = GetUriSafe(reader.GetIconUrl());
                    LicenseUrl = GetUriSafe(reader.GetLicenseUrl());
                    ProjectUrl = GetUriSafe(reader.GetProjectUrl());
                    RequireLicenseAcceptance = reader.GetRequireLicenseAcceptance();
                    DevelopmentDependency = reader.GetDevelopmentDependency();
                    Description = reader.GetDescription();
                    Summary = reader.GetSummary();
                    ReleaseNotes = reader.GetReleaseNotes();
                    Language = reader.GetLanguage();
                    Tags = reader.GetTags();
                    Serviceable = reader.IsServiceable();
                    Copyright = reader.GetCopyright();
                    Icon = reader.GetIcon();
                    Readme = reader.GetReadme();
                    DependencyGroups = reader.GetDependencyGroups();
                    PackageTypes = reader.GetPackageTypes();
                    Repository = reader.GetRepositoryMetadata();
                    LicenseMetadata = reader.GetLicenseMetadata();
                    FrameworkReferenceGroups = reader.GetFrameworkRefGroups();
                }
            }
            else if (filesystem.get_file_extension(packagePath) == NuGetConstants.ManifestExtension)
            {
                var reader = new NuspecReader(packagePath);

                ProjectSourceUrl = GetUriSafe(reader.GetProjectSourceUrl());
                PackageSourceUrl = GetUriSafe(reader.GetPackageSourceUrl());
                DocsUrl = GetUriSafe(reader.GetDocsUrl());
                WikiUrl = GetUriSafe(reader.GetWikiUrl());
                MailingListUrl = GetUriSafe(reader.GetMailingListUrl());
                BugTrackerUrl = GetUriSafe(reader.GetBugTrackerUrl());
                Replaces = reader.GetReplaces().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                Provides = reader.GetProvides().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                Conflicts = reader.GetConflicts().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                SoftwareDisplayName = reader.GetSoftwareDisplayName();
                SoftwareDisplayVersion = reader.GetSoftwareDisplayVersion();
                Id = reader.GetId();
                Version = reader.GetVersion();
                Title = reader.GetTitle();
                Authors = reader.GetAuthors().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                Owners = reader.GetOwners().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                IconUrl = GetUriSafe(reader.GetIconUrl());
                LicenseUrl = GetUriSafe(reader.GetLicenseUrl());
                ProjectUrl = GetUriSafe(reader.GetProjectUrl());
                RequireLicenseAcceptance = reader.GetRequireLicenseAcceptance();
                DevelopmentDependency = reader.GetDevelopmentDependency();
                Description = reader.GetDescription();
                Summary = reader.GetSummary();
                ReleaseNotes = reader.GetReleaseNotes();
                Language = reader.GetLanguage();
                Tags = reader.GetTags();
                Serviceable = reader.IsServiceable();
                Copyright = reader.GetCopyright();
                Icon = reader.GetIcon();
                Readme = reader.GetReadme();
                DependencyGroups = reader.GetDependencyGroups();
                PackageTypes = reader.GetPackageTypes();
                Repository = reader.GetRepositoryMetadata();
                LicenseMetadata = reader.GetLicenseMetadata();
                FrameworkReferenceGroups = reader.GetFrameworkRefGroups();
            }
            else
            {
                throw new ArgumentException("Package Path not a .nupkg or .nuspec");
            }
        }

        public Uri ProjectSourceUrl { get; }
        public Uri PackageSourceUrl { get; }
        public Uri DocsUrl { get; }
        public Uri WikiUrl { get; }
        public Uri MailingListUrl { get; }
        public Uri BugTrackerUrl { get; }
        public IEnumerable<string> Replaces { get; }
        public IEnumerable<string> Provides { get; }
        public IEnumerable<string> Conflicts { get; }
        public string SoftwareDisplayName { get; }
        public string SoftwareDisplayVersion { get; }
        public string Id { get; }
        public NuGetVersion Version { get; private set; }
        public string Title { get; }
        public IEnumerable<string> Authors { get; }
        public IEnumerable<string> Owners { get; }
        public Uri IconUrl { get; }
        public Uri LicenseUrl { get; }
        public Uri ProjectUrl { get; }
        public bool RequireLicenseAcceptance { get; }
        public bool DevelopmentDependency { get; }
        public string Description { get; }
        public string Summary { get; }
        public string ReleaseNotes { get; }
        public string Language { get; }
        public string Tags { get; }
        public bool Serviceable { get; }
        public string Copyright { get; }
        public string Icon { get; }
        public string Readme { get; }
        public IEnumerable<FrameworkAssemblyReference> FrameworkReferences => null;
        public IEnumerable<PackageReferenceSet> PackageAssemblyReferences => null;
        public IEnumerable<PackageDependencyGroup> DependencyGroups { get; }
        public Version MinClientVersion => null;
        public IEnumerable<ManifestContentFiles> ContentFiles => null;
        public IEnumerable<PackageType> PackageTypes { get; }
        public RepositoryMetadata Repository { get; }
        public LicenseMetadata LicenseMetadata { get; }
        public IEnumerable<FrameworkReferenceGroup> FrameworkReferenceGroups { get; }

        private static Uri GetUriSafe(string url)
        {
            Uri uri = null;
            Uri.TryCreate(url, UriKind.Absolute, out uri);
            return uri;
        }

        public void OverrideOriginalVersion(NuGetVersion overrideVersion)
        {
            Version = overrideVersion;
        }

    }
}
