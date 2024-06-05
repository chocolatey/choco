# This package purposely uses files that don't exists.
# As such, we need to continue on error.
$ErrorActionPreference = 'SilentlyContinue'
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$fileLocation = "$toolsDir\ConsoleApp1"
$exeArgs = @{
    packageName   = $env:ChocolateyPackageName
    silentArgs    = "/exe "
    fileType = 'exe'
    file = "$fileLocation.exe"
}
$msiArgs = @{
    packageName   = $env:ChocolateyPackageName
    silentArgs    = "/qn "
    # msiexec will exit with 1619 when the file doesn't exist.
    validExitCodes = @(1619)
    fileType = 'msi'
    file = "$fileLocation.msi"
}
$msuArgs = @{
    packageName   = $env:ChocolateyPackageName
    silentArgs    = "/quiet "
    # wusa will exit with 2 when the file doesn't exist.
    validExitCodes = @(2)
    fileType = 'msu'
    file = "$fileLocation.msu"
}

# Since the exe doesn't exist, Install-ChocolateyInstallPackage is expected to
# throw an exception. We don't care if it throws, we care that it outputs the
# correct parameters.
try {
    Install-ChocolateyInstallPackage @exeArgs
} catch {}
Install-ChocolateyInstallPackage @msiArgs
Install-ChocolateyInstallPackage @msuArgs

# Set the chocolateyInstallArgument environment variable to mimic Chocolatey
# setting this variable.
$env:chocolateyInstallArguments = '/norestart '
try {
    Install-ChocolateyInstallPackage @exeArgs
} catch {}
Install-ChocolateyInstallPackage @msiArgs
Install-ChocolateyInstallPackage @msuArgs
