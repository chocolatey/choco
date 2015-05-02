param(
  [alias("ia","installArgs")][string] $installArguments = '',
  [alias("o","override","overrideArguments","notSilent")]
  [switch] $overrideArgs = $false,
  [alias("x86")][switch] $forceX86 = $false,
  [alias("params")][alias("parameters")][alias("pkgParams")][string]$packageParameters = '',
  [string]$packageScript
)

Write-Debug "Running 'ChocolateyScriptRunner' for $($env:packageName) v$($env:packageVersion) with packageScript `'$packageScript`', packageFolder:`'$($env:packageFolder)`', installArguments: `'$installArguments`', packageParameters: `'$packageParameters`',"

## Set the culture to invariant
$currentThread = [System.Threading.Thread]::CurrentThread;
$culture = [System.Globalization.CultureInfo]::InvariantCulture;
$currentThread.CurrentCulture = $culture;
$currentThread.CurrentUICulture = $culture;

$RunNote = "DarkCyan"
$Warning = "Magenta"
$ErrorColor = "Red"
$Note = "Green"

$version = $env:packageVersion
$packageName = $env:packageName
$packageFolder = $env:packageFolder

$helpersPath = (Split-Path -parent $MyInvocation.MyCommand.Definition);
$nugetChocolateyPath = (Split-Path -parent $helpersPath)
$nugetPath = $nugetChocolateyPath
$nugetExePath = Join-Path $nuGetPath 'bin'
$nugetLibPath = Join-Path $nuGetPath 'lib'
$badLibPath = Join-Path $nuGetPath 'lib-bad'
$extensionsPath = Join-Path $nugetPath 'extensions'
$chocInstallVariableName = "ChocolateyInstall"
$chocoTools = Join-Path $nuGetPath 'tools'
$nugetExe = Join-Path $chocoTools 'nuget.exe'
$7zip = Join-Path $chocoTools '7za.exe'
$ShimGen = Join-Path $chocoTools 'shimgen.exe'
$checksumExe = Join-Path $chocoTools 'checksum.exe'

Write-Debug "Running `'$packageScript`'";
& "$packageScript"