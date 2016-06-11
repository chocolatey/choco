﻿# Push Command (choco push)

Chocolatey will attempt to push a compiled nupkg to a package feed. 
 Some may prefer to use `cpush` as a shortcut for `choco push`.

**NOTE:** 100% compatible with older chocolatey client (0.9.8.32 and below)
 with options and switches. Default push location is deprecated and 
 will be removed by v1. In most cases you can still pass options and 
 switches with one dash (`-`). For more details, see 
 [[how to pass arguments|CommandsReference#how-to-pass-options--switches]] (`choco -?`).

A feed can be a local folder, a file share, the [community feed](https://chocolatey.org/packages) 
 (https://chocolatey.org/), or a custom/private feed. For web
 feeds, it has a requirement that it implements the proper OData
 endpoints required for NuGet packages.

## Usage

    choco push [<path to nupkg>] [<options/switches>]
    cpush [<path to nupkg>] [<options/switches>]

**NOTE:** If there is more than one nupkg file in the folder, the command 
 will require specifying the path to the file.

## Examples

    choco push --source https://chocolatey.org/
    choco push --source "'https://chocolatey.org/'" -t 500
    choco push --source "'https://chocolatey.org/'" -k="'123-123123-123'"

## Troubleshooting

To use this command, you must have your API key saved for the community
 feed (chocolatey.org) or the source you want to push to. Or you can 
 explicitly pass the apikey to the command. See [[`apikey`|Commandsapikey]] command help 
 for instructions on saving your key:

    choco apikey -?

A common error is `Failed to process request. 'The specified API key 
 does not provide the authority to push packages.' The remote server 
 returned an error: (403) Forbidden..` This means the package already 
 exists with a different user (API key). The package could be unlisted. 
 You can verify by going to https://chocolatey.org/packages/packageName. 
 Please contact the administrators of https://chocolatey.org/ if you see this 
 and you don't see a good reason for it.

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
     Source - The source we are pushing the package to. Use 
       https://chocolatey.org/ to push to [community feed](https://chocolatey.org/packages).

 -k, --key, --apikey, --api-key=VALUE
     ApiKey - The api key for the source. If not specified (and not local 
       file source), does a lookup. If not specified and one is not found for 
       an https source, push will fail.

 -t=VALUE
     Timeout (in seconds) - The time to allow a package push to occur before 
       timing out. Defaults to execution timeout 2700.

~~~

[[Command Reference|CommandsReference]]


***NOTE:*** This documentation has been automatically generated from `choco push -h`. 

