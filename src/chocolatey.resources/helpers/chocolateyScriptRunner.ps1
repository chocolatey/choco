param(
  [alias("ia","installArgs")][string] $installArguments = '',
  [alias("o","override","overrideArguments","notSilent")]
  [switch] $overrideArgs = $false,
  [alias("x86")][switch] $forceX86 = $false,
  [alias("params","parameters","pkgParams")][string]$packageParameters = '',
  [string]$packageScript
)

$global:DebugPreference = "SilentlyContinue"
if ($env:ChocolateyEnvironmentDebug -eq 'true') { $global:DebugPreference = "Continue"; }
$global:VerbosePreference = "SilentlyContinue"
if ($env:ChocolateyEnvironmentVerbose -eq 'true') { $global:VerbosePreference = "Continue"; $verbosity = $true }

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
$packageVersion = $env:packageVersion
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
$7zip = Join-Path $chocoTools '7z.exe'
$ShimGen = Join-Path $chocoTools 'shimgen.exe'
$checksumExe = Join-Path $chocoTools 'checksum.exe'

Write-Debug "Running `'$packageScript`'";
& "$packageScript"
$scriptSuccess = $?
$lastExecutableExitCode = $LASTEXITCODE

if ($lastExecutableExitCode -ne $null -and $lastExecutableExitCode -ne '') {
  Write-Debug "The last executable that ran had an exit code of '$lastExecutableExitCode'."
} 

if (-not $scriptSuccess) {
 Write-Debug "The script exited with a failure."
} 

$exitCode = 0
if ($env:ChocolateyCheckLastExitCode -ne $null -and $env:ChocolateyCheckLastExitCode -eq 'true' -and $lastExecutableExitCode -ne $null -and $lastExecutableExitCode -ne '') {
  $exitCode = $lastExecutableExitCode
}

if ($exitCode -eq 0 -and -not $scriptSuccess) {
  $exitCode = 1
}

if ($env:ChocolateyExitCode -ne $null -and $env:ChocolateyExitCode -ne '') {
 $exitCode = $env:ChocolateyExitCode
}

if ($exitCode -ne $null -and $exitCode -ne '' -and $exitCode -ne 0) {
  Set-PowerShellExitCode $exitCode
}

Exit $exitCode