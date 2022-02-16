$ErrorActionPreference = 'Stop';

[[AutomaticPackageNotesInstaller]]
$packageName= '[[PackageName]]'
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
#$fileLocation = Join-Path $toolsDir 'NAME_OF_EMBEDDED_INSTALLER_FILE'

$packageArgs = @{
  packageName   = $packageName
  unzipLocation = $toolsDir
  fileType      = 'MSI'
  url           = '[[Url]]' # download url, HTTPS preferred
  url64bit      = '[[Url64]]' # 64bit URL here (HTTPS preferred) or remove - if installer contains both (very rare), use $url
  #file         = $fileLocation

  softwareName  = '[[PackageName]]*' #part or all of the Display Name as you see it in Programs and Features. It should be enough to be unique

  checksum      = '[[Checksum]]'
  checksumType  = 'sha256' #default is md5, can also be sha1, sha256 or sha512
  checksum64    = '[[Checksum64]]'
  checksumType64= 'sha256'

  silentArgs    = "[[SilentArgs]]" # ALLUSERS=1 DISABLEDESKTOPSHORTCUT=1 ADDDESKTOPICON=0 ADDSTARTMENU=0
  validExitCodes= @(0, 3010, 1641)
}

#https://chocolatey.org/docs/helpers-install-chocolatey-package
Install-ChocolateyPackage @packageArgs
## If you are making your own internal packages (organizations), you can embed the installer or
## put on internal file share and use the following instead (you'll need to add $file to the above)
# https://chocolatey.org/docs/helpers-install-chocolatey-install-package
#Install-ChocolateyInstallPackage @packageArgs
