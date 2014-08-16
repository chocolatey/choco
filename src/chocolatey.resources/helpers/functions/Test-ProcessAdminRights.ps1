function Test-ProcessAdminRights {
<#
.SYNOPSIS
Tests whether the current process is running with administrative rights.

.DESCRIPTION
This function checks whether the current process has administrative rights
by checking if the current user identity is a member of the Administrators group.
It returns $true if the current process is running with administrative rights,
$false otherwise.

On Windows Vista and later, with UAC enabled, the returned value represents the
actual rights available to the process, i.e. if it returns $true, the process is
running elevated.

.OUTPUTS
System.Boolean

#>

  $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent([Security.Principal.TokenAccessLevels]'Query,Duplicate'))
  $isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
  Write-Debug "Test-ProcessAdminRights: returning $isAdmin"
  return $isAdmin
}
