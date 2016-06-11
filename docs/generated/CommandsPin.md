# Pin Command (choco pin)

Pin a package to suppress upgrades. 

This is especially helpful when running [[`choco upgrade`|Commandsupgrade]] for all 
 packages, as it will automatically skip those packages. Another 
 alternative is `choco upgrade --except="pkg1,pk2"`.

## Usage

    choco pin [list]|add|remove [<options/switches>]

## Examples

    choco pin   
    choco pin list  
    choco pin add -n=git
    choco pin add -n=git --version 1.2.3
    choco pin remove --name git


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
     Name - the name of the package. Required with some actions. Defaults to 
       empty.

     --version=VALUE
     Version - Used when multiple versions of a package are installed.  
       Defaults to empty.

~~~

[[Command Reference|CommandsReference]]


***NOTE:*** This documentation has been automatically generated from `choco pin -h`. 

