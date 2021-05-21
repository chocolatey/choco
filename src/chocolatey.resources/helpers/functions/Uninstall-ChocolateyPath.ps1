# Copyright 2011 - Present RealDimensions Software, LLC
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

function Uninstall-ChocolateyPath {
<#
.SYNOPSIS
**NOTE:** Administrative Access Required when `-PathType 'Machine'.`

This puts a directory to the PATH environment variable of the
requested scope (Machine or User).

.DESCRIPTION
Removes path from target path scope.  Removes multiple occurances (if they exist)
and all occurances with or without a trailing slash.

.NOTES
This command will assert UAC/Admin privileges on the machine if
`-PathType 'Machine'`.

This is used when the application/tool is not being linked by Chocolatey
(not in the lib folder).

.INPUTS
None

.OUTPUTS
None

.PARAMETER PathToUninstall
The full path to a location to remove from the PATH.

.PARAMETER PathType
Which PATH to remove it from. If specifying `Machine`, this requires admin
privileges to run correctly.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
Uninstall-ChocolateyPath -PathToUninstall "$($env:SystemDrive)\tools\gittfs"

.EXAMPLE
Uninstall-ChocolateyPath "$($env:SystemDrive)\Program Files\MySQL\MySQL Server 5.5\bin" -PathType 'Machine'

.LINK
Install-ChocolateyPath

.LINK
Install-ChocolateyEnvironmentVariable

.LINK
Get-EnvironmentVariable

.LINK
Set-EnvironmentVariable

.LINK
Get-ToolsLocation
#>
param(
  [parameter(Mandatory=$true, Position=0)][alias("Path")][string] $PathToUninstall,
  [parameter(Mandatory=$false, Position=1)][ValidateSet('User','Machine','All')][alias("Scope")][String] $pathType = 'User',
  [parameter(Mandatory=$false)][switch] $RecursiveCall,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)
  Write-Debug "Running 'Uninstall-ChocolateyPath' with PathToUninstall:`'$PathToUninstall`'";
  If (!$RecursiveCall -AND ($pathType -ine 'All')) {Write-Host "Only evaluating and updating path scope `"$pathType`", path will not be assessed nor removed for other scope, so path may exist in other scope as well."}
  $originalPathToUninstall = $PathToUninstall
  #First half on handling trailing slash properly - remove it from requested path:
  $PathToUninstall = $PathToUninstall.trimend('\')
  #array drops blanks (one of which is always created by final semi-colon)
  $actualPathArrayUser = (Get-EnvironmentVariable -Name 'Path' -Scope 'user' -PreserveVariables).split(';',[System.StringSplitOptions]::RemoveEmptyEntries)
  $actualPathArrayMachine = (Get-EnvironmentVariable -Name 'Path' -Scope 'machine' -PreserveVariables).split(';',[System.StringSplitOptions]::RemoveEmptyEntries)

  $PathFoundInMachine = $PathFoundInUser = $False
  If (($actualpathArrayMachine -icontains "$($PathToUninstall.ToLower())") -OR ($actualpathArrayMachine -icontains "$(($PathToUninstall + '\').ToLower())"))
  {
    $PathFoundInMachine = $True
  }

  If (($actualpathArrayUser -icontains "$($PathToUninstall.ToLower())") -OR ($actualpathArrayUser -icontains "$(($PathToUninstall + '\').ToLower())"))
  {
    $PathFoundInUser = $True
  }

  #Process machine first to minimize suppression of messaging when recursion is necessary to process machine path
  If ($PathFoundInMachine)
  {
    If (!$RecursiveCall) {Write-Host "Target path `"$PathToUninstall`" exists in Machine scope..."}
    If ($pathType -ieq 'User' -AND ($pathType -ine 'All'))
    {
      If (!$RecursiveCall) {Write-Host "`"$PathToUninstall`" will only be removed from Machine scope per your request.  Use -PathType 'User' to remove only from Machine scope or -PathType 'All' to remove from all scopes."}
    }


    If (($pathType -ieq 'Machine') -OR ($pathType -ieq 'All'))
    {
      If (!$RecursiveCall) {Write-Host "PATH environment variable for scope `"Machine`" contains `"$PathToUninstall`". Removing..."}
      $actualpathArray = $actualPathArrayMachine
      [string[]]$Newpatharray = $null
      foreach ($path in $actualpathArray)
      {
        #second half of handling trailing slash properly - compare to both options in target path
        If (($path -ine "$PathToUninstall") -AND ($path -ine "$($PathToUninstall)\"))
        {
          [string[]]$Newpatharray += "$path"
        }
      }
      $actualPath = ($Newpatharray -join(';')) + ';'

      if (Test-ProcessAdminRights)
      {
        Set-EnvironmentVariable -Name 'Path' -Value $actualPath -Scope 'Machine'
      }
      ElseIf (!$RecursiveCall)
      {
        $psArgs = "Uninstall-ChocolateyPath -PathToUninstall `'$originalPathToUninstall`' -pathType `'Machine`' -RecursiveCall"
        Start-ChocolateyProcessAsAdmin "$psArgs"
        If ($RecursiveCall) {Return}
      }
      Else
      {
        Throw "Did not gain admin rights on the recursive call, exiting to avoid going into recursive loop."
      }
    }
  }

  If ($PathFoundInUser)
  {
    Write-Host "Target path `"$PathToUninstall`" exists in User scope..."
    If ($pathType -ine 'Machine' -AND ($pathType -ine 'All'))
    {
      Write-Host "`"$PathToUninstall`" will only be removed from User scope per your request.  Use -PathType 'Machine' to remove only from Machine scope or -PathType 'All' to remove from all scopes."
    }

    If (($pathType -ieq 'User') -OR ($pathType -ieq 'All'))
    {
      Write-Host "PATH environment variable for scope `"User`" contains `"$PathToUninstall`". Removal Removing..."
      $actualpathArray = $actualPathArrayUser
      [string[]]$Newpatharray = $null
      foreach ($path in $actualpathArray)
      {
        #second half of handling trailing slash properly - compare to both options in target path
        If (($path -ine "$PathToUninstall") -AND ($path -ine "$($PathToUninstall)\"))
        {
          [string[]]$Newpatharray += "$path"
        }
      }
      $actualPath = ($Newpatharray -join(';')) + ';'
      Set-EnvironmentVariable -Name 'Path' -Value $actualPath -Scope 'User'
    }
  }

  If ($PathFoundInUser -OR $PathFoundInMachine)
  {
    Write-Host "Updating environment for current process"
    Update-SessionEnvironment
  }
  Else
  {
    Write-Host "`"$PathToUninstall`" was not found in requested scope `"$PathType`". Nothing to do..."
  }
}
