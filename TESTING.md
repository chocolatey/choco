# Testing Chocolatey

Tests for Chocolatey are written in C# or in PowerShell depending on what type of test is being created.

* NUnit Unit Tests are written in C# and found in the `chocolatey.tests` project. They are individual tests of the various Chocolatey components.
* NUnit Integration tests are written in C# and found in the `chocolatey.tests.integration` project. They are tests of various Chocolatey components that reach outside of Chocolatey.
* Pester test are written in PowerShell and found in the `tests` directory of the repository. They test the overall integration of Chocolatey into a larger system.

## Running Tests

### NUnit Tests

The NUnit tests get run automtically when you run `./build.bat`, and you can also run them without completing the full build process by running `./test.bat`.

### Pester Tests

The Pester tests have been modelled in a way to be testable without installing Chocolatey to your local system. We have made efforts to prevent installing software with the tests, or at least uninstalling any that is installed. That being said, we also provide a `Vagrantfile` with a base configuration that will build the Chocolatey package (inside the VM) and run the tests for you.

To run them locally on your system: Open an administrative PowerShell prompt to the root of the repository, and run `./Invoke-Tests.ps1`. This script will then "install" Chocolatey to a temporary test directory, run the tests, and when complete attempt to restore the system as close to when it started as possible. The script takes the following parameters: `TestPath` The location to use as the base for the Chocolatey Tests, defaults to `$env:TEMP\chocolateyTests`. `TestPackage` The path to the `.nupkg` package to run tests against, defaults to `$chocolateyRepository\code_drop\nuget\chocolatey.<version>.nupkg`. `SkipPackaging` Optionally skip the packaging of the test packages.

To use the `Vagrantfile` you need to change directory into the `tests` directory, then run `vagrant up`. The Vagrantfile has been tested with VirtualBox. The [box being used](https://app.vagrantup.com/StefanScherer/boxes/windows_2019) is currently only updated for vmware_desktop and virtualbox providers, but there is a dated hyperv one that should work.
