namespace chocolatey.tests.infrastructure.commands
{
    using System;
    using System.IO;
    using Moq;
    using NUnit.Framework;
    using Should;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.filesystem;

    public class PowershellExecutorSpecs
    {
        public abstract class PowerShellExecutorSpecsBase : TinySpec
        {
            protected Mock<IFileSystem> FileSystem;

            public override void Context()
            {
                FileSystem = new Mock<IFileSystem>();
            }
        }

        public class when_powershellExecutor_is_searching_for_powershell_locations_and_all_locations_exist : PowerShellExecutorSpecsBase
        {
            private string result = string.Empty;
            private string expected = Environment.ExpandEnvironmentVariables("%windir%\\SysNative\\WindowsPowerShell\\v1.0\\powershell.exe");
         
            public override void Context()
            {
                base.Context();
                FileSystem.Setup(fs => fs.file_exists(It.IsAny<string>())).Returns(true);
            }

            public override void Because()
            {
               result = PowershellExecutor.get_powershell_location(FileSystem.Object);
            }

            [Fact]
            public void should_not_return_null()
            {
                result.ShouldNotBeNull();
            }

            [Fact]
            public void should_find_powershell()
            {
                result.ShouldNotBeEmpty();
            }

            [Fact]
            public void should_return_the_sysnative_path()
            {
                result.ShouldEqual(expected);
            }
        }    
        
        public class when_powershellExecutor_is_searching_for_powershell_locations_there_is_no_sysnative : PowerShellExecutorSpecsBase
        {
            private string result = string.Empty;
            private string expected = Environment.ExpandEnvironmentVariables("%windir%\\System32\\WindowsPowerShell\\v1.0\\powershell.exe");
         
            public override void Context()
            {
                base.Context();
               
                FileSystem.Setup(fs => fs.file_exists(expected)).Returns(true);
                FileSystem.Setup(fs => fs.file_exists(It.Is<string>(v => v != expected))).Returns(false);
            }

            public override void Because()
            {
               result = PowershellExecutor.get_powershell_location(FileSystem.Object);
            }

            [Fact]
            public void should_return_system32_path()
            {
                result.ShouldEqual(expected);
            }
        }  
        
        public class when_powershellExecutor_is_searching_for_powershell_locations_and_powershell_is_not_found : PowerShellExecutorSpecsBase
        {
            public override void Context()
            {
                base.Context();
                FileSystem.Setup(fs => fs.file_exists(It.IsAny<string>())).Returns(false);
            }

            public override void Because()
            {
              //nothing
            }

            [Fact]
            public void should_throw_an_error()
            {
                Assert.Throws<FileNotFoundException>(() => PowershellExecutor.get_powershell_location(FileSystem.Object));
         
            }
        }
    }
}