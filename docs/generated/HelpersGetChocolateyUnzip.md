# Get-ChocolateyUnzip

Unzips an archive file and returns the location for further processing.

## Syntax

~~~powershell
Get-ChocolateyUnzip `
  -FileFullPath <String> `
  -Destination <String> `
  [-SpecificFolder <String>] `
  [-PackageName <String>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

This unzips files using the 7-zip standalone command line tool 7za.exe.
Supported archive formats are: 7z, lzma, cab, zip, gzip, bzip2, and tar.

## Notes

If extraction fails, an exception is thrown.

If you are embedding files into a package, ensure that you have the
rights to redistribute those files if you are sharing this package
publicly (like on the [community feed](https://chocolatey.org/packages)). Otherwise, please use
Install-ChocolateyZipPackage to download those resources from their
official distribution points.

## Aliases

None

## Examples

 **EXAMPLE 1**

~~~powershell

# Path to the folder where the script is executing
$toolsDir = (Split-Path -parent $MyInvocation.MyCommand.Definition)
Get-ChocolateyUnzip -FileFullPath "c:\someFile.zip" -Destination $toolsDir
~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -FileFullPath &lt;String&gt;
This is the full path to the zip file. If embedding it in the package
next to the install script, the path will be like
`"$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\\file.zip"`

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -Destination &lt;String&gt;
This is a directory where you would like the unzipped files to end up.
If it does not exist, it will be created.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -SpecificFolder [&lt;String&gt;]
OPTIONAL - This is a specific directory within zip file to extract.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | 
Accept Pipeline Input? | false
 
###  -PackageName [&lt;String&gt;]
OPTIONAL - This will faciliate logging unzip activity for subsequent
uninstalls

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 4
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

 * [[Install-ChocolateyZipPackage|HelpersInstallChocolateyZipPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Get-ChocolateyUnzip -Full`.
