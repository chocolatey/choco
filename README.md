# Chocolatey - like yum or apt-get, but for Windows
You can just call me choco.

![Chocolatey Logo](https://cdn.rawgit.com/chocolatey/choco/14a627932c78c8baaba6bef5f749ebfa1957d28d/docs/logo/chocolateyicon.gif "Chocolatey")

[![](https://img.shields.io/chocolatey/dt/chocolatey.svg)](https://chocolatey.org/packages/chocolatey) [![](https://img.shields.io/chocolatey/v/chocolatey.svg)](https://chocolatey.org/packages/chocolatey) [![](https://img.shields.io/gratipay/Chocolatey.svg)](https://www.gratipay.com/Chocolatey/) [![Project Stats](https://www.openhub.net/p/chocolatey/widgets/project_thin_badge.gif)](https://www.openhub.net/p/chocolatey)

<!-- TOC -->

- [Build Status](#build-status)
- [Chat Room](#chat-room)
- [Support Chocolatey!](#support-chocolatey)
- [See Chocolatey In Action](#see-chocolatey-in-action)
- [Etiquette Regarding Communication](#etiquette-regarding-communication)
- [Information](#information)
  - [Documentation](#documentation)
  - [Requirements](#requirements)
  - [License / Credits](#license--credits)
- [Submitting Issues](#submitting-issues)
- [Contributing](#contributing)
- [Committers](#committers)
  - [Compiling / Building Source](#compiling--building-source)
    - [Windows](#windows)
    - [Other Platforms](#other-platforms)
      - [Prerequisites:](#prerequisites)
      - [Build Process:](#build-process)
- [Credits](#credits)

<!-- /TOC -->

## Build Status
TeamCity  | AppVeyor | Travis
------------- | ------------- | -------------
[![TeamCity Build Status](https://img.shields.io/teamcity/codebetter/bt429.svg)](http://teamcity.codebetter.com/viewType.html?buildTypeId=bt429) | [![AppVeyor Build Status](https://ci.appveyor.com/api/projects/status/jfxywa3xuwowt20w/branch/master?svg=true)](https://ci.appveyor.com/project/ferventcoder/choco/branch/master) | [![Travis Build Status](https://travis-ci.org/chocolatey/choco.svg?branch=master)](https://travis-ci.org/chocolatey/choco)

## Chat Room
Come join in the conversation about Chocolatey in our Gitter Chat Room.

[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/chocolatey/choco?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Or, you can find us in IRC at #chocolatey on freenode. IRC is not as often checked by committers, so it is recommended you stick to Gitter if you need more timely assistance.

Please make sure you've read over and agree with the [etiquette regarding communication](#etiquette-regarding-communication).

## Support Chocolatey!
 * Purchase [Chocolatey Pro / Chocolatey for Business](https://chocolatey.org/pricing#compare)

## See Chocolatey In Action
Chocolatey FOSS install showing tab completion and `refreshenv` (a way to update environment variables without restarting your shell):

![install](https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_install.gif "Wat? Tab completion and updating environment variables!")

[Chocolatey Pro](https://chocolatey.org/compare) showing private CDN download cache and virus scan protection:

![install w/pro](https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/chocopro_install_stopped.gif "Chocolatey Pro availability now! A great option for individuals looking for that community PLUS option.")

## Etiquette Regarding Communication
If you are an open source user requesting support, please remember that most folks in the Chocolatey community are volunteers that have lives outside of open source and are not paid to ensure things work for you, so please be considerate of others' time when you are asking for things. Many of us have families that also need time as well and only have so much time to give on a daily basis. A little consideration and patience can go a long way. After all, you are using a pretty good tool without cost. It may not be perfect (yet), and we know that.

If you are using a [commercial edition of Chocolatey](https://chocolatey.org/compare#compare), you have different terms! Please see [support](https://chocolatey.org/support).

## Information
 * [Chocolatey Website and Community Package Repository](https://chocolatey.org)
 * [Mailing List](http://groups.google.com/group/chocolatey) / [Release Announcements Only Mailing List](https://groups.google.com/group/chocolatey-announce) / [Build Status Mailing List](http://groups.google.com/group/chocolatey-build-status)
 * [Twitter](https://twitter.com/chocolateynuget) / [Facebook](https://www.facebook.com/ChocolateySoftware) / [Github](https://github.com/chocolatey)
 * [Blog](https://chocolatey.org/blog) / [Newsletter](https://chocolatey.us8.list-manage1.com/subscribe?u=86a6d80146a0da7f2223712e4&id=73b018498d)
 * [Documentation](https://chocolatey.org/docs) / [Support](https://chocolatey.org/support)

### Documentation
Please see the [docs](https://chocolatey.org/docs)

Give `choco.exe -?` a shot (or `choco.exe -h`). For specific commands, add the command and then the help switch e.g. `choco.exe install -h`.

### Requirements
* .NET Framework 4.0+
* PowerShell 2.0+
* Windows Server 2003+ / Windows 7+

### License / Credits
Apache 2.0 - see [LICENSE](https://github.com/chocolatey/choco/blob/master/LICENSE) and [NOTICE](https://github.com/chocolatey/choco/blob/master/NOTICE) files.

## Submitting Issues
![submitting issues](https://cloud.githubusercontent.com/assets/63502/12534554/6ea7cc04-c224-11e5-82ad-3805d0b5c724.png)

 * If you are having issue with a package, please see [Request Package Fixes or Updates / Become a maintainer of an existing package](https://chocolatey.org/docs/package-triage-process).
 * If you are looking for packages to be added to the community feed (aka https://chocolatey.org/packages), please see [Package Requests](https://chocolatey.org/docs/package-triage-process#package-request-package-missing).

 1. Start with [Troubleshooting](https://github.com/chocolatey/choco/wiki/Troubleshooting) and the [FAQ](https://github.com/chocolatey/choco/wiki/ChocolateyFAQs) to see if your question or issue already has an answer.
 1. If not found or resolved, please follow one of the following avenues:
    * If you are a licensed customer, please see [support](https://chocolatey.org/support). You can also log an issue to [Licensed Issues](https://github.com/chocolatey/chocolatey-licensed-issues) and we will submit issues to all other places on your behalf. Another avenue is to use email support to have us submit tickets and other avenues on your behalf (allowing you to maintain privacy).
    * If it is an enhancement request or issue with the website (the community package repository aka [https://chocolatey.org](https://chocolatey.org)), please submit the issue to the [Chocolatey.org repo](https://github.com/chocolatey/chocolatey.org).
    * If you have found an issue with the GUI (Chocolatey GUI) or you want to submit an enhancement, please see [the ChocolateyGUI repository](https://github.com/chocolatey/ChocolateyGUI#submitting-issues).
    * If you have found an issue with the client (choco.exe), you are in the right place. Keep reading below.

Observe the following help for submitting an issue:

Prerequisites:

 * The issue has to do with choco itself and is not a package or website issue.
 * Please check to see if your issue already exists with a quick search of the issues. Start with one relevant term and then add if you get too many results.
 * You are not submitting an "Enhancement". Enhancements should observe [CONTRIBUTING](https://github.com/chocolatey/choco/blob/master/CONTRIBUTING.md) guidelines.
 * You are not submitting a question - questions are better served as [emails](https://groups.google.com/group/chocolatey) or [gitter chat questions](https://gitter.im/chocolatey/choco).
 * Please make sure you've read over and agree with the [etiquette regarding communication](#etiquette-regarding-communication).

Submitting a ticket:

 * We'll need debug and verbose output, so please run and capture the log with `-dv` or `--debug --verbose`. You can submit that with the issue or create a gist and link it.
 * **Please note** that the debug/verbose output for some commands may have sensitive data (passwords or apiKeys) related to Chocolatey, so please remove those if they are there prior to submitting the issue.
 * choco.exe logs to a file in `$env:ChocolateyInstall\log\`. You can grab the specific log output from there so you don't have to capture or redirect screen output. Please limit the amount included to just the command run (the log is appended to with every command).
 * Please save the log output in a [gist](https://gist.github.com) (save the file as `log.sh`) and link to the gist from the issue. Feel free to create it as secret so it doesn't fill up against your public gists. Anyone with a direct link can still get to secret gists. If you accidentally include secret information in your gist, please delete it and create a new one (gist history can be seen by anyone) and update the link in the ticket (issue history is not retained except by email - deleting the gist ensures that no one can get to it). Using gists this way also keeps accidental secrets from being shared in the ticket in the first place as well.
 * We'll need the entire log output from the run, so please don't limit it down to areas you feel are relevant. You may miss some important details we'll need to know. This will help expedite issue triage.
 * It's helpful to include the version of choco, the version of the OS, and the version of PowerShell (Posh) - the debug script should capture all of those pieces of information.
 * Include screenshots and/or animated gifs whenever possible, they help show us exactly what the problem is.

## Contributing
If you would like to contribute code or help squash a bug or two, that's awesome. Please familiarize yourself with [CONTRIBUTING](https://github.com/chocolatey/choco/blob/master/CONTRIBUTING.md).

## Committers
Committers, you should be very familiar with [COMMITTERS](https://github.com/chocolatey/choco/blob/master/COMMITTERS.md).

### Compiling / Building Source
There is a `build.bat`/`build.sh` file that creates a necessary generated file named `SolutionVersion.cs`. It must be run at least once before Visual Studio will build.

#### Windows
Prerequisites:

 * .NET Framework 3.5 (This is a windows feature installation).
 * .NET Framework 4+
 * Visual Studio is helpful for working on source.
 * ReSharper is immensely helpful (and there is a `.sln.DotSettings` file to help with code conventions).

Build Process:

 * Run `build.bat`.

Running the build on Windows should produce an artifact that is tested and ready to be used.

#### Other Platforms
##### Prerequisites:

 * Install and configure Mono 3.12.0 (3.8.0 should also work).
  * [Debian based](http://www.mono-project.com/docs/getting-started/install/linux/#debian-ubuntu-and-derivatives)

```sh
# add the key

sudo apt-key adv --keyserver keyserver.ubuntu.com --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
# add the package repository
echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
# Ubuntu 12.10/12.04 - add this deb as well
echo "deb http://download.mono-project.com/repo/debian wheezy-libtiff-compat main" | sudo tee -a /etc/apt/sources.list.d/mono-xamarin.list

# update package indexes
sudo apt-get update
# install
sudo apt-get install mono-devel -y
```

  * [RPM Based](http://www.mono-project.com/docs/getting-started/install/linux/#centos-fedora-and-derivatives)

```sh
### NOT FULLY TESTED AND WORKING ###
# add the EPEL
sudo yum install epel-release -y
# Add the key
sudo rpm --import "http://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"

# Add the package repository
sudo yum-config-manager --add-repo http://download.mono-project.com/repo/centos/

# update your system
sudo yum update -y

# Install mono-devel
sudo yum install mono-devel -y


```

 * Xamarin Studio is helpful for working on source.
 * Consider adding the following to your `~/.profile` (or other relevant dot source file):

```sh
# mono
# http://www.michaelruck.de/2010/03/solving-pkg-config-and-mono-35-profile.html
# http://cloudgen.wordpress.com/2013/03/06/configure-nant-to-run-under-mono-3-06-beta-for-mac-osx/
export PKG_CONFIG_PATH=/opt/local/lib/pkgconfig:/Library/Frameworks/Mono.framework/Versions/Current/lib/pkgconfig:$PKG_CONFIG_PATH
```

 * Set your permissions correctly:

```sh
chmod +x build.sh
chmod +x zip.sh
```

##### Build Process:

 * Run `./build.sh`.

Running the build on Mono produces an artifact similar to Windows but may have more rough edges. You may get a failure or two in the build script that can be safely ignored.

## Credits
Chocolatey is brought to you by quite a few people and frameworks. See [CREDITS](https://github.com/chocolatey/choco/blob/master/docs/legal/CREDITS.md) (just LEGAL/Credits.md in the zip folder).
