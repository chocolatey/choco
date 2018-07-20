using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using chocolatey.infrastructure.app.domain;
using chocolatey.infrastructure.app.nuget;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace chocolatey.tests.infrastructure.app.services
{
    public class NugetListSpecs
    {
        public abstract class NuGetListReflectionSpecsBase : TinySpec
        {
            protected IPackageSearchMetadata CreateMetadata(
                string id,
                string title,
                string version,
                DateTimeOffset? published,
                long? downloadCount,
                int? versionDownloadCount)
            {
                var identity = new NuGet.Packaging.Core.PackageIdentity(id, NuGetVersion.Parse(version));

                var mock = new Mock<IPackageSearchMetadata>();
                mock.Setup(m => m.Identity).Returns(identity);
                mock.Setup(m => m.Title).Returns(title);
                mock.Setup(m => m.Published).Returns(published);
                mock.Setup(m => m.DownloadCount).Returns(downloadCount);
                mock.Setup(m => m.VersionDownloadCount).Returns(versionDownloadCount);

                return mock.Object;
            }

            protected T InvokeStaticMethod<T>(Type type, string methodName, object[] parameters)
            {
                var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);

                if (method == null)
                {
                    throw new MissingMethodException($"Could not find static method '{methodName}' on type '{type.FullName}'");
                }

                return (T)method.Invoke(null, parameters);
            }

            protected IOrderedEnumerable<IPackageSearchMetadata> SortPackages(
                IEnumerable<IPackageSearchMetadata> packages,
                PackageOrder orderBy)
            {
                return InvokeStaticMethod<IOrderedEnumerable<IPackageSearchMetadata>>(
                    typeof(NugetList),
                    "ApplyPackageSort",
                    new object[] { packages, orderBy });
            }

            protected SearchOrderBy? GetSortOrder(PackageOrder orderBy, bool useMultiVersionOrdering)
            {
                return InvokeStaticMethod<SearchOrderBy?>(
                    typeof(NugetList),
                    "GetSortOrder",
                    new object[] { orderBy, useMultiVersionOrdering });
            }
        }


        public class When_Sorting_By_Last_Published : NuGetListReflectionSpecsBase
        {
            private IPackageSearchMetadata[] _items;
            private List<IPackageSearchMetadata> _result;

            public override void Context()
            {
                _items = new[]
                {
                    CreateMetadata("A", "Title1", "1.0.0", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), 100, 20),
                    CreateMetadata("B", "Title1", "1.0.0", new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), 100, 20),
                    CreateMetadata("C", "Title1", "1.0.1", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), 100, 20),
                };
            }

            public override void Because()
            {
                _result = SortPackages(_items, PackageOrder.LastPublished).ToList();
            }

            [Fact]
            public void Should_Sort_Packages_By_LastPublished()
            {
                using (new AssertionScope())
                {
                    _result[0].Identity.Id.Should().Be("B");
                    _result[1].Identity.Id.Should().Be("A");
                    _result[1].Identity.Version.ToNormalizedString().Should().Be("1.0.0");
                    _result[2].Identity.Version.ToNormalizedString().Should().Be("1.0.1");
                }
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Sorting_By_Id : NuGetListReflectionSpecsBase
        {
            private IPackageSearchMetadata[] _items;
            private List<IPackageSearchMetadata> _result;

            public override void Context()
            {
                _items = new[]
                {
                    CreateMetadata("Zebra", "Alpha", "1.0.0", null, null, null),
                    CreateMetadata("Alpha", "Beta", "1.0.0", null, null, null),
                    CreateMetadata("Alpha", "Alpha", "0.9.0", null, null, null),
                };
            }

            public override void Because()
            {
                _result = SortPackages(_items, PackageOrder.Id).ToList();
            }

            [Fact]
            public void Should_Sort_By_Id_Then_Title_Then_Version()
            {
                using (new AssertionScope())
                {
                    _result[0].Identity.Id.Should().Be("Alpha");
                    _result[0].Title.Should().Be("Alpha");
                    _result[1].Title.Should().Be("Beta");
                    _result[2].Identity.Id.Should().Be("Zebra");
                }
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Sorting_By_Title : NuGetListReflectionSpecsBase
        {
            private IPackageSearchMetadata[] _items;
            private List<IPackageSearchMetadata> _result;

            public override void Context()
            {
                _items = new[]
                {
                    CreateMetadata("PackageB", "Gamma", "1.0.0", null, null, null),
                    CreateMetadata("PackageA", "Alpha", "1.0.0", null, null, null),
                    CreateMetadata("PackageC", "Gamma", "0.9.0", null, null, null),
                };
            }

            public override void Because()
            {
                _result = SortPackages(_items, PackageOrder.Title).ToList();
            }

            [Fact]
            public void Should_Sort_By_Title_Then_Id_Then_Version()
            {
                using (new AssertionScope())
                {
                    _result[0].Title.Should().Be("Alpha");
                    _result[1].Title.Should().Be("Gamma");
                    _result[1].Identity.Id.Should().Be("PackageB");
                    _result[2].Identity.Id.Should().Be("PackageC");
                }
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Sorting_By_Popularity : NuGetListReflectionSpecsBase
        {
            private IPackageSearchMetadata[] _items;
            private List<IPackageSearchMetadata> _result;

            public override void Context()
            {
                _items = new[]
                {
                    CreateMetadata("Alpha", "Alpha", "1.0.0", null, 1000, 50),
                    CreateMetadata("Beta", "Beta", "1.0.0", null, 1500, 40),
                    CreateMetadata("Gamma", "Gamma", "1.0.0", null, 1500, 100),
                };
            }

            public override void Because()
            {
                _result = SortPackages(_items, PackageOrder.Popularity).ToList();
            }

            [Fact]
            public void Should_Sort_By_DownloadCount_Then_VersionDownloadCount_Then_Id()
            {
                using (new AssertionScope())
                {
                    _result[0].Identity.Id.Should().Be("Gamma");
                    _result[1].Identity.Id.Should().Be("Beta");
                    _result[2].Identity.Id.Should().Be("Alpha");
                }
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Sorting_With_Unsorted_Fallback : NuGetListReflectionSpecsBase
        {
            private IPackageSearchMetadata[] _items;
            private List<IPackageSearchMetadata> _result;

            public override void Context()
            {
                _items = new[]
                {
                    CreateMetadata("A", "X", "1.0.0", null, null, null),
                    CreateMetadata("B", "Y", "1.0.0", null, null, null),
                    CreateMetadata("C", "Z", "1.0.0", null, null, null),
                };
            }

            public override void Because()
            {
                _result = SortPackages(_items, (PackageOrder)999).ToList(); // unknown value triggers fallback
            }

            [Fact]
            public void Should_Apply_Default_Order_For_Unrecognized_Sort()
            {
                // fallback just applies OrderBy(_ => 0), which preserves input order
                _result.Select(p => p.Identity.Id).Should().ContainInOrder("A", "B", "C");
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Getting_SortOrder_For_Popularity_With_MultiVersion_Disabled : NuGetListReflectionSpecsBase
        {
            private SearchOrderBy? _result;

            public override void Context()
            {
                // No-op
            }

            public override void Because()
            {
                _result = GetSortOrder(PackageOrder.Popularity, false);
            }

            [Fact]
            public void Should_Return_DownloadCount()
            {
                _result.Should().Be(SearchOrderBy.DownloadCount);
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Getting_SortOrder_For_Popularity_With_MultiVersion_Enabled : NuGetListReflectionSpecsBase
        {
            private SearchOrderBy? _result;

            public override void Context()
            {
                // No-op
            }

            public override void Because()
            {
                _result = GetSortOrder(PackageOrder.Popularity, true);
            }

            [Fact]
            public void Should_Return_DownloadCountAndVersion()
            {
                _result.Should().Be(SearchOrderBy.DownloadCountAndVersion);
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Getting_SortOrder_For_Id : NuGetListReflectionSpecsBase
        {
            private SearchOrderBy? _result;

            public override void Context()
            {
                // No-op
            }

            public override void Because()
            {
                _result = GetSortOrder(PackageOrder.Id, false);
            }

            [Fact]
            public void Should_Return_Id()
            {
                _result.Should().Be(SearchOrderBy.Id);
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Getting_SortOrder_For_Title : NuGetListReflectionSpecsBase
        {
            private SearchOrderBy? _result;

            public override void Context()
            {
                // No-op
            }

            public override void Because()
            {
                _result = GetSortOrder(PackageOrder.Title, false);
            }

            [Fact]
            public void Should_Return_Id()
            {
                _result.Should().Be(SearchOrderBy.Id);
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Getting_SortOrder_For_Unsorted : NuGetListReflectionSpecsBase
        {
            private SearchOrderBy? _result;

            public override void Context()
            {
                // No-op
            }

            public override void Because()
            {
                _result = GetSortOrder(PackageOrder.Unsorted, false);
            }

            [Fact]
            public void Should_Return_Null()
            {
                _result.Should().BeNull();
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Getting_SortOrder_For_LastPublished_With_Warning : NuGetListReflectionSpecsBase
        {
            private SearchOrderBy? _result;

            public override void Context()
            {
                // Ensure logger is clean
                MockLogger.Reset();
            }

            public override void Because()
            {
                _result = GetSortOrder(PackageOrder.LastPublished, false);
            }

            [Fact]
            public void Should_Return_Null()
            {
                _result.Should().BeNull();
            }

            [Fact]
            public void Should_Log_A_Warning_About_ClientSide_Sorting()
            {
                MockLogger.ShouldHaveWarningContaining("OrderBy 'LastPublished' is applied on the client side");
            }
        }

        public class When_Sorting_An_Empty_List : NuGetListReflectionSpecsBase
        {
            private List<IPackageSearchMetadata> _result;

            public override void Context()
            {
                // No items
            }

            public override void Because()
            {
                _result = SortPackages(Enumerable.Empty<IPackageSearchMetadata>(), PackageOrder.Title).ToList();
            }

            [Fact]
            public void Should_Return_Empty_List()
            {
                _result.Should().BeEmpty();
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Sorting_Items_With_Null_Title : NuGetListReflectionSpecsBase
        {
            private List<IPackageSearchMetadata> _result;

            public override void Context()
            {
                // One package has null Title
            }

            public override void Because()
            {
                var items = new[]
                {
                    CreateMetadata("PackageB", null, "1.0.0", null, null, null),
                    CreateMetadata("PackageA", "Alpha", "1.0.0", null, null, null),
                };

                _result = SortPackages(items, PackageOrder.Title).ToList();
            }

            [Fact]
            public void Should_Not_Throw()
            {
                _result.Should().HaveCount(2);
            }

            [Fact]
            public void Should_Sort_By_Title_Or_Id_When_Title_Is_Null()
            {
                _result.Select(p => p.Identity.Id).Should().ContainInOrder("PackageA", "PackageB");
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Sorting_Items_With_Null_Published : NuGetListReflectionSpecsBase
        {
            private List<IPackageSearchMetadata> _result;

            public override void Context()
            {
                // One package has null Published
            }

            public override void Because()
            {
                var items = new[]
                {
                    CreateMetadata("PackageA", "A", "1.0.0", null, null, null),
                    CreateMetadata("PackageB", "B", "1.0.0", new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), null, null),
                };

                _result = SortPackages(items, PackageOrder.LastPublished).ToList();
            }

            [Fact]
            public void Should_Not_Throw()
            {
                _result.Should().HaveCount(2);
            }

            [Fact]
            public void Should_Order_By_Published_Descending_With_Nulls_Last()
            {
                _result.Select(p => p.Identity.Id).Should().ContainInOrder("PackageB", "PackageA");
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Sorting_Items_With_Null_DownloadCounts : NuGetListReflectionSpecsBase
        {
            private List<IPackageSearchMetadata> _result;

            public override void Context()
            {
                // All download counts are null
            }

            public override void Because()
            {
                var items = new[]
                {
                    CreateMetadata("PackageA", "A", "1.0.0", null, null, null),
                    CreateMetadata("PackageB", "B", "1.0.0", null, null, null),
                };

                _result = SortPackages(items, PackageOrder.Popularity).ToList();
            }

            [Fact]
            public void Should_Not_Throw()
            {
                _result.Should().HaveCount(2);
            }

            [Fact]
            public void Should_Order_By_Id_When_DownloadCounts_Are_Null()
            {
                _result.Select(p => p.Identity.Id).Should().ContainInOrder("PackageA", "PackageB");
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Getting_SortOrder_For_Title_With_MultiVersion_Enabled : NuGetListReflectionSpecsBase
        {
            private SearchOrderBy? _result;

            public override void Context()
            {
                // No-op
            }

            public override void Because()
            {
                _result = GetSortOrder(PackageOrder.Title, true);
            }

            [Fact]
            public void Should_Return_Version()
            {
                _result.Should().Be(SearchOrderBy.Version);
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Getting_SortOrder_For_Id_With_MultiVersion_Enabled : NuGetListReflectionSpecsBase
        {
            private SearchOrderBy? _result;

            public override void Context()
            {
                // No-op
            }

            public override void Because()
            {
                _result = GetSortOrder(PackageOrder.Id, true);
            }

            [Fact]
            public void Should_Return_Version()
            {
                _result.Should().Be(SearchOrderBy.Version);
            }

            [Fact]
            public void Should_Not_Output_Any_Warnings()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Warn.ToString());
            }
        }

        public class When_Getting_SortOrder_For_Unknown_Value_With_MultiVersion_Enabled : NuGetListReflectionSpecsBase
        {
            private SearchOrderBy? _result;

            public override void Context()
            {
                MockLogger.Reset();
            }

            public override void Because()
            {
                _result = GetSortOrder((PackageOrder)999, true);
            }

            [Fact]
            public void Should_Return_Version()
            {
                _result.Should().Be(SearchOrderBy.Version);
            }

            [Fact]
            public void Should_Log_ClientSide_Warning()
            {
                MockLogger.ShouldHaveWarningContaining("OrderBy '999' is applied on the client side");
            }
        }

        public class When_Getting_SortOrder_For_Unknown_Value_With_MultiVersion_Disabled : NuGetListReflectionSpecsBase
        {
            private SearchOrderBy? _result;

            public override void Context()
            {
                MockLogger.Reset();
            }

            public override void Because()
            {
                _result = GetSortOrder((PackageOrder)999, false);
            }

            [Fact]
            public void Should_Return_Null()
            {
                _result.Should().BeNull();
            }

            [Fact]
            public void Should_Log_ClientSide_Warning()
            {
                MockLogger.ShouldHaveWarningContaining("OrderBy '999' is applied on the client side");
            }
        }

    }
}
