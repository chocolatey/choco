function Write-Error {
param(
  [Parameter(Position=0,Mandatory=$true,ValueFromPipeline=$true)][string] $Message='',
  [Parameter(Mandatory=$false)][System.Management.Automation.ErrorCategory] $Category,
  [Parameter(Mandatory=$false)][string] $ErrorId,
  [Parameter(Mandatory=$false)][object] $TargetObject,
  [Parameter(Mandatory=$false)][string] $CategoryActivity,
  [Parameter(Mandatory=$false)][string] $CategoryReason,
  [Parameter(Mandatory=$false)][string] $CategoryTargetName,
  [Parameter(Mandatory=$false)][string] $CategoryTargetType,
  [Parameter(Mandatory=$false)][string] $RecommendedAction
)

  $chocoPath = (Split-Path -parent $helpersPath)
  $chocoInstallLog = Join-Path $chocoPath 'chocolateyInstall.log'
  "$(get-date -format 'yyyyMMdd-HH:mm:ss') [ERROR] $Message" | Out-File -FilePath $chocoInstallLog -Force -Append

  $oc = Get-Command 'Write-Error' -Module 'Microsoft.PowerShell.Utility' 
  & $oc @PSBoundParameters
}