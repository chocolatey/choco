# Start-ChocolateyProcessAsAdmin

**NOTE:** Administrative Access Required.

Runs a process with administrative privileges. If `-ExeToRun` is not
specified, it is run with PowerShell.

## Syntax

~~~powershell
Start-ChocolateyProcessAsAdmin `
  -Statements <String> `
  [-ExeToRun <String>] `
  [-Minimized] `
  [-NoSleep] `
  [-ValidExitCodes <Object>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~


## Notes

This command will assert UAC/Admin privileges on the machine.

## Aliases

None

## Inputs

None

## Outputs

None

## Parameters

###  -Statements &lt;String&gt;
Arguments to pass to `ExeToRun` or the PowerShell script block to be
run.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
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

## Links

 * [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]
 * [[Install-ChocolateyInstallPackage|HelpersInstallChocolateyInstallPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Start-ChocolateyProcessAsAdmin -Full`.
