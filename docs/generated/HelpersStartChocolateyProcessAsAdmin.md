# Start-ChocolateyProcessAsAdmin

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Start-ChocolateyProcessAsAdmin.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

**NOTE:** Administrative Access Required.

Runs a process with administrative privileges. If `-ExeToRun` is not
specified, it is run with PowerShell.

## Syntax

~~~powershell
Start-ChocolateyProcessAsAdmin `
  [-Statements <String[]>] `
  [-ExeToRun <String>] `
  [-Elevated] `
  [-Minimized] `
  [-NoSleep] `
  [-ValidExitCodes <Object>] `
  [-WorkingDirectory <String>] `
  [-SensitiveStatements <String>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~


## Notes

This command will assert UAC/Admin privileges on the machine.

Starting in 0.9.10, will automatically call Set-PowerShellExitCode to
set the package exit code in the following ways:

- 4 if the binary turns out to be a text file.
- The same exit code returned from the process that is run. If a 3010 is returned, it will set 3010 for the package.

Aliases `Start-ChocolateyProcess` and `Invoke-ChocolateyProcess`
available in 0.10.2+.

## Aliases

`Invoke-ChocolateyProcess`
`Start-ChocolateyProcess`


## Examples

 **EXAMPLE 1**

~~~powershell
Start-ChocolateyProcessAsAdmin -Statements "$msiArgs" -ExeToRun 'msiexec'

~~~

**EXAMPLE 2**

~~~powershell
Start-ChocolateyProcessAsAdmin -Statements "$silentArgs" -ExeToRun $file

~~~

**EXAMPLE 3**

~~~powershell
Start-ChocolateyProcessAsAdmin -Statements "$silentArgs" -ExeToRun $file -ValidExitCodes @(0,21)

~~~

**EXAMPLE 4**

~~~powershell

# Run PowerShell statements
$psFile = Join-Path "$(Split-Path -parent $MyInvocation.MyCommand.Definition)" 'someInstall.ps1'
Start-ChocolateyProcessAsAdmin "& `'$psFile`'"
~~~

**EXAMPLE 5**

~~~powershell
# This also works for cmd and is required if you have any spaces in the paths within your command
$appPath = "$env:ProgramFiles\myapp"
$cmdBatch = "/c `"$appPath\bin\installmyappservice.bat`""
Start-ChocolateyProcessAsAdmin $cmdBatch cmd
# or more explicitly
Start-ChocolateyProcessAsAdmin -Statements $cmdBatch -ExeToRun "cmd.exe"
~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -Statements [&lt;String[]&gt;]
Arguments to pass to `ExeToRun` or the PowerShell script block to be
run.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -ExeToRun [&lt;String&gt;]
The executable/application/installer to run. Defaults to `'powershell'`.

Property               | Value
---------------------- | ----------
Aliases                | 
Required?              | false
Position?              | 2
Default Value          | powershell
Accept Pipeline Input? | false
 
###  -Elevated
Indicate whether the process should run elevated.

Available in 0.10.2+.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | True
Accept Pipeline Input? | false
 
###  -Minimized
Switch indicating if a Windows pops up (if not called with a silent
argument) that it should be minimized.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | False
Accept Pipeline Input? | false
 
###  -NoSleep
Used only when calling PowerShell - indicates the window that is opened
should return instantly when it is complete.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | False
Accept Pipeline Input? | false
 
###  -ValidExitCodes [&lt;Object&gt;]
Array of exit codes indicating success. Defaults to `@(0)`.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | @(0)
Accept Pipeline Input? | false
 
###  -WorkingDirectory [&lt;String&gt;]
The working directory for the running process. Defaults to
`Get-Location`.

Available in 0.10.1+.

Property               | Value
---------------------- | ---------------
Aliases                | 
Required?              | false
Position?              | named
Default Value          | $(Get-Location)
Accept Pipeline Input? | false
 
###  -SensitiveStatements [&lt;String&gt;]
Arguments to pass to  `ExeToRun` that are not logged.

Note that only [licensed versions](https://chocolatey.org/compare) of Chocolatey provide a way to pass
those values completely through without having them in the install
script or on the system in some way.

Available in 0.10.1+.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | 
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

 * [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]
 * [[Install-ChocolateyInstallPackage|HelpersInstallChocolateyInstallPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Start-ChocolateyProcessAsAdmin -Full`.

View the source for [Start-ChocolateyProcessAsAdmin](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Start-ChocolateyProcessAsAdmin.ps1)
