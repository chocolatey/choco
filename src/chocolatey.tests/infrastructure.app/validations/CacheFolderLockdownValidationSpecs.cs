using chocolatey.infrastructure.app;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.validations;
using chocolatey.infrastructure.filesystem;
using chocolatey.infrastructure.information;
using chocolatey.infrastructure.validations;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chocolatey.tests.infrastructure.app.validations
{
    public class CacheFolderLockdownValidationSpecs
    {
        public abstract class CacheFolderLockdownValidationSpecsBase : TinySpec
        {
            protected readonly bool IsElevated = ProcessInformation.IsElevated();
            protected readonly ChocolateyConfiguration Config = new ChocolateyConfiguration();
            protected CacheFolderLockdownValidation Validation;
            protected ICollection<ValidationResult> Results;

            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();

            public override void Context()
            {
                FileSystem.Invocations.Clear();
                Config.UseHttpCache = true;

                Validation = new CacheFolderLockdownValidation(FileSystem.Object);
            }

            public override void Because()
            {
                Results = Validation.Validate(Config);
            }
        }

        public abstract class ElevatedCacheFolderLockdownValidationSpecsBase : CacheFolderLockdownValidationSpecsBase
        {
            [SetUp]
            public void CheckElevation()
            {
                if (!IsElevated)
                {
                    Assert.Ignore("Ignored due to not running elevated.");
                }
            }
        }

        public sealed class When_not_elevated : CacheFolderLockdownValidationSpecsBase
        {
            [SetUp]
            public void CheckElevation()
            {
                if (IsElevated)
                {
                    Assert.Ignore("Ignored due to running elevated.");
                }
            }

            [Fact]
            public void Should_return_one_validation_result()
            {
                Results.Should().HaveCount(1);
            }

            [Fact]
            public void Should_succeed_with_user_cache_directory()
            {
                var result = Results.First();
                result.Status.Should().Be(ValidationStatus.Success);
                result.Message.Should().Be("User Cache directory is valid.");
                result.ExitCode.Should().Be(0);
            }
        }

        public sealed class When_cache_folder_exists_and_is_locked : ElevatedCacheFolderLockdownValidationSpecsBase
        {
            public override void Context()
            {
                base.Context();

                FileSystem
                    .Setup(fs => fs.DirectoryExists(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation)))
                    .Returns(true);

                FileSystem
                    .Setup(fs => fs.IsLockedDirectory(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation)))
                    .Returns(true);
            }

            [Fact]
            public void Should_return_one_validation_result()
            {
                Results.Should().HaveCount(1);
            }

            [Fact]
            public void Should_successfully_validate_locked_directory()
            {
                var result = Results.First();
                result.Status.Should().Be(ValidationStatus.Success);
                result.Message.Should().Be("System Cache directory is locked down to administrators.");
                result.ExitCode.Should().Be(0);
            }
        }

        public sealed class When_cache_folder_exists_and_is_not_locked : ElevatedCacheFolderLockdownValidationSpecsBase
        {
            public override void Context()
            {
                base.Context();

                FileSystem
                    .Setup(fs => fs.DirectoryExists(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation)))
                    .Returns(true);

                FileSystem
                    .Setup(fs => fs.IsLockedDirectory(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation)))
                    .Returns(false);

                FileSystem
                    .Setup(fs => fs.DeleteDirectory(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation), It.Is<bool>(b => b)))
                    .Verifiable();

                FileSystem
                    .Setup(fs => fs.LockDirectory(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation)))
                    .Returns(true);
            }

            [Fact]
            public void Should_delete_the_existing_directory()
            {
                FileSystem.Verify(fs => fs.DeleteDirectory(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation), It.Is<bool>(b => b)));
            }

            [Fact]
            public void Should_return_two_validation_results()
            {
                Results.Should().HaveCount(2);
            }

            [Fact]
            public void Should_warn_cache_directory_is_deleted()
            {
                var result = Results.First(r => r.Status == ValidationStatus.Warning);
                result.ExitCode.Should().Be(0);
                result.Status.Should().Be(ValidationStatus.Warning);
                result.Message.Should().Be("The HTTP Cache was not correctly locked down, purging the cache before continuing.".SplitOnSpace(linePrefix: "   "));
            }

            [Fact]
            public void Should_report_successful_validation()
            {
                var result = Results.First(r => r.Status == ValidationStatus.Success);
                result.ExitCode.Should().Be(0);
                result.Status.Should().Be(ValidationStatus.Success);
                result.Message.Should().Be("System Cache directory successfully created and locked down to administrators.".SplitOnSpace("   "));
            }
        }

        public sealed class When_bad_cache_exists_and_cannot_be_deleted : ElevatedCacheFolderLockdownValidationSpecsBase
        {
            private string _message = "test error message";

            public override void Context()
            {
                base.Context();

                FileSystem
                    .Setup(fs => fs.DirectoryExists(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation)))
                    .Returns(true);

                FileSystem
                    .Setup(fs => fs.IsLockedDirectory(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation)))
                    .Returns(false);

                FileSystem
                    .Setup(fs => fs.DeleteDirectory(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation), It.Is<bool>(b => b)))
                    .Throws(new System.IO.IOException(_message));
            }

            [Fact]
            public void Should_return_one_validation_result()
            {
                Results.Should().HaveCount(1);
            }

            [Fact]
            public void Should_fail_validation_stating_directory_cannot_be_deleted()
            {
                var result = Results.First();
                result.ExitCode.Should().Be(1);
                result.Status.Should().Be(ValidationStatus.Error);
                result.Message.Should().Be($"System Cache directory exists, but could not be locked down to administrators or deleted: {_message}. Remove the '{ApplicationParameters.HttpCacheLocation}' directory and try again, or provide the '--ignore-http-cache' option to temporarily ignore this error.".SplitOnSpace(linePrefix: "   "));
            }
        }

        public sealed class When_bad_cache_exists_and_cannot_be_deleted_with_http_cache_disabled : ElevatedCacheFolderLockdownValidationSpecsBase
        {
            private string _message = "test error message";

            public override void Context()
            {
                base.Context();

                Config.UseHttpCache = false;

                FileSystem
                    .Setup(fs => fs.DirectoryExists(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation)))
                    .Returns(true);

                FileSystem
                    .Setup(fs => fs.IsLockedDirectory(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation)))
                    .Returns(false);

                FileSystem
                    .Setup(fs => fs.DeleteDirectory(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation), It.Is<bool>(b => b)))
                    .Throws(new System.IO.IOException(_message));
            }

            [Fact]
            public void Should_return_one_validation_result()
            {
                Results.Should().HaveCount(1);
            }

            [Fact]
            public void Should_warn_about_the_cache_not_being_deleted()
            {
                var result = Results.First();
                result.ExitCode.Should().Be(0);
                result.Status.Should().Be(ValidationStatus.Warning);
                result.Message.Should().Be($"System Cache directory exists, but could not be locked down to administrators or deleted: {_message}.".SplitOnSpace(linePrefix: "   "));
            }
        }

        public sealed class When_cache_does_not_exist : ElevatedCacheFolderLockdownValidationSpecsBase
        {
            public override void Context()
            {
                base.Context();

                FileSystem
                    .Setup(fs => fs.DirectoryExists(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation)))
                    .Returns(false);

                FileSystem
                    .Setup(fs => fs.LockDirectory(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation)))
                    .Returns(true);
            }

            [Fact]
            public void Should_return_one_validation_result()
            {
                Results.Should().HaveCount(1);
            }

            [Fact]
            public void Should_report_successful_validation()
            {
                var result = Results.First();
                result.ExitCode.Should().Be(0);
                result.Status.Should().Be(ValidationStatus.Success);
                result.Message.Should().Be("System Cache directory successfully created and locked down to administrators.".SplitOnSpace("   "));
            }
        }

        public sealed class When_cache_does_not_exist_and_cannot_be_created_or_locked_down : ElevatedCacheFolderLockdownValidationSpecsBase
        {
            public override void Context()
            {
                base.Context();

                FileSystem
                    .Setup(fs => fs.DirectoryExists(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation)))
                    .Returns(false);

                FileSystem
                    .Setup(fs => fs.LockDirectory(It.Is<string>(s => s == ApplicationParameters.HttpCacheLocation)))
                    .Returns(false);
            }

            [Fact]
            public void Should_return_one_validation_result()
            {
                Results.Should().HaveCount(1);
            }

            [Fact]
            public void Should_fail_validation_stating_directory_cannot_be_locked_down()
            {
                var result = Results.First();
                result.ExitCode.Should().Be(1);
                result.Status.Should().Be(ValidationStatus.Error);
                result.Message.Should().Be("System Cache directory was not created, or could not be locked down to administrators.".SplitOnSpace(linePrefix: "   "));
            }
        }
    }
}
