. $PSScriptRoot\Install-ChocolateyPath.ps1
. $PSScriptRoot\Get-EnvironmentVariable.ps1
. $PSScriptRoot\Set-EnvironmentVariable.ps1
. $PSScriptRoot\Update-SessionEnvironment.ps1
. $PSScriptRoot\Test-ProcessAdminRights.ps1
. $PSScriptRoot\Start-ChocolateyProcessAsAdmin.ps1
. $PSScriptRoot\Write-FunctionCallLogMessage.ps1
. $PSScriptRoot\Get-EnvironmentVariableNames.ps1

$scriptRoot = $PSScriptRoot

Describe "Install-ChocolateyPath" {
  BeforeEach { 
    New-Variable -Name "USERPATH" -Value "" -Scope "GLOBAL" -Force
    New-Variable -Name "MACHINEPATH" -Value "%SystemRoot%\system32;%SystemRoot%;%SystemRoot%\System32\Wbem;%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\;" -Scope "GLOBAL" -Force
    New-Variable -Name "PROCESSPATH" -Value "%SystemRoot%\system32;%SystemRoot%;%SystemRoot%\System32\Wbem;%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\;" -Scope "GLOBAL" -Force
  }

  AfterEach {
    Remove-Variable -Name "USERPATH" -Scope "GLOBAL" -ErrorAction Ignore
    Remove-Variable -Name "MACHINEPATH" -Scope "GLOBAL" -ErrorAction Ignore
    Remove-Variable -Name "PROCESSPATH" -Scope "GLOBAL" -ErrorAction Ignore
  }

  Mock -CommandName Set-EnvironmentVariable -ParameterFilter { $Name -eq "PATH" } -MockWith {
    param (
      [parameter(Mandatory=$true, Position=0)][string] $Name,
      [parameter(Mandatory=$false, Position=1)][string] $Value,
      [parameter(Mandatory=$false, Position=2)][System.EnvironmentVariableTarget] $Scope,
      [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
    )
    Write-Host "Mock Set-EnvironmentVariable"
    if ($Scope -eq [System.EnvironmentVariableTarget]::Machine) {
      Set-Variable -Name "MACHINEPATH" -Value $Value -Scope "GLOBAL"
    } elseif ($Scope -eq [System.EnvironmentVariableTarget]::User) {
      Set-Variable -Name "USERPATH" -Value $Value -Scope "GLOBAL"
    } else {
      Set-Variable -Name "PROCESSPATH" -Value $Value -Scope "GLOBAL"
    }
  }

  Mock -CommandName Get-EnvironmentVariable -ParameterFilter { $Name -eq "PATH" } -MockWith {
    param(
      [Parameter(Mandatory=$true)][string] $Name,
      [Parameter(Mandatory=$true)][System.EnvironmentVariableTarget] $Scope,
      [Parameter(Mandatory=$false)][switch] $PreserveVariables = $false,
      [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
    )
    Write-Host "Mock Get-EnvironmentVariable $Scope"
    if ($Scope -eq [System.EnvironmentVariableTarget]::Machine) {
      return (Get-Variable -Name "MACHINEPATH" -Scope "GLOBAL").Value
    } elseif ($Scope -eq [System.EnvironmentVariableTarget]::User) {
      return (Get-Variable -Name "USERPATH" -Scope "GLOBAL").Value
    } else {
      return (Get-Variable -Name "PROCESSPATH" -Scope "GLOBAL").Value
    }
  }

  Mock -CommandName Test-ProcessAdminRights -MockWith { return $true }
  Mock -CommandName Write-FunctionCallLogMessage -MockWith { return }
  Mock -CommandName Update-SessionEnvironment -MockWith { return }

  Context "TEST-Install-ChocolateyPath-Already-Existing-Paths" {
    It "Add pre-existing path" {
      Install-ChocolateyPath -PathToInstall "C:\Windows"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::Machine) | Should Be "%SystemRoot%\system32;%SystemRoot%;%SystemRoot%\System32\Wbem;%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\;"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::User) | Should Be ""
    }
    It "Trailing-Backslash (SystemPath has 'C:\Window' but testing for 'C:\Windows\')" {
      Install-ChocolateyPath -PathToInstall "C:\Windows\"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::Machine) | Should Be "%SystemRoot%\system32;%SystemRoot%;%SystemRoot%\System32\Wbem;%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\;"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::User) | Should Be ""
    }
    It "Add Already-Existing path (cmd-type variables)" {
      Install-ChocolateyPath -PathToInstall "%SystemRoot%"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::Machine) | Should Be "%SystemRoot%\system32;%SystemRoot%;%SystemRoot%\System32\Wbem;%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\;"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::User) | Should Be ""
    }
    It "Add Already-Existing path that has a terminating \\" {
      Install-ChocolateyPath -PathToInstall "C:\Windows\System32\WindowsPowerShell\v1.0\" -Scope [System.EnvironmentVariableTarget]::Machine
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::Machine) | Should Be "%SystemRoot%\system32;%SystemRoot%;%SystemRoot%\System32\Wbem;%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\;"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::User) | Should Be ""
    }

  }
  Context "TEST-Install-ChocolateyPath-Dont-Exist-On-Disk" {
    It "Add non-existing path" {
      Install-ChocolateyPath -PathToInstall "C:\WHATEVER"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::Machine) | Should Be "%SystemRoot%\system32;%SystemRoot%;%SystemRoot%\System32\Wbem;%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\;"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::User) | Should Be ""
    }
    It "Add non-existing path that has an extra \\" {
      Install-ChocolateyPath -PathToInstall "C:\WHATEVER\" -PathType ([System.EnvironmentVariableTarget]::Machine)
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::Machine) | Should Be "%SystemRoot%\system32;%SystemRoot%;%SystemRoot%\System32\Wbem;%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\;"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::User) | Should Be ""
    }
    It "Add non-existing path that is a subset of an existing one" {
      Install-ChocolateyPath -PathToInstall "%SystemRoot%\Sys"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::Machine) | Should Be "%SystemRoot%\system32;%SystemRoot%;%SystemRoot%\System32\Wbem;%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\;"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::User) | Should Be ""
    }
  }

  Context "TEST-Install-ChocolateyPath" {
    It "Adds an Existing Directory that is not in the system Path (machine)" {
      Install-ChocolateyPath -PathToInstall "C:\Windows\System32\drivers" -PathType ([System.EnvironmentVariableTarget]::Machine)
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::Machine) | Should Be "C:\Windows\System32\drivers;%SystemRoot%\system32;%SystemRoot%;%SystemRoot%\System32\Wbem;%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\;"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::User) | Should Be ""
    }
    It "Adds an Existing Directory that is not in the system Path (user)" {
      Install-ChocolateyPath -PathToInstall "C:\Windows\System32\drivers" -PathType ([System.EnvironmentVariableTarget]::User)
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::Machine) | Should Be "%SystemRoot%\system32;%SystemRoot%;%SystemRoot%\System32\Wbem;%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\;"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::User) | Should Be "C:\Windows\System32\drivers;"
    }
    It "Adds an Existing Directory that is not in the system Path (cmd-type variables)" {
      Install-ChocolateyPath -PathToInstall "%WINDIR%\System32\drivers" -PathType ([System.EnvironmentVariableTarget]::Machine)
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::Machine) | Should Be "%WINDIR%\System32\drivers;%SystemRoot%\system32;%SystemRoot%;%SystemRoot%\System32\Wbem;%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\;"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::User) | Should Be ""
    }
    It "Adds an Existing Directory that is not in the system Path (pwrshell-type variables)" {
      Install-ChocolateyPath -PathToInstall "$env:SystemRoot\System32\drivers" -PathType ([System.EnvironmentVariableTarget]::Machine)
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::Machine) | Should Be "C:\Windows\System32\drivers;%SystemRoot%\system32;%SystemRoot%;%SystemRoot%\System32\Wbem;%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\;"
      Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::User) | Should Be ""
    }
  }
}