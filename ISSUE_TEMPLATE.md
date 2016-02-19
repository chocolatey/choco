## Prerequisites

Ensure the following - (most of which is contained in [submitting issues](https://github.com/chocolatey/choco#submitting-issues)):

 * [ ] You have read over [Submitting Issues](https://github.com/chocolatey/choco#submitting-issues)
 * [ ] You are not submitting a question - questions are better served as [emails](http://groups.google.com/group/chocolatey) or [gitter chat questions](https://gitter.im/chocolatey/choco).
 * [ ] You agree to follow the [etiquette regarding communication](https://github.com/chocolatey/choco#etiquette-regarding-communication).
 * [ ] This is not a package issue - If you are having issue with a package, please see [Request Package Fixes or Updates / Become a maintainer of an existing package](https://github.com/chocolatey/choco/wiki/PackageTriageProcess).
 * [ ] This is not a package request - If you are looking for packages to be added to the community feed (aka https://chocolatey.org/packages), please see [Package Requests](https://github.com/chocolatey/choco/wiki/PackageTriageProcess#package-request-package-missing).
 * [ ] This is not an issue with the website - If it is an issue with the website (the community feed aka https://chocolatey.org), please submit the issue to the [Chocolatey.org repo](https://github.com/chocolatey/chocolatey.org).
 * [ ] This is not an issue with the GUI - If you have found an issue with the GUI (Chocolatey GUI), please see [the ChocolateyGUI repository](https://github.com/chocolatey/ChocolateyGUI#submitting-issues).
 * [ ] This is not an issue with a missing `SolutionVersion.cs`. Please see [Compiling / Building Source](https://github.com/chocolatey/choco#compiling--building-source).
 
### Feature? 

 * [ ] Read over [Contributing](https://github.com/chocolatey/choco/blob/master/CONTRIBUTING.md) as well.


## Bug?

 * We'll need debug and verbose output, so please run and capture the log with `-dv` or `--debug --verbose`. You can submit that with the issue or create a gist and link it.
 * **Please note** that the debug/verbose output for some commands may have sensitive data (passwords or apiKeys) related to Chocolatey, so please remove those if they are there prior to submitting the issue.
 * choco.exe logs to a file in `$env:ChocolateyInstall\log\`. You can grab the specific log output from there so you don't have to capture or redirect screen output. Please limit the amount included to just the command run (the log is appended to with every command).
 * Please save the log output in a [gist](https://gist.github.com) (save the file as `log.sh`) and link to the gist from the issue. Feel free to create it as secret so it doesn't fill up against your public gists. Anyone with a direct link can still get to secret gists. If you accidentally include secret information in your gist, please delete it and create a new one (gist history can be seen by anyone) and update the link in the ticket (issue history is not retained except by email - deleting the gist ensures that no one can get to it). Using gists this way also keeps accidental secrets from being shared in the ticket in the first place as well.
 * We'll need the entire log output from the run, so please don't limit it down to areas you feel are relevant. You may miss some important details we'll need to know. This will help expedite issue triage.
 * It's helpful to include the version of choco, the version of the OS, and the version of PowerShell (Posh) - the debug script should capture all of those pieces of information.
 * Include screenshots and/or animated gifs whenever possible, they help show us exactly what the problem is.

## Template for Bug Reports

Everything below this point stays with the actual issue request (everything above can be deleted):
 
### Expected Behavior

### Actual Behavior

### Steps to Reproduce the Behavior

