# ApiKey Command (choco apiKey)

This lists api keys that are set or sets an api key for a particular   
 source so it doesn't need to be specified every time.

Anything that doesn't contain source and key will list api keys.

## Usage

    choco apikey [<options/switches>]
    choco setapikey [<options/switches>]

## Examples

    choco apikey
    choco apikey -s"https://somewhere/out/there"
    choco apikey -s"https://somewhere/out/there/" -k="value"
    choco apikey -s"https://chocolatey.org/" -k="123-123123-123"

## Connecting to Chocolatey.org

In order to save your API key for https://chocolatey.org/, 
 log in (or register, confirm and then log in) to
 https://chocolatey.org/, go to https://chocolatey.org/account, 
 copy the API Key, and then use it in the following command:

    choco apikey -k <your key here> -s https://chocolatey.org/


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
     Debug - Show debug messaging.

 -v, --verbose
     Verbose - Show verbose messaging.

     --acceptlicense, --accept-license
     AcceptLicense - Accept license dialogs automatically. Reserved for 
       future use.

 -y, --yes, --confirm
     Confirm all prompts - Chooses affirmative answer instead of prompting. 
       Implies --accept-license

 -f, --force
     Force - force the behavior. Do not use force during normal operation - 
       it subverts some of the smart behavior for commands.

     --noop, --whatif, --what-if
     NoOp / WhatIf - Don't actually do anything.

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
       instead of the built-in PowerShell host. Should only be used when 
       internal host is failing. Available in 0.9.10+.

 -s, --source=VALUE
     Source [REQUIRED] - The source location for the key

 -k, --key, --apikey, --api-key=VALUE
     ApiKey - The api key for the source.

~~~

[[Command Reference|CommandsReference]]


***NOTE:*** This documentation has been automatically generated from `choco apiKey -h`. 

