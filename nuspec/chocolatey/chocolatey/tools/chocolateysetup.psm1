$thisScriptFolder = (Split-Path -Parent $MyInvocation.MyCommand.Definition)
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
    }
    catch {
        Write-Output "WARNING: $message"
    }
}

function  Write-ChocolateyError {
    param (
        [string]$message = ''
    )

    try {
        Write-Host "ERROR: $message" -ForegroundColor "Red" -ErrorAction "Stop"
    }
    catch {
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

    $shims = @("cpack.exe", "cver.exe", "chocolatey.exe", "cinst.exe", "clist.exe", "cpush.exe", "cuninst.exe", "cup.exe")

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
            c) Installs .net 4.8 if needed
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
        [Parameter(Mandatory = $false)][string]$chocolateyPath = ''
    )
    Write-Debug "Initialize-Chocolatey"

    $installModule = Join-Path $thisScriptFolder 'chocolateyInstall\helpers\chocolateyInstaller.psm1'
    Import-Module $installModule -Force

    Install-DotNet48IfMissing

    if ($chocolateyPath -eq '') {
        $programData = [Environment]::GetFolderPath("CommonApplicationData")
        $chocolateyPath = Join-Path "$programData" 'chocolatey'
    }

    # variable to allow insecure directory:
    $allowInsecureRootInstall = $false
    if ($env:ChocolateyAllowInsecureRootDirectory -eq 'true') {
        $allowInsecureRootInstall = $true
    }

    # if we have an already environment variable path, use it.
    $alreadyInitializedNugetPath = Get-ChocolateyInstallFolder
    if ($alreadyInitializedNugetPath -and $alreadyInitializedNugetPath -ne $chocolateyPath -and ($allowInsecureRootInstall -or $alreadyInitializedNugetPath -ne $defaultChocolateyPathOld)) {
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

    $yourPkgPath = [System.IO.Path]::Combine($chocolateyLibPath, "yourPackageName")
    @"
We are setting up the Chocolatey package repository.
The packages themselves go to `'$chocolateyLibPath`'
  (i.e. $yourPkgPath).
A shim file for the command line goes to `'$chocolateyExePath`'
  and points to an executable in `'$yourPkgPath`'.

Creating Chocolatey CLI folders if they do not already exist.

"@ | Write-Output

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

    $chocolateyExePathVariable = $chocolateyExePath.ToLower().Replace($chocolateyPath.ToLower(), "%DIR%..\").Replace("\\", "\")
    Initialize-ChocolateyPath $chocolateyExePath $chocolateyExePathVariable
    Process-ChocolateyBinFiles $chocolateyExePath $chocolateyExePathVariable

    $realModule = Join-Path $chocolateyPath "helpers\chocolateyInstaller.psm1"
    Import-Module "$realModule" -Force

    if (-not $allowInsecureRootInstall -and (Test-Path($defaultChocolateyPathOld))) {
        Upgrade-OldChocolateyInstall $defaultChocolateyPathOld $chocolateyPath
        Install-ChocolateyBinFiles $chocolateyPath $chocolateyExePath
    }

    Add-ChocolateyProfile
    Invoke-Chocolatey-Initial
    if ($env:ChocolateyExitCode -eq $null -or $env:ChocolateyExitCode -eq '') {
        $env:ChocolateyExitCode = 0
    }

    if ($script:DotNetInstallRequiredReboot) {
        @"
Chocolatey CLI (choco.exe) is nearly ready.
You need to restart this machine prior to using choco.
"@ | Write-Output
    } else {
        @"
Chocolatey CLI (choco.exe) is now ready.
You can call choco from anywhere, command line or powershell by typing choco.
Run choco /? for a list of functions.
You may need to shut down and restart powershell and/or consoles
 first prior to using choco.
"@ | Write-Output
    }

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
    }
    else {
        Write-ChocolateyWarning "Setting ChocolateyInstall Environment Variable on USER and not SYSTEM variables.`n  This is due to either non-administrator install OR the process you are running is not being run as an Administrator."
    }

    Write-Output "Creating $chocoInstallVariableName as an environment variable (targeting `'$environmentTarget`') `n  Setting $chocoInstallVariableName to `'$folder`'"
    Write-ChocolateyWarning "It's very likely you will need to close and reopen your shell `n  before you can use choco."
    Install-ChocolateyEnvironmentVariable -variableName "$chocoInstallVariableName" -variableValue "$folder" -variableType $environmentTarget
}

function Get-ChocolateyInstallFolder() {
    Write-Debug "Get-ChocolateyInstallFolder"
    [Environment]::GetEnvironmentVariable($chocoInstallVariableName)
}

function Create-DirectoryIfNotExists($folderName) {
    Write-Debug "Create-DirectoryIfNotExists"
    if (![System.IO.Directory]::Exists($folderName)) {
        [System.IO.Directory]::CreateDirectory($folderName) | Out-Null
    }
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
    }
    catch {
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
        }
        else {
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
    }
    catch {
        Write-ChocolateyWarning "Not able to set permissions for $folder."
    }
    $ErrorActionPreference = $currentEA
}

function Upgrade-OldChocolateyInstall {
    param(
        [string]$chocolateyPathOld = "$sysDrive\Chocolatey",
        [string]$chocolateyPath = "$($env:ALLUSERSPROFILE)\chocolatey"
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
                $updatedPath = [System.Text.RegularExpressions.Regex]::Replace($path, [System.Text.RegularExpressions.Regex]::Escape($chocolateyExePathOld) + '(?>;)?', '', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
                if ($updatedPath -ne $path) {
                    Write-Output "Updating `'$_`' PATH to reflect removal of '$chocolateyPathOld'."
                    try {
                        Set-EnvironmentVariable -Name 'Path' -Value $updatedPath -Scope $_ -ErrorAction Stop
                    }
                    catch {
                        Write-ChocolateyWarning "Was not able to remove the old environment variable from PATH. You will need to do this manually"
                    }
                }
            }

        Copy-Item "$chocolateyPathOld\lib\*" "$chocolateyPath\lib" -Force -Recurse

        $from = "$chocolateyPathOld\bin"
        $to = "$chocolateyPath\bin"
        # TODO: This exclusion list needs to be updated once shims are removed
        $exclude = @("choco.exe", "RefreshEnv.cmd")
        Get-ChildItem -Path $from -Recurse -Exclude $exclude |
            ForEach-Object {
                Write-Debug "Copying $_ `n to $to"
                if ($_.PSIsContainer) {
                    Copy-Item $_ -Destination (Join-Path $to $_.Parent.FullName.Substring($from.length)) -Force -ErrorAction SilentlyContinue
                }
                else {
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
                    Remove-Item $_.FullName -Exclude *.log -Recurse -Force -ErrorAction SilentlyContinue
                }
            }

            Write-Output "Attempting to remove `'$chocolateyPathOld`'. This may fail if something in the folder is being used or locked."
            Remove-Item "$($chocolateyPathOld)" -Force -Recurse -ErrorAction Stop
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
                    # If this is an upgrade, we can't *delete* Chocolatey.PowerShell.dll, as it will be currently loaded and thus locked.
                    # Instead, rename it with a .old suffix. The code in the installer module will delete the .old file next time it runs.
                    # This works similarly to how we move rather than overwriting choco.exe itself.
                    if ($_.Name -ne "Chocolatey.PowerShell.dll") {
                        Write-Debug "Removing $_ unless matches .log"
                        Remove-Item $_.FullName -Exclude *.log -Recurse -Force -ErrorAction SilentlyContinue
                    }
                    else {
                        $oldPath = "$($_.FullName).old"
                        Write-Debug "Moving $_ to $oldPath"
                        
                        # Remove any still-existing Chocolatey.PowerShell.dll.old files before moving/renaming the current one.
                        Get-Item -Path $oldPath -ErrorAction SilentlyContinue | Remove-Item -Force
                        Move-Item $_.Fullname -Destination $oldPath
                    }
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
            Remove-Item "$chocoExe.old" -Force -ErrorAction SilentlyContinue
            Move-Item $chocoExe "$chocoExe.old" -Force -ErrorAction SilentlyContinue
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
    Copy-Item $chocoExe $chocoExeDest -Force

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
                Where-Object { $_.name -match "^chocolatey.*nupkg" } |
                Sort-Object name -Descending |
                Select-Object -First 1
            if ($chocoPkg -ne '') {
                $chocoPkg = $chocoPkg.FullName
            }
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

    $exeFiles = Get-ChildItem "$redirectsPath" -Include @("*.exe", "*.cmd") -Recurse
    foreach ($exeFile in $exeFiles) {
        $exeFilePath = $exeFile.FullName
        $exeFileName = [System.IO.Path]::GetFileName("$exeFilePath")
        $binFilePath = Join-Path $chocolateyExePath $exeFileName
        $binFilePathRename = $binFilePath + '.old'
        $batchFilePath = $binFilePath.Replace(".exe", ".bat")
        $bashFilePath = $binFilePath.Replace(".exe", "")
        if (Test-Path ($batchFilePath)) {
            Remove-Item $batchFilePath -Force -ErrorAction SilentlyContinue
        }
        if (Test-Path ($bashFilePath)) {
            Remove-Item $bashFilePath -Force -ErrorAction SilentlyContinue
        }
        if (Test-Path ($binFilePathRename)) {
            try {
                Write-Debug "Attempting to remove $binFilePathRename"
                Remove-Item $binFilePathRename -Force -ErrorAction Stop
            }
            catch {
                Write-ChocolateyWarning "Was not able to remove `'$binFilePathRename`'. This may cause errors."
            }
        }
        if (Test-Path ($binFilePath)) {
            try {
                Write-Debug "Attempting to rename $binFilePath to $binFilePathRename"
                Move-Item -Path $binFilePath -Destination $binFilePathRename -Force -ErrorAction Stop
            }
            catch {
                Write-ChocolateyWarning "Was not able to rename `'$binFilePath`' to `'$binFilePathRename`'."
            }
        }

        try {
            Write-Debug "Attempting to copy $exeFilePath to $binFilePath"
            Copy-Item -Path $exeFilePath -Destination $binFilePath -Force -ErrorAction Stop
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
    }
    else {
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
    if (!(Test-Path $processedMarkerFile)) {
        $files = Get-ChildItem $chocolateyExePath -Include *.bat -Recurse
        if ($files -ne $null -and $files.Count -gt 0) {
            Write-Debug "Processing Bin files"
            foreach ($file in $files) {
                Write-Output "Processing $($file.Name) to make it portable"
                $fileStream = [System.IO.File]::Open("$file", 'Open', 'Read', 'ReadWrite')
                $reader = New-Object System.IO.StreamReader($fileStream)
                $fileText = $reader.ReadToEnd()
                $reader.Close()
                $fileStream.Close()

                $fileText = $fileText.ToLower().Replace("`"" + $chocolateyPath.ToLower(), "SET DIR=%~dp0%`n""%DIR%..\").Replace("\\", "\")

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

    if (!$bytes) {
        return 'utf8'
    }

    switch -regex ('{0:x2}{1:x2}{2:x2}{3:x2}' -f $bytes[0], $bytes[1], $bytes[2], $bytes[3]) {
        '^efbbbf' {
            return 'utf8'
        }
        '^2b2f76' {
            return 'utf7'
        }
        '^fffe' {
            return 'unicode'
        }
        '^feff' {
            return 'bigendianunicode'
        }
        '^0000feff' {
            return 'utf32'
        }
        default {
            return 'ascii'
        }
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
        if (Select-String -Path $profileFile -Pattern $chocoProfileSearch -Quiet -SimpleMatch) {
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
    }
    catch {
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

$netFx48InstallTries = 0

function Install-DotNet48IfMissing {
    param(
        $forceFxInstall = $false
    )
    # we can't take advantage of any chocolatey module functions, because they
    # haven't been unpacked because they require .NET Framework 4.8

    Write-Debug "Install-DotNet48IfMissing called with `$forceFxInstall=$forceFxInstall"
    $NetFxArch = "Framework"
    if ([IntPtr]::Size -eq 8) {
        $NetFxArch = "Framework64"
    }

    $NetFx48Url = 'https://download.visualstudio.microsoft.com/download/pr/2d6bb6b2-226a-4baa-bdec-798822606ff1/8494001c276a4b96804cde7829c04d7f/ndp48-x86-x64-allos-enu.exe'
    $NetFx48Path = "$tempDir"
    $NetFx48InstallerFile = 'ndp48-x86-x64-allos-enu.exe'
    $NetFx48Installer = Join-Path $NetFx48Path $NetFx48InstallerFile

    if (((Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -ErrorAction SilentlyContinue).Release -lt 528040) -or $forceFxInstall) {
        Write-Output "The registry key for .Net 4.8 was not found or this is forced"

        if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending") {
            Write-Warning "A reboot is required. `n If you encounter errors, reboot the system and attempt the operation again"
        }

        $netFx48InstallTries += 1

        if (!(Test-Path $NetFx48Installer)) {
            Write-Output "Downloading `'$NetFx48Url`' to `'$NetFx48Installer`' - the installer is 100+ MBs, so this could take a while on a slow connection."
            (New-Object Net.WebClient).DownloadFile("$NetFx48Url","$NetFx48Installer")
        }

        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.WorkingDirectory = "$NetFx48Path"
        $psi.FileName = "$NetFx48InstallerFile"
        # https://msdn.microsoft.com/library/ee942965(v=VS.100).aspx#command_line_options
        # http://blogs.msdn.com/b/astebner/archive/2010/05/12/10011664.aspx
        # For the actual setup.exe (if you want to unpack first) - /repair /x86 /x64 /ia64 /parameterfolder Client /q /norestart
        $psi.Arguments = "/q /norestart"

        Write-Output "Installing `'$NetFx48Installer`' - this may take awhile with no output."
        $s = [System.Diagnostics.Process]::Start($psi);
        $s.WaitForExit();
        if ($s.ExitCode -eq 1641 -or $s.ExitCode -eq 3010) {
          Write-Warning ".NET Framework 4.8 was installed, but a reboot is required before using Chocolatey CLI."
          $script:DotNetInstallRequiredReboot = $true
        } elseif ($s.ExitCode -ne 0) {
            if ($netFx48InstallTries -ge 2) {
                Write-ChocolateyError ".NET Framework install failed with exit code `'$($s.ExitCode)`'. `n This will cause the rest of the install to fail."
                throw "Error installing .NET Framework 4.8 (exit code $($s.ExitCode)). `n Please install the .NET Framework 4.8 manually and reboot the system `n and then try to install/upgrade Chocolatey again. `n Download at `'$NetFx48Url`'"
            } else {
                Write-ChocolateyWarning "Try #$netFx48InstallTries of .NET framework install failed with exit code `'$($s.ExitCode)`'. Trying again."
                Install-DotNet48IfMissing $true
            }
        }
    }
}

function Invoke-Chocolatey-Initial {
    if ($PSVersionTable.Major -le 3 -and $script:DotNetInstallRequiredReboot) {
        Write-Debug "Skipping initialization due to known issues before rebooting."
        return
    }

    Write-Debug "Initializing Chocolatey files, etc by running Chocolatey CLI..."

    try {
        $chocoInstallationFolder = Get-ChocolateyInstallFolder
        $chocoExe = Join-Path -Path $chocoInstallationFolder -ChildPath "choco.exe"
        $runResult = & $chocoExe -v *>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Debug "Chocolatey CLI execution completed successfully."
        }
        else {
            throw
        }
    }
    catch {
        Write-ChocolateyWarning "Unable to run Chocolatey CLI at this time:`n$($runResult)"
    }
}

Export-ModuleMember -Function Initialize-Chocolatey

# SIG # Begin signature block
# MIInKwYJKoZIhvcNAQcCoIInHDCCJxgCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCC8Wmtrs9N0S3FW
# cp2ebCXvAwP1tJVxI2dlqSwsrccSpKCCIK4wggWNMIIEdaADAgECAhAOmxiO+dAt
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
# Vzu0nAPthkX0tGFuv2jiJmCG6sivqf6UHedjGzqGVnhOMIIGwjCCBKqgAwIBAgIQ
# BUSv85SdCDmmv9s/X+VhFjANBgkqhkiG9w0BAQsFADBjMQswCQYDVQQGEwJVUzEX
# MBUGA1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRpZ2lDZXJ0IFRydXN0
# ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENBMB4XDTIzMDcxNDAw
# MDAwMFoXDTM0MTAxMzIzNTk1OVowSDELMAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRp
# Z2lDZXJ0LCBJbmMuMSAwHgYDVQQDExdEaWdpQ2VydCBUaW1lc3RhbXAgMjAyMzCC
# AiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBAKNTRYcdg45brD5UsyPgz5/X
# 5dLnXaEOCdwvSKOXejsqnGfcYhVYwamTEafNqrJq3RApih5iY2nTWJw1cb86l+uU
# UI8cIOrHmjsvlmbjaedp/lvD1isgHMGXlLSlUIHyz8sHpjBoyoNC2vx/CSSUpIIa
# 2mq62DvKXd4ZGIX7ReoNYWyd/nFexAaaPPDFLnkPG2ZS48jWPl/aQ9OE9dDH9kgt
# XkV1lnX+3RChG4PBuOZSlbVH13gpOWvgeFmX40QrStWVzu8IF+qCZE3/I+PKhu60
# pCFkcOvV5aDaY7Mu6QXuqvYk9R28mxyyt1/f8O52fTGZZUdVnUokL6wrl76f5P17
# cz4y7lI0+9S769SgLDSb495uZBkHNwGRDxy1Uc2qTGaDiGhiu7xBG3gZbeTZD+BY
# QfvYsSzhUa+0rRUGFOpiCBPTaR58ZE2dD9/O0V6MqqtQFcmzyrzXxDtoRKOlO0L9
# c33u3Qr/eTQQfqZcClhMAD6FaXXHg2TWdc2PEnZWpST618RrIbroHzSYLzrqawGw
# 9/sqhux7UjipmAmhcbJsca8+uG+W1eEQE/5hRwqM/vC2x9XH3mwk8L9CgsqgcT2c
# kpMEtGlwJw1Pt7U20clfCKRwo+wK8REuZODLIivK8SgTIUlRfgZm0zu++uuRONhR
# B8qUt+JQofM604qDy0B7AgMBAAGjggGLMIIBhzAOBgNVHQ8BAf8EBAMCB4AwDAYD
# VR0TAQH/BAIwADAWBgNVHSUBAf8EDDAKBggrBgEFBQcDCDAgBgNVHSAEGTAXMAgG
# BmeBDAEEAjALBglghkgBhv1sBwEwHwYDVR0jBBgwFoAUuhbZbU2FL3MpdpovdYxq
# II+eyG8wHQYDVR0OBBYEFKW27xPn783QZKHVVqllMaPe1eNJMFoGA1UdHwRTMFEw
# T6BNoEuGSWh0dHA6Ly9jcmwzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRH
# NFJTQTQwOTZTSEEyNTZUaW1lU3RhbXBpbmdDQS5jcmwwgZAGCCsGAQUFBwEBBIGD
# MIGAMCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdpY2VydC5jb20wWAYIKwYB
# BQUHMAKGTGh0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0
# ZWRHNFJTQTQwOTZTSEEyNTZUaW1lU3RhbXBpbmdDQS5jcnQwDQYJKoZIhvcNAQEL
# BQADggIBAIEa1t6gqbWYF7xwjU+KPGic2CX/yyzkzepdIpLsjCICqbjPgKjZ5+PF
# 7SaCinEvGN1Ott5s1+FgnCvt7T1IjrhrunxdvcJhN2hJd6PrkKoS1yeF844ektrC
# QDifXcigLiV4JZ0qBXqEKZi2V3mP2yZWK7Dzp703DNiYdk9WuVLCtp04qYHnbUFc
# jGnRuSvExnvPnPp44pMadqJpddNQ5EQSviANnqlE0PjlSXcIWiHFtM+YlRpUurm8
# wWkZus8W8oM3NG6wQSbd3lqXTzON1I13fXVFoaVYJmoDRd7ZULVQjK9WvUzF4UbF
# KNOt50MAcN7MmJ4ZiQPq1JE3701S88lgIcRWR+3aEUuMMsOI5ljitts++V+wQtaP
# 4xeR0arAVeOGv6wnLEHQmjNKqDbUuXKWfpd5OEhfysLcPTLfddY2Z1qJ+Panx+VP
# NTwAvb6cKmx5AdzaROY63jg7B145WPR8czFVoIARyxQMfq68/qTreWWqaNYiyjvr
# moI1VygWy2nyMpqy0tg6uLFGhmu6F/3Ed2wVbK6rr3M66ElGt9V/zLY4wNjsHPW2
# obhDLN9OTH0eaHDAdwrUAuBcYLso/zjlUlrWrBciI0707NMX+1Br/wd3H3GXREHJ
# uEbTbDJ8WC9nR2XlG3O2mflrLAZG70Ee8PBf4NvZrZCARK+AEEGKMIIG7TCCBNWg
# AwIBAgIQBNI793flHTneCMtwLiiYFTANBgkqhkiG9w0BAQsFADBpMQswCQYDVQQG
# EwJVUzEXMBUGA1UEChMORGlnaUNlcnQsIEluYy4xQTA/BgNVBAMTOERpZ2lDZXJ0
# IFRydXN0ZWQgRzQgQ29kZSBTaWduaW5nIFJTQTQwOTYgU0hBMzg0IDIwMjEgQ0Ex
# MB4XDTI0MDUwOTAwMDAwMFoXDTI3MDUxMTIzNTk1OVowdTELMAkGA1UEBhMCVVMx
# DzANBgNVBAgTBkthbnNhczEPMA0GA1UEBxMGVG9wZWthMSEwHwYDVQQKExhDaG9j
# b2xhdGV5IFNvZnR3YXJlLCBJbmMxITAfBgNVBAMTGENob2NvbGF0ZXkgU29mdHdh
# cmUsIEluYzCCAaIwDQYJKoZIhvcNAQEBBQADggGPADCCAYoCggGBAPDJgdZWj0RV
# lBBBniCyGy19FB736U5AahB+dAw3nmafOEeG+syql0m9kzV0gu4bSd4Al587ioAG
# DUPAGhXf0R+y11cx7c1cgdyxvfBvfMEkgD7sOUeF9ggZJc0YZ4qc7Pa6qqMpHDru
# pjshvLmQMSLaGKF68m+w2mJiZkLMYBEotPiAC3+IzI1MQqidCfN6rfQUmtcKyrVz
# 2zCt8CvuR3pSyNCBcQgKZ/+NwBfDqPTt1wKq5JCIQiLnbDZwJ9F5433enzgUGQgh
# KRoIwfp/hap7t7lrNf859Xe1/zHT4qtNgzGqSdJ2Kbz1YAMFjZokYHv/sliyxJN9
# 7++0BApX2t45JsQaqyQ60TSKxqOH0JIIDeYgwxfJ8YFmuvt7T4zVM8u02Axp/1YV
# nKP2AOVca6FDe9EiccrexAWPGoP+WQi8WFQKrNVKr5XTLI0MNTjadOHfF0XUToyF
# H8FVnZZV1/F1kgd/bYbt/0M/QkS4FGmJoqT8dyRyMkTlTynKul4N3QIDAQABo4IC
# AzCCAf8wHwYDVR0jBBgwFoAUaDfg67Y7+F8Rhvv+YXsIiGX0TkIwHQYDVR0OBBYE
# FFpfZUilS5A+fjYV80ib5qKkBoczMD4GA1UdIAQ3MDUwMwYGZ4EMAQQBMCkwJwYI
# KwYBBQUHAgEWG2h0dHA6Ly93d3cuZGlnaWNlcnQuY29tL0NQUzAOBgNVHQ8BAf8E
# BAMCB4AwEwYDVR0lBAwwCgYIKwYBBQUHAwMwgbUGA1UdHwSBrTCBqjBToFGgT4ZN
# aHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0VHJ1c3RlZEc0Q29kZVNp
# Z25pbmdSU0E0MDk2U0hBMzg0MjAyMUNBMS5jcmwwU6BRoE+GTWh0dHA6Ly9jcmw0
# LmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRHNENvZGVTaWduaW5nUlNBNDA5
# NlNIQTM4NDIwMjFDQTEuY3JsMIGUBggrBgEFBQcBAQSBhzCBhDAkBggrBgEFBQcw
# AYYYaHR0cDovL29jc3AuZGlnaWNlcnQuY29tMFwGCCsGAQUFBzAChlBodHRwOi8v
# Y2FjZXJ0cy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkRzRDb2RlU2lnbmlu
# Z1JTQTQwOTZTSEEzODQyMDIxQ0ExLmNydDAJBgNVHRMEAjAAMA0GCSqGSIb3DQEB
# CwUAA4ICAQAW9ANNkR2cF6ulbM+/XUWeWqC7UTqtsRwj7WAo8XTr52JebRchTGDH
# BZP9sDRZsFt+lPcPvBrv41kWoaFBmebTaPMh6YDHaON+uc19CTWXsMh8eog0lzGU
# iA3mKdbVit0udrgNlBUqTIuvMlMFIARWSz90FMeQrCFokLmqoqjp7u0sVPM7ng6T
# 9D8ct/m5LSpIa5TJCjAfyfw75GK0wzTDdTi1MgiAIyX0EedMrEwXjOjSApQ+uhIW
# v/AHDf8ukJzDFTTeiUkYZ1w++z70QZkzLfQTi6eH9vqgyXWcnGCwOxKquqe8RSIe
# M3FdtLstn9nI8S4qeiKdmomG6FAZTzYiGULJdJGsLh6Uii56zZdq3bSre/yrfed4
# hf/0MqEtWSU7LpkWM8AApRkIKRBZIQ73/7WxwsF9kHoZxqoRMDGTzWt+S7/XrSOa
# QbKf0CxdxMPHKC2A1u3xGNDChtQEwpHxYXf/teD7GeFYFQJg/wn4dC72mZze97+c
# YcpmI4R13Q7owmRthK1hnuq4EOQIcoTPbQXiaRzULbYrcOnJi7EbXcqdeAAnZAyV
# b6zGqAaE9Sw4RYvkosL5IlBgrdIwSFJMbeirBoM2GukIHQ8UaEu3l1PoNQvVbqM1
# 8zHiN4WA4rp9G9wfcAlZWq9iKF34sA+Xu03qSVaKPKn6YJMl5PfUsDGCBdMwggXP
# AgEBMH0waTELMAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRpZ2lDZXJ0LCBJbmMuMUEw
# PwYDVQQDEzhEaWdpQ2VydCBUcnVzdGVkIEc0IENvZGUgU2lnbmluZyBSU0E0MDk2
# IFNIQTM4NCAyMDIxIENBMQIQBNI793flHTneCMtwLiiYFTANBglghkgBZQMEAgEF
# AKCBhDAYBgorBgEEAYI3AgEMMQowCKACgAChAoAAMBkGCSqGSIb3DQEJAzEMBgor
# BgEEAYI3AgEEMBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMC8GCSqGSIb3
# DQEJBDEiBCDMHmRGjDIKOEla+IaM/zMWLkvV/rMxHg45my3wmHLVfDANBgkqhkiG
# 9w0BAQEFAASCAYB3TE6Lis+z/D5XQ11XK2+4We7m53DgAY7C0+CWG/yiJ7ImRdNF
# iJQV4TXHtT1fT3ke/ZXkZE95r+W6KPg1bsm8SBBEPm/8n4lgfcP00uw/rAcejuXD
# bVXrlrygICzuG+iocYgGcLL1+UxMr5mnNIye4mLMQkrluPSXf6+3jPZrIMCS8d7G
# V6zHEyiK1BVqfl0708/LHFQ3aPwI5z9xIZqTZFDjnDgFw5z2D1bq+wZY/tB04v1F
# pLIDxgLI7l++TJZS5yjxJP54W2ysevYamY6lj6pS+3x7DMohf4yiZ/v/gOAZxFiT
# ir0rkdzIjWc/wIdGaGg+AvQPisOvndFLUp3BzsmOqjC1QyA1zv7F4QD99FAw6QVb
# vl+zF9z8XahH5uYGoe/cZiVjO1W+it5/MhB/eyXUL6vV95pUjD/mackEDBOPHD85
# bvd2Z695m+l47/7W9lDw2OFeRIxIDn8p2e/7MC3u2DeUy7wKjLy430SOzgVPYvzj
# fiuYyp2COsJOTumhggMgMIIDHAYJKoZIhvcNAQkGMYIDDTCCAwkCAQEwdzBjMQsw
# CQYDVQQGEwJVUzEXMBUGA1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRp
# Z2lDZXJ0IFRydXN0ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENB
# AhAFRK/zlJ0IOaa/2z9f5WEWMA0GCWCGSAFlAwQCAQUAoGkwGAYJKoZIhvcNAQkD
# MQsGCSqGSIb3DQEHATAcBgkqhkiG9w0BCQUxDxcNMjQwNTI0MDcwNjM4WjAvBgkq
# hkiG9w0BCQQxIgQgDWxmHrnUlxZntijseYmN7DudgquOHVAn9iBTIT0OrJ4wDQYJ
# KoZIhvcNAQEBBQAEggIAoJiyvNMSQc/zxvIMthnw6kZ/3Lx6rywEFkPcdYsaAttW
# G4YytqV8fAmcmOgh2Og73ZGgwlIgFrIzAAtSKoRq/gIYFjri6c9/MapIYUzJEznB
# IV1c+/0f4RsU+Al+t+S0fbsov9g9I42ukS4rCVY0fYz+2TH6cd9teBNPm/Q6h1Uu
# Dy1q8LX5mn310mjaqcQJ8clDRmxYpTlI9nmsk6f+KNGMib85iiWC6fqpLGhP/M5k
# D/MDdJFXm2R//TWmCLTTG8ZcVEtf+TzH5EQWdfGTc/8cMnYL8eiqJ6saftIBuEb+
# Pl9XUaC4aha4Ekq9Pfn0/6eMk9TGId8PlnBiWygRlgaVhVFwXjzQ3x2EZi3Ba25q
# Qd45EmEMRZU0LJ+/enM9N5S1ZqJksy08M+f1tMpl91J38nzQqxap6obQ8kXzCFGJ
# kFDDLNVgwFw9KdvJ8UE++V5F72tgKv5K2ZScjG14id0xi/HAjpL7GIwHgEFaiDPZ
# ysWT8Kt/Aq+VbyUYHrhJqxekpwmd1DTAkd2GeVfj/kaIZuPGpmAQTdamecSQBjMp
# WMITKQH3GKGh+RvQHqJ6h88Xuwhcf7A4yFE9qiGb45a72Glg9xwePOoMctE1K8uE
# BsbbYdjHpjf+edum40GZM/22OIoOOyZXUP73ZCl5egZ+QPEgMBnYJVD3SPjJ/6s=
# SIG # End signature block
