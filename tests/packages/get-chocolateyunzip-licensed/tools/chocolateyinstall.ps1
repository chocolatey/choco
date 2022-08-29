$ErrorActionPreference = 'Stop'

$toolsDir     = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$fileLocation = Join-Path $toolsDir 'cmake-3.21.2-windows-i386.zip'
$fileLocation64 = Join-Path $toolsDir 'cmake-3.21.2-windows-x86_64.zip'
if (Get-ProcessorBits 64) {
$forceX86 = $env:chocolateyForceX86
  if ($forceX86 -eq 'true') {
    Write-Debug "User specified '-x86' so forcing 32-bit"
  } else {
    $fileLocation = $fileLocation64
  }
}

#Based on Custom
$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  softwareName  = 'get-chocolateyunzip-licensed*'
  file          = $fileLocation
  fileType      = 'zip'
  silentArgs    = ""
  #OTHERS
  # Uncomment matching EXE type (sorted by most to least common)
  #$silentArgs = '/S'           # NSIS
  #silentArgs   = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-' # Inno Setup
  #silentArgs   = '/s'           # InstallShield
  #silentArgs   = '/s /v"/qn"'   # InstallShield with MSI
  #silentArgs   = '/s'           # Wise InstallMaster
  #silentArgs   = '-s'           # Squirrel
  #silentArgs   = '-q'           # Install4j
  #silentArgs   = '-s'           # Ghost
  # Note that some installers, in addition to the silentArgs above, may also need assistance of AHK to achieve silence.
  #silentArgs   = ''             # none; make silent with input macro script like AutoHotKey (AHK)
                                 #       https://chocolatey.org/packages/autohotkey.portable
  
  validExitCodes= @(0)
  url           = ""
  checksum      = '9374249E8CA5CFE899F6A8DC95252E79242290E452B3CE12A88449560143B6E9'
  checksumType  = 'sha256'
  url64bit      = ""
  checksum64    = '213A4E6485B711CB0A48CBD97B10DFE161A46BFE37B8F3205F47E99FFEC434D2'
  checksumType64= 'sha256'
  destination   = $toolsDir
}

Get-ChocolateyUnzipCmdlet @packageArgs

