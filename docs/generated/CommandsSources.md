﻿# Source Command (choco sources)

Chocolatey will allow you to interact with sources.

**NOTE:** Mostly compatible with older chocolatey client (0.9.8.x and
 below) with options and switches. When enabling, disabling or removing
 a source, use `-name` in front of the option now. In most cases you
 can still pass options and switches with one dash (`-`). For more
 details, see [[how to pass arguments|CommandsReference#how-to-pass-options--switches]] (`choco -?`).

## Usage

    choco source [list]|add|remove|disable|enable [<options/switches>]
    choco sources [list]|add|remove|disable|enable [<options/switches>]

## Examples

    choco source
    choco source list
    choco source add -n=bob -s"https://somewhere/out/there/api/v2/"
    choco source add -n=bob -s"'https://somewhere/out/there/api/v2/'" -cert=\Users\bob\bob.pfx
    choco source add -n=bob -s"'https://somewhere/out/there/api/v2/'" -u=bob -p=12345
    choco source disable -n=bob
    choco source enable -n=bob
    choco source remove -n=bob

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

 -n, --name=VALUE
     Name - the name of the source. Required with some actions. Defaults to 
       empty.

 -s, --source=VALUE
     Source - The source. Defaults to empty.

 -u, --user=VALUE
     User - used with authenticated feeds. Defaults to empty.

 -p, --password=VALUE
     Password - the user's password to the source. Encrypted in chocolate-
       y.config file.

     --cert=VALUE
     Client certificate - PFX pathname for an x509 authenticated feeds. 
       Defaults to empty. Available in 0.9.10+.

     --cp, --certpassword=VALUE
     Certificate Password - the client certificate's password to the source. 
       Defaults to empty. Available in 0.9.10+.

     --priority=VALUE
     Priority - The priority order of this source as compared to other 
       sources, lower is better. Defaults to 0 (no priority). All priorities 
       above 0 will be evaluated first, then zero-based values will be 
       evaluated in config file order. Available in 0.9.9.9+.

~~~

[[Command Reference|CommandsReference]]


***NOTE:*** This documentation has been automatically generated from `choco sources -h`. 

