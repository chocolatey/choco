$thisScriptFolder = (Split-Path -parent $MyInvocation.MyCommand.Definition)
$chocoInstallVariableName = "ChocolateyInstall"
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

function Remove-ShimWithAuthenticodeSignature {
  param (
    [string] $filePath
  )
  if (!(Test-Path $filePath)) {
    return
  }

  $signature = Get-AuthenticodeSignature $filePath -ErrorAction SilentlyContinue

  if (!$signature -or !$signature.SignerCertificate) {
    Write-ChocolateyWarning "Shim found in $filePath, but was not signed. Ignoring removal..."
    return
  }

  $possibleSignatures = @(
    'RealDimensions Software, LLC'
    'Chocolatey Software, Inc\.'
    'Chocolatey Software, Inc'
  )

  $possibleSignatures | ForEach-Object {
    if ($signature.SignerCertificate.Subject -match "$_") {
      Write-Output "Removing shim $filePath"
      $null = Remove-Item "$filePath"

      if (Test-Path "$filePath.ignore") {
        $null = Remove-Item "$filePath.ignore"
      }

      if (Test-Path "$filePath.old") {
        $null = Remove-Item "$filePath.old"
      }
    }
  }

  # This means the file was found, however did not get removed as it contained a authenticode signature that
  # is not ours.
  if (Test-Path $filePath) {
    Write-ChocolateyWarning "Shim found in $filePath, but did not match our signature. Ignoring removal..."
    return
  }
}

function Remove-UnsupportedShimFiles {
  param([string[]]$Paths)

  $shims = @("cpack.exe", "cver.exe")

  $Paths | ForEach-Object {
    $path = $_
    $shims | ForEach-Object { Join-Path $path $_ } | Where-Object { Test-Path $_ } | ForEach-Object {
      $shimPath = $_
      Write-Debug "Removing shim from '$shimPath'."

      try {
        Remove-ShimWithAuthenticodeSignature -filePath $shimPath
      }
      catch {
        Write-ChocolateyWarning "Unable to remove '$shimPath'. Please remove the file manually."
      }
    }
  }
}

function Initialize-Chocolatey {
<#
  .DESCRIPTION
    This will initialize the Chocolatey tool by
      a) setting up the "chocolateyPath" (the location where all chocolatey nuget packages will be installed)
      b) Installs chocolatey into the "chocolateyPath"
            c) Installs .net 4.0 if needed
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

  $possibleShimPaths = @(
    Join-Path "$chocolateyPath" "redirects"
    Join-Path "$thisScriptFolder" "chocolateyInstall\redirects"
  )
  Remove-UnsupportedShimFiles -Paths $possibleShimPaths

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

  Remove-UnsupportedShimFiles -Paths $chocolateyExePath
}

function Set-ChocolateyInstallFolder {
param(
  [string]$folder
)
  Write-Debug "Set-ChocolateyInstallFolder"

  $environmentTarget = [System.EnvironmentVariableTarget]::User
  # removing old variable
  Install-ChocolateyEnvironmentVariable -variableName "$chocoInstallVariableName" -variableValue $null -variableType $environmentTarget
  if (Test-ProcessAdminRights) {
    Write-Debug "Administrator installing so using Machine environment variable target instead of User."
    $environmentTarget = [System.EnvironmentVariableTarget]::Machine
    # removing old variable
    Install-ChocolateyEnvironmentVariable -variableName "$chocoInstallVariableName" -variableValue $null -variableType $environmentTarget
  } else {
    Write-ChocolateyWarning "Setting ChocolateyInstall Environment Variable on USER and not SYSTEM variables.`n  This is due to either non-administrator install OR the process you are running is not being run as an Administrator."
  }

  Write-Output "Creating $chocoInstallVariableName as an environment variable (targeting `'$environmentTarget`') `n  Setting $chocoInstallVariableName to `'$folder`'"
  Write-ChocolateyWarning "It's very likely you will need to close and reopen your shell `n  before you can use choco."
  Install-ChocolateyEnvironmentVariable -variableName "$chocoInstallVariableName" -variableValue "$folder" -variableType $environmentTarget
}

function Get-ChocolateyInstallFolder(){
  Write-Debug "Get-ChocolateyInstallFolder"
  [Environment]::GetEnvironmentVariable($chocoInstallVariableName)
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
    $acl = Get-Acl $folder

    Write-Debug "Removing existing permissions."
    $acl.Access | ForEach-Object { $acl.RemoveAccessRuleAll($_) }

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
    Set-Acl -Path $folder -AclObject $acl

    # set an explicit append permission on the logs folder
    Write-Debug "Allow users to append to log files."
    $logsFolder = "$folder\logs"
    Create-DirectoryIfNotExists $logsFolder
    $logsAcl = Get-Acl $logsFolder
    $usersAppendAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($builtinUsers, $rightsWrite, [Security.AccessControl.InheritanceFlags]::ObjectInherit, [Security.AccessControl.PropagationFlags]::InheritOnly, "Allow")
    $logsAcl.SetAccessRule($usersAppendAccessRule)
    $logsAcl.SetAccessRuleProtection($false, $true)
    Set-Acl -Path $logsFolder -AclObject $logsAcl
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
    ForEach-Object {
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
    # TODO: This exclusion list needs to be updated once shims are removed
    $exclude = @("choco.exe", "chocolatey.exe", "cinst.exe", "clist.exe", "cpush.exe", "cuninst.exe", "cup.exe", "RefreshEnv.cmd")
    Get-ChildItem -Path $from -recurse -Exclude $exclude |
      ForEach-Object {
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
      Get-ChildItem -Path "$chocolateyPathOld" | ForEach-Object {
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
  "$chocolateyPath\chocolateyInstall", "$chocolateyPath\helpers", "$chocolateyPath\redirects", "$chocolateyPath\tools" | ForEach-Object {
    #Write-Debug "Checking path $_"

    if (Test-Path $_) {
      Get-ChildItem -Path "$_" | ForEach-Object {
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
  $chocoInstallFolder = Join-Path $thisScriptFolder "chocolateyInstall"
  $chocoExe = Join-Path $chocoInstallFolder 'choco.exe'
  $chocoExeDest = Join-Path $chocolateyPath 'choco.exe'
  Copy-Item $chocoExe $chocoExeDest -force

  Write-Debug "Copying the contents of `'$chocoInstallFolder`' to `'$chocolateyPath`'."
  Copy-Item $chocoInstallFolder\* $chocolateyPath -Recurse -Force
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
    $chocoZipFile = Join-Path $tempDir "chocolatey\chocoInstall\chocolatey.zip"

    Write-Debug "First the zip file at '$chocoZipFile'."
    Write-Debug "Then from a neighboring chocolatey.*nupkg file '$thisScriptFolder/../../'."

    if (Test-Path("$chocoZipFile")) {
      Write-Debug "Copying '$chocoZipFile' to '$chocoPkgDirectory\chocolatey.nupkg'."
      Copy-Item "$chocoZipFile" "$chocoPkgDirectory\chocolatey.nupkg" -Force -ErrorAction SilentlyContinue
    }

    if (!(Test-Path("$chocoPkgDirectory\chocolatey.nupkg"))) {
      $chocoPkg = Get-ChildItem "$thisScriptFolder/../../" | 
        Where-Object {$_.name -match "^chocolatey.*nupkg" } | 
        Sort-Object name -Descending | 
        Select-Object -First 1
      if ($chocoPkg -ne '') { $chocoPkg = $chocoPkg.FullName }
      "$chocoZipFile", "$chocoPkg" | ForEach-Object {
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
  [string]$chocolateyExePathVariable = "%$($chocoInstallVariableName)%\bin"
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
  [string]$chocolateyExePathVariable = "%$($chocoInstallVariableName)%\bin"
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
    if ($PSVersionTable.PSVersion.Major -lt 6) {
      Write-Debug "Detected Powershell version < 6 ; Using -Encoding byte parameter"
      $bytes = [byte[]](Get-Content $Path -Encoding byte -ReadCount 4 -TotalCount 4)
    }
    else {
      Write-Debug "Detected Powershell version >= 6 ; Using -AsByteStream parameter"
      $bytes = [byte[]](Get-Content $Path -AsByteStream -ReadCount 4 -TotalCount 4)
    }

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

    # Check authenticode, but only if file is greater than 5 bytes
    $profileFileInfo = New-Object System.IO.FileInfo($profileFile)
    if ($profileFileInfo.Length -gt 5) {
      $signature = Get-AuthenticodeSignature $profile
      if ($signature.Status -ne 'NotSigned') {
        Write-Warning "Not setting tab completion: File is Authenticode signed at '$profile'."
        return
      }
    }

    $profileInstall = @'

# Import the Chocolatey Profile that contains the necessary code to enable
# tab-completions to function for `choco`.
# Be aware that if you are missing these lines from your profile, tab completion
# for `choco` will not function.
# See https://ch0.co/tab-completion for details.
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
    Write-ChocolateyWarning "Unable to run Chocolatey at this time.  It is likely that .Net Framework installation requires a system reboot"
  }
}

Export-ModuleMember -function Initialize-Chocolatey;

# SIG # Begin signature block
# MIInJQYJKoZIhvcNAQcCoIInFjCCJxICAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCC4NJHRAbm1O5Sz
# PHmfw+WqRms4eWssOEn2uHg9GTx8H6CCIKgwggWNMIIEdaADAgECAhAOmxiO+dAt
# 5+/bUOIIQBhaMA0GCSqGSIb3DQEBDAUAMGUxCzAJBgNVBAYTAlVTMRUwEwYDVQQK
# EwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xJDAiBgNV
# BAMTG0RpZ2lDZXJ0IEFzc3VyZWQgSUQgUm9vdCBDQTAeFw0yMjA4MDEwMDAwMDBa
# Fw0zMTExMDkyMzU5NTlaMGIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2Vy
# dCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xITAfBgNVBAMTGERpZ2lD
# ZXJ0IFRydXN0ZWQgUm9vdCBHNDCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoC
# ggIBAL/mkHNo3rvkXUo8MCIwaTPswqclLskhPfKK2FnC4SmnPVirdprNrnsbhA3E
# MB/zG6Q4FutWxpdtHauyefLKEdLkX9YFPFIPUh/GnhWlfr6fqVcWWVVyr2iTcMKy
# unWZanMylNEQRBAu34LzB4TmdDttceItDBvuINXJIB1jKS3O7F5OyJP4IWGbNOsF
# xl7sWxq868nPzaw0QF+xembud8hIqGZXV59UWI4MK7dPpzDZVu7Ke13jrclPXuU1
# 5zHL2pNe3I6PgNq2kZhAkHnDeMe2scS1ahg4AxCN2NQ3pC4FfYj1gj4QkXCrVYJB
# MtfbBHMqbpEBfCFM1LyuGwN1XXhm2ToxRJozQL8I11pJpMLmqaBn3aQnvKFPObUR
# WBf3JFxGj2T3wWmIdph2PVldQnaHiZdpekjw4KISG2aadMreSx7nDmOu5tTvkpI6
# nj3cAORFJYm2mkQZK37AlLTSYW3rM9nF30sEAMx9HJXDj/chsrIRt7t/8tWMcCxB
# YKqxYxhElRp2Yn72gLD76GSmM9GJB+G9t+ZDpBi4pncB4Q+UDCEdslQpJYls5Q5S
# UUd0viastkF13nqsX40/ybzTQRESW+UQUOsxxcpyFiIJ33xMdT9j7CFfxCBRa2+x
# q4aLT8LWRV+dIPyhHsXAj6KxfgommfXkaS+YHS312amyHeUbAgMBAAGjggE6MIIB
# NjAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBTs1+OC0nFdZEzfLmc/57qYrhwP
# TzAfBgNVHSMEGDAWgBRF66Kv9JLLgjEtUYunpyGd823IDzAOBgNVHQ8BAf8EBAMC
# AYYweQYIKwYBBQUHAQEEbTBrMCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdp
# Y2VydC5jb20wQwYIKwYBBQUHMAKGN2h0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNv
# bS9EaWdpQ2VydEFzc3VyZWRJRFJvb3RDQS5jcnQwRQYDVR0fBD4wPDA6oDigNoY0
# aHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElEUm9vdENB
# LmNybDARBgNVHSAECjAIMAYGBFUdIAAwDQYJKoZIhvcNAQEMBQADggEBAHCgv0Nc
# Vec4X6CjdBs9thbX979XB72arKGHLOyFXqkauyL4hxppVCLtpIh3bb0aFPQTSnov
# Lbc47/T/gLn4offyct4kvFIDyE7QKt76LVbP+fT3rDB6mouyXtTP0UNEm0Mh65Zy
# oUi0mcudT6cGAxN3J0TU53/oWajwvy8LpunyNDzs9wPHh6jSTEAZNUZqaVSwuKFW
# juyk1T3osdz9HNj0d1pcVIxv76FQPfx2CWiEn2/K2yCNNWAcAgPLILCsWKAOQGPF
# mCLBsln1VWvPJ6tsds5vIy30fnFqI2si/xK4VC0nftg62fC2h5b9W9FcrBjDTZ9z
# twGpn1eqXijiuZQwggauMIIElqADAgECAhAHNje3JFR82Ees/ShmKl5bMA0GCSqG
# SIb3DQEBCwUAMGIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMx
# GTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xITAfBgNVBAMTGERpZ2lDZXJ0IFRy
# dXN0ZWQgUm9vdCBHNDAeFw0yMjAzMjMwMDAwMDBaFw0zNzAzMjIyMzU5NTlaMGMx
# CzAJBgNVBAYTAlVTMRcwFQYDVQQKEw5EaWdpQ2VydCwgSW5jLjE7MDkGA1UEAxMy
# RGlnaUNlcnQgVHJ1c3RlZCBHNCBSU0E0MDk2IFNIQTI1NiBUaW1lU3RhbXBpbmcg
# Q0EwggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAwggIKAoICAQDGhjUGSbPBPXJJUVXH
# JQPE8pE3qZdRodbSg9GeTKJtoLDMg/la9hGhRBVCX6SI82j6ffOciQt/nR+eDzMf
# UBMLJnOWbfhXqAJ9/UO0hNoR8XOxs+4rgISKIhjf69o9xBd/qxkrPkLcZ47qUT3w
# 1lbU5ygt69OxtXXnHwZljZQp09nsad/ZkIdGAHvbREGJ3HxqV3rwN3mfXazL6IRk
# tFLydkf3YYMZ3V+0VAshaG43IbtArF+y3kp9zvU5EmfvDqVjbOSmxR3NNg1c1eYb
# qMFkdECnwHLFuk4fsbVYTXn+149zk6wsOeKlSNbwsDETqVcplicu9Yemj052FVUm
# cJgmf6AaRyBD40NjgHt1biclkJg6OBGz9vae5jtb7IHeIhTZgirHkr+g3uM+onP6
# 5x9abJTyUpURK1h0QCirc0PO30qhHGs4xSnzyqqWc0Jon7ZGs506o9UD4L/wojzK
# QtwYSH8UNM/STKvvmz3+DrhkKvp1KCRB7UK/BZxmSVJQ9FHzNklNiyDSLFc1eSuo
# 80VgvCONWPfcYd6T/jnA+bIwpUzX6ZhKWD7TA4j+s4/TXkt2ElGTyYwMO1uKIqjB
# Jgj5FBASA31fI7tk42PgpuE+9sJ0sj8eCXbsq11GdeJgo1gJASgADoRU7s7pXche
# MBK9Rp6103a50g5rmQzSM7TNsQIDAQABo4IBXTCCAVkwEgYDVR0TAQH/BAgwBgEB
# /wIBADAdBgNVHQ4EFgQUuhbZbU2FL3MpdpovdYxqII+eyG8wHwYDVR0jBBgwFoAU
# 7NfjgtJxXWRM3y5nP+e6mK4cD08wDgYDVR0PAQH/BAQDAgGGMBMGA1UdJQQMMAoG
# CCsGAQUFBwMIMHcGCCsGAQUFBwEBBGswaTAkBggrBgEFBQcwAYYYaHR0cDovL29j
# c3AuZGlnaWNlcnQuY29tMEEGCCsGAQUFBzAChjVodHRwOi8vY2FjZXJ0cy5kaWdp
# Y2VydC5jb20vRGlnaUNlcnRUcnVzdGVkUm9vdEc0LmNydDBDBgNVHR8EPDA6MDig
# NqA0hjJodHRwOi8vY3JsMy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkUm9v
# dEc0LmNybDAgBgNVHSAEGTAXMAgGBmeBDAEEAjALBglghkgBhv1sBwEwDQYJKoZI
# hvcNAQELBQADggIBAH1ZjsCTtm+YqUQiAX5m1tghQuGwGC4QTRPPMFPOvxj7x1Bd
# 4ksp+3CKDaopafxpwc8dB+k+YMjYC+VcW9dth/qEICU0MWfNthKWb8RQTGIdDAiC
# qBa9qVbPFXONASIlzpVpP0d3+3J0FNf/q0+KLHqrhc1DX+1gtqpPkWaeLJ7giqzl
# /Yy8ZCaHbJK9nXzQcAp876i8dU+6WvepELJd6f8oVInw1YpxdmXazPByoyP6wCeC
# RK6ZJxurJB4mwbfeKuv2nrF5mYGjVoarCkXJ38SNoOeY+/umnXKvxMfBwWpx2cYT
# gAnEtp/Nh4cku0+jSbl3ZpHxcpzpSwJSpzd+k1OsOx0ISQ+UzTl63f8lY5knLD0/
# a6fxZsNBzU+2QJshIUDQtxMkzdwdeDrknq3lNHGS1yZr5Dhzq6YBT70/O3itTK37
# xJV77QpfMzmHQXh6OOmc4d0j/R0o08f56PGYX/sr2H7yRp11LB4nLCbbbxV7HhmL
# NriT1ObyF5lZynDwN7+YAN8gFk8n+2BnFqFmut1VwDophrCYoCvtlUG3OtUVmDG0
# YgkPCr2B2RP+v6TR81fZvAT6gt4y3wSJ8ADNXcL50CN/AAvkdgIm2fBldkKmKYcJ
# RyvmfxqkhQ/8mJb2VVQrH4D6wPIOK+XW+6kvRBVK5xMOHds3OBqhK/bt1nz8MIIG
# sDCCBJigAwIBAgIQCK1AsmDSnEyfXs2pvZOu2TANBgkqhkiG9w0BAQwFADBiMQsw
# CQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5jMRkwFwYDVQQLExB3d3cu
# ZGlnaWNlcnQuY29tMSEwHwYDVQQDExhEaWdpQ2VydCBUcnVzdGVkIFJvb3QgRzQw
# HhcNMjEwNDI5MDAwMDAwWhcNMzYwNDI4MjM1OTU5WjBpMQswCQYDVQQGEwJVUzEX
# MBUGA1UEChMORGlnaUNlcnQsIEluYy4xQTA/BgNVBAMTOERpZ2lDZXJ0IFRydXN0
# ZWQgRzQgQ29kZSBTaWduaW5nIFJTQTQwOTYgU0hBMzg0IDIwMjEgQ0ExMIICIjAN
# BgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA1bQvQtAorXi3XdU5WRuxiEL1M4zr
# PYGXcMW7xIUmMJ+kjmjYXPXrNCQH4UtP03hD9BfXHtr50tVnGlJPDqFX/IiZwZHM
# gQM+TXAkZLON4gh9NH1MgFcSa0OamfLFOx/y78tHWhOmTLMBICXzENOLsvsI8Irg
# nQnAZaf6mIBJNYc9URnokCF4RS6hnyzhGMIazMXuk0lwQjKP+8bqHPNlaJGiTUyC
# EUhSaN4QvRRXXegYE2XFf7JPhSxIpFaENdb5LpyqABXRN/4aBpTCfMjqGzLmysL0
# p6MDDnSlrzm2q2AS4+jWufcx4dyt5Big2MEjR0ezoQ9uo6ttmAaDG7dqZy3SvUQa
# khCBj7A7CdfHmzJawv9qYFSLScGT7eG0XOBv6yb5jNWy+TgQ5urOkfW+0/tvk2E0
# XLyTRSiDNipmKF+wc86LJiUGsoPUXPYVGUztYuBeM/Lo6OwKp7ADK5GyNnm+960I
# HnWmZcy740hQ83eRGv7bUKJGyGFYmPV8AhY8gyitOYbs1LcNU9D4R+Z1MI3sMJN2
# FKZbS110YU0/EpF23r9Yy3IQKUHw1cVtJnZoEUETWJrcJisB9IlNWdt4z4FKPkBH
# X8mBUHOFECMhWWCKZFTBzCEa6DgZfGYczXg4RTCZT/9jT0y7qg0IU0F8WD1Hs/q2
# 7IwyCQLMbDwMVhECAwEAAaOCAVkwggFVMBIGA1UdEwEB/wQIMAYBAf8CAQAwHQYD
# VR0OBBYEFGg34Ou2O/hfEYb7/mF7CIhl9E5CMB8GA1UdIwQYMBaAFOzX44LScV1k
# TN8uZz/nupiuHA9PMA4GA1UdDwEB/wQEAwIBhjATBgNVHSUEDDAKBggrBgEFBQcD
# AzB3BggrBgEFBQcBAQRrMGkwJAYIKwYBBQUHMAGGGGh0dHA6Ly9vY3NwLmRpZ2lj
# ZXJ0LmNvbTBBBggrBgEFBQcwAoY1aHR0cDovL2NhY2VydHMuZGlnaWNlcnQuY29t
# L0RpZ2lDZXJ0VHJ1c3RlZFJvb3RHNC5jcnQwQwYDVR0fBDwwOjA4oDagNIYyaHR0
# cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0VHJ1c3RlZFJvb3RHNC5jcmww
# HAYDVR0gBBUwEzAHBgVngQwBAzAIBgZngQwBBAEwDQYJKoZIhvcNAQEMBQADggIB
# ADojRD2NCHbuj7w6mdNW4AIapfhINPMstuZ0ZveUcrEAyq9sMCcTEp6QRJ9L/Z6j
# fCbVN7w6XUhtldU/SfQnuxaBRVD9nL22heB2fjdxyyL3WqqQz/WTauPrINHVUHmI
# moqKwba9oUgYftzYgBoRGRjNYZmBVvbJ43bnxOQbX0P4PpT/djk9ntSZz0rdKOtf
# JqGVWEjVGv7XJz/9kNF2ht0csGBc8w2o7uCJob054ThO2m67Np375SFTWsPK6Wrx
# oj7bQ7gzyE84FJKZ9d3OVG3ZXQIUH0AzfAPilbLCIXVzUstG2MQ0HKKlS43Nb3Y3
# LIU/Gs4m6Ri+kAewQ3+ViCCCcPDMyu/9KTVcH4k4Vfc3iosJocsL6TEa/y4ZXDlx
# 4b6cpwoG1iZnt5LmTl/eeqxJzy6kdJKt2zyknIYf48FWGysj/4+16oh7cGvmoLr9
# Oj9FpsToFpFSi0HASIRLlk2rREDjjfAVKM7t8RhWByovEMQMCGQ8M4+uKIw8y4+I
# Cw2/O/TOHnuO77Xry7fwdxPm5yg/rBKupS8ibEH5glwVZsxsDsrFhsP2JjMMB0ug
# 0wcCampAMEhLNKhRILutG4UI4lkNbcoFUCvqShyepf2gpx8GdOfy1lKQ/a+FSCH5
# Vzu0nAPthkX0tGFuv2jiJmCG6sivqf6UHedjGzqGVnhOMIIGvDCCBKSgAwIBAgIQ
# C65mvFq6f5WHxvnpBOMzBDANBgkqhkiG9w0BAQsFADBjMQswCQYDVQQGEwJVUzEX
# MBUGA1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRpZ2lDZXJ0IFRydXN0
# ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENBMB4XDTI0MDkyNjAw
# MDAwMFoXDTM1MTEyNTIzNTk1OVowQjELMAkGA1UEBhMCVVMxETAPBgNVBAoTCERp
# Z2lDZXJ0MSAwHgYDVQQDExdEaWdpQ2VydCBUaW1lc3RhbXAgMjAyNDCCAiIwDQYJ
# KoZIhvcNAQEBBQADggIPADCCAgoCggIBAL5qc5/2lSGrljC6W23mWaO16P2RHxjE
# iDtqmeOlwf0KMCBDEr4IxHRGd7+L660x5XltSVhhK64zi9CeC9B6lUdXM0s71EOc
# Re8+CEJp+3R2O8oo76EO7o5tLuslxdr9Qq82aKcpA9O//X6QE+AcaU/byaCagLD/
# GLoUb35SfWHh43rOH3bpLEx7pZ7avVnpUVmPvkxT8c2a2yC0WMp8hMu60tZR0Cha
# V76Nhnj37DEYTX9ReNZ8hIOYe4jl7/r419CvEYVIrH6sN00yx49boUuumF9i2T8U
# uKGn9966fR5X6kgXj3o5WHhHVO+NBikDO0mlUh902wS/Eeh8F/UFaRp1z5SnROHw
# SJ+QQRZ1fisD8UTVDSupWJNstVkiqLq+ISTdEjJKGjVfIcsgA4l9cbk8Smlzddh4
# EfvFrpVNnes4c16Jidj5XiPVdsn5n10jxmGpxoMc6iPkoaDhi6JjHd5ibfdp5uzI
# Xp4P0wXkgNs+CO/CacBqU0R4k+8h6gYldp4FCMgrXdKWfM4N0u25OEAuEa3Jyidx
# W48jwBqIJqImd93NRxvd1aepSeNeREXAu2xUDEW8aqzFQDYmr9ZONuc2MhTMizch
# NULpUEoA6Vva7b1XCB+1rxvbKmLqfY/M/SdV6mwWTyeVy5Z/JkvMFpnQy5wR14GJ
# cv6dQ4aEKOX5AgMBAAGjggGLMIIBhzAOBgNVHQ8BAf8EBAMCB4AwDAYDVR0TAQH/
# BAIwADAWBgNVHSUBAf8EDDAKBggrBgEFBQcDCDAgBgNVHSAEGTAXMAgGBmeBDAEE
# AjALBglghkgBhv1sBwEwHwYDVR0jBBgwFoAUuhbZbU2FL3MpdpovdYxqII+eyG8w
# HQYDVR0OBBYEFJ9XLAN3DigVkGalY17uT5IfdqBbMFoGA1UdHwRTMFEwT6BNoEuG
# SWh0dHA6Ly9jcmwzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRHNFJTQTQw
# OTZTSEEyNTZUaW1lU3RhbXBpbmdDQS5jcmwwgZAGCCsGAQUFBwEBBIGDMIGAMCQG
# CCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdpY2VydC5jb20wWAYIKwYBBQUHMAKG
# TGh0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRHNFJT
# QTQwOTZTSEEyNTZUaW1lU3RhbXBpbmdDQS5jcnQwDQYJKoZIhvcNAQELBQADggIB
# AD2tHh92mVvjOIQSR9lDkfYR25tOCB3RKE/P09x7gUsmXqt40ouRl3lj+8QioVYq
# 3igpwrPvBmZdrlWBb0HvqT00nFSXgmUrDKNSQqGTdpjHsPy+LaalTW0qVjvUBhcH
# zBMutB6HzeledbDCzFzUy34VarPnvIWrqVogK0qM8gJhh/+qDEAIdO/KkYesLyTV
# OoJ4eTq7gj9UFAL1UruJKlTnCVaM2UeUUW/8z3fvjxhN6hdT98Vr2FYlCS7Mbb4H
# v5swO+aAXxWUm3WpByXtgVQxiBlTVYzqfLDbe9PpBKDBfk+rabTFDZXoUke7zPgt
# d7/fvWTlCs30VAGEsshJmLbJ6ZbQ/xll/HjO9JbNVekBv2Tgem+mLptR7yIrpaid
# RJXrI+UzB6vAlk/8a1u7cIqV0yef4uaZFORNekUgQHTqddmsPCEIYQP7xGxZBIhd
# mm4bhYsVA6G2WgNFYagLDBzpmk9104WQzYuVNsxyoVLObhx3RugaEGru+SojW4dH
# PoWrUhftNpFC5H7QEY7MhKRyrBe7ucykW7eaCuWBsBb4HOKRFVDcrZgdwaSIqMDi
# CLg4D+TPVgKx2EgEdeoHNHT9l3ZDBD+XgbF+23/zBjeCtxz+dL/9NWR6P2eZRi7z
# cEO1xwcdcqJsyz/JceENc2Sg8h3KeFUCS7tpFk7CrDqkMIIG7TCCBNWgAwIBAgIQ
# BNI793flHTneCMtwLiiYFTANBgkqhkiG9w0BAQsFADBpMQswCQYDVQQGEwJVUzEX
# MBUGA1UEChMORGlnaUNlcnQsIEluYy4xQTA/BgNVBAMTOERpZ2lDZXJ0IFRydXN0
# ZWQgRzQgQ29kZSBTaWduaW5nIFJTQTQwOTYgU0hBMzg0IDIwMjEgQ0ExMB4XDTI0
# MDUwOTAwMDAwMFoXDTI3MDUxMTIzNTk1OVowdTELMAkGA1UEBhMCVVMxDzANBgNV
# BAgTBkthbnNhczEPMA0GA1UEBxMGVG9wZWthMSEwHwYDVQQKExhDaG9jb2xhdGV5
# IFNvZnR3YXJlLCBJbmMxITAfBgNVBAMTGENob2NvbGF0ZXkgU29mdHdhcmUsIElu
# YzCCAaIwDQYJKoZIhvcNAQEBBQADggGPADCCAYoCggGBAPDJgdZWj0RVlBBBniCy
# Gy19FB736U5AahB+dAw3nmafOEeG+syql0m9kzV0gu4bSd4Al587ioAGDUPAGhXf
# 0R+y11cx7c1cgdyxvfBvfMEkgD7sOUeF9ggZJc0YZ4qc7Pa6qqMpHDrupjshvLmQ
# MSLaGKF68m+w2mJiZkLMYBEotPiAC3+IzI1MQqidCfN6rfQUmtcKyrVz2zCt8Cvu
# R3pSyNCBcQgKZ/+NwBfDqPTt1wKq5JCIQiLnbDZwJ9F5433enzgUGQghKRoIwfp/
# hap7t7lrNf859Xe1/zHT4qtNgzGqSdJ2Kbz1YAMFjZokYHv/sliyxJN97++0BApX
# 2t45JsQaqyQ60TSKxqOH0JIIDeYgwxfJ8YFmuvt7T4zVM8u02Axp/1YVnKP2AOVc
# a6FDe9EiccrexAWPGoP+WQi8WFQKrNVKr5XTLI0MNTjadOHfF0XUToyFH8FVnZZV
# 1/F1kgd/bYbt/0M/QkS4FGmJoqT8dyRyMkTlTynKul4N3QIDAQABo4ICAzCCAf8w
# HwYDVR0jBBgwFoAUaDfg67Y7+F8Rhvv+YXsIiGX0TkIwHQYDVR0OBBYEFFpfZUil
# S5A+fjYV80ib5qKkBoczMD4GA1UdIAQ3MDUwMwYGZ4EMAQQBMCkwJwYIKwYBBQUH
# AgEWG2h0dHA6Ly93d3cuZGlnaWNlcnQuY29tL0NQUzAOBgNVHQ8BAf8EBAMCB4Aw
# EwYDVR0lBAwwCgYIKwYBBQUHAwMwgbUGA1UdHwSBrTCBqjBToFGgT4ZNaHR0cDov
# L2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0VHJ1c3RlZEc0Q29kZVNpZ25pbmdS
# U0E0MDk2U0hBMzg0MjAyMUNBMS5jcmwwU6BRoE+GTWh0dHA6Ly9jcmw0LmRpZ2lj
# ZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRHNENvZGVTaWduaW5nUlNBNDA5NlNIQTM4
# NDIwMjFDQTEuY3JsMIGUBggrBgEFBQcBAQSBhzCBhDAkBggrBgEFBQcwAYYYaHR0
# cDovL29jc3AuZGlnaWNlcnQuY29tMFwGCCsGAQUFBzAChlBodHRwOi8vY2FjZXJ0
# cy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkRzRDb2RlU2lnbmluZ1JTQTQw
# OTZTSEEzODQyMDIxQ0ExLmNydDAJBgNVHRMEAjAAMA0GCSqGSIb3DQEBCwUAA4IC
# AQAW9ANNkR2cF6ulbM+/XUWeWqC7UTqtsRwj7WAo8XTr52JebRchTGDHBZP9sDRZ
# sFt+lPcPvBrv41kWoaFBmebTaPMh6YDHaON+uc19CTWXsMh8eog0lzGUiA3mKdbV
# it0udrgNlBUqTIuvMlMFIARWSz90FMeQrCFokLmqoqjp7u0sVPM7ng6T9D8ct/m5
# LSpIa5TJCjAfyfw75GK0wzTDdTi1MgiAIyX0EedMrEwXjOjSApQ+uhIWv/AHDf8u
# kJzDFTTeiUkYZ1w++z70QZkzLfQTi6eH9vqgyXWcnGCwOxKquqe8RSIeM3FdtLst
# n9nI8S4qeiKdmomG6FAZTzYiGULJdJGsLh6Uii56zZdq3bSre/yrfed4hf/0MqEt
# WSU7LpkWM8AApRkIKRBZIQ73/7WxwsF9kHoZxqoRMDGTzWt+S7/XrSOaQbKf0Cxd
# xMPHKC2A1u3xGNDChtQEwpHxYXf/teD7GeFYFQJg/wn4dC72mZze97+cYcpmI4R1
# 3Q7owmRthK1hnuq4EOQIcoTPbQXiaRzULbYrcOnJi7EbXcqdeAAnZAyVb6zGqAaE
# 9Sw4RYvkosL5IlBgrdIwSFJMbeirBoM2GukIHQ8UaEu3l1PoNQvVbqM18zHiN4WA
# 4rp9G9wfcAlZWq9iKF34sA+Xu03qSVaKPKn6YJMl5PfUsDGCBdMwggXPAgEBMH0w
# aTELMAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRpZ2lDZXJ0LCBJbmMuMUEwPwYDVQQD
# EzhEaWdpQ2VydCBUcnVzdGVkIEc0IENvZGUgU2lnbmluZyBSU0E0MDk2IFNIQTM4
# NCAyMDIxIENBMQIQBNI793flHTneCMtwLiiYFTANBglghkgBZQMEAgEFAKCBhDAY
# BgorBgEEAYI3AgEMMQowCKACgAChAoAAMBkGCSqGSIb3DQEJAzEMBgorBgEEAYI3
# AgEEMBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMC8GCSqGSIb3DQEJBDEi
# BCBGNG0jmM9Z6su/NJxqRQburc1Pzit3Pw4Sw4dWurdqBzANBgkqhkiG9w0BAQEF
# AASCAYDNsfuiq4B37ND7g7bS8mC9WWqIMIILvGNCXBl/PnBhxsTa59YZVArVbtmg
# INuABwM0tUayw2hWyTO5At1U0NibDkqV6xtyIt2DVLuosiVJYk5/ErbS9A/GJi9+
# +36WUlfzWT6lUiFqm5Mh9taDy9CqKhQO8dfowaYeP99d2YuZg++ZxtXK2aGvilxh
# cEGqooHyo3HZgdw8f//SE8zpH7xsBl76UXerm1yCLDGkPTDKQXv2TQQ+YAa4xy8f
# LMKlDPZSAACxspjlhO5AbPLFkMWr60p0HKpW7q4ftG0Wvyzr6nGWzhLEmSA2P+Lo
# aYKI64QNGT0z9yqNaBaF8YFqdXChXFK5zcboO7fl1bUdjVf9SwI1lL4RI7m9egDC
# lNu/hfNHGBlM7NG+kAzE3q4U2Uhm4mZqFKGBJc3i85eAsVcBxfYkjr2l0kCuhgEG
# Sg8zsMHzVILrtv3zbEORnQgaYz47GzvIhYrQH1esBnU5TrSV+iN8EoB/xfI5yX0v
# 20MPePahggMgMIIDHAYJKoZIhvcNAQkGMYIDDTCCAwkCAQEwdzBjMQswCQYDVQQG
# EwJVUzEXMBUGA1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRpZ2lDZXJ0
# IFRydXN0ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENBAhALrma8
# Wrp/lYfG+ekE4zMEMA0GCWCGSAFlAwQCAQUAoGkwGAYJKoZIhvcNAQkDMQsGCSqG
# SIb3DQEHATAcBgkqhkiG9w0BCQUxDxcNMjQxMTI5MTczOTA2WjAvBgkqhkiG9w0B
# CQQxIgQgGtSJBzDFxFzD3a7bXwIJopeFAF4az8TX8SvkUK0GVYcwDQYJKoZIhvcN
# AQEBBQAEggIAprW/HK34glJZomOgl4O3h6f4Ys1rH2LFgz0jJqkDBd4tWxE/riwh
# 27UfjrgHckjElLfEZpOGUflhdD83WBXA3ZezQWDFlV9W71DUbgbLHSLk/Q8Vnh/L
# dZ4OvcXRRzxIThXkqWjYN18L04Tx7VIGyssKA638fs0piry6kG/mgONphXQlcYWX
# sxFDHyAe46VchCcvJ6FdbceOTM/3KO1Z2vLqrP1Qln06BWSHwbfvRW/4tkjyC8hv
# z1+2joXqVpDU0BqtUViGkoWAtUHirX/NAScR1NwNfFmMfYfG51LHWhjoUNUhYAjr
# 14UFY65oVFfCWtV68q50k891Q+Yeveibl/n3+Wj4WgCxsPruwJi92djwmNgbJvwD
# NgdsNngLRX6m3MC6CGreJhGquxQ9LXt4ofAiSecv9CzgymFrb2bLuHIGpy4p+qDd
# /KGVADe4Vz7zRDukeUNcaVyTKtAoNk//H8qqQjEPW/0s/dktWyyUU5DUUVLVP30q
# e9pNF5j3J1jyl5iknPmepZyUu2xAq5GfqNhhr8izur1pjTw63Vt7geXk7hqCqFKj
# mU1cCZVCrzeVx+wvYkOOo1kUtSZ1TWtw1o7XQV7gX1JcLHl9AhUOIZ6tyVq3cZaQ
# Y2sYQZHc4LhM+myUU6fVoQMPNKmqnRFMEfx+KLOivIq+EQIWkiIxDQ0=
# SIG # End signature block
