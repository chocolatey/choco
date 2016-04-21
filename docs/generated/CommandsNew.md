# New Command (choco new)

Chocolatey will generate package specification files for a new package.

## Usage

    choco new <name> [<options/switches>] [<property=value> <propertyN=valueN>]

Possible properties to pass:
    packageversion
    maintainername
    maintainerrepo
    installertype
    url
    url64
    silentargs

**NOTE:** Starting in 0.9.10, you can pass arbitrary property value pairs 
 through to templates. This really unlocks your ability to create 
 packages automatically!

**NOTE:** [Chocolatey for Business](https://bit.ly/choco_pro_business) can create complete packages by just 
 pointing the new command to native installers! 

## Examples

    choco new bob
    choco new bob -a --version 1.2.0 maintainername="'This guy'"
    choco new bob silentargs="'/S'" url="'https://somewhere/out/there.msi'"
    choco new bob --outputdirectory Packages


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

 -a, --auto, --automaticpackage
     AutomaticPackage - Generate automatic package instead of normal. 
       Defaults to false

 -t, --template, --template-name=VALUE
     TemplateName - Use a named template in 
       C:\ProgramData\chocolatey\templates\templatename instead of built-in 
       template. Available in 0.9.9.9+. Manage templates as packages in 0.9.10+.

     --name=VALUE
     Name [Required]- the name of the package. Can be passed as first 
       parameter without "--name=".

     --version=VALUE
     Version - the version of the package. Can also be passed as the property 
       PackageVersion=somevalue

     --maintainer=VALUE
     Maintainer - the name of the maintainer. Can also be passed as the 
       property MaintainerName=somevalue

     --outputdirectory=VALUE
     OutputDirectory - Specifies the directory for the created Chocolatey 
       package file. If not specified, uses the current directory. Available in 
       0.9.10+.

~~~

[[Command Reference|CommandsReference]]


***NOTE:*** This documentation has been automatically generated from `choco new -h`. 

