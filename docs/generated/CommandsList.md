﻿# List/Search Command (choco list)

Chocolatey will perform a search for a package local or remote. Some 
 may prefer to use [[`clist`|Commandslist]] as a shortcut for [[`choco list`|Commandslist]].

**NOTE:** 100% compatible with older Chocolatey client (0.9.8.x and below) 
 with options and switches. In most cases you can still pass options 
 and switches  with one dash (`-`). For more details, see 
 [[how to pass arguments|CommandsReference#how-to-pass-options--switches]] (`choco -?`).

## Usage

    choco search <filter> [<options/switches>]
    choco list <filter> [<options/switches>]
    clist <filter> [<options/switches>]

## Examples

    choco list --local-only
    choco list -li
    choco list -lai
    choco list --page=0 --page-size=25
    choco search git
    choco search git -s "'https://somewhere/out/there'"
    choco search bob -s "'https://somewhere/protected'" -u user -p pass

## See It In Action

![choco search](https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_search.gif)


## Alternative Sources
 
Available in 0.9.10+.

### WebPI
This specifies the source is Web PI (Web Platform Installer) and that 
 we are searching for a WebPI product, such as IISExpress. If you do 
 not have the Web PI command line installed, it will install that first 
 and then perform the search requested.
 e.g. `choco list --source webpi`

### Windows Features
This specifies that the source is a Windows Feature and we should 
 install via the Deployment Image Servicing and Management tool (DISM) 
 on the local machine.
 e.g. `choco list --source windowsfeatures`

## Options and Switches

**NOTE:** Options and switches apply to all items passed, so if you are
 running a command like install that allows installing multiple
 packages, and you use `--version=1.0.0`, it is going to look for and
 try to install version 1.0.0 of every package passed. So please split
 out multiple package calls when wanting to pass specific options.

Includes [[default options/switches|CommandsReference#default-options-and-switches]] (included below for completeness).

~~~

 -?, --help, -h
     Prints out the help menu.

 -d, --debug
     Debug - Run in Debug Mode.

 -v, --verbose
     Verbose - See verbose messaging.

     --acceptlicense, --accept-license
     AcceptLicense - Accept license dialogs automatically.

 -y, --yes, --confirm
     Confirm all prompts - Chooses affirmative answer instead of prompting. 
       Implies --accept-license

 -f, --force
     Force - force the behavior

     --noop, --whatif, --what-if
     NoOp - Don't actually do anything.

 -r, --limitoutput, --limit-output
     LimitOutput - Limit the output to essential information

     --timeout, --execution-timeout=VALUE
     CommandExecutionTimeout (in seconds) - The time to allow a command to 
       finish before timing out. Overrides the default execution timeout in the 
       configuration of 2700 seconds.

 -c, --cache, --cachelocation, --cache-location=VALUE
     CacheLocation - Location for download cache, defaults to %TEMP% or value 
       in chocolatey.config file.

     --allowunofficial, --allow-unofficial, --allowunofficialbuild, --allow-unofficial-build
     AllowUnofficialBuild - When not using the official build you must set 
       this flag for choco to continue.

     --failstderr, --failonstderr, --fail-on-stderr, --fail-on-standard-error, --fail-on-error-output
     FailOnStandardError - Fail on standard error output (stderr), typically 
       received when running external commands during install providers. This 
       overrides the feature failOnStandardError.

     --use-system-powershell
     UseSystemPowerShell - Execute PowerShell using an external process 
       instead of the built-in PowerShell host. Available in 0.9.10+.

 -s, --source=VALUE
     Source - Source location for install. Can include special 'webpi'. 
       Defaults to sources.

 -l, --lo, --localonly, --local-only
     LocalOnly - Only search against local machine items.

     --pre, --prerelease
     Prerelease - Include Prereleases? Defaults to false.

 -i, --includeprograms, --include-programs
     IncludePrograms - Used in conjunction with LocalOnly, filters out apps 
       chocolatey has listed as packages and includes those in the list. 
       Defaults to false.

 -a, --all, --allversions, --all-versions
     AllVersions - include results from all versions.

 -u, --user=VALUE
     User - used with authenticated feeds. Defaults to empty.

 -p, --password=VALUE
     Password - the user's password to the source. Defaults to empty.

     --cert=VALUE
     Client certificate - PFX pathname for an x509 authenticated feeds. 
       Defaults to empty. Available in 0.9.10+.

     --cp, --certpassword=VALUE
     Certificate Password - the client certificate's password to the source. 
       Defaults to empty. Available in 0.9.10+.

     --page=VALUE
     Page - the 'page' of results to return. Defaults to return all results. 
       Available in 0.9.10+.

     --page-size=VALUE
     Page Size - the amount of package results to return per page. Defaults 
       to 25. Available in 0.9.10+.

 -e, --exact
     Exact - Only return packages with this exact name. Available in 0.9.10+.

     --by-id-only
     ByIdOnly - Only return packages where the id contains the search filter. 
       Available in 0.9.10+.

     --id-starts-with
     IdStartsWith - Only return packages where the id starts with the search 
       filter. Available in 0.9.10+.

     --order-by-popularity
     OrderByPopularity - Sort by package results by popularity. Available in 
       0.9.10+.

     --approved-only
     ApprovedOnly - Only return approved packages - this option will filter 
       out results not from the [community repository](https://chocolatey.org/packages). Available in 0.9.10+.

     --download-cache, --download-cache-only
     DownloadCacheAvailable - Only return packages that have a download cache 
       available - this option will filter out results not from the community 
       repository. Available in 0.9.10+.

     --not-broken
     NotBroken - Only return packages that are not failing testing - this 
       option only filters out failing results from the [community feed](https://chocolatey.org/packages). It will 
       not filter against other sources. Available in 0.9.10+.

     --detail, --detailed
     Detailed - Alias for verbose. Available in 0.9.10+.

~~~

[[Command Reference|CommandsReference]]


***NOTE:*** This documentation has been automatically generated from `choco list -h`. 

