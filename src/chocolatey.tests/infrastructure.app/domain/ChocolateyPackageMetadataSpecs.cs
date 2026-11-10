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
using System.IO.Compression;
using System.Text;
using chocolatey.infrastructure.app.domain;
using chocolatey.infrastructure.filesystem;
using NuGet.Packaging;
using NUnit.Framework;
using FluentAssertions;

namespace chocolatey.tests.infrastructure.app.domain
{
    public class ChocolateyPackageMetadataSpecs
    {
        // The value of the supportsInstances element read through each of the three ways a package's metadata is read.
        public abstract class ChocolateyPackageMetadataSpecsBase : TinySpec
        {
            protected static string NuspecWith(string supportsInstancesElement)
            {
                return "<?xml version=\"1.0\"?>"
                    + "<package xmlns=\"http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd\"><metadata>"
                    + "<id>instancespec</id><version>1.0.0</version><authors>spec</authors><description>spec</description>"
                    + supportsInstancesElement
                    + "</metadata></package>";
            }

            protected static string Element(string value)
            {
                return "<supportsInstances>" + value + "</supportsInstances>";
            }

            protected abstract bool ReadSupportsInstances(string nuspec);

            public override void Because()
            {
            }

            [Fact]
            public void Should_be_true_when_it_is_true()
            {
                ReadSupportsInstances(NuspecWith(Element("true"))).Should().BeTrue();
            }

            [Fact]
            public void Should_be_true_when_it_is_True()
            {
                ReadSupportsInstances(NuspecWith(Element("True"))).Should().BeTrue();
            }

            [Fact]
            public void Should_be_true_when_it_is_TRUE()
            {
                ReadSupportsInstances(NuspecWith(Element("TRUE"))).Should().BeTrue();
            }

            [Fact]
            public void Should_be_true_when_it_is_surrounded_by_spaces()
            {
                ReadSupportsInstances(NuspecWith(Element(" true "))).Should().BeTrue();
            }

            [Fact]
            public void Should_be_true_when_it_is_1()
            {
                ReadSupportsInstances(NuspecWith(Element("1"))).Should().BeTrue();
            }

            [Fact]
            public void Should_be_false_when_the_element_is_absent()
            {
                ReadSupportsInstances(NuspecWith(string.Empty)).Should().BeFalse();
            }

            [Fact]
            public void Should_be_false_when_it_is_false()
            {
                ReadSupportsInstances(NuspecWith(Element("false"))).Should().BeFalse();
            }

            [Fact]
            public void Should_be_false_when_it_is_0()
            {
                ReadSupportsInstances(NuspecWith(Element("0"))).Should().BeFalse();
            }

            [Fact]
            public void Should_be_false_when_it_is_empty()
            {
                ReadSupportsInstances(NuspecWith(Element(string.Empty))).Should().BeFalse();
            }

            [Fact]
            public void Should_be_false_when_it_is_not_a_boolean()
            {
                ReadSupportsInstances(NuspecWith(Element("yes"))).Should().BeFalse();
            }
        }

        public class When_reading_supportsInstances_from_a_nuspec_reader : ChocolateyPackageMetadataSpecsBase
        {
            public override void Context()
            {
            }

            protected override bool ReadSupportsInstances(string nuspec)
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(nuspec)))
                {
                    return new ChocolateyPackageMetadata(new NuspecReader(stream)).SupportsInstances;
                }
            }
        }

        public class When_reading_supportsInstances_from_a_nuspec_file : ChocolateyPackageMetadataSpecsBase
        {
            public override void Context()
            {
            }

            protected override bool ReadSupportsInstances(string nuspec)
            {
                var path = Path.Combine(Path.GetTempPath(), "instancespec-" + Guid.NewGuid().ToString("N") + ".nuspec");
                try
                {
                    File.WriteAllText(path, nuspec, new UTF8Encoding(false));

                    return new ChocolateyPackageMetadata(path, new DotNetFileSystem()).SupportsInstances;
                }
                finally
                {
                    File.Delete(path);
                }
            }
        }

        public class When_reading_supportsInstances_from_a_nupkg_file : ChocolateyPackageMetadataSpecsBase
        {
            public override void Context()
            {
            }

            protected override bool ReadSupportsInstances(string nuspec)
            {
                var path = Path.Combine(Path.GetTempPath(), "instancespec-" + Guid.NewGuid().ToString("N") + ".nupkg");
                try
                {
                    using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
                    {
                        var entry = archive.CreateEntry("instancespec.nuspec");
                        using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                        {
                            writer.Write(nuspec);
                        }
                    }

                    return new ChocolateyPackageMetadata(path, new DotNetFileSystem()).SupportsInstances;
                }
                finally
                {
                    File.Delete(path);
                }
            }
        }
    }
}
