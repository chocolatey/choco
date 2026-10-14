namespace chocolatey.tests.infrastructure.app.nuget
{
    using System;
    using System.IO;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.nuget;
    using FluentAssertions;

    public class ChocolateySourceCacheContextSpecs
    {
        public abstract class ChocolateySourceCacheContextSpecsBase : TinySpec
        {
            protected ChocolateyConfiguration Configuration;

            // We on purpose do not dispose the Service although it leads to memory leaks due to
            // disposal requires touching the file system, which should not be done as part of
            // standard unit tests. The disposal is tested in the integration project.
            protected ChocolateySourceCacheContext Service;

            public override void Context()
            {
                Configuration = new ChocolateyConfiguration()
                {
                    CacheLocation = Path.Combine(Path.GetTempPath(), "choco-test"),
                };
                Service = new ChocolateySourceCacheContext(Configuration);
            }
        }

        public class When_Creating_a_Source_Cache_Context : ChocolateySourceCacheContextSpecsBase
        {
            public override void Because()
            { }

            [Fact]
            public void Should_Set_GeneratedTempFolder_to_a_Unique_Path()
            {
                var tempFolder1 = Service.GeneratedTempFolder;
                var tempFolder2 = Service.GeneratedTempFolder;
                tempFolder1.Should().NotBeNullOrEmpty();
                tempFolder2.Should().NotBeNullOrEmpty();
                tempFolder1.Should().Be(tempFolder2); // Should return the same path on subsequent calls
            }

            [Fact]
            public void Should_Set_Max_Age_to_Ten_Minutes_Ago_When_Cache_Expiration_is_Positive()
            {
                Configuration.CacheExpirationInMinutes = 10;
                Configuration.UseHttpCache = true;
                var expectedMaxAge = DateTime.UtcNow.AddMinutes(-10);
                Service = new ChocolateySourceCacheContext(Configuration);

                Service.MaxAge.Should().BeCloseTo(expectedMaxAge, TimeSpan.FromSeconds(5));
                Service.RefreshMemoryCache.Should().BeFalse();
            }

            [Fact]
            public void Should_Set_Max_Age_to_Now_When_Cache_Expiration_is_Negative()
            {
                Configuration.CacheExpirationInMinutes = -5;
                Configuration.UseHttpCache = true;
                var expectedMaxAge = DateTime.UtcNow;
                Service = new ChocolateySourceCacheContext(Configuration);

                Service.MaxAge.Should().BeCloseTo(expectedMaxAge, TimeSpan.FromSeconds(5));
                Service.RefreshMemoryCache.Should().BeTrue();
            }

            [Fact]
            public void Should_Set_Max_Age_to_Now_When_Cache_Expiration_is_Positive_but_UseHttpCache_is_False()
            {
                Configuration.CacheExpirationInMinutes = 10;
                Configuration.UseHttpCache = false;
                var expectedMaxAge = DateTime.UtcNow;
                Service = new ChocolateySourceCacheContext(Configuration);

                Service.MaxAge.Should().BeCloseTo(expectedMaxAge, TimeSpan.FromSeconds(5));
                Service.RefreshMemoryCache.Should().BeTrue();
            }

            [Fact]
            public void Should_Set_Max_Age_to_Now_When_Cache_Expiration_is_Zero()
            {
                Configuration.CacheExpirationInMinutes = 0;
                Configuration.UseHttpCache = true;
                var expectedMaxAge = DateTime.UtcNow;
                Service = new ChocolateySourceCacheContext(Configuration);

                Service.MaxAge.Should().BeCloseTo(expectedMaxAge, TimeSpan.FromSeconds(5));
                Service.RefreshMemoryCache.Should().BeTrue();
            }
        }
    }
}
