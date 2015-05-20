$thisScriptFolder = (Split-Path -parent $MyInvocation.MyCommand.Definition)
$chocInstallVariableName = "ChocolateyInstall"
$sysDrive = $env:SystemDrive
$tempDir = $env:TEMP
$defaultChocolateyPathOld = "$sysDrive\Chocolatey"
#$ErrorActionPreference = 'Stop'
$debugModeParams = $null

function Initialize-Chocolatey {
<#
  .DESCRIPTION
    This will initialize the Chocolatey tool by
      a) setting up the "nugetPath" (the location where all chocolatey nuget packages will be installed)
      b) Installs chocolatey into the "nugetPath"
            c) Instals .net 4.0 if needed
      d) Adds chocolaty to the PATH environment variable so you have access to the chocolatey|cinst commands.
  .PARAMETER  NuGetPath
    Allows you to override the default path of (C:\Chocolatey\) by specifying a directory chocolaty will install nuget packages.

  .EXAMPLE
    C:\PS> Initialize-Chocolatey

    Installs chocolatey into the default C:\Chocolatey\ directory.

  .EXAMPLE
    C:\PS> Initialize-Chocolatey -nugetPath "D:\ChocolateyInstalledNuGets\"

    Installs chocolatey into the custom directory D:\ChocolateyInstalledNuGets\

#>
param(
  [Parameter(Mandatory=$false)][string]$chocolateyPath = ''
)
  Write-Debug "Initialize-Chocolatey"

  if ($env:ChocolateyEnvironmentDebug -eq 'true') {
    $debugModeParams = '-dv'
  }

  Install-DotNet4IfMissing

  $chocoNew = Join-Path $thisScriptFolder 'chocolateyInstall\choco.exe'
  & $chocoNew unpackself -fy $debugModeParams

  $installModule = Join-Path $thisScriptFolder 'chocolateyInstall\helpers\chocolateyInstaller.psm1'
  Import-Module $installModule -Force

  if ($chocolateyPath -eq '') {
    $programData = [Environment]::GetFolderPath("CommonApplicationData")
    $chocolateyPath = Join-Path "$programData" 'chocolatey'
  }

  # variable to allow insecure directory:
  $allowInsecureRootInstall = $false
  if ($env:ChocolateyAllowInsecureRootDirectory -eq 'true') { $allowInsecureRootInstall = $true }

  # if we have an already environment variable path, use it.
  $alreadyInitializedNugetPath = Get-ChocolateyInstallFolder
  if ($alreadyInitializedNugetPath -and $alreadyInitializedNugetPath -ne $chocolateyPath -and ($allowInsecureRootInstall -or $alreadyInitializedNugetPath -ne $defaultChocolateyPathOld)){
    $chocolateyPath = $alreadyInitializedNugetPath
  }
  else {
    Set-ChocolateyInstallFolder $chocolateyPath
  }
  Create-DirectoryIfNotExists $chocolateyPath

  Ensure-UserPermissions $chocolateyPath

  #set up variables to add
  $chocolateyExePath = Join-Path $chocolateyPath 'bin'
  $chocolateyLibPath = Join-Path $chocolateyPath 'lib'

  if ($tempDir -eq $null) {
    $tempDir = Join-Path $chocolateyPath 'temp'
    Create-DirectoryIfNotExists $tempDir
  }

  $yourPkgPath = [System.IO.Path]::Combine($chocolateyLibPath,"yourPackageName")
@"
We are setting up the Chocolatey package repository.
The packages themselves go to `'$chocolateyLibPath`'
  (i.e. $yourPkgPath).
A shim file for the command line goes to `'$chocolateyExePath`'
  and points to an executable in `'$yourPkgPath`'.

Creating Chocolatey folders if they do not already exist.

"@ | Write-Output

  Write-Warning "You can safely ignore errors related to missing log files when `n  upgrading from a version of Chocolatey less than 0.9.9. `n  'Batch file could not be found' is also safe to ignore. `n  'The system cannot find the file specified' - also safe."

  #create the base structure if it doesn't exist
  Create-DirectoryIfNotExists $chocolateyExePath
  Create-DirectoryIfNotExists $chocolateyLibPath

  Install-ChocolateyFiles $chocolateyPath
  Ensure-ChocolateyLibFiles $chocolateyLibPath

  Install-ChocolateyBinFiles $chocolateyPath $chocolateyExePath

  $chocolateyExePathVariable = $chocolateyExePath.ToLower().Replace($chocolateyPath.ToLower(), "%DIR%..\").Replace("\\","\")
  Initialize-ChocolateyPath $chocolateyExePath $chocolateyExePathVariable
  Process-ChocolateyBinFiles $chocolateyExePath $chocolateyExePathVariable

  $realModule = Join-Path $chocolateyPath "helpers\chocolateyInstaller.psm1"
  Import-Module "$realModule" -Force

  if (-not $allowInsecureRootInstall -and (Test-Path($defaultChocolateyPathOld))) {
    Upgrade-OldChocolateyInstall $defaultChocolateyPathOld $chocolateyPath
    Install-ChocolateyBinFiles $chocolateyPath $chocolateyExePath
  }

@"
Chocolatey (choco.exe) is now ready.
You can call choco from anywhere, command line or powershell by typing choco.
Run choco /? for a list of functions.
You may need to shut down and restart powershell and/or consoles
 first prior to using choco.
"@ | write-Output

  if (-not $allowInsecureRootInstall) {
    Remove-OldChocolateyInstall $defaultChocolateyPathOld
  }
}

function Set-ChocolateyInstallFolder {
param(
  [string]$folder
)
  Write-Debug "Set-ChocolateyInstallFolder"

  $environmentTarget = [System.EnvironmentVariableTarget]::User
  # removing old variable
  Install-ChocolateyEnvironmentVariable -variableName "$chocInstallVariableName" -variableValue $null -variableType $environmentTarget
  if (Test-ProcessAdminRights) {
    Write-Debug "Administrator installing so using Machine environment variable target instead of User."
    $environmentTarget = [System.EnvironmentVariableTarget]::Machine
    # removing old variable
    Install-ChocolateyEnvironmentVariable -variableName "$chocInstallVariableName" -variableValue $null -variableType $environmentTarget
  } else {
    Write-Warning "Setting ChocolateyInstall Environment Variable on USER and not SYSTEM variables.`n  This is due to either non-administrator install OR the process you are running is not being run as an Administrator."
  }

  Write-Output "Creating $chocInstallVariableName as an environment variable (targeting `'$environmentTarget`') `n  Setting $chocInstallVariableName to `'$folder`'"
  Write-Warning "It's very likely you will need to close and reopen your shell `n  before you can use choco."
  Install-ChocolateyEnvironmentVariable -variableName "$chocInstallVariableName" -variableValue "$folder" -variableType $environmentTarget
}

function Get-ChocolateyInstallFolder(){
  Write-Debug "Get-ChocolateyInstallFolder"
  [Environment]::GetEnvironmentVariable($chocInstallVariableName)
}

function Create-DirectoryIfNotExists($folderName){
  Write-Debug "Create-DirectoryIfNotExists"
  if (![System.IO.Directory]::Exists($folderName)) { [System.IO.Directory]::CreateDirectory($folderName) | Out-Null }
}

function Ensure-UserPermissions {
param(
  [string]$folder
)
  Write-Debug "Ensure-UserPermissions"

  if (!(Test-ProcessAdminRights)) {
    Write-Warning "User is not running elevated, cannot set user permissions."
    return
  }

  $currentEA = $ErrorActionPreference
  $ErrorActionPreference = 'Stop'
  try {
    # get current user
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    # get current acl
    $acl = Get-Acl $folder

    # define rule to set
    $rights = "Modify"
    $userAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($currentUser.Name, $rights, "Allow")

    # this is idempotent
    Write-Output "Adding Modify permission for current user to '$folder'"
    $acl.SetAccessRuleProtection($false,$true)
    $acl.SetAccessRule($userAccessRule)
    Set-Acl $folder $acl
  } catch {
    Write-Warning "Not able to set permissions for user."
  }
  $ErrorActionPreference = $currentEA
}

function Upgrade-OldChocolateyInstall {
param(
  [string]$chocolateyPathOld = "$sysDrive\Chocolatey",
  [string]$chocolateyPath =  "$($env:ALLUSERSPROFILE)\chocolatey"
)

  Write-Debug "Upgrade-OldChocolateyInstall"

  if (Test-Path $chocolateyPathOld) {
    Write-Output "Attempting to upgrade `'$chocolateyPathOld`' to `'$chocolateyPath`'."
    Write-Warning "Copying the contents of `'$chocolateyPathOld`' to `'$chocolateyPath`'. `n This step may fail if you have anything in this folder running or locked."
    Write-Output 'If it fails, just manually copy the rest of the items out and then delete the folder.'
    Write-Host "!!!! ATTN: YOU WILL NEED TO CLOSE AND REOPEN YOUR SHELL !!!!" -ForegroundColor Magenta -BackgroundColor Black

    $chocolateyExePathOld = Join-Path $chocolateyPathOld 'bin'
    'Machine', 'User' |
    % {
      $path = Get-EnvironmentVariable -Name 'PATH' -Scope $_
      $updatedPath = [System.Text.RegularExpressions.Regex]::Replace($path,[System.Text.RegularExpressions.Regex]::Escape($chocolateyExePathOld) + '(?>;)?', '', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
      if ($updatedPath -ne $path) {
        Write-Output "Updating `'$_`' PATH to reflect removal of '$chocolateyPathOld'."
        try {
          Set-EnvironmentVariable -Name 'Path' -Value $updatedPath -Scope $_ -ErrorAction Stop
        } catch {
          Write-Warning "Was not able to remove the old environment variable from PATH. You will need to do this manually"
        }

      }
    }

    Copy-Item "$chocolateyPathOld\lib\*" "$chocolateyPath\lib" -force -recurse

    $from = "$chocolateyPathOld\bin"
    $to = "$chocolateyPath\bin"
    $exclude = @("choco.exe", "chocolatey.exe", "cinst.exe", "clist.exe", "cpack.exe", "cpush.exe", "cuninst.exe", "cup.exe", "cver.exe", "RefreshEnv.cmd")
    Get-ChildItem -Path $from -recurse -Exclude $exclude |
      % {
        Write-Debug "Copying $_ `n to $to"
        if ($_.PSIsContainer) {
          Copy-Item $_ -Destination (Join-Path $to $_.Parent.FullName.Substring($from.length)) -Force -ErrorAction SilentlyContinue
        } else {
          $fileToMove = (Join-Path $to $_.FullName.Substring($from.length))
          try {
           Copy-Item $_ -Destination $fileToMove -Exclude $exclude -Force -ErrorAction Stop
          }
          catch {
            Write-Warning "Was not able to move `'$fileToMove`'. You may need to reinstall the shim"
          }
        }
      }
  }
}

function Remove-OldChocolateyInstall {
param(
  [string]$chocolateyPathOld = "$sysDrive\Chocolatey"
)
  Write-Debug "Remove-OldChocolateyInstall"

  if (Test-Path $chocolateyPathOld) {
    Write-Warning "This action will result in Log Errors, you can safely ignore those. `n You may need to finish removing '$chocolateyPathOld' manually."
    try {
      Get-ChildItem -Path "$chocolateyPathOld" | % {
        if (Test-Path $_.FullName) {
          Write-Debug "Removing $_ unless matches .log"
          Remove-Item $_.FullName -exclude *.log -recurse -force -ErrorAction SilentlyContinue
        }
      }

      Write-Output "Attempting to remove `'$chocolateyPathOld`'. This may fail if something in the folder is being used or locked."
      Remove-Item "$($chocolateyPathOld)" -force -recurse -ErrorAction Stop
    }
    catch {
      Write-Warning "Was not able to remove `'$chocolateyPathOld`'. You will need to manually remove it."
    }
  }
}

function Install-ChocolateyFiles {
param(
  [string]$chocolateyPath
)
  Write-Debug "Install-ChocolateyFiles"

  Write-Debug "Removing install files in chocolateyInstall, helpers, redirects, and tools"
  "$chocolateyPath\chocolateyInstall", "$chocolateyPath\helpers", "$chocolateyPath\redirects", "$chocolateyPath\tools" | % {
    #Write-Debug "Checking path $_"

    if (Test-Path $_) {
      Get-ChildItem -Path "$_" | % {
        #Write-Debug "Checking child path $_ ($($_.FullName))"
        if (Test-Path $_.FullName) {
          Write-Debug "Removing $_ unless matches .log"
          Remove-Item $_.FullName -exclude *.log -recurse -force -ErrorAction SilentlyContinue
        }
      }
    }
  }

  Write-Debug "Attempting to move choco.exe to choco.exe.old so we can place the new version here."
  # rename the currently running process / it will be locked if it exists
  $chocoExe = Join-Path $chocolateyPath 'choco.exe'
  if (Test-Path ($chocoExe)) {
    Write-Debug "Renaming '$chocoExe' to '$chocoExe.old'"
    try {
      Remove-Item "$chocoExe.old" -force -ErrorAction SilentlyContinue
      Move-Item $chocoExe "$chocoExe.old" -force -ErrorAction SilentlyContinue
    }
    catch {
      Write-Warning "Was not able to rename `'$chocoExe`' to `'$chocoExe.old`'."
    }
  }

  Write-Debug "Unpacking files required for Chocolatey."
  $chocInstallFolder = Join-Path $thisScriptFolder "chocolateyInstall"
  $chocoExe = Join-Path $chocInstallFolder 'choco.exe'
  $chocoExeDest = Join-Path $chocolateyPath 'choco.exe'
  Copy-Item $chocoExe $chocoExeDest -force

  & $chocoExeDest unpackself -fy $debugModeParams
}

function Ensure-ChocolateyLibFiles {
param(
  [string]$chocolateyLibPath
)
  Write-Debug "Ensure-ChocolateyLibFiles"
  $chocoPkgDirectory = Join-Path $chocolateyLibPath 'chocolatey'

  if ( -not (Test-Path("$chocoPkgDirectory\chocolatey.nupkg")) ) {
    Write-Output "Ensuring '$chocoPkgDirectory' exists."
    Create-DirectoryIfNotExists $chocoPkgDirectory

    $chocoPkg = Get-ChildItem "$thisScriptFolder/../../" | ?{$_.name -match "^chocolatey.*nupkg"} | Sort name -Descending | Select -First 1
    if ($chocoPkg -ne '') { $chocoPkg = $chocoPkg.FullName }
    "$tempDir\chocolatey.zip", "$chocoPkg" | % {
      if ($_ -ne $null -and $_ -ne '') {
        if (Test-Path $_) {
          Copy-Item $_ "$chocoPkgDirectory\chocolatey.nupkg" -force -ErrorAction SilentlyContinue
        }
      }
    }
  }
}

function Install-ChocolateyBinFiles {
param(
  [string] $chocolateyPath,
  [string] $chocolateyExePath
)
  Write-Debug "Install-ChocolateyBinFiles"
  Write-Debug "Installing the bin file redirects"
  $redirectsPath = Join-Path $chocolateyPath 'redirects'
  if (!(Test-Path "$redirectsPath")) {
    Write-Warning "$redirectsPath does not exist"
    return
  }

  $exeFiles = Get-ChildItem "$redirectsPath" -include @("*.exe","*.cmd") -recurse
  foreach ($exeFile in $exeFiles) {
    $exeFilePath = $exeFile.FullName
    $exeFileName = [System.IO.Path]::GetFileName("$exeFilePath")
    $binFilePath = Join-Path $chocolateyExePath $exeFileName
    $binFilePathRename = $binFilePath + '.old'
    $batchFilePath = $binFilePath.Replace(".exe",".bat")
    $bashFilePath = $binFilePath.Replace(".exe","")
    if (Test-Path ($batchFilePath)) { Remove-Item $batchFilePath -force -ErrorAction SilentlyContinue }
    if (Test-Path ($bashFilePath)) { Remove-Item $bashFilePath -force -ErrorAction SilentlyContinue }
    if (Test-Path ($binFilePathRename)) {
      try {
        Write-Debug "Attempting to remove $binFilePathRename"
        Remove-Item $binFilePathRename -force -ErrorAction Stop
      }
      catch {
        Write-Warning "Was not able to remove `'$binFilePathRename`'. This may cause errors."
      }
    }
    if (Test-Path ($binFilePath)) {
     try {
        Write-Debug "Attempting to rename $binFilePath to $binFilePathRename"
        Move-Item -path $binFilePath -destination $binFilePathRename -force -ErrorAction Stop
      }
      catch {
        Write-Warning "Was not able to rename `'$binFilePath`' to `'$binFilePathRename`'."
      }
    }

    try {
      Write-Debug "Attempting to copy $exeFilePath to $binFilePath"
      Copy-Item -path $exeFilePath -destination $binFilePath -force -ErrorAction Stop
    }
    catch {
      Write-Warning "Was not able to replace `'$binFilePath`' with `'$exeFilePath`'. You may need to do this manually."
    }

    $commandShortcut = [System.IO.Path]::GetFileNameWithoutExtension("$exeFilePath")
    Write-Debug "Added command $commandShortcut"
  }
}

function Initialize-ChocolateyPath {
param(
  [string]$chocolateyExePath = "$($env:ALLUSERSPROFILE)\chocolatey\bin",
  [string]$chocolateyExePathVariable = "%$($chocInstallVariableName)%\bin"
)
  Write-Debug "Initialize-ChocolateyPath"
  Write-Debug "Initializing Chocolatey Path if required"
  $environmentTarget = [System.EnvironmentVariableTarget]::User
  if (Test-ProcessAdminRights) {
    Write-Debug "Administrator installing so using Machine environment variable target instead of User."
    $environmentTarget = [System.EnvironmentVariableTarget]::Machine
  } else {
    Write-Warning "Setting ChocolateyInstall Path on USER PATH and not SYSTEM Path.`n  This is due to either non-administrator install OR the process you are running is not being run as an Administrator."
  }

  Install-ChocolateyPath -pathToInstall "$chocolateyExePath" -pathType $environmentTarget
}

function Process-ChocolateyBinFiles {
param(
  [string]$chocolateyExePath = "$($env:ALLUSERSPROFILE)\chocolatey\bin",
  [string]$chocolateyExePathVariable = "%$($chocInstallVariableName)%\bin"
)
  Write-Debug "Process-ChocolateyBinFiles"
  $processedMarkerFile = Join-Path $chocolateyExePath '_processed.txt'
  if (!(test-path $processedMarkerFile)) {
    $files = get-childitem $chocolateyExePath -include *.bat -recurse
    if ($files -ne $null -and $files.Count -gt 0) {
      Write-Debug "Processing Bin files"
      foreach ($file in $files) {
        Write-Output "Processing $($file.Name) to make it portable"
        $fileStream = [System.IO.File]::Open("$file", 'Open', 'Read', 'ReadWrite')
        $reader = New-Object System.IO.StreamReader($fileStream)
        $fileText = $reader.ReadToEnd()
        $reader.Close()
        $fileStream.Close()

        $fileText = $fileText.ToLower().Replace("`"" + $chocolateyPath.ToLower(), "SET DIR=%~dp0%`n""%DIR%..\").Replace("\\","\")

        Set-Content $file -Value $fileText -Encoding Ascii
      }
    }

    Set-Content $processedMarkerFile -Value "$([System.DateTime]::Now.Date)" -Encoding Ascii
  }
}

$netFx4InstallTries = 0

function Install-DotNet4IfMissing {
param(
  $forceFxInstall = $false
)
  # we can't take advantage of any chocolatey module functions, because they
  # haven't been unpacked because they require .NET Framework 4.0

  Write-Debug "Install-DotNet4IfMissing"
  if ([IntPtr]::Size -eq 8) {$fx="framework64"} else {$fx="framework"}

  $NetFx4ClientUrl = 'http://download.microsoft.com/download/5/6/2/562A10F9-C9F4-4313-A044-9C94E0A8FAC8/dotNetFx40_Client_x86_x64.exe'
  $NetFx4FullUrl = 'http://download.microsoft.com/download/9/5/A/95A9616B-7A37-4AF6-BC36-D6EA96C8DAAE/dotNetFx40_Full_x86_x64.exe'
  $NetFx4Url = $NetFx4FullUrl
  $NetFx4Path = "$tempDir"
  $NetFx4InstallerFile = 'dotNetFx40_Full_x86_x64.exe'
  $NetFx4Installer = Join-Path $NetFx4Path $NetFx4InstallerFile

  if(!(test-path "$env:windir\Microsoft.Net\$fx\v4.0.30319") -or $forceFxInstall) {
    if (!(Test-Path $NetFx4Path)) {
      Write-Host "Creating folder `'$NetFx4Path`'"
      $null = New-Item -Path "$NetFx4Path" -ItemType Directory
    }

    $netFx4InstallTries += 1

    if (!(Test-Path $NetFx4Installer)) {
      Write-Host "Downloading `'$NetFx4Url`' to `'$NetFx4Installer`' - the installer is 40+ MBs, so this could take a while on a slow connection."
      (New-Object Net.WebClient).DownloadFile("$NetFx4Url","$NetFx4Installer")
    }

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.WorkingDirectory = "$NetFx4Path"
    $psi.FileName = "$NetFx4InstallerFile"
    # https://msdn.microsoft.com/library/ee942965(v=VS.100).aspx#command_line_options
    # http://blogs.msdn.com/b/astebner/archive/2010/05/12/10011664.aspx
    # For the actual setup.exe (if you want to unpack first) - /repair /x86 /x64 /ia64 /parameterfolder Client /q /norestart
    $psi.Arguments = "/q /norestart /repair"

    Write-Host "Installing `'$NetFx4Installer`' - this may take awhile with no output."
    $s = [System.Diagnostics.Process]::Start($psi);
    $s.WaitForExit();
    if ($s.ExitCode -ne 0 -and $s.ExitCode -ne 3010) {
      if ($netFx4InstallTries -eq 2) {
        Write-Error ".NET Framework install failed with exit code `'$($s.ExitCode)`'. `n This will cause the rest of the install to fail."
        throw "Error installing .NET Framework 4.0 (exit code $($s.ExitCode)). `n Please install the .NET Framework 4.0 manually and then try to install Chocolatey again. `n Download at `'$NetFx4Url`'"
      } else {
        Write-Warning "First try of .NET framework install failed with exit code `'$($s.ExitCode)`'. Trying again."
        Install-DotNet4IfMissing $true
      }
    }
  }
}

Export-ModuleMember -function Initialize-Chocolatey;
