$thisScriptFolder = (Split-Path -parent $MyInvocation.MyCommand.Definition)
$chocInstallVariableName = "ChocolateyInstall"
$sysDrive = $env:SystemDrive
$tempDir = $env:TEMP
$defaultChocolateyPathOld = "$sysDrive\Chocolatey"

$originalForegroundColor = $host.ui.RawUI.ForegroundColor

function Write-ChocolateyWarning {
param (
  [string]$message = ''
)

  try {
    Write-Host "WARNING: $message" -ForegroundColor "Yellow" -ErrorAction "Stop"
  } catch {
    Write-Output "WARNING: $message"
  }
}

function  Write-ChocolateyError {
param (
  [string]$message = ''
)

  try {
    Write-Host "ERROR: $message" -ForegroundColor "Red" -ErrorAction "Stop"
  } catch {
    Write-Output "ERROR: $message"
  }
}

function Initialize-Chocolatey {
<#
  .DESCRIPTION
    This will initialize the Chocolatey tool by
      a) setting up the "chocolateyPath" (the location where all chocolatey nuget packages will be installed)
      b) Installs chocolatey into the "chocolateyPath"
            c) Instals .net 4.0 if needed
      d) Adds Chocolatey to the PATH environment variable so you have access to the choco commands.
  .PARAMETER  ChocolateyPath
    Allows you to override the default path of (C:\ProgramData\chocolatey\) by specifying a directory chocolatey will install nuget packages.

  .EXAMPLE
    C:\PS> Initialize-Chocolatey

    Installs chocolatey into the default C:\ProgramData\Chocolatey\ directory.

  .EXAMPLE
    C:\PS> Initialize-Chocolatey -chocolateyPath "D:\ChocolateyInstalledNuGets\"

    Installs chocolatey into the custom directory D:\ChocolateyInstalledNuGets\

#>
param(
  [Parameter(Mandatory=$false)][string]$chocolateyPath = ''
)
  Write-Debug "Initialize-Chocolatey"

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
  Ensure-Permissions $chocolateyPath

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

  Write-ChocolateyWarning "You can safely ignore errors related to missing log files when `n  upgrading from a version of Chocolatey less than 0.9.9. `n  'Batch file could not be found' is also safe to ignore. `n  'The system cannot find the file specified' - also safe."

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

  Add-ChocolateyProfile
  Install-DotNet4IfMissing
  Invoke-Chocolatey-Initial
  if ($env:ChocolateyExitCode -eq $null -or $env:ChocolateyExitCode -eq '') {
    $env:ChocolateyExitCode = 0
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
    Write-ChocolateyWarning "Setting ChocolateyInstall Environment Variable on USER and not SYSTEM variables.`n  This is due to either non-administrator install OR the process you are running is not being run as an Administrator."
  }

  Write-Output "Creating $chocInstallVariableName as an environment variable (targeting `'$environmentTarget`') `n  Setting $chocInstallVariableName to `'$folder`'"
  Write-ChocolateyWarning "It's very likely you will need to close and reopen your shell `n  before you can use choco."
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

function Get-LocalizedWellKnownPrincipalName {
param (
  [Parameter(Mandatory = $true)]
  [Security.Principal.WellKnownSidType] $WellKnownSidType
)
  $sid = New-Object -TypeName 'System.Security.Principal.SecurityIdentifier' -ArgumentList @($WellKnownSidType, $null)
  $account = $sid.Translate([Security.Principal.NTAccount])

  return $account.Value
}

function Ensure-Permissions {
param(
  [string]$folder
)
  Write-Debug "Ensure-Permissions"

  $defaultInstallPath = "$env:SystemDrive\ProgramData\chocolatey"
  try {
    $defaultInstallPath = Join-Path ([Environment]::GetFolderPath("CommonApplicationData")) 'chocolatey'
  } catch {
      # keep first setting
  }

  if ($folder.ToLower() -ne $defaultInstallPath.ToLower()) {
    Write-ChocolateyWarning "Installation folder is not the default. Not changing permissions. Please ensure your installation is secure."
    return
  }

  # Everything from here on out applies to the default installation folder

  if (!(Test-ProcessAdminRights)) {
    throw "Installation of Chocolatey to default folder requires Administrative permissions. Please run from elevated prompt. Please see https://chocolatey.org/install for details and alternatives if needing to install as a non-administrator."
  }

  $currentEA = $ErrorActionPreference
  $ErrorActionPreference = 'Stop'
  try {
    # get current acl
    $acl = (Get-Item $folder).GetAccessControl('Access,Owner')

    Write-Debug "Removing existing permissions."
    $acl.Access | % { $acl.RemoveAccessRuleAll($_) }

    $inheritanceFlags = ([Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit)
    $propagationFlags = [Security.AccessControl.PropagationFlags]::None

    $rightsFullControl = [Security.AccessControl.FileSystemRights]::FullControl
    $rightsModify = [Security.AccessControl.FileSystemRights]::Modify
    $rightsReadExecute = [Security.AccessControl.FileSystemRights]::ReadAndExecute
    $rightsWrite = [Security.AccessControl.FileSystemRights]::Write

    Write-Output "Restricting write permissions to Administrators"
    $builtinAdmins = Get-LocalizedWellKnownPrincipalName -WellKnownSidType ([Security.Principal.WellKnownSidType]::BuiltinAdministratorsSid)
    $adminsAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($builtinAdmins, $rightsFullControl, $inheritanceFlags, $propagationFlags, "Allow")
    $acl.SetAccessRule($adminsAccessRule)
    $localSystem = Get-LocalizedWellKnownPrincipalName -WellKnownSidType ([Security.Principal.WellKnownSidType]::LocalSystemSid)
    $localSystemAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($localSystem, $rightsFullControl, $inheritanceFlags, $propagationFlags, "Allow")
    $acl.SetAccessRule($localSystemAccessRule)
    $builtinUsers = Get-LocalizedWellKnownPrincipalName -WellKnownSidType ([Security.Principal.WellKnownSidType]::BuiltinUsersSid)
    $usersAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($builtinUsers, $rightsReadExecute, $inheritanceFlags, $propagationFlags, "Allow")
    $acl.SetAccessRule($usersAccessRule)

    $allowCurrentUser = $env:ChocolateyInstallAllowCurrentUser -eq 'true'
    if ($allowCurrentUser) {
      # get current user
      $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()

      if ($currentUser.Name -ne $localSystem) {
        $userAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($currentUser.Name, $rightsModify, $inheritanceFlags, $propagationFlags, "Allow")
        Write-ChocolateyWarning 'Adding Modify permission for current user due to $env:ChocolateyInstallAllowCurrentUser. This could lead to escalation of privilege attacks. Consider not allowing this.'
        $acl.SetAccessRule($userAccessRule)
      }
    } else {
      Write-Debug 'Current user no longer set due to possible escalation of privileges - set $env:ChocolateyInstallAllowCurrentUser="true" if you require this.'
    }

    Write-Debug "Set Owner to Administrators"
    $builtinAdminsSid = New-Object System.Security.Principal.SecurityIdentifier([Security.Principal.WellKnownSidType]::BuiltinAdministratorsSid, $null)
    $acl.SetOwner($builtinAdminsSid)

    Write-Debug "Default Installation folder - removing inheritance with no copy"
    $acl.SetAccessRuleProtection($true, $false)

    # enact the changes against the actual
    (Get-Item $folder).SetAccessControl($acl)

    # set an explicit append permission on the logs folder
    Write-Debug "Allow users to append to log files."
    $logsFolder = "$folder\logs"
    Create-DirectoryIfNotExists $logsFolder
    $logsAcl = (Get-Item $logsFolder).GetAccessControl('Access')
    $usersAppendAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($builtinUsers, $rightsWrite, [Security.AccessControl.InheritanceFlags]::ObjectInherit, [Security.AccessControl.PropagationFlags]::InheritOnly, "Allow")
    $logsAcl.SetAccessRule($usersAppendAccessRule)
    $logsAcl.SetAccessRuleProtection($false, $true)
    (Get-Item $logsFolder).SetAccessControl($logsAcl)
  } catch {
    Write-ChocolateyWarning "Not able to set permissions for $folder."
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
    Write-ChocolateyWarning "Copying the contents of `'$chocolateyPathOld`' to `'$chocolateyPath`'. `n This step may fail if you have anything in this folder running or locked."
    Write-Output 'If it fails, just manually copy the rest of the items out and then delete the folder.'
    Write-ChocolateyWarning "!!!! ATTN: YOU WILL NEED TO CLOSE AND REOPEN YOUR SHELL !!!!"
    #-ForegroundColor Magenta -BackgroundColor Black

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
          Write-ChocolateyWarning "Was not able to remove the old environment variable from PATH. You will need to do this manually"
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
            Write-ChocolateyWarning "Was not able to move `'$fileToMove`'. You may need to reinstall the shim"
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
    Write-ChocolateyWarning "This action will result in Log Errors, you can safely ignore those. `n You may need to finish removing '$chocolateyPathOld' manually."
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
      Write-ChocolateyWarning "Was not able to remove `'$chocolateyPathOld`'. You will need to manually remove it."
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
      Write-ChocolateyWarning "Was not able to rename `'$chocoExe`' to `'$chocoExe.old`'."
    }
  }

  # remove pdb file if it is found
  $chocoPdb = Join-Path $chocolateyPath 'choco.pdb'
  if (Test-Path ($chocoPdb)) {
    Remove-Item "$chocoPdb" -Force -ErrorAction SilentlyContinue
  }

  Write-Debug "Unpacking files required for Chocolatey."
  $chocInstallFolder = Join-Path $thisScriptFolder "chocolateyInstall"
  $chocoExe = Join-Path $chocInstallFolder 'choco.exe'
  $chocoExeDest = Join-Path $chocolateyPath 'choco.exe'
  Copy-Item $chocoExe $chocoExeDest -force

  Write-Debug "Copying the contents of `'$chocInstallFolder`' to `'$chocolateyPath`'."
  Copy-Item $chocInstallFolder\* $chocolateyPath -Recurse -Force
}

function Ensure-ChocolateyLibFiles {
param(
  [string]$chocolateyLibPath
)
  Write-Debug "Ensure-ChocolateyLibFiles"
  $chocoPkgDirectory = Join-Path $chocolateyLibPath 'chocolatey'

  Create-DirectoryIfNotExists $chocoPkgDirectory

  if (!(Test-Path("$chocoPkgDirectory\chocolatey.nupkg"))) {
    Write-Output "chocolatey.nupkg file not installed in lib.`n Attempting to locate it from bootstrapper."
    $chocoZipFile = Join-Path $tempDir "chocolatey\chocInstall\chocolatey.zip"

    Write-Debug "First the zip file at '$chocoZipFile'."
    Write-Debug "Then from a neighboring chocolatey.*nupkg file '$thisScriptFolder/../../'."

    if (Test-Path("$chocoZipFile")) {
      Write-Debug "Copying '$chocoZipFile' to '$chocoPkgDirectory\chocolatey.nupkg'."
      Copy-Item "$chocoZipFile" "$chocoPkgDirectory\chocolatey.nupkg" -Force -ErrorAction SilentlyContinue
    }

    if (!(Test-Path("$chocoPkgDirectory\chocolatey.nupkg"))) {
      $chocoPkg = Get-ChildItem "$thisScriptFolder/../../" | ?{$_.name -match "^chocolatey.*nupkg" } | Sort name -Descending | Select -First 1
      if ($chocoPkg -ne '') { $chocoPkg = $chocoPkg.FullName }
      "$chocoZipFile", "$chocoPkg" | % {
        if ($_ -ne $null -and $_ -ne '') {
          if (Test-Path $_) {
            Write-Debug "Copying '$_' to '$chocoPkgDirectory\chocolatey.nupkg'."
            Copy-Item $_ "$chocoPkgDirectory\chocolatey.nupkg" -Force -ErrorAction SilentlyContinue
          }
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
    Write-ChocolateyWarning "$redirectsPath does not exist"
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
        Write-ChocolateyWarning "Was not able to remove `'$binFilePathRename`'. This may cause errors."
      }
    }
    if (Test-Path ($binFilePath)) {
     try {
        Write-Debug "Attempting to rename $binFilePath to $binFilePathRename"
        Move-Item -path $binFilePath -destination $binFilePathRename -force -ErrorAction Stop
      }
      catch {
        Write-ChocolateyWarning "Was not able to rename `'$binFilePath`' to `'$binFilePathRename`'."
      }
    }

    try {
      Write-Debug "Attempting to copy $exeFilePath to $binFilePath"
      Copy-Item -path $exeFilePath -destination $binFilePath -force -ErrorAction Stop
    }
    catch {
      Write-ChocolateyWarning "Was not able to replace `'$binFilePath`' with `'$exeFilePath`'. You may need to do this manually."
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
    Write-ChocolateyWarning "Setting ChocolateyInstall Path on USER PATH and not SYSTEM Path.`n  This is due to either non-administrator install OR the process you are running is not being run as an Administrator."
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

# Adapted from http://www.west-wind.com/Weblog/posts/197245.aspx
function Get-FileEncoding($Path) {
    $bytes = [byte[]](Get-Content $Path -Encoding byte -ReadCount 4 -TotalCount 4)

    if(!$bytes) { return 'utf8' }

    switch -regex ('{0:x2}{1:x2}{2:x2}{3:x2}' -f $bytes[0],$bytes[1],$bytes[2],$bytes[3]) {
        '^efbbbf'   { return 'utf8' }
        '^2b2f76'   { return 'utf7' }
        '^fffe'     { return 'unicode' }
        '^feff'     { return 'bigendianunicode' }
        '^0000feff' { return 'utf32' }
        default     { return 'ascii' }
    }
}

function Add-ChocolateyProfile {
  Write-Debug "Add-ChocolateyProfile"
  try {
    $profileFile = "$profile"
    if ($profileFile -eq $null -or $profileFile -eq '') {
      Write-Output 'Not setting tab completion: Profile variable ($profile) resulted in an empty string.'
      return
    }

    $profileDirectory = (Split-Path -Parent $profileFile)

    if ($env:ChocolateyNoProfile -ne $null -and $env:ChocolateyNoProfile -ne '') {
      Write-Warning "Not setting tab completion: Environment variable "ChocolateyNoProfile" exists and is set."
      return
    }

    $localSystem = Get-LocalizedWellKnownPrincipalName -WellKnownSidType ([Security.Principal.WellKnownSidType]::LocalSystemSid)
    # get current user
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    if ($currentUser.Name -eq $localSystem) {
      Write-Warning "Not setting tab completion: Current user is SYSTEM user."
      return
    }

    if (!(Test-Path($profileDirectory))) {
      Write-Debug "Creating '$profileDirectory'"
      New-Item "$profileDirectory" -Type Directory -Force -ErrorAction SilentlyContinue | Out-Null
    }

    if (!(Test-Path($profileFile))) {
      Write-Warning "Not setting tab completion: Profile file does not exist at '$profileFile'."
      return

      #Write-Debug "Creating '$profileFile'"
      #"" | Out-File $profileFile -Encoding UTF8
    }

    # Check authenticode, but only if file is greater than 4 bytes
    $profileFileInfo = New-Object System.IO.FileInfo($profileFile)
    if ($profileFileInfo.Length -ge 5) {
      $signature = Get-AuthenticodeSignature $profile
      if ($signature.Status -ne 'NotSigned') {
        Write-Warning "Not setting tab completion: File is Authenticode signed at '$profile'."
        return
      }
    }

    $profileInstall = @'

# Chocolatey profile
$ChocolateyProfile = "$env:ChocolateyInstall\helpers\chocolateyProfile.psm1"
if (Test-Path($ChocolateyProfile)) {
  Import-Module "$ChocolateyProfile"
}
'@

    $chocoProfileSearch = '$ChocolateyProfile'
    if(Select-String -Path $profileFile -Pattern $chocoProfileSearch -Quiet -SimpleMatch) {
      Write-Debug "Chocolatey profile is already installed."
      return
    }

    Write-Output 'Adding Chocolatey to the profile. This will provide tab completion, refreshenv, etc.'
    $profileInstall | Out-File $profileFile -Append -Encoding (Get-FileEncoding $profileFile)
    Write-ChocolateyWarning 'Chocolatey profile installed. Reload your profile - type . $profile'

    if ($PSVersionTable.PSVersion.Major -lt 3) {
      Write-ChocolateyWarning "Tab completion does not currently work in PowerShell v2. `n Please upgrade to a more recent version of PowerShell to take advantage of tab completion."
      #Write-ChocolateyWarning "To load tab expansion, you need to install PowerTab. `n See https://powertab.codeplex.com/ for details."
    }

  } catch {
    Write-ChocolateyWarning "Unable to add Chocolatey to the profile. You will need to do it manually. Error was '$_'"
@'
This is how add the Chocolatey Profile manually.
Find your $profile. Then add the following lines to it:

$ChocolateyProfile = "$env:ChocolateyInstall\helpers\chocolateyProfile.psm1"
if (Test-Path($ChocolateyProfile)) {
  Import-Module "$ChocolateyProfile"
}
'@ | Write-Output
  }
}

$netFx4InstallTries = 0

function Install-DotNet4IfMissing {
param(
  $forceFxInstall = $false
)
  # we can't take advantage of any chocolatey module functions, because they
  # haven't been unpacked because they require .NET Framework 4.0

  Write-Debug "Install-DotNet4IfMissing called with `$forceFxInstall=$forceFxInstall"
  $NetFxArch = "Framework"
  if ([IntPtr]::Size -eq 8) {$NetFxArch="Framework64" }

  $NetFx4ClientUrl = 'https://download.microsoft.com/download/5/6/2/562A10F9-C9F4-4313-A044-9C94E0A8FAC8/dotNetFx40_Client_x86_x64.exe'
  $NetFx4FullUrl = 'https://download.microsoft.com/download/9/5/A/95A9616B-7A37-4AF6-BC36-D6EA96C8DAAE/dotNetFx40_Full_x86_x64.exe'
  $NetFx4Url = $NetFx4FullUrl
  $NetFx4Path = "$tempDir"
  $NetFx4InstallerFile = 'dotNetFx40_Full_x86_x64.exe'
  $NetFx4Installer = Join-Path $NetFx4Path $NetFx4InstallerFile

  if ((!(Test-Path "$env:SystemRoot\Microsoft.Net\$NetFxArch\v4.0.30319") -and !(Test-Path "C:\Windows\Microsoft.Net\$NetFxArch\v4.0.30319")) -or $forceFxInstall) {
    Write-Output "'$env:SystemRoot\Microsoft.Net\$NetFxArch\v4.0.30319' was not found or this is forced"
    if (!(Test-Path $NetFx4Path)) {
      Write-Output "Creating folder `'$NetFx4Path`'"
      $null = New-Item -Path "$NetFx4Path" -ItemType Directory
    }

    $netFx4InstallTries += 1

    if (!(Test-Path $NetFx4Installer)) {
      Write-Output "Downloading `'$NetFx4Url`' to `'$NetFx4Installer`' - the installer is 40+ MBs, so this could take a while on a slow connection."
      (New-Object Net.WebClient).DownloadFile("$NetFx4Url","$NetFx4Installer")
    }

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.WorkingDirectory = "$NetFx4Path"
    $psi.FileName = "$NetFx4InstallerFile"
    # https://msdn.microsoft.com/library/ee942965(v=VS.100).aspx#command_line_options
    # http://blogs.msdn.com/b/astebner/archive/2010/05/12/10011664.aspx
    # For the actual setup.exe (if you want to unpack first) - /repair /x86 /x64 /ia64 /parameterfolder Client /q /norestart
    $psi.Arguments = "/q /norestart /repair"

    Write-Output "Installing `'$NetFx4Installer`' - this may take awhile with no output."
    $s = [System.Diagnostics.Process]::Start($psi);
    $s.WaitForExit();
    if ($s.ExitCode -ne 0 -and $s.ExitCode -ne 3010) {
      if ($netFx4InstallTries -ge 2) {
        Write-ChocolateyError ".NET Framework install failed with exit code `'$($s.ExitCode)`'. `n This will cause the rest of the install to fail."
        throw "Error installing .NET Framework 4.0 (exit code $($s.ExitCode)). `n Please install the .NET Framework 4.0 manually and then try to install Chocolatey again. `n Download at `'$NetFx4Url`'"
      } else {
        Write-ChocolateyWarning "Try #$netFx4InstallTries of .NET framework install failed with exit code `'$($s.ExitCode)`'. Trying again."
        Install-DotNet4IfMissing $true
      }
    }
  }
}

function Invoke-Chocolatey-Initial {
  Write-Debug "Initializing Chocolatey files, etc by running Chocolatey..."

  try {
    $chocoInstallationFolder = Get-ChocolateyInstallFolder
    $chocoExe = Join-Path -Path $chocoInstallationFolder -ChildPath "choco.exe"
    & $chocoExe -v | Out-Null
    Write-Debug "Chocolatey execution completed successfully."
  } catch {
    Write-ChocolateyWarning "Unable to run Chocolately at this time.  It is likely that .Net Framework installation requires a system reboot"
  }
}

Export-ModuleMember -function Initialize-Chocolatey;
