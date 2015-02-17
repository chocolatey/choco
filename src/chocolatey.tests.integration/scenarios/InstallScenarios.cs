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

namespace chocolatey.tests.integration.scenarios
{
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using NuGet;
    using Should;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.results;

    public class InstallScenarios
    {
        public abstract class ScenariosBase : TinySpec
        {
            protected ConcurrentDictionary<string, PackageResult> Results;
            protected ChocolateyConfiguration Configuration;
            protected IChocolateyPackageService Service;

            public override void Context()
            {
                Configuration = Scenario.install();
                Scenario.reset(Configuration);
                Configuration.PackageNames = Configuration.Input = "installpackage";
                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "badpackage*" + Constants.PackageExtension);

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
            }
        }

        public class when_installing_a_package_happy_path : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                Directory.Exists(packageResult.InstallLocation).ShouldBeTrue();
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }

            [Fact]
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                packageResult.Version.ShouldEqual("1.0.0");
            }
        }      
        
        public class when_installing_an_already_installed_package : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_still_have_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_was_unable_to_install_any_packages()
            {
                bool installWarning = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("0/1")) installWarning = true;
                }

                installWarning.ShouldBeTrue();
            }   
            
            [Fact]
            public void should_contain_a_message_about_force_to_reinstall()
            {
                bool installWarning = false;
                foreach (var messageType in MockLogger.Messages.or_empty_list_if_null())
                {
                    foreach (var message in messageType.Value)
                    {
                        if (message.Contains("Use --force to reinstall")) installWarning = true;    
                    }
                }

                installWarning.ShouldBeTrue();
            }

            [Fact]
            public void should_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeTrue();
            }

            [Fact]
            public void should_ave_warning_package_result()
            {
                packageResult.Warning.ShouldBeTrue();
            }
        }
       
        public class when_force_installing_an_already_installed_package : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                Directory.Exists(packageResult.InstallLocation).ShouldBeTrue();
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }

            [Fact]
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                packageResult.Version.ShouldEqual("1.0.0");
            }
        }

        public class when_installing_a_package_with_dependencies_happy : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency*" + Constants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    Directory.Exists(packageResult.Value.InstallLocation).ShouldBeTrue();
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }
      
            [Fact]
            public void should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("2/2")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeTrue();
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Version.ShouldEqual("1.0.0");
                }
            }
        }

        public class when_installing_a_package_with_dependencies_and_dependency_cannot_be_found : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency*" + Constants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_not_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_was_unable_to_install_any_packages()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("0/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_a_successful_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_have_an_error_package_result()
            {
                bool errorFound = false;

                foreach (var packageResult in Results)
                {
                    foreach (var message in packageResult.Value.Messages)
                    {
                        if (message.MessageType == ResultType.Error)
                        {
                            errorFound = true;
                        }
                    }
                }

                errorFound.ShouldBeTrue();
            }

            [Fact]
            public void should_have_expected_error_in_package_result()
            {
                bool errorFound = false;

                foreach (var packageResult in Results)
                {
                    foreach (var message in packageResult.Value.Messages)
                    {
                        if (message.MessageType == ResultType.Error)
                        {
                            if (message.Message.Contains("Unable to resolve dependency 'isdependency")) errorFound = true;
                        }
                    }
                }

                errorFound.ShouldBeTrue();
            }
        } 
        
        public class when_installing_a_package_ignoring_dependencies_that_cannot_be_found : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency*" + Constants.PackageExtension);
                Configuration.IgnoreDependencies = true;
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                Directory.Exists(packageResult.InstallLocation).ShouldBeTrue();
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_not_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }
        }

        public class when_installing_an_existing_package_but_a_version_that_does_not_exist : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Version = "1.0.1";
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_did_not_install_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("0/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void should_have_an_error_package_result()
            {
                bool errorFound = false;
                foreach (var message in packageResult.Messages)
                {
                    if (message.MessageType == ResultType.Error)
                    {
                        errorFound = true;
                    }
                }

                errorFound.ShouldBeTrue();
            }

            [Fact]
            public void should_have_expected_error_in_package_result()
            {
                bool errorFound = false;
                foreach (var message in packageResult.Messages)
                {
                    if (message.MessageType == ResultType.Error)
                    {
                        if (message.Message.Contains("The package was not found")) errorFound = true;
                    }
                }

                errorFound.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_version_of_one_dot_zero_dot_one()
            {
                packageResult.Version.ShouldEqual("1.0.1");
            }
        }

        public class when_installing_a_package_that_does_not_exist : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "nonexisting";
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_was_unable_to_install_a_package()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("0/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void should_have_an_error_package_result()
            {
                bool errorFound = false;
                foreach (var message in packageResult.Messages)
                {
                    if (message.MessageType == ResultType.Error)
                    {
                        errorFound = true;
                    }
                }

                errorFound.ShouldBeTrue();
            }

            [Fact]
            public void should_have_expected_error_in_package_result()
            {
                bool errorFound = false;
                foreach (var message in packageResult.Messages)
                {
                    if (message.MessageType == ResultType.Error)
                    {
                        if (message.Message.Contains("The package was not found")) errorFound = true;
                    }
                }

                errorFound.ShouldBeTrue();
            }
        }

        public class when_installing_a_package_that_errors : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "badpackage";
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_put_a_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bad", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_was_unable_to_install_a_package()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("0/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void should_have_an_error_package_result()
            {
                bool errorFound = false;
                foreach (var message in packageResult.Messages)
                {
                    if (message.MessageType == ResultType.Error)
                    {
                        errorFound = true;
                    }
                }

                errorFound.ShouldBeTrue();
            }

            [Fact]
            public void should_have_expected_error_in_package_result()
            {
                bool errorFound = false;
                foreach (var message in packageResult.Messages)
                {
                    if (message.MessageType == ResultType.Error)
                    {
                        if (message.Message.Contains("chocolateyInstall.ps1")) errorFound = true;
                    }
                }

                errorFound.ShouldBeTrue();
            }
        }
    }
}