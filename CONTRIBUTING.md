# Reporting an Issue/Bug?
![submitting isseus](https://cloud.githubusercontent.com/assets/63502/12534440/fc223b74-c21e-11e5-9a41-1ffc1c9af48f.png)

Submitting an Issue (or a Bug)? See the **[Submitting Issues](https://github.com/chocolatey/choco#submitting-issues) section** in the [README](https://github.com/chocolatey/choco/blob/master/README.md#submitting-issues).

**NOTE**: Do not submit issues for missing `SolutionVersion.cs`. Please see [Compiling / Building Source](https://github.com/chocolatey/choco#compiling--building-source).

# Submitting an Enhancement / Feature Request?

This is the right place. See below.

## Package Issue?
Please see [Request Package Fixes or Updates / Become a maintainer of an existing package](https://github.com/chocolatey/choco/wiki/PackageTriageProcess).

## Package Request? Package Missing?
If you are looking for packages to be added to the community feed (aka https://chocolatey.org/packages), please see [Package Requests](https://github.com/chocolatey/choco/wiki/PackageTriageProcess#package-request-package-missing).

# Submitting an Enhancement

Log a github issue. There are less constraints on this versus reporting issues.

# Contributors
The process for contributions is roughly as follows:

## Prerequisites

 * Submit the Enhancement ticket. You will need the issue id for your commits.
 * Ensure you have signed the Contributor License Agreement (CLA) - without this we are not able to take contributions that are not trivial.
  * [Sign the Contributor License Agreement](https://www.clahub.com/agreements/chocolatey/choco).
  * You must do this for each Chocolatey project that requires it.
  * If you are curious why we would require a CLA, we agree with Julien Ponge - take a look at his [post](https://julien.ponge.org/blog/in-defense-of-contributor-license-agreements/).
 * You agree to follow the [etiquette regarding communication](https://github.com/chocolatey/choco#etiquette-regarding-communication).

### Definition of Trivial Contributions
It's hard to define what is a trivial contribution. Sometimes even a 1 character change can be considered significant. Unfortunately because it can be subjective, the decision on what is trivial comes from the committers of the project and not from folks contributing to the project. It is generally safe to assume that you may be subject to signing the [CLA](https://www.clahub.com/agreements/chocolatey/choco) and be prepared to do so. Ask in advance if you are not sure and for reasons are not able to sign the [CLA](https://www.clahub.com/agreements/chocolatey/choco).

What is generally considered trivial:

* Fixing a typo
* Documentation changes
* Fixes to non-production code - like fixing something small in the build code.

What is generally not considered trivial:

 * Changes to any code that would be delivered as part of the final product. This includes any scripts that are delivered, such as PowerShell scripts. Yes, even 1 character changes could be considered non-trivial.

## Contributing Process
### Get Buyoff Or Find Open Community Issues/Features

 * Through GitHub, or through the [mailing list](https://groups.google.com/forum/#!forum/chocolatey) (preferred), you talk about a feature you would like to see (or a bug), and why it should be in Chocolatey.
   * If approved through the mailing list, ensure the accompanying GitHub issue is created with information and a link back to the mailing list discussion.
 * Once you get a nod from one of the [Chocolatey Team](https://github.com/chocolatey?tab=members), you can start on the feature.
 * Alternatively, if a feature is on the issues list with the [Up For Grabs](https://github.com/chocolatey/choco/issues?q=is%3Aopen+is%3Aissue+label%3A%22Up+For+Grabs%22) label, it is open for a community member (contributor) to patch. You should comment that you are signing up for it on the issue so someone else doesn't also sign up for the work.

### Set Up Your Environment

 * You create, or update, a fork of chocolatey/choco under your GitHub account.
 * From there you create a branch named specific to the feature.
 * In the branch you do work specific to the feature.
 * Please also observe the following:
    * No reformatting
    * No changing files that are not specific to the feature
    * More covered below in the **Prepare commits** section.
 * Test your changes and please help us out by updating and implementing some automated tests. It is recommended that all contributors spend some time looking over the tests in the source code. You can't go wrong emulating one of the existing tests and then changing it specific to the behavior you are testing.
 * Please do not update your branch from the master unless we ask you to. See the responding to feedback section below.

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
    * Should indent at `72` characters.
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

We may have feedback for you to fix or change some things. We generally like to see that pushed against the same topic branch (it will automatically update the Pull Request). You can also fix/squash/rebase commits and push the same topic branch with `--force` (it's generally acceptable to do this on topic branches not in the main repository, it is generally unacceptable and should be avoided at all costs against the main repository).

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
  * When there are updates made to the original by someone other than the original contributor. Then the old branch is closed with a note on the newer branch this supersedes #github_number.

## Other General Information
The helpers/utility functions that are available to the packages are what we consider the API. If you are working in the API, please note that you will need to maintain backwards compatibility. If you plan to rename a function or make it more generic, you must provide an alias in the [chocolateyInstaller.psm1](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/chocolateyInstaller.psm1) as part of what gets exported. You should not remove or reorder parameters, only add optional parameters to the end. They should be named and not positional (we are moving away from positional parameters as much as possible).

If you reformat code or hit core functionality without an approval from a person on the Chocolatey Team, it's likely that no matter how awesome it looks afterwards, it will probably not get accepted. Reformatting code makes it harder for us to evaluate exactly what was changed.

If you do these things, it will be make evaluation and acceptance easy. Now if you stray outside of the guidelines we have above, it doesn't mean we are going to ignore your pull request. It will just make things harder for us.  Harder for us roughly translates to a longer SLA for your pull request.
