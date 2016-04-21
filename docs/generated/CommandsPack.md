# Pack Command (choco pack)

Chocolatey will attempt to package a nuspec into a compiled nupkg. Some
 may prefer to use `cpack` as a shortcut for `choco pack`.

**NOTE:** 100% compatible with older chocolatey client (0.9.8.32 and below)
 with options and switches. In most cases you can still pass options 
 and switches with one dash (`-`). For more details, see 
 [[how to pass arguments|CommandsReference#how-to-pass-options--switches]] (`choco -?`).

**NOTE:** `cpack` has been deprecated as it has a name collision with CMake. Please 
 use `choco pack` instead. The shortcut will be removed in v1.


## Usage

    choco pack [<path to nuspec>] [<options/switches>]
    cpack [<path to nuspec>] [<options/switches>] (DEPRECATED)

## Examples

    choco pack
    choco pack --version 1.2.3
    choco pack path/to/nuspec


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

     --execution-timeout=VALUE
     CommandExecutionTimeoutSeconds - Override the default execution timeout 
       in the configuration of 2700 seconds.

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

     --version=VALUE
     Version - The version you would like to insert into the package.

~~~

[[Command Reference|CommandsReference]]


***NOTE:*** This documentation has been automatically generated from `choco pack -h`. 

