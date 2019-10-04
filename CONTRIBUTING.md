# Contributing
The Chocolatey team has very explicit information here regarding the process for contributions, and we will be sticklers about the way you write your commit messages (yes, really), so to save yourself some rework, please make sure you read over this entire document prior to contributing.

<!-- TOC -->

- [Are You In the Right Place?](#are-you-in-the-right-place)
  - [Reporting an Issue/Bug?](#reporting-an-issuebug)
    - [SolutionVersion.cs](#solutionversioncs)
  - [Package Issue?](#package-issue)
  - [Package Request? Package Missing?](#package-request-package-missing)
  - [Submitting an Enhancement / Feature Request?](#submitting-an-enhancement--feature-request)
    - [Submitting an Enhancement For Choco](#submitting-an-enhancement-for-choco)
- [Contributing](#contributing)
  - [Prerequisites](#prerequisites)
    - [Definition of Trivial Contributions](#definition-of-trivial-contributions)
    - [Is the CLA Really Required?](#is-the-cla-really-required)
- [Contributing Process](#contributing-process)
  - [Get Buyoff Or Find Open Community Issues/Features](#get-buyoff-or-find-open-community-issuesfeatures)
  - [Set Up Your Environment](#set-up-your-environment)
  - [Code Format / Design](#code-format--design)
    - [CSharp](#csharp)
    - [PowerShell](#powershell)
  - [Debugging / Testing](#debugging--testing)
    - [Visual Studio](#visual-studio)
      - [Automated Tests](#automated-tests)
    - [Chocolatey Build](#chocolatey-build)
  - [Prepare Commits](#prepare-commits)
  - [Submit Pull Request (PR)](#submit-pull-request-pr)
  - [Respond to Feedback on Pull Request](#respond-to-feedback-on-pull-request)
- [Other General Information](#other-general-information)

<!-- /TOC -->

## Are You In the Right Place?
Chocolatey is a large ecosystem and each component has their own location for submitting issues and enhancement requests. While the website (the community package repository) may be all you know for packages, it represents only a tiny fraction of existing packages (organizations typically maintain and host their own packages internally). This is the repository for choco.exe (the client CLI tool) for Chocolatey, which spans multiple types of environments.

Please follow these decision criteria to see if you are in the right location or if you should head to a different location to submit your request.

### Reporting an Issue/Bug?
![submitting issues](https://cloud.githubusercontent.com/assets/63502/12534440/fc223b74-c21e-11e5-9a41-1ffc1c9af48f.png)

Submitting an Issue (or a Bug)? See the **[Submitting Issues](https://github.com/chocolatey/choco#submitting-issues) section** in the [README](https://github.com/chocolatey/choco/blob/master/README.md#submitting-issues).

#### SolutionVersion.cs
Do not submit issues for missing `SolutionVersion.cs`. Please see [Compiling / Building Source](https://github.com/chocolatey/choco#compiling--building-source).

### Package Issue?
Please see [Request Package Fixes or Updates / Become a maintainer of an existing package](https://chocolatey.org/docs/package-triage-process).

### Package Request? Package Missing?
If you are looking for packages to be added to the community feed (aka https://chocolatey.org/packages), please see [Package Requests](https://chocolatey.org/docs/package-triage-process#package-request-package-missing).

### Submitting an Enhancement / Feature Request?
If this is for choco (the CLI tool), this is the right place. See below. Otherwise see [Submitting Issues](https://github.com/chocolatey/choco#submitting-issues) for enhancements to the website, enhancements to the ChocolateyGUI, etc.

#### Submitting an Enhancement For Choco
Log a github issue. There are fewer constraints on this versus reporting issues.

## Contributing
The process for contributions is roughly as follows:

### Prerequisites
 * Submit the Enhancement ticket. You will need the issue id for your commits.
 * Ensure you have signed the Contributor License Agreement (CLA) - without this we are not able to take contributions that are not trivial.
  * [Sign the Contributor License Agreement](https://www.clahub.com/agreements/chocolatey/choco).
  * You must do this for each Chocolatey project that requires it.
  * If you are curious why we would require a CLA, we agree with Julien Ponge - take a look at his [post](https://julien.ponge.org/blog/in-defense-of-contributor-license-agreements/).
 * You agree to follow the [etiquette regarding communication](https://github.com/chocolatey/choco#etiquette-regarding-communication).

#### Definition of Trivial Contributions
It's hard to define what is a trivial contribution. Sometimes even a 1 character change can be considered significant. Unfortunately because it can be subjective, the decision on what is trivial comes from the committers of the project and not from folks contributing to the project. It is generally safe to assume that you may be subject to signing the [CLA](https://www.clahub.com/agreements/chocolatey/choco) and be prepared to do so. Ask in advance if you are not sure and for reasons are not able to sign the [CLA](https://www.clahub.com/agreements/chocolatey/choco).

What is generally considered trivial:

* Fixing a typo
* Documentation changes
* Fixes to non-production code - like fixing something small in the build code.

What is generally not considered trivial:

 * Changes to any code that would be delivered as part of the final product. This includes any scripts that are delivered, such as PowerShell scripts. Yes, even 1 character changes could be considered non-trivial.

#### Is the CLA Really Required?

Yes, and this aspect is not up for discussion. If you would like more resources on understanding CLAs, please see the following articles:

* [What is a CLA and why do I care?](https://www.clahub.com/pages/why_cla)
* [In defense of Contributor License Agreements](https://julien.ponge.org/blog/in-defense-of-contributor-license-agreements/)
* [Contributor License Agreements](http://oss-watch.ac.uk/resources/cla)
* Dissenting opinion - [Why your project doesn't need a Contributor License Agreement](https://sfconservancy.org/blog/2014/jun/09/do-not-need-cla/)

Overall, the flexibility and legal protections provided by a CLA make it necessary to require a CLA. As there is a company and a licensed version behind Chocolatey, those protections must be afforded. We understand this means some folks won't be able to contribute and that's completely fine. We prefer you to know up front this is required so you can make the best decision about contributing.

If you work for an organization that does not allow you to contribute without attempting to own the rights to your work, please do not sign the CLA.

## Contributing Process

Start with [Prerequisites](#prerequisites) and make sure you can sign the Contributor License Agreement (CLA).

### Get Buyoff Or Find Open Community Issues/Features
 * Through a Github issue (preferred), through the [mailing list](https://groups.google.com/forum/#!forum/chocolatey), or through [Gitter](https://gitter.im/chocolatey/choco), talk about a feature you would like to see (or a bug fix), and why it should be in Chocolatey.
   * If approved through the mailing list or in Gitter chat, ensure the accompanying GitHub issue is created with information and a link back to the mailing list discussion (or the Gitter conversation).
 * Once you get a nod from one of the [Chocolatey Team](https://github.com/chocolatey?tab=members), you can start on the feature.
 * Alternatively, if a feature is on the issues list with the [Up For Grabs](https://github.com/chocolatey/choco/issues?q=is%3Aopen+is%3Aissue+label%3A%22Up+For+Grabs%22) label, it is open for a community member (contributor) to patch. You should comment that you are signing up for it on the issue so someone else doesn't also sign up for the work.

### Set Up Your Environment
 * Visual Studio 2010+ is recommended for code contributions.
 * For git specific information:
    1. Create a fork of chocolatey/choco under your GitHub account. See [forks](https://help.github.com/articles/working-with-forks/) for more information.
    1. [Clone your fork](https://help.github.com/articles/cloning-a-repository/) locally.
    1. Open a command line and navigate to that directory.
    1. Add the upstream fork - `git remote add upstream git@github.com:chocolatey/choco.git`
    1. Run `git fetch upstream`
    1. Ensure you have user name and email set appropriately to attribute your contributions - see [Name](https://help.github.com/articles/setting-your-username-in-git/) / [Email](https://help.github.com/articles/setting-your-email-in-git/).
    1. Ensure that the local repository has the following settings (without `--global`, these only apply to the *current* repository):
      * `git config core.autocrlf false`
      * `git config core.symlinks false`
      * `git config merge.ff false`
      * `git config merge.log true`
      * `git config fetch.prune true`
    1. From there you create a branch named specific to the feature.
    1. In the branch you do work specific to the feature.
    1. For committing the code, please see [Prepare Commits](#prepare-commits).
    1. See [Submit Pull Request (PR)](#submit-pull-request-pr).
 * Please also observe the following:
    * Unless specifically requested, do not reformat the code. It makes it very difficult to see the change you've made.
    * Do not change files that are not specific to the feature.
    * More covered below in the [**Prepare commits**](#prepare-commits) section.
 * Test your changes and please help us out by updating and implementing some automated tests. It is recommended that all contributors spend some time looking over the tests in the source code. You can't go wrong emulating one of the existing tests and then changing it specific to the behavior you are testing.
    * While not an absolute requirement, automated tests will help reviewers feel comfortable about your changes, which gets your contributions accepted faster.
 * Please do not update your branch from the master unless we ask you to. See the responding to feedback section below.

### Code Format / Design
#### CSharp
 * If you are using ReSharper, all of this is already in the shared resharper settings.
 * Class names and Properties are `PascalCase` - this is nearly the only time you start with uppercase.
 * Namespaces (and their representative folders) are lowercase.
 * Methods and functions are lowercase. Breaks between words in functions are typically met with an underscore (`_`, e.g. `run_actual()`).
 * Variables and parameters are `camelCase`.
 * Constants are `UPPER_CASE`.
 * There are some adapters over the .NET Framework to ensure some additional functionality works and is consistent. Sometimes this is completely seamless that you are using these (e.g. `Console`).

#### PowerShell
 * PowerShell must be CRLF and UTF-8. Git attributes are not used, so Git will not ensure this for you.
 * The minimum version of PowerShell this must work with is v2. This makes things somewhat more limited but compatible across the board for all areas Chocolatey is deployed. It is getting harder to find a reference for PowerShell v2, but this is a good one: http://adamringenberg.com/powershell2/table-of-contents/.
 * If you add a new file, also ensure you add it to the Visual Studio project and ensure it becomes an embedded resource.
 * The last parameter in every function must be `[parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments`. This allows for future expansion and compatibility - as new parameters are introduced and used, it doesn't break older versions of Chocolatey.
 * Do not add new positional elements to functions. We want to promote using named parameters in calling functions.
 * Do not remove any existing positional elements from functions. We need to maintain compatibility with older versions of Chocolatey.
 * One of the first calls in a function is to debug what was passed to it - `Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters`

### Debugging / Testing
When you want to manually verify your changes and run Choco, you have some options.

**NOTE:** Chocolatey behaves differently when built with `Debug` and `Release` configurations. Release is always going to seek out the machine installation (`$env:ChocolateyInstall`), where Debug just runs right next to wherever the choco.exe file is.

#### Visual Studio
When you are using Visual Studio, ensure the following:

 * Use `Debug` configuration - debug configuration keeps your local changes separate from the machine installed Chocolatey.
 * `chocolatey.console` is the project you are looking to run.
 * If you make changes to anything that is in `chocolatey.resources`, delete the folder in `chocolatey.console\bin\Debug` that corresponds to where you've made changes as Chocolatey does not automatically detect changes in the files it is extracting from resource manifests.
 * The automated testing framework that Chocolatey uses is [NUnit](https://www.nunit.org/), [TinySpec](https://www.nuget.org/packages/TinySpec.NUnit), [Moq](https://www.nuget.org/packages/moq), and [Should](https://www.nuget.org/packages/Should/). Do not be thrown off thinking it using something else based on seeing `Fact` attributes for specs/tests. That is TinySpec.
 * For a good understanding of all frameworks used, please see [CREDITS](https://github.com/chocolatey/choco/blob/master/docs/legal/CREDITS.md)

##### Automated Tests

> "Testing doesn't prove the absence of bugs, they can only prove code works in the way you've tested."

The design of our automated test suite is to get the testing framework out of the way and make it easy to swap out should it ever need to be (the former is the important goal). We test behaviors of the system, which doesn't simply mean ensuring code coverage. It means we want to see how the system behaves under certain behaviors. As you may see from looking over the tests, we have an interesting way of setting up our specs. We recommend importing the ReSharper templates in `docs\resharper_templates`. This will make adding specs and new spec files quite a bit easier.

The method of testing as you will see is a file that contains many test classes (scenarios) that set up and perform a behavior, then perform one or more validations (tests/facts) on that scenario. Typically when in a unit test suite, there would be a file for every representative class in the production code. You may not see this as much in this codebase as there are areas that could use more coverage.

We recognize the need for a very tight feedback loop (running and debugging tests right inside Visual Studio). Some great tools for running and debugging automated tests are [TestDriven.NET](http://www.testdriven.net/) and [ReSharper](https://www.jetbrains.com/resharper/) (you only need one, although both are recommended for development). We recommend TestDriven over other tools as it is absolutely wonderful in what it does.

With the way the testing framework is designed, it is helpful to gain an understanding on how you can debug into tests. There are a couple of known oddities when it comes to trying to run tests in Visual Studio:

 * You can run a test or tests within a class.
 * You can also right click on a folder (and solution folder), a project, or the solution and run tests.
 * You can ***not*** click on a file and attempt to run/debug automated tests. You will see the following message: "The target type doesn't contain tests from a known test framework or a 'Main' method."
 * You also cannot run all tests within a file by selecting somewhere outside a testing class and attempting to run tests. You will see the message above.

Some quick notes on testing terminology (still a WIP):

 * Testing - anything done to test, whether manual, automated, or otherwise.
 * Automated Testing - Any type of written test that can be run in an automated way, typically in the form of C# tests.
 * Spec / Fact / Observation - these are synonyms for a test or validation.
 * System Under Test (SUT) - the code or concern you are testing.
 * Mock / Fake / Stub / Double - an object that provides a known state back to the system under test when the system under test interacts with other objects. This can be done with unit and whitebox integration testing. This allows for actual unit testing as most units (classes/functions) depend on working with other units (classes/functions) to get or set information and state. While each of [these are slightly different](https://martinfowler.com/articles/mocksArentStubs.html), the basic functionality is that they are standing in for other production code.
 * Concern - an area of production code you are testing in e.g. "Concern for AutoUninstallerService".
 * Regression Test Suite / Regression Suite - The automated tests that are in the form of code.
 * Whitebox Testing - tests where you access the internals of the application.
    * Unit Testing - We define a unit as a class and a method in C#. In PowerShell this is per function. If it involves another class or function, you have graduated to an integration. This is where Mocks come in to ensure no outside state is introduced.
    * Whitebox Integration Testing - testing anything that is more than a unit.
    * System Integration Testing - testing anything that goes out of the bounds of the written production code. Typically when running the code to get or set some state is where you will see this. And yes, even using DateTime.Now counts as system integration testing as it accesses something external to the application. This is why you will see we insulate those calls to something in the application so they can be easily tested against.
 * Blackbox Testing - tests where you do not access internals of the application
    * Physical Integration Testing - This is where you are testing the application with other components such as config files.
    * Blackbox Integration Testing / End to End Testing - This is where you are testing inputs and outputs of the system.

As far as testing goes, unit tests are extremely quick feedback and great for longer term maintenance, where black box tests give you the most coverage, but are the slowest feedback loops and typically the most frail. Each area of testing has strengths and weaknesses and it's good to understand each of them.

**NOTE**: One of the hardest forms of automated testing is unit testing, as it almost always requires faking out other parts of the system (also known as mocking).

#### Chocolatey Build
**NOTE:** When you are doing this, we almost always recommend you take the output of the build to another machine to do the testing, like the [Chocolatey Test Environment](https://github.com/chocolatey/chocolatey-test-environment).

 * Run `build.bat`.
 * There is a detailed log of the output in both `build_output` and `code_drop\build_artifacts`. The `build_artifacts` folders contain a lot of detail with each individual tool's output and reporting (helpful when wanting to see a visual of what tests failed).
 * There are two folders created - `build_output` and `code_drop`. You are looking for `code_drop\chocolatey\console` or `code_drop\nuget`. The `choco.exe` file contains everything it needs, but it does unpack the manifest on first use, so you could run into [#1292](https://github.com/chocolatey/choco/issues/1292).
 * You will need to pass `--allow-unofficial-build` for it to work when built with release mode.
 * You can also try `build.debug.bat` - note that it is newer and it may have an issue or two. It doesn't require `--allow-unofficial-build` as the binaries are built for debugging.
 * Use `.\choco.exe` to point to the local file. By default in PowerShell.exe, if you have Chocolatey installed, when you call `choco`, that is using the installed `choco` and not the one in the folder you are currently in. You must be explicit. This catches nearly everyone.

### Prepare Commits
This section serves to help you understand what makes a good commit.

A commit should observe the following:

 * A commit is a small logical unit that represents a change.
 * Should include new or changed tests relevant to the changes you are making.
 * No unnecessary whitespace. Check for whitespace with `git diff --check` and `git diff --cached --check` before commit.
 * You can stage parts of a file for commit.

A commit message should observe the following (based on ["A Note About Git Commit Messages"](http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html)):

  * The first line of the commit message should be a short description around 50 characters in length and be prefixed with the GitHub issue it refers to with parentheses surrounding that. If the GitHub issue is #25, you should have `(GH-25)` prefixed to the message.
  * If the commit is about documentation, the message should be prefixed with `(doc)`.
  * If it is a trivial commit or one of formatting/spaces fixes, it should be prefixed with `(maint)`.
  * After the subject, skip one line and fill out a body if the subject line is not informative enough.
  * Sometimes you will find that even a tiny code change has a commit body that needs to be very detailed and make take more time to do than the actual change itself!
  * The body:
    * Should wrap at `72` characters.
    * Explains more fully the reason(s) for the change and contrasts with previous behavior.
    * Uses present tense. "Fix" versus "Fixed".

A good example of a commit message is as follows:

```
(GH-7) Installation Adds All Required Folders

Previously the installation script worked for the older version of
Chocolatey. It does not work similarly for the newer version of choco
due to location changes for the newer folders. Update the install
script to ensure all folder paths exist.

Without this change the install script will not fully install the new
choco client properly.
```

### Submit Pull Request (PR)
Prerequisites:

 * You are making commits in a feature branch.
 * All specs should be passing.

Submitting PR:

 * Once you feel it is ready, submit the pull request to the `chocolatey/choco` repository against the `master` branch ([more information on this can be found here](https://help.github.com/articles/creating-a-pull-request)) unless specifically requested to submit it against another branch (usually `stable` in these instances).
  * In the case of a larger change that is going to require more discussion, please submit a PR sooner. Waiting until you are ready may mean more changes than you are interested in if the changes are taking things in a direction the committers do not want to go.
 * In the pull request, outline what you did and point to specific conversations (as in URLs) and issues that you are are resolving. This is a tremendous help for us in evaluation and acceptance.
 * Once the pull request is in, please do not delete the branch or close the pull request (unless something is wrong with it).
 * One of the Chocolatey Team members, or one of the committers, will evaluate it within a reasonable time period (which is to say usually within 2-4 weeks). Some things get evaluated faster or fast tracked. We are human and we have active lives outside of open source so don't fret if you haven't seen any activity on your pull request within a month or two. We don't have a Service Level Agreement (SLA) for pull requests. Just know that we will evaluate your pull request.

### Respond to Feedback on Pull Request
We may have feedback for you in the form of requested changes or fixes. We generally like to see that pushed against the same topic branch (it will automatically update the PR). You can also fix/squash/rebase commits and push the same topic branch with `--force` (while it is generally acceptable to do this on topic branches not in the main repository, a force push should be avoided at all costs against the main repository).

If we have comments or questions when we do evaluate it and receive no response, it will probably lessen the chance of getting accepted. Eventually this means it will be closed if it is not accepted. Please know this doesn't mean we don't value your contribution, just that things go stale. If in the future you want to pick it back up, feel free to address our concerns/questions/feedback and reopen the issue/open a new PR (referencing old one).

Sometimes we may need you to rebase your commit against the latest code before we can review it further. If this happens, you can do the following:

 * `git fetch upstream` (upstream would be the mainstream repo or `chocolatey/choco` in this case)
 * `git checkout master`
 * `git rebase upstream/master`
 * `git checkout your-branch`
 * `git rebase master`
 * Fix any merge conflicts
 * `git push origin your-branch` (origin would be your GitHub repo or `your-github-username/choco` in this case). You may need to `git push origin your-branch --force` to get the commits pushed. This is generally acceptable with topic branches not in the mainstream repository.

The only reasons a pull request should be closed and resubmitted are as follows:

  * When the pull request is targeting the wrong branch (this doesn't happen as often).
  * When there are updates made to the original by someone other than the original contributor (and the PR is not open for contributions). Then the old branch is closed with a note on the newer branch this supersedes #github_number.

## Other General Information
The helpers/utility functions that are available to the packages are what we consider the API. If you are working in the API, please note that you will need to maintain backwards compatibility. If you plan to rename a function or make it more generic, you must provide an alias in the [chocolateyInstaller.psm1](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/chocolateyInstaller.psm1) as part of what gets exported. You should not remove or reorder parameters, only add optional parameters to the end. They should be named and not positional (we are moving away from positional parameters as much as possible).

If you reformat code or hit core functionality without an approval from a person on the Chocolatey Team, it's likely that no matter how awesome it looks afterwards, it will probably not get accepted. Reformatting code makes it harder for us to evaluate exactly what was changed.

If you do these things, it will be make evaluation and acceptance easy. Now if you stray outside of the guidelines we have above, it doesn't mean we are going to ignore your pull request. It will just make things harder for us.  Harder for us roughly translates to a longer SLA for your pull request.
