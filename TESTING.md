# Testing Chocolatey

Tests for Chocolatey are written in C# or in PowerShell depending on what type of test is being created.

* NUnit Unit Tests are written in C# and found in the `chocolatey.tests` project. They are individual tests of the various Chocolatey components.
* NUnit Integration tests are written in C# and found in the `chocolatey.tests.integration` project. They are tests of various Chocolatey components that reach outside of Chocolatey.
* Pester test are written in PowerShell and found in the `tests` directory of the repository. They test the overall integration of Chocolatey into a larger system.

## Testing Overview

> "Testing doesn't prove the absence of bugs, they can only prove code works in the way you've tested."

The design of our automated test suite is to get the testing framework out of the way and make it easy to swap out should it ever need to be (the former is the important goal). We test behaviors of the system, which doesn't simply mean ensuring code coverage. It means we want to see how the system behaves under certain behaviors. As you may see from looking over the tests, we have an interesting way of setting up our specs. We recommend importing the ReSharper templates in `docs\resharper_templates`. This will make adding specs and new spec files quite a bit easier.

The method of testing as you will see is a file that contains many test classes (scenarios) that set up and perform a behavior, then perform one or more validations (tests/facts) on that scenario. Typically when in a unit test suite, there would be a file for every representative class in the production code. You may not see this as much in this codebase as there are areas that could use more coverage.

We recognize the need for a very tight feedback loop (running and debugging tests right inside Visual Studio). Some great tools for running and debugging automated tests are [TestDriven.NET](http://www.testdriven.net/) and [ReSharper](https://www.jetbrains.com/resharper/) (you only need one, although both are recommended for development). We recommend TestDriven over other tools as it is absolutely wonderful in what it does.

With the way the testing framework is designed, it is helpful to gain an understanding on how you can debug into tests. There are a couple of known oddities when it comes to trying to run tests in Visual Studio:

- You can run a test or tests within a class.
- You can also right click on a folder (and solution folder), a project, or the solution and run tests.
- You can ***not*** click on a file and attempt to run/debug automated tests. You will see the following message: "The target type doesn't contain tests from a known test framework or a 'Main' method."
- You also cannot run all tests within a file by selecting somewhere outside a testing class and attempting to run tests. You will see the message above.

Some quick notes on testing terminology (still a WIP):

- Testing - anything done to test, whether manual, automated, or otherwise.
- Automated Testing - Any type of written test that can be run in an automated way, typically in the form of C# tests.
- Spec / Fact / Observation - these are synonyms for a test or validation.
- System Under Test (SUT) - the code or concern you are testing.
- Mock / Fake / Stub / Double - an object that provides a known state back to the system under test when the system under test interacts with other objects. This can be done with unit and whitebox integration testing. This allows for actual unit testing as most units (classes/functions) depend on working with other units (classes/functions) to get or set information and state. While each of [these are slightly different](https://martinfowler.com/articles/mocksArentStubs.html), the basic functionality is that they are standing in for other production code.
- Concern - an area of production code you are testing in e.g. "Concern for AutoUninstallerService".
- Regression Test Suite / Regression Suite - The automated tests that are in the form of code.
- Whitebox Testing - tests where you access the internals of the application.
  - Unit Testing - We define a unit as a class and a method in C#. In PowerShell this is per function. If it involves another class or function, you have graduated to an integration. This is where Mocks come in to ensure no outside state is introduced.
  - Whitebox Integration Testing - testing anything that is more than a unit.
  - System Integration Testing - testing anything that goes out of the bounds of the written production code. Typically when running the code to get or set some state is where you will see this. And yes, even using DateTime.Now counts as system integration testing as it accesses something external to the application. This is why you will see we insulate those calls to something in the application so they can be easily tested against.
- Blackbox Testing - tests where you do not access internals of the application
  - Physical Integration Testing - This is where you are testing the application with other components such as config files.
  - Blackbox Integration Testing / End to End Testing - This is where you are testing inputs and outputs of the system.

As far as testing goes, unit tests are extremely quick feedback and great for longer term maintenance, where black box tests give you the most coverage, but are the slowest feedback loops and typically the most frail. Each area of testing has strengths and weaknesses and it's good to understand each of them.

**NOTE**: One of the hardest forms of automated testing is unit testing, as it almost always requires faking out other parts of the system (also known as mocking).

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
