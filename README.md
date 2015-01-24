# Chocolatey - like yum or apt-get, but for Windows
You can just call me choco.

![Chocolatey Logo](https://github.com/chocolatey/chocolatey/raw/master/docs/logo/chocolateyicon.gif "Chocolatey")

[![](http://img.shields.io/chocolatey/dt/chocolatey.svg)](https://chocolatey.org/packages/chocolatey) [![](http://img.shields.io/chocolatey/v/chocolatey.svg)](https://chocolatey.org/packages/chocolatey) [![](http://img.shields.io/gittip/Chocolatey.svg)](https://www.gittip.com/Chocolatey/)

[![AppVeyor Build Status](https://ci.appveyor.com/api/projects/status/jfxywa3xuwowt20w/branch/master?svg=true)](https://ci.appveyor.com/project/ferventcoder/choco/branch/master) [![TeamCity Build Status](http://img.shields.io/teamcity/codebetter/bt429.svg)](http://teamcity.codebetter.com/viewType.html?buildTypeId=bt429) [![Travis Build Status](https://travis-ci.org/chocolatey/choco.svg?branch=master)](https://travis-ci.org/chocolatey/choco)

[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/chocolatey/choco?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## Information

 * [Community Feed aka Chocolatey.org](https://chocolatey.org) (if this is down, try the backup at http://chocolatey.apphb.com )
 * [Mailing List/Forum](http://groups.google.com/group/chocolatey)
 * [Twitter](https://twitter.com/chocolateynuget)
 * [Build Status Email List](http://groups.google.com/group/chocolatey-build-status)

## License / Credits
Apache 2.0 - see [LICENSE](https://github.com/chocolatey/choco/blob/master/LICENSE) and [NOTICE](https://github.com/chocolatey/choco/blob/master/NOTICE) files.

## Documentation
Please see the [wiki](https://github.com/chocolatey/chocolatey/wiki) (WILL NEED TO UPDATE TO A NEW LOCAL WIKI with all the fun changes)

Give `choco.exe /?` a shot (or `choco.exe -h`). For specific commands, add the command and then the help switch e.g. `choco.exe install -h`.

## Requirements
* .NET Framework 4.0
* PowerShell 2.0+

## Submitting Issues

If you have found an issue with the client (choco.exe), this is the place to submit. If it is an issue with the website, please submit the issue to the [Chocolatey.org repo](https://github.com/chocolatey/chocolatey.org). If you are having issue with a package and it is the package itself, please submit the issue directly to the package maintainer(s).

Observe the following help for submitting an issue:

Prerequisites:

 * The issue has to do with choco itself and is not a package or website issue.
 * Please check to see if your issue already exists with a quick search of the issues. Start with one relevant term and then add if you get too many results.
 * You are not submitting an Enhancement. Enhancements should observe [CONTRIBUTING](https://github.com/chocolatey/choco/blob/master/CONTRIBUTING.md) guidlines.

Submitting a ticket:

 * We'll need debug and verbose output, so please run and capture the log with `-dv` or `--debug --verbose`. You can submit that with the issue or create a gist and link it.
 * **Please note** that the debug/verbose output for some commands may have sensitive data (passwords or apiKeys) related to Chocolatey, so please remove those if they are there prior to submitting the issue.
 * choco.exe logs to a file in `$env:ChocolateyInstall\log\`. You can grab the specific log output from there so you don't have to capture or redirect screen output. Please limit the amount included to just the command run (the log is appended to with every command).
 * Please save the log output in a [gist](https://gist.github.com) (save the file as `log.sh`) and link to the gist from the issue. Feel free to create it as secret so it doesn't fill up against your public gists. Anyone with a direct link can still get to secret gists. If you accidentally include secret information in your gist, please delete it and create a new one (gist history can be seen by anyone) and update the link in the ticket (issue history is not retained except by email - deleting the gist ensures that no one can get to it). Using gists this way also keeps accidental secrets from being shared in the ticket in the first place as well.
 * We'll need the entire log output from the run, so please don't limit it down to areas you feel are relevant. You may miss some important details we'll need to know. This will help expedite issue triage.
 * It's helpful to include the version of choco, the version of the OS, and the version of PowerShell (Posh), but the debug script should capture all of those pieces of information.
 * Include screenshots and/or animated gifs whenever possible, they help show us exactly what the problem is.

## Contributing

If you would like to contribute code or help squash a bug or two, that's awesome. Please familiarize yourself with [CONTRIBUTING](https://github.com/chocolatey/choco/blob/master/CONTRIBUTING.md).

## Committers

Committers, you should be very familiar with [COMMITTERS](https://github.com/chocolatey/choco/blob/master/COMMITTERS.md).

### Compiling / Building Source

There is a `build.bat`/`build.sh` file that creates a necessary generated file named `SolutionVersion.cs`. It must be run at least once before Visual Studio will build.

#### Windows

Prerequisites:

 * .NET Framework 4+
 * Visual Studio is helpful for working on source.
 * ReSharper is immensely helpful (and there is a `.sln.DotSettings` file to help with code conventions).

Build Process:

 * Run `build.bat`.

Running the build on Windows should produce an artifact that is tested and ready to be used.

#### Other Platforms

On other operating systems, you will need to install and configure Mono first prior to building.

```
$ mono -V
Mono JIT compiler version 3.8.0 ((no/45d0ba1 Tue Aug 26 20:33:43 EDT 2014)
Copyright (C) 2002-2014 Novell, Inc, Xamarin Inc and Contributors. www.mono-project.com
```

Prerequisites:

 * Install and configure Mono 3.8.0 (no guarantees that newer versions will work appropriately).
 * Xamarin Studio is helpful for working on source.
 * Add the following to your `~/.profile` (or other relevant dot source file):

```sh
# mono
# http://www.michaelruck.de/2010/03/solving-pkg-config-and-mono-35-profile.html
# http://cloudgen.wordpress.com/2013/03/06/configure-nant-to-run-under-mono-3-06-beta-for-mac-osx/
export PKG_CONFIG_PATH=/opt/local/lib/pkgconfig:/Library/Frameworks/Mono.framework/Versions/Current/lib/pkgconfig
```

Build Process:

 * Run `./build.sh`.

Running the build on Mono produces an artifact similar to Windows but may have more rough edges. You may get a failure or two in the build script that can be safely ignored.

## Credits

Chocolatey is brought to you by quite a few people and frameworks. See [CREDITS](https://github.com/chocolatey/choco/blob/master/docs/legal/CREDITS.md) (just LEGAL/Credits.md in the zip folder)
