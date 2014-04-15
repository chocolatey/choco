function Update-SessionEnvironment {
<#
.SYNOPSIS
Updates the environment variables of the current powershell session with 
any environment variable changes that may have occured during a chocolatey 
package install.

.DESCRIPTION
When chocolatey installs a package, the package author may ad or change 
certain environment variables that will affect how the application runs 
or how it is accessed. Often, these changes are not visible to the current 
powershell session. This means the user needs to open a new powershell 
session before these settings take effect which can render the installed 
application unfunctional until that time.

Use the Update-SessionEnvironment command to refresh the current 
powershell session with all environment settings possibly performed by 
chocolatey package installs.

#>
  $user = 'HKCU:\Environment'
  $machine ='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment'
  #ordering is important here, $user comes after so we can override $machine
  $machine, $user |
    Get-Item |
    % {
      $regPath = $_.PSPath
      $_ |
        Select -ExpandProperty Property |
        % {
          Set-Item "Env:$($_)" -Value (Get-ItemProperty $regPath -Name $_).$_
        }
    }

  #Path gets special treatment b/c it munges the two together
  $paths = 'Machine', 'User' |
    % {
      (Get-EnvironmentVar 'PATH' $_) -split ';'
    } |
    Select -Unique
  $Env:PATH = $paths -join ';'
}

function Get-EnvironmentVar($key, $scope) {
  [Environment]::GetEnvironmentVariable($key, $scope)
}
