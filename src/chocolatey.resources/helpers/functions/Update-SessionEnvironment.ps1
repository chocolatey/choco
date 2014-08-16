function Update-SessionEnvironment {
<#
.SYNOPSIS
Updates the environment variables of the current powershell session with
any environment variable changes that may have occured during a chocolatey
package install.

.DESCRIPTION
When chocolatey installs a package, the package author may add or change
certain environment variables that will affect how the application runs
or how it is accessed. Often, these changes are not visible to the current
powershell session. This means the user needs to open a new powershell
session before these settings take effect which can render the installed
application unfunctional until that time.

Use the Update-SessionEnvironment command to refresh the current
powershell session with all environment settings possibly performed by
chocolatey package installs.

#>
  #ordering is important here, $user comes after so we can override $machine
  'Machine', 'User' |
    % {
      $scope = $_
      Get-EnvironmentVariableNames -Scope $scope |
        % {
          Set-Item "Env:$($_)" -Value (Get-EnvironmentVariable -Scope $scope -Name $_)
        }
    }

  #Path gets special treatment b/c it munges the two together
  $paths = 'Machine', 'User' |
    % {
      (Get-EnvironmentVariable -Name 'PATH' -Scope $_) -split ';'
    } |
    Select -Unique
  $Env:PATH = $paths -join ';'
}
