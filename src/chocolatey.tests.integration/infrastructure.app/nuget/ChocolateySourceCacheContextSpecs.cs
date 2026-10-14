namespace chocolatey.tests.integration.infrastructure.app.nuget
{
    using System;
    using System.IO;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.nuget;
    using FluentAssertions;
    using NUnit.Framework;

    public class ChocolateySourceCacheContextSpecs
    {
        public abstract class ChocolateySourceCacheContextSpecsBase : TinySpec
        {
            protected readonly string TempFolder = Path.Combine(Path.GetTempPath(), "ChocolateySourceCacheContextSpecs", Guid.NewGuid().ToString("N"));
            protected ChocolateyConfiguration Configuration;
            protected ChocolateySourceCacheContext Service;

            public override void Context()
            {
                Configuration = new ChocolateyConfiguration();
                Service = new ChocolateySourceCacheContext(Configuration)
                {
                    GeneratedTempFolder = TempFolder
                };
            }
        }

        [Timeout(100)]
        public class When_Disposing_a_Source_Cache_Context : ChocolateySourceCacheContextSpecsBase
        {
            public override void Because()
            {
                Service.Dispose();
            }

            public override void Context()
            {
                base.Context();

                // Create the temporary folder and a single file inside it to simulate a generated
                // temp folder
                Directory.CreateDirectory(TempFolder);
                File.WriteAllText(Path.Combine(TempFolder, "tempfile.txt"), "This is a temporary file.");
            }

            [Fact]
            public void Should_Delete_the_GeneratedTempFolder()
            {
                Directory.Exists(TempFolder).Should().BeFalse();
            }

            [Fact]
            public void Should_Not_Throw_Exception_When_Disposing_Again()
            {
                Action disposeAgain = () => Service.Dispose();
                disposeAgain.Should().NotThrow();
            }
        }
    }
}
