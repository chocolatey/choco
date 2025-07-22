$script:thisScriptFolder = Split-Path -Parent $MyInvocation.MyCommand.Definition
$script:chocoInstallVariableName = "ChocolateyInstall"
$script:tempDir = $env:TEMP
$script:insecureRootInstallPath = "$env:SystemDrive\Chocolatey"
$script:netFx48InstallTries = 0

function Write-ChocolateyWarning {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]
        $Message
    )

    try {
        Write-Host "WARNING: $Message" -ForegroundColor Yellow -ErrorAction Stop
    }
    catch {
        Write-Output "WARNING: $Message"
    }
}

function  Write-ChocolateyError {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]
        $Message
    )

    try {
        Write-Host "ERROR: $Message" -ForegroundColor Red -ErrorAction Stop
    }
    catch {
        Write-Output "ERROR: $Message"
    }
}

function Write-ChocolateyInfo {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline = $true)]
        [string]
        $Message
    )
    process {
        try {
            Write-Host $Message -ErrorAction Stop
        }
        catch {
            Write-Output $Message
        }
    }
}

function Remove-SignedShim {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]
        $Path
    )

    if (-not (Test-Path $Path)) {
        return
    }

    $signature = Get-AuthenticodeSignature $Path -ErrorAction SilentlyContinue

    if (-not ($signature -and $signature.SignerCertificate)) {
        Write-ChocolateyWarning "Shim found in $Path, but was not signed. Ignoring removal..."
        return
    }

    $possibleSignaturesPattern = @(
        'RealDimensions Software, LLC'
        'Chocolatey Software, Inc\.?'
    ) -join '|'

    if ($signature.SignerCertificate.Subject -notmatch $possibleSignaturesPattern) {
        # This means the file was found, however did not get removed as it contained a authenticode signature that
        # is not ours.
        Write-ChocolateyWarning "Shim found in $Path, but will not be removed as it has an unexpected signature"
        return
    }

    Write-ChocolateyInfo "Removing shim $Path"
    $null = Remove-Item $Path

    foreach ($file in "$Path.ignore", "$Path.old") {
        Remove-Item $file
    }
}

function Remove-UnsupportedShim {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string[]]
        $Path
    )

    $shims = @("cpack.exe", "cver.exe", "chocolatey.exe", "cinst.exe", "clist.exe", "cpush.exe", "cuninst.exe", "cup.exe")

    foreach ($item in $Path) {
        foreach ($exe in $shims) {
            $shimPath = Join-Path -Path $item -ChildPath $exe

            if (Test-Path $shimPath) {
                Write-Debug "Attempting to remove shim from '$shimPath'."
                try {
                    Remove-SignedShim -Path $shimPath
                }
                catch {
                    Write-ChocolateyWarning "Unable to remove '$shimPath'. Please remove the file manually."
                }
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
        C:\PS> Initialize-Chocolatey -Path "D:\ChocolateyInstalledNuGets\"

        Installs chocolatey into the custom directory D:\ChocolateyInstalledNuGets\

#>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]
        $Path
    )
    Write-Debug "Initialize-Chocolatey"

    $installModule = Join-Path $script:thisScriptFolder 'chocolateyInstall\helpers\chocolateyInstaller.psm1'
    Import-Module $installModule -Force

    Install-DotNet48IfMissing

    $chocolateyPath = if ($Path) {
        $Path
    }
    else {
        $programData = [Environment]::GetFolderPath("CommonApplicationData")
        Join-Path -Path $programData -ChildPath 'chocolatey'
    }

    # variable to allow insecure directory:
    $allowInsecureRootInstall = $env:ChocolateyAllowInsecureRootDirectory -eq 'true'

    # if we have an already environment variable path, use it.
    $alreadyInitializedNugetPath = Get-ChocolateyInstallFolder

    $useCustomInstallPath = $alreadyInitializedNugetPath -and
        $alreadyInitializedNugetPath -ne $chocolateyPath -and
        ($allowInsecureRootInstall -or $alreadyInitializedNugetPath -ne $script:insecureRootInstallPath)
    if ($useCustomInstallPath) {
        $chocolateyPath = $alreadyInitializedNugetPath
    }
    else {
        Set-ChocolateyInstallEnv $chocolateyPath
    }
    Initialize-Directory -Path $chocolateyPath
    Set-Permissions $chocolateyPath

    #set up variables to add
    $chocolateyExePath = Join-Path -Path $chocolateyPath -ChildPath 'bin'
    $chocolateyLibPath = Join-Path -Path $chocolateyPath -ChildPath 'lib'

    if (-not $script:tempDir) {
        $script:tempDir = Join-Path -Path $chocolateyPath -ChildPath 'temp'
        Initialize-Directory -Path $script:tempDir
    }

    $yourPkgPath = Join-Path -Path $chocolateyLibPath -ChildPath "yourPackageName"
    @"
We are setting up the Chocolatey package repository.
The packages themselves go to '$chocolateyLibPath'
  (i.e. $yourPkgPath).
A shim file for the command line goes to '$chocolateyExePath'
  and points to an executable in '$yourPkgPath'.

Creating Chocolatey CLI folders if they do not already exist.

"@ | Write-ChocolateyInfo

    #create the base structure if it doesn't exist
    Initialize-Directory -Path $chocolateyExePath
    Initialize-Directory -Path $chocolateyLibPath

    $possibleShimPaths = @(
        Join-Path -Path $chocolateyPath -ChildPath "redirects"
        Join-Path -Path $script:thisScriptFolder -ChildPath "chocolateyInstall\redirects"
    )
    Remove-UnsupportedShim -Path $possibleShimPaths

    Install-ChocolateyFiles -Path $chocolateyPath
    Initialize-ChocolateyLibFolder -Path $chocolateyLibPath

    Install-ChocolateyBinFiles -Path $chocolateyPath -ExecutablePath $chocolateyExePath

    $chocolateyExePathVariable = $chocolateyExePath.ToLower().Replace($chocolateyPath.ToLower(), "%DIR%..\").Replace("\\", "\")
    Initialize-ChocolateyPath $chocolateyExePath $chocolateyExePathVariable
    Initialize-ChocolateyBatchFiles $chocolateyExePath $chocolateyExePathVariable

    $realModule = Join-Path $chocolateyPath "helpers\chocolateyInstaller.psm1"
    Import-Module "$realModule" -Force

    Add-ChocolateyProfile
    Invoke-ChocolateyFirstRun
    if (-not $env:ChocolateyExitCode) {
        $env:ChocolateyExitCode = 0
    }

    if ($script:DotNetInstallRequiredReboot) {
        @"
Chocolatey CLI (choco.exe) is nearly ready.
You need to restart this machine prior to using choco.
"@ | Write-ChocolateyInfo
    } else {
        @"
Chocolatey CLI (choco.exe) is now ready.
You can call choco from anywhere, command line or PowerShell by typing choco.
Run choco /? for a list of functions.
You may need to shut down and restart PowerShell and/or consoles
 first prior to using choco.
"@ | Write-ChocolateyInfo
    }

    Remove-UnsupportedShim -Path $chocolateyExePath
}

function Set-ChocolateyInstallEnv {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]
        $Path
    )
    Write-Debug "Set-ChocolateyInstallEnv"

    $environmentTarget = [System.EnvironmentVariableTarget]::User
    # removing old variable
    Install-ChocolateyEnvironmentVariable -variableName "$script:chocoInstallVariableName" -variableValue $null -variableType $environmentTarget
    if (Test-ProcessAdminRights) {
        Write-Debug "Administrator installing so using Machine environment variable target instead of User."
        $environmentTarget = [System.EnvironmentVariableTarget]::Machine
        # removing old variable
        Install-ChocolateyEnvironmentVariable -variableName "$script:chocoInstallVariableName" -variableValue $null -variableType $environmentTarget
    }
    else {
        Write-ChocolateyWarning "Setting ChocolateyInstall Environment Variable on USER and not SYSTEM variables.`n  This is due to either non-administrator install OR the process you are running is not being run as an Administrator."
    }

    Write-ChocolateyInfo "Creating $script:chocoInstallVariableName as an environment variable (targeting '$environmentTarget') `n  Setting $script:chocoInstallVariableName to '$Path'"
    Write-ChocolateyWarning "It's very likely you will need to close and reopen your shell `n  before you can use choco."
    Install-ChocolateyEnvironmentVariable -variableName "$script:chocoInstallVariableName" -variableValue "$Path" -variableType $environmentTarget
}

function Get-ChocolateyInstallFolder {
    [CmdletBinding()]
    param()

    Write-Debug "Get-ChocolateyInstallFolder"
    [Environment]::GetEnvironmentVariable($script:chocoInstallVariableName)
}

function Initialize-Directory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]
        $Path
    )

    Write-Debug "Initialize-Directory"
    if (-not (Test-Path $Path)) {
        New-Item -Path $Path -ItemType Directory | Out-Null
    }
}

function Get-LocalizedWellKnownPrincipalName {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [Security.Principal.WellKnownSidType]
        $WellKnownSidType
    )
    $sid = New-Object -TypeName 'System.Security.Principal.SecurityIdentifier' -ArgumentList @($WellKnownSidType, $null)
    $account = $sid.Translate([Security.Principal.NTAccount])

    return $account.Value
}

function Set-Permissions {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]
        $Path
    )

    Write-Debug "Set-Permissions"

    $defaultInstallPath = "$env:SystemDrive\ProgramData\chocolatey"
    try {
        $defaultInstallPath = Join-Path -Path ([Environment]::GetFolderPath("CommonApplicationData")) -ChildPath 'chocolatey'
    }
    catch {
        # keep first setting
    }

    if ($Path.ToLower() -ne $defaultInstallPath.ToLower()) {
        Write-ChocolateyWarning "Installation folder is not the default. Not changing permissions. Please ensure your installation is secure."
        return
    }

    # Everything from here on out applies to the default installation folder

    if (-not (Test-ProcessAdminRights)) {
        throw "Installation of Chocolatey to default folder requires Administrative permissions. Please run from elevated prompt. Please see https://chocolatey.org/install for details and alternatives if needing to install as a non-administrator."
    }

    $currentEA = $ErrorActionPreference
    $ErrorActionPreference = 'Stop'
    try {
        # get current acl
        $acl = Get-Acl $Path

        Write-Debug "Removing existing permissions."
        $acl.Access | ForEach-Object { $acl.RemoveAccessRuleAll($_) }

        $inheritanceFlags = ([Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit)
        $propagationFlags = [Security.AccessControl.PropagationFlags]::None

        $rightsFullControl = [Security.AccessControl.FileSystemRights]::FullControl
        $rightsModify = [Security.AccessControl.FileSystemRights]::Modify
        $rightsReadExecute = [Security.AccessControl.FileSystemRights]::ReadAndExecute
        $rightsWrite = [Security.AccessControl.FileSystemRights]::Write

        Write-ChocolateyInfo "Restricting write permissions to Administrators"
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
        $logsFolder = "$Path\logs"
        Initialize-Directory -Path $logsFolder
        $logsAcl = Get-Acl $logsFolder
        $usersAppendAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($builtinUsers, $rightsWrite, [Security.AccessControl.InheritanceFlags]::ObjectInherit, [Security.AccessControl.PropagationFlags]::InheritOnly, "Allow")
        $logsAcl.SetAccessRule($usersAppendAccessRule)
        $logsAcl.SetAccessRuleProtection($false, $true)
        Set-Acl -Path $logsFolder -AclObject $logsAcl
    }
    catch {
        Write-ChocolateyWarning "Not able to set permissions for $Path."
    }
    finally {
        $ErrorActionPreference = $currentEA
    }
}

function Install-ChocolateyFiles {
    param(
        [string]$Path
    )
    Write-Debug "Install-ChocolateyFiles"

    Write-Debug "Removing install files in chocolateyInstall, helpers, redirects, and tools"
    "$Path\chocolateyInstall", "$Path\helpers", "$Path\redirects", "$Path\tools" | ForEach-Object {
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
                        Move-Item -Path $_.Fullname -Destination $oldPath
                    }
                }
            }
        }
    }

    Write-Debug "Attempting to move choco.exe to choco.exe.old so we can place the new version here."
    # rename the currently running process / it will be locked if it exists
    $chocoExe = Join-Path -Path $Path -ChildPath 'choco.exe'
    if (Test-Path ($chocoExe)) {
        Write-Debug "Renaming '$chocoExe' to '$chocoExe.old'"
        try {
            Remove-Item -Path "$chocoExe.old" -Force -ErrorAction SilentlyContinue
            Move-Item -Path $chocoExe -Destination "$chocoExe.old" -Force -ErrorAction SilentlyContinue
        }
        catch {
            Write-ChocolateyWarning "Was not able to rename '$chocoExe' to '$chocoExe.old'."
        }
    }

    # remove pdb file if it is found
    $chocoPdb = Join-Path -Path $Path -ChildPath 'choco.pdb'
    if (Test-Path ($chocoPdb)) {
        Remove-Item "$chocoPdb" -Force -ErrorAction SilentlyContinue
    }

    Write-Debug "Unpacking files required for Chocolatey."
    $chocoInstallFolder = Join-Path $script:thisScriptFolder "chocolateyInstall"
    $chocoExe = Join-Path $chocoInstallFolder 'choco.exe'
    $chocoExeDest = Join-Path $Path 'choco.exe'
    Copy-Item -Path $chocoExe -Destination $chocoExeDest -Force

    Write-Debug "Copying the contents of '$chocoInstallFolder' to '$Path'."
    Copy-Item -Path "$chocoInstallFolder\*" -Destination $Path -Recurse -Force
}

function Initialize-ChocolateyLibFolder {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]
        $Path
    )

    Write-Debug "Initialize-ChocolateyLibFolder"
    $chocoPkgDirectory = Join-Path -Path $Path -ChildPath 'chocolatey'

    Initialize-Directory -Path $chocoPkgDirectory

    if (-not (Test-Path "$chocoPkgDirectory\chocolatey.nupkg")) {
        Write-ChocolateyInfo "chocolatey.nupkg file not installed in lib.`n Attempting to locate it from bootstrapper."
        $chocoZipFile = Join-Path -Path $script:tempDir -ChildPath "chocolatey\chocoInstall\chocolatey.zip"

        Write-Debug "First the zip file at '$chocoZipFile'."
        Write-Debug "Then from a neighboring chocolatey.*nupkg file '$script:thisScriptFolder/../../'."

        if (Test-Path "$chocoZipFile") {
            Write-Debug "Copying '$chocoZipFile' to '$chocoPkgDirectory\chocolatey.nupkg'."
            Copy-Item "$chocoZipFile" "$chocoPkgDirectory\chocolatey.nupkg" -Force -ErrorAction SilentlyContinue
        }

        if (-not (Test-Path "$chocoPkgDirectory\chocolatey.nupkg")) {
            $chocoPkg = Get-ChildItem "$script:thisScriptFolder/../../" |
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
        [string] $Path,
        [string] $ExecutablePath
    )
    Write-Debug "Install-ChocolateyBinFiles"
    Write-Debug "Installing the bin file redirects"
    $redirectsPath = Join-Path -Path $Path -ChildPath 'redirects'
    if (-not (Test-Path $redirectsPath)) {
        Write-ChocolateyWarning "$redirectsPath does not exist"
        return
    }

    $exeFiles = Get-ChildItem "$redirectsPath" -Include @("*.exe", "*.cmd") -Recurse
    foreach ($exeFile in $exeFiles) {
        $exeFilePath = $exeFile.FullName
        $exeFileName = [System.IO.Path]::GetFileName($exeFilePath)
        $binFilePath = Join-Path $ExecutablePath $exeFileName
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
                Write-ChocolateyWarning "Was not able to remove '$binFilePathRename'. This may cause errors."
            }
        }
        if (Test-Path ($binFilePath)) {
            try {
                Write-Debug "Attempting to rename $binFilePath to $binFilePathRename"
                Move-Item -Path $binFilePath -Destination $binFilePathRename -Force -ErrorAction Stop
            }
            catch {
                Write-ChocolateyWarning "Was not able to rename '$binFilePath' to '$binFilePathRename'."
            }
        }

        try {
            Write-Debug "Attempting to copy $exeFilePath to $binFilePath"
            Copy-Item -Path $exeFilePath -Destination $binFilePath -Force -ErrorAction Stop
        }
        catch {
            Write-ChocolateyWarning "Was not able to replace '$binFilePath' with '$exeFilePath'. You may need to do this manually."
        }

        $commandShortcut = [System.IO.Path]::GetFileNameWithoutExtension("$exeFilePath")
        Write-Debug "Added command $commandShortcut"
    }
}

function Initialize-ChocolateyPath {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]
        $chocolateyExePath = "$($env:ALLUSERSPROFILE)\chocolatey\bin",

        [Parameter()]
        [string]
        $chocolateyExePathVariable = "%$($script:chocoInstallVariableName)%\bin"
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

function Initialize-ChocolateyBatchFiles {
    param(
        [string]$chocolateyExePath = "$($env:ALLUSERSPROFILE)\chocolatey\bin",
        [string]$chocolateyExePathVariable = "%$($script:chocoInstallVariableName)%\bin"
    )
    Write-Debug "Initialize-ChocolateyBatchFiles"
    $processedMarkerFile = Join-Path $chocolateyExePath '_processed.txt'
    if (-not (Test-Path $processedMarkerFile)) {
        $files = Get-ChildItem $chocolateyExePath -Include *.bat -Recurse
        if ($files -and $files.Count -gt 0) {
            Write-Debug "Processing Bin files"
            foreach ($file in $files) {
                Write-ChocolateyInfo "Processing $($file.Name) to make it portable"
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
function Get-FileEncoding {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]
        $Path
    )
    if ($PSVersionTable.PSVersion.Major -lt 6) {
        Write-Debug "Detected Powershell version < 6 ; Using -Encoding byte parameter"
        $bytes = [byte[]](Get-Content $Path -Encoding byte -ReadCount 4 -TotalCount 4)
    }
    else {
        Write-Debug "Detected Powershell version >= 6 ; Using -AsByteStream parameter"
        $bytes = [byte[]](Get-Content $Path -AsByteStream -ReadCount 4 -TotalCount 4)
    }

    if (-not $bytes) {
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
        if (-not $profileFile) {
            Write-ChocolateyInfo 'Not setting tab completion: Profile variable ($profile) resulted in an empty string.'
            return
        }

        $profileDirectory = Split-Path -Parent $profileFile

        if ($env:ChocolateyNoProfile) {
            Write-Warning "Not setting tab completion: Environment variable `"ChocolateyNoProfile`" exists and is set."
            return
        }

        $localSystem = Get-LocalizedWellKnownPrincipalName -WellKnownSidType ([Security.Principal.WellKnownSidType]::LocalSystemSid)
        # get current user
        $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
        if ($currentUser.Name -eq $localSystem) {
            Write-Warning "Not setting tab completion: Current user is SYSTEM user."
            return
        }

        if (-not (Test-Path $profileDirectory)) {
            Write-Debug "Creating '$profileDirectory'"
            New-Item "$profileDirectory" -Type Directory -Force -ErrorAction SilentlyContinue | Out-Null
        }

        if (-not (Test-Path $profileFile)) {
            Write-Warning "Not setting tab completion: Profile file does not exist at '$profileFile'."
            return

            #Write-Debug "Creating '$profileFile'"
            #"" | Out-File $profileFile -Encoding UTF8
        }

        # Check authenticode, but only if file is greater than 5 bytes
        $profileFileInfo = Get-Item -Path $profileFile
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
if (Test-Path $ChocolateyProfile) {
  Import-Module $ChocolateyProfile
}
'@

        $chocoProfileSearch = '$ChocolateyProfile'
        if (Select-String -Path $profileFile -Pattern $chocoProfileSearch -Quiet -SimpleMatch) {
            Write-Debug "Chocolatey profile is already installed."
            return
        }

        Write-ChocolateyInfo 'Adding Chocolatey to the profile. This will provide tab completion, refreshenv, etc.'
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
'@ | Write-ChocolateyInfo
    }
}

function Install-DotNet48IfMissing {
    [CmdletBinding()]
    param(
        [Parameter()]
        [switch]
        $Force
    )
    # we can't take advantage of any chocolatey module functions, because they
    # haven't been unpacked because they require .NET Framework 4.8

    Write-Debug "Install-DotNet48IfMissing called with `$Force=$Force"

    $NetFx48Url = 'https://download.visualstudio.microsoft.com/download/pr/2d6bb6b2-226a-4baa-bdec-798822606ff1/8494001c276a4b96804cde7829c04d7f/ndp48-x86-x64-allos-enu.exe'
    $NetFx48Path = $script:tempDir
    $NetFx48InstallerFile = 'ndp48-x86-x64-allos-enu.exe'
    $NetFx48Installer = Join-Path -Path $NetFx48Path -ChildPath $NetFx48InstallerFile

    if ($Force -or (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -ErrorAction SilentlyContinue).Release -lt 528040) {
        Write-ChocolateyInfo "The registry key for .Net 4.8 was not found or this is forced"

        if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending") {
            Write-Warning "A reboot is required. `n If you encounter errors, reboot the system and attempt the operation again"
        }

        $script:netFx48InstallTries += 1

        if (-not (Test-Path $NetFx48Installer)) {
            Write-ChocolateyInfo "Downloading '$NetFx48Url' to '$NetFx48Installer' - the installer is 100+ MBs, so this could take a while on a slow connection."
            (New-Object Net.WebClient).DownloadFile($NetFx48Url, $NetFx48Installer)
        }

        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.WorkingDirectory = "$NetFx48Path"
        $psi.FileName = "$NetFx48InstallerFile"
        # https://msdn.microsoft.com/library/ee942965(v=VS.100).aspx#command_line_options
        # http://blogs.msdn.com/b/astebner/archive/2010/05/12/10011664.aspx
        # For the actual setup.exe (if you want to unpack first) - /repair /x86 /x64 /ia64 /parameterfolder Client /q /norestart
        $psi.Arguments = "/q /norestart"

        Write-ChocolateyInfo "Installing '$NetFx48Installer' - this may take awhile with no output."
        $s = [System.Diagnostics.Process]::Start($psi);
        $s.WaitForExit();
        if ($s.ExitCode -eq 1641 -or $s.ExitCode -eq 3010) {
          Write-Warning ".NET Framework 4.8 was installed, but a reboot is required before using Chocolatey CLI."
          $script:DotNetInstallRequiredReboot = $true
        }
        elseif ($s.ExitCode -ne 0) {
            if ($script:netFx48InstallTries -ge 2) {
                Write-ChocolateyError ".NET Framework install failed with exit code '$($s.ExitCode)'. `n This will cause the rest of the install to fail."
                throw "Error installing .NET Framework 4.8 (exit code $($s.ExitCode)). `n Please install the .NET Framework 4.8 manually and reboot the system `n and then try to install/upgrade Chocolatey again. `n Download at '$NetFx48Url'"
            }
            else {
                Write-ChocolateyWarning "Try #$script:netFx48InstallTries of .NET framework install failed with exit code '$($s.ExitCode)'. Trying again."
                Install-DotNet48IfMissing -Force
            }
        }
    }
}

function Invoke-ChocolateyFirstRun {
    if ($PSVersionTable.Major -le 3 -and $script:DotNetInstallRequiredReboot) {
        Write-Debug "Skipping initialization due to known issues before rebooting."
        return
    }

    Write-Debug "Initializing Chocolatey files, etc by running Chocolatey CLI..."

    try {
        $chocoInstallationFolder = Get-ChocolateyInstallFolder
        $chocoExe = Join-Path -Path $chocoInstallationFolder -ChildPath "choco.exe"
        $runResult = & $chocoExe -v 2>&1
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
# MIInJQYJKoZIhvcNAQcCoIInFjCCJxICAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCBouJlXeWXDnvZO
# Jivqdm12UFqwJv5AHFObI0ZLoZF7Y6CCIKgwggWNMIIEdaADAgECAhAOmxiO+dAt
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
# BCAulOXTVNSrX9mcCftHw+GTa1cwKgcbhSkpqvQbuXqVljANBgkqhkiG9w0BAQEF
# AASCAYCE37jOFd5tjTCmRFjGmz+gtmZ/vy1uxPvB1OfqcSzNDLyjgtc+Jzm92lS4
# x0jhKey3RIwIDKS7VtjbSikec22jzsIRvVL59OLVTk65y3D36J3mJwbvcfADhGx+
# rT3iwbDHF/PwuzNyVL559GJiQcsthWF7iGKR9MZCAJ2W2CccPdHlgzTTEqdws5Ns
# yAapqJ9fGue+tQhpbWD+7wn7eJ9BlOR7alA/i56jOWDUHwmmwYACj8Phz+HxGPUc
# HA/53n01aMty2XiSzhTOR3ojWTQ5qEjIP0p9tzVwyiLtcfztogHjKRtEefiI8KWt
# Ai0qUrds2FYaOZY8iQ2Jmw7ttcPzc+9Wtq5rgiAKd0S1W5afOgWEsJLRTHXuyEkU
# k0YA+dZjCsbhQOGhp2UzdytvwxPUgyvEkA8WBT4PwRqcP5d1O8QtWyBlrt0FTq4Y
# 8J4wis1KWdPHFLAOdjbDVSIQtTnrimIeyE/+6SMmF/1GsFbDORBWZLh70NPlw0UW
# /exCqCmhggMgMIIDHAYJKoZIhvcNAQkGMYIDDTCCAwkCAQEwdzBjMQswCQYDVQQG
# EwJVUzEXMBUGA1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRpZ2lDZXJ0
# IFRydXN0ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENBAhALrma8
# Wrp/lYfG+ekE4zMEMA0GCWCGSAFlAwQCAQUAoGkwGAYJKoZIhvcNAQkDMQsGCSqG
# SIb3DQEHATAcBgkqhkiG9w0BCQUxDxcNMjUwNjA5MDkwNzQ3WjAvBgkqhkiG9w0B
# CQQxIgQgQh+iiFyevbuC4o9+rTvn5QuyH16GF3SL9keEJdGw41wwDQYJKoZIhvcN
# AQEBBQAEggIAKLUi2D9FQJa6F9ZRZwRo3SkkJv6a2jA4juYIYlRZk4UOxJYjWULJ
# XiUaUB+uP9cgdr2fx71JrEFDlJSqSBCUw2cnlRQRRsMWieVHxuI1nLJHfUkbMTOK
# h05+SHzhkMQLXFADeroS5AXhTQnyz0L4E468QYmffV/DuVfFl2OFHt06QOnO4nA9
# S0MQ0tTcPF37e5w6nNK9Io4pBkv+GxX7T2CdfPdN+nz5cbMTCCg3xwcB0CaKGeew
# XQTjgE3Zy2l235aLb+sb1lSUuLPu0XzSWg/fkj7d3zEcG3E5RRS89CQ3F+uTuefH
# mJgqBxrln6DwgrVMOpDYgHRr6lcAbat3U1urM05tVPHvQ9VE7VlKhUXSqkJ5wMhG
# gDxLFFZy1aJRq4XQOPDP6L3peOq+s7GczY5D/21ljKgBrlmgnPlT4TXx+Gvv/pyH
# S/HT+jfgekfrGKWBB2Xd+hgdLzmh+MlJN6nA+Nhg/J3/U+s/Dntwk6uesZqtQdkL
# 6eNCuMYtyIrt3qlsnbMDc+bWPkOfDE3s61B1BUYRVSUpxEg2EXhaZ5+mWZuroXWd
# 8c7aCJn9cCiNmkcOkumFdWdDPyokVlVdW+KvPdj8Y0RKu61FtNjxsK+ejaaQA1Zi
# BqmKxQjJEw9RnHFBkA/yZjFw6/9EYTMhIseP3/TQrAuXCM1vtEVEMZY=
# SIG # End signature block
