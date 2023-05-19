# Testing Chocolatey

Tests for Chocolatey are written in C# or in PowerShell depending on what type of test is being created.

* NUnit Unit Tests are written in C# and found in the `chocolatey.tests` project. They are individual tests of the various Chocolatey components.
* NUnit Integration tests are written in C# and found in the `chocolatey.tests.integration` project. They are tests of various Chocolatey components that reach outside of Chocolatey.
* Pester test are written in PowerShell and found in the `tests` directory of the repository. They test the overall integration of Chocolatey into a larger system.

## Running Tests

### NUnit Tests

The NUnit tests get run automatically when you run `./build.bat` or `./build.sh`, and you can also run them without completing the full build process by running `./build.bat --target=test-nunit --exclusive`, or `./build.sh --target=test-nunit --exclusive`.

### NUnit Integration Tests

If you need to run the integration tests, you can do so using: `./build.bat --target=test-nunit --exclusive --testExecutionType=integration --shouldRunOpenCover=false`, or `./build.sh --target=test-nunit --exclusive --testExecutionType=integration --shouldRunOpenCover=false`.

### All NUnit Integration Tests

If you need to run all the tests, you can do so using: `./build.bat --target=test-nunit --exclusive --testExecutionType=all --shouldRunOpenCover=false`, or `./build.sh --target=test-nunit --exclusive --testExecutionType=all --shouldRunOpenCover=false`.

The `shouldRunOpenCover` argument is required when running the integration tests because some of the integration tests rely on the standard output and error output, which is not available when run via OpenCover. This switch changes the NUnit tests to run on NUnit directly, instead of on NUnit via OpenCover.

### Skipping NUnit Tests

If you need to skip the execution of tests, you can run the following: `./build.bat --testExecutionType=none`, or `./build.sh --testExecutionType=none`.

### Pester Tests

The Pester tests have been modelled in a way to be testable without installing Chocolatey to your local system. We have made efforts to prevent installing software with the tests, or at least uninstalling any that is installed. That being said, we also provide a `Vagrantfile` with a base configuration that will build the Chocolatey package (inside the VM) and run the tests for you.

To run them locally on your system: Open an administrative PowerShell prompt to the root of the repository, and run `./Invoke-Tests.ps1`. This script will then "install" Chocolatey to a temporary test directory, run the tests, and when complete attempt to restore the system as close to when it started as possible. The script takes the following parameters: `TestPath` The location to use as the base for the Chocolatey Tests, defaults to `$env:TEMP\chocolateyTests`. `TestPackage` The path to the `.nupkg` package to run tests against, defaults to `$chocolateyRepository\code_drop\Packages\Chocolatey\chocolatey.<version>.nupkg`. `SkipPackaging` Optionally skip the packaging of the test packages.

To use the `Vagrantfile` you need to change directory into the `tests` directory, then run `vagrant up`. The Vagrantfile has been tested with VirtualBox. The [box being used](https://app.vagrantup.com/StefanScherer/boxes/windows_2019) is currently only updated for vmware_desktop and virtualbox providers, but there is a dated hyperv one that should work.
