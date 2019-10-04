# Install-ChocolateyPath

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyPath.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

**NOTE:** Administrative Access Required when `-PathType 'Machine'.`

This puts a directory to the PATH environment variable.

## Syntax

~~~powershell
Install-ChocolateyPath `
  -PathToInstall <String> `
  [-PathType {Process | User | Machine}] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Looks at both PATH environment variables to ensure a path variable
correctly shows up on the right PATH.

## Notes

This command will assert UAC/Admin privileges on the machine if
`-PathType 'Machine'`.

This is used when the application/tool is not being linked by Chocolatey
(not in the lib folder).

## Aliases

None

## Examples

 **EXAMPLE 1**

~~~powershell
Install-ChocolateyPath -PathToInstall "$($env:SystemDrive)\tools\gittfs"

~~~

**EXAMPLE 2**

~~~powershell
Install-ChocolateyPath "$($env:SystemDrive)\Program Files\MySQL\MySQL Server 5.5\bin" -PathType 'Machine'

~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -PathToInstall &lt;String&gt;
The full path to a location to add / ensure is in the PATH.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -PathType
Which PATH to add it to. If specifying `Machine`, this requires admin
privileges to run correctly.


Valid options: Process, User, Machine

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 2
Default Value          | User
Accept Pipeline Input? | false
 
###  -IgnoredArguments [&lt;Object[]&gt;]
Allows splatting with arguments that do not apply. Do not use directly.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | 
Accept Pipeline Input? | false
 
### &lt;CommonParameters&gt;

This cmdlet supports the common parameters: -Verbose, -Debug, -ErrorAction, -ErrorVariable, -OutBuffer, and -OutVariable. For more information, see `about_CommonParameters` http://go.microsoft.com/fwlink/p/?LinkID=113216 .


## Links

 * [[Install-ChocolateyEnvironmentVariable|HelpersInstallChocolateyEnvironmentVariable]]
 * [[Get-EnvironmentVariable|HelpersGetEnvironmentVariable]]
 * [[Set-EnvironmentVariable|HelpersSetEnvironmentVariable]]
 * [[Get-ToolsLocation|HelpersGetToolsLocation]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyPath -Full`.

View the source for [Install-ChocolateyPath](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyPath.ps1)
