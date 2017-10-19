# Get-WebFileName

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-WebFileName.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Gets the original file name from a url. Used by Get-WebFile to determine
the original file name for a file.

## Syntax

~~~powershell
Get-WebFileName `
  [-Url <String>] `
  -DefaultName <String> `
  [-UserAgent <String>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Uses several techniques to determine the original file name of the file
based on the url for the file.

## Notes

Available in 0.9.10+.
Falls back to DefaultName when the name cannot be determined.

Chocolatey works best when the packages contain the software it is
managing and doesn't require downloads. However most software in the
Windows world requires redistribution rights and when sharing packages
publicly (like on the [community feed](https://chocolatey.org/packages)), maintainers may not have those
aforementioned rights. Chocolatey understands how to work with that,
hence this function. You are not subject to this limitation with
internal packages.

## Aliases

None

## Examples

 **EXAMPLE 1**

~~~powershell
Get-WebFileName -Url $url -DefaultName $originalFileName

~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -Url [&lt;String&gt;]
This is the url to a file that will be possibly downloaded.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -DefaultName &lt;String&gt;
The name of the file to use when not able to determine the file name
from the url response.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -UserAgent [&lt;String&gt;]
The user agent to use as part of the request. Defaults to 'chocolatey
command line'.

Property               | Value
---------------------- | -----------------------
Aliases                | 
Required?              | false
Position?              | named
Default Value          | chocolatey command line
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

 * [[Get-WebHeaders|HelpersGetWebHeaders]]
 * [[Get-ChocolateyWebFile|HelpersGetChocolateyWebFile]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Get-WebFileName -Full`.

View the source for [Get-WebFileName](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-WebFileName.ps1)
