Chocolatey (like yum or apt-get, but for Windows)
=======
![Chocolatey Logo](https://github.com/chocolatey/chocolatey/raw/master/docs/logo/chocolateyicon.gif "Chocolatey")

## License / Credits
Apache 2.0 - see [LICENSE](https://github.com/chocolatey/choco/blob/master/LICENSE) and [NOTICE](https://github.com/chocolatey/choco/blob/master/NOTICE) files.

## Documentation
Please see the [wiki](https://github.com/chocolatey/chocolatey/wiki)

## Requirements
* .NET Framework 4.0
* PowerShell 2.0+

## Submitting Issues

If you have found an issue with the client (choco.exe), this is the place to submit. If it is an issue with the website, please submit the issue to the [Chocolatey.org repo](https://github.com/chocolatey/chocolatey.org). If you are having issue with a package and it is the package itself, please submit the issue directly to the package maintainer(s).

Observe the following help for submitting an issue:

 * The issue has to do with choco itself and is not a package or website issue.
 * We'll need debug output, so please run and capture the log with `-d` or `--debug`. You can submit that with the issue or create a gist and link it.
 * **Please note** that the debug output for some commands may have sensitive data (passwords or apiKeys) related to Chocolatey, so please remove those if they are there prior to submitting the issue.
 * It's helpful to include the version of choco, the version of the OS, and the version of PowerShell (Posh), but the debug script should capture all of those pieces of information.
 * Include screenshots and/or animated gifs whenever possible, they help show us exactly what the problem is.

## Contributing

If you would like to contribute code or help squash a bug or two, that's awesome. Please familiarize yourself with [Contributing](https://github.com/chocolatey/choco/blob/master/CONTRIBUTING.md).

## Committers

Committers, you should be very familiar with [Committers](https://github.com/chocolatey/choco/blob/master/COMMITTERS.md).

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
