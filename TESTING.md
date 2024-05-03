# Testing Chocolatey

- [Testing Overview](#testing-overview)
- [Testing Terminology](#testing-terminology)
- [Writing Tests](#writing-tests)
  - [A Test Or Group Of Tests Should Be Self Contained.](#a-test-or-group-of-tests-should-be-self-contained)
  - [Tests Should Not Depend Upon The Order That They Are Executed.](#tests-should-not-depend-upon-the-order-that-they-are-executed)
  - [Assertions Should Be Consistent.](#assertions-should-be-consistent)
  - [Tests Should Not Be Skipped By A Version Check Of The Product Being Tested.](#tests-should-not-be-skipped-by-a-version-check-of-the-product-being-tested)
  - [Pester Specific: All Test Code Should Be Within Pester Controlled Blocks.](#pester-specific-all-test-code-should-be-within-pester-controlled-blocks)
- [Running Tests](#running-tests)
  - [NUnit Tests](#nunit-tests)
  - [NUnit Integration Tests](#nunit-integration-tests)
  - [All NUnit Integration Tests](#all-nunit-integration-tests)
  - [Skipping NUnit Tests](#skipping-nunit-tests)
  - [Pester Tests](#pester-tests)

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

As far as testing goes, unit tests are extremely quick feedback and great for longer term maintenance, where black box tests give you the most coverage, but are the slowest feedback loops and typically the most frail. Each area of testing has strengths and weaknesses and it's good to understand each of them.

**NOTE**: One of the hardest forms of automated testing is unit testing, as it almost always requires faking out other parts of the system (also known as mocking).

## Testing Terminology

Some quick notes on testing terminology (still a WIP):

- **Testing** - anything done to test, whether manual, automated, or otherwise.
- **Automated Testing** - Any type of written test that can be run in an automated way, typically in the form of C# tests.
- **Spec / Fact / Observation** - these are synonyms for a test or validation.
- **System Under Test (SUT)** - the code or concern you are testing.
- **Mock / Fake / Stub / Double** - an object that provides a known state back to the system under test when the system under test interacts with other objects. This can be done with unit and whitebox integration testing. This allows for actual unit testing as most units (classes/functions) depend on working with other units (classes/functions) to get or set information and state. While each of [these are slightly different](https://martinfowler.com/articles/mocksArentStubs.html), the basic functionality is that they are standing in for other production code.
- **Concern** - an area of production code you are testing in e.g. "Concern for AutoUninstallerService".
- **Regression Test Suite / Regression Suite** - The automated tests that are in the form of code.
- **Whitebox Testing** - tests where you access the internals of the application.
  - **Unit Testing** - We define a unit as a class and a method in C#. In PowerShell this is per function. If it involves another class or function, you have graduated to an integration. This is where Mocks come in to ensure no outside state is introduced.
  - **Whitebox Integration Testing** - testing anything that is more than a unit.
  - **System Integration Testing** - testing anything that goes out of the bounds of the written production code. Typically when running the code to get or set some state is where you will see this. And yes, even using DateTime.Now counts as system integration testing as it accesses something external to the application. This is why you will see we insulate those calls to something in the application so they can be easily tested against.
- **Blackbox Testing** - tests where you do not access internals of the application
  - **Physical Integration Testing** - This is where you are testing the application with other components such as config files.
  - **Blackbox Integration Testing / End to End Testing** - This is where you are testing inputs and outputs of the system.
- **Version Gate** - A check that is performed to ensure certain versions are in use before performing tests.
- **Test Structure** - All components that make up a single test. In Pester this would be a `Describe` or `Context` block. In Nunit this would be the containing class.

## Writing Tests

The purpose of the tests we write for Chocolatey products is to ensure that we do not regress on issues that have been fixed.
Part of ensuring that is to do the best we can to reduce test flakiness.

### A Test Or Group Of Tests Should Be Self Contained.

Everything needed for a test to pass consistently should be within a single test structure.
Whenever possible, you should be able to select any test case and run it without failure.

### Tests Should Not Depend Upon The Order That They Are Executed.

Expanding on the previous rule that tests should be self contained, they should also not depend on other tests running in a specific order.
If a test requires a previous test to pass, it is beneficial to have some validation that the previous test passed and to fail early if it did not.

For example: suppose `Test B` relies on `Test A` having completed successfully.
Further, `Test B` when successful takes between ten and twenty-five minutes to complete.
`Test B` is already written to fail after thirty minutes, but makes no accounting for `Test A`.
If `Test A` fails, then it is already known that `Test B` will fail, but it will potentially wait thirty minutes to fail.
Whereas, if a verification of `Test A` were performed early in `Test B`, it could return failure within a minute, shortening the feedback loop significantly.

### Assertions Should Be Consistent.

Some assertions need to be made in multiple tests.
When this happens, the assertions should be made in a consistent manner.

For example: many Pester tests execute Chocolatey CLI, then assert that the exit code was as expected.
All of the Pester tests should therefore assert the exit code in the same manner.
Previously, the Pester tests contained multiple different ways of checking the exit code, and sometimes would display the command output on error.
Efforts have been taken to make this consistent so that when a failure occurs, the command output is displayed.

### Tests Should Not Be Skipped By A Version Check Of The Product Being Tested.

Sometimes tests are written for features that are coming in a future version.
In those instances, a version gate would be used.
It is expected that the tests being run will be for the version being built from the same point in the repository.
As such, we should not be skipping tests due to a version mismatch.

The exception to this rule is tests that require another product of a specific version.
For example: a test that requires Chocolatey Licensed Extension version 6.1 or greater, but is a part of the Chocolatey CLI tests.

### Pester Specific: All Test Code Should Be Within Pester Controlled Blocks.

Currently Pester tests are targeted at Pester 5.x.
The guidance of [Pester](https://pester.dev/docs/usage/discovery-and-run#execution-order) is for all test code to be within `It`, `BeforeAll`, `BeforeEach`, `AfterAll`, or `AfterEach` blocks.
To quote [Pester's documentation](https://pester.dev/docs/usage/test-file-structure#beforediscovery):

> In Pester5 the mantra is to put all code in Pester controlled blocks.
> No code should be directly in the script, or directly in `Describe` or `Context` block without wrapping it in some other block.

## Running Tests

### NUnit Tests

The NUnit tests get run automatically when you run `./build.bat` or `./build.sh`, and you can also run them without completing the full build process by running `./build.bat --target=test-nunit --exclusive`, or `./build.sh --target=test-nunit --exclusive`.

### NUnit Integration Tests

If you need to run the integration tests, you can do so using: `./build.bat --target=test-nunit --exclusive --testExecutionType=integration --shouldRunOpenCover=false`, or `./build.sh --target=test-nunit --exclusive --testExecutionType=integration --shouldRunOpenCover=false`.

### All NUnit Integration Tests

If you need to run all the tests, you can do so using: `./build.bat --target=test-nunit --exclusive --testExecutionType=all --shouldRunOpenCover=false`, or `./build.sh --target=test-nunit --exclusive --testExecutionType=all --shouldRunOpenCover=false`.

The `shouldRunOpenCover` argument is required when running the integration tests because some of the integration tests rely on the standard output and error output, which is not available when run via OpenCover. This switch changes the NUnit tests to run on NUnit directly, instead of on NUnit via OpenCover.

### Skipping NUnit Tests

If you need to skip the execution of tests, you can run the following: `./build.bat --shouldRunTests=false`, or `./build.sh --shouldRunTests=false`.

### Pester Tests

The Pester tests have been modelled in a way to be testable without installing Chocolatey to your local system. We have made efforts to prevent installing software with the tests, or at least uninstalling any that is installed. That being said, we also provide a `Vagrantfile` with a base configuration that will build the Chocolatey package (inside the VM) and run the tests for you.

To run them locally on your system: Open an administrative PowerShell prompt to the root of the repository, and run `./Invoke-Tests.ps1`. This script will then "install" Chocolatey to a temporary test directory, run the tests, and when complete attempt to restore the system as close to when it started as possible. The script takes the following parameters: `TestPath` The location to use as the base for the Chocolatey Tests, defaults to `$env:TEMP\chocolateyTests`. `TestPackage` The path to the `.nupkg` package to run tests against, defaults to `$chocolateyRepository\code_drop\Packages\Chocolatey\chocolatey.<version>.nupkg`. `SkipPackaging` Optionally skip the packaging of the test packages.

#### Using the provided Vagrantfile

To use the `Vagrantfile` you need to change directory into the `tests` directory, then run `vagrant up`. The Vagrantfile has been tested with VirtualBox. The [box being used](https://app.vagrantup.com/StefanScherer/boxes/windows_2019) is currently only updated for vmware_desktop and virtualbox providers, but there is a dated hyperv one that may work.

Once the Vagrant box is booted, you can re-run just the tests by running `vagrant provision default --provision-with test`. If you would like to clear the packages and run fresh tests, you can run `vagrant provision default --provision-with clear-packages,test`.
