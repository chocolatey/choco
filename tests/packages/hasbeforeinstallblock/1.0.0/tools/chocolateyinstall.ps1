$ErrorActionPreference = 'Stop' # stop on all errors

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  fileType      = 'msi'
  url           = 'https://github.com/chocolatey/ChocolateyGUI/releases/download/0.18.1/ChocolateyGUI.msi'

  softwareName  = 'Chocolatey GUI*'

  checksum      = ''
  checksumType  = 'sha256'

  # MSI
  silentArgs    = "/qn /norestart /l*v `"$($env:TEMP)\$($packageName).$($env:chocolateyPackageVersion).MsiInstall.log`"" # ALLUSERS=1 DISABLEDESKTOPSHORTCUT=1 ADDDESKTOPICON=0 ADDSTARTMENU=0
  validExitCodes= @(0, 3010, 1641)

  beforeInstall = {
    # This is just to notify that the before install script
    # block have been ran
    Write-Host "Running necessary Pre-Install step"
  }
}

$pp = Get-PackageParameters

$algorithms = @{
  "MD5" = "27272275D1851F8E68C02BCE79538E2A"
  "SHA1" = "2D8214F84162069FB809776287DBB5BCAA5AE725"
  "SHA256" = "490DCAC8A2BF52CB55A84686EEB2D23A4B303578AE09A0290D3208AEFBE5B59D"
  "SHA512" = "D8FB6BE60863D145CEE1CF12AEA66C6E5042CE0C04DB6FABF4C2659EEE87C63E6830514945C56B059B76311C02CC6AFD64BC73FF3AF0B4EE993CCA4545BF0FD3"
}

if ($pp.Algorithm) {
  if ($pp.Algorithm -eq $true) {
    # This allows us to test when the checksum type is specified
    # but is set to empty
    $packageArgs["checksumType"] = ""
    $packageArgs["checksum"] = $algorithms["MD5"] # Default is MD5 when type is not specified, or is empty
  }
  else {
    $packageArgs["checksumType"] = $pp.Algorithm
    $packageArgs["checksum"] = $algorithms[$pp.Algorithm]
  }
} else {
  $packageArgs["checksum"] = $algorithms["SHA256"]
}

if ($pp.Checksum) {
  if ($pp.Checksum -eq $true) {
    # This allows us to test when there is an empty checksum
    $packageArgs["checksum"] = ""
  } else {
    $packageArgs["checksum"] = $pp.Checksum
  }
}

Write-Host "Using Algorithm: $($packageArgs["checksumType"])"
Write-Host "Expects checksum: $($packageArgs["checksum"])"

Install-ChocolateyPackage @packageArgs
