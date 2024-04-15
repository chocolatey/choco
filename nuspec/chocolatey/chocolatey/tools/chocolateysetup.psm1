$script:thisScriptFolder = Split-Path -Parent $MyInvocation.MyCommand.Definition
$script:chocoInstallVariableName = "ChocolateyInstall"
$script:tempDir = $env:TEMP
$script:defaultChocolateyPathOld = "$env:SystemDrive\Chocolatey"
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

function Write-ChocolateyError {
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
        [Parameter()]
        [string]
        $Message
    )

    try {
        Write-Host $Message -ErrorAction Stop
    }
    catch {
        Write-Output $Message
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
        Write-ChocolateyDebug "No shim to remove was found at $Path"
        return
    }

    $signature = Get-AuthenticodeSignature $Path -ErrorAction SilentlyContinue

    if (-not $signature -or -not $signature.SignerCertificate) {
        Write-ChocolateyWarning "Shim found at $Path, but will not be removed as it is unsigned"
        return
    }

    $possibleSignatures = @(
        'RealDimensions Software, LLC'
        'Chocolatey Software, Inc\.'
    ) -join '|'

    if ($signature.SignerCertificate.Subject -notmatch $possibleSignatures) {
        # This means the file was found, however did not get removed as it contained a authenticode signature that
        # is not ours.
        Write-ChocolateyWarning "Shim found in $Path, but will not be removed as it has an unexpected signature"
        return
    }

    Write-ChocolateyInfo "Removing shim $Path"
    $null = Remove-Item -Path $Path

    foreach ($file in "$Path.ignore", "$Path.old") {
        if (Test-Path $file) {
            $null = Remove-Item -Path $file
        }
    }
}

function Remove-UnsupportedShims {
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
    .PARAMETER  Path
    Allows you to override the default chocolateyPath of (C:\ProgramData\chocolatey\) by specifying a directory to install Chocolatey into.

    .EXAMPLE
    C:\PS> Initialize-Chocolatey

    Installs chocolatey into the default C:\ProgramData\Chocolatey\ directory.

    .EXAMPLE
    C:\PS> Initialize-Chocolatey -Path "D:\ChocolateyInstalledNuGets\"

    Installs chocolatey into the custom directory D:\ChocolateyInstalledNuGets\

#>
    [CmdletBinding()]
    param(
        # The path to install/initialize Chocolatey CLI into.
        [Parameter()]
        [string]
        $Path
    )

    Write-Debug "Initialize-Chocolatey"

    # Import the chocolateyInstaller module to load helper functions
    $installModule = Join-Path -Path $script:thisScriptFolder -ChildPath 'chocolateyInstall\helpers\chocolateyInstaller.psm1'
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
    $allowInsecureRootInstall = $false
    if ($env:ChocolateyAllowInsecureRootDirectory -eq 'true') {
        $allowInsecureRootInstall = $true
    }

    # if we have an already set environment variable path, use it.
    $alreadyInitializedNugetPath = Get-ChocolateyInstallPath
    $useExistingChocolateyPath = $alreadyInitializedNugetPath -and
        $alreadyInitializedNugetPath -ne $chocolateyPath -and
        ($allowInsecureRootInstall -or $alreadyInitializedNugetPath -ne $script:defaultChocolateyPathOld)

    if ($useExistingChocolateyPath) {
        $chocolateyPath = $alreadyInitializedNugetPath
    }
    else {
        Set-EnvChocolateyInstall -Path $chocolateyPath
    }

    Initialize-Directory -Path $chocolateyPath
    Set-Permissions -Path $chocolateyPath

    $chocolateyExePath = Join-Path -Path $chocolateyPath -ChildPath 'bin'
    $chocolateyLibPath = Join-Path -Path $chocolateyPath -ChildPath 'lib'

    if (-not $script:tempDir) {
        $script:tempDir = Join-Path -Path $chocolateyPath -ChildPath 'temp'
        Initialize-Directory -Path $script:tempDir
    }

    $yourPkgPath =  -Path $chocolateyLibPath -ChildPath "yourPackageName"
    Write-ChocolateyInfo @"
We are setting up the Chocolatey package repository.
The packages themselves go to '$chocolateyLibPath'
  (i.e. $yourPkgPath).
A shim file for the command line goes to '$chocolateyExePath'
  and points to an executable in '$yourPkgPath'.

Creating Chocolatey folders if they do not already exist.

"@

    # create the base structure if it doesn't exist
    $chocolateyExePath, $chocolateyLibPath | Initialize-Directory

    $possibleShimPaths = @(
        Join-Path -Path $chocolateyPath -ChildPath "redirects"
        Join-Path -Path $script:thisScriptFolder -ChildPath "chocolateyInstall\redirects"
    )
    Remove-UnsupportedShims -Path $possibleShimPaths

    Install-ChocolateyFiles -Path $chocolateyPath
    Initialize-ChocolateyLibFolder -Path $chocolateyLibPath

    Install-ChocolateyBinFiles -Path $chocolateyExePath -ChocolateyPath $chocolateyPath

    Add-ChocolateyToPath -Path $chocolateyExePath
    Edit-ChocolateyBinFiles -Path $chocolateyExePath

    # Reload the chocolateyInstaller module from the actual choco install path
    $realModule = Join-Path -Path $chocolateyPath -VChildPath "helpers\chocolateyInstaller.psm1"
    Import-Module $realModule -Force

    if (-not $allowInsecureRootInstall -and (Test-Path $script:defaultChocolateyPathOld)) {
        Move-OldChocolateyInstall -Path $script:defaultChocolateyPathOld -Destination $chocolateyPath
        Install-ChocolateyBinFiles -Path $chocolateyExePath -ChocolateyPath $chocolateyPath
    }

    Add-ChocolateyProfile
    Invoke-Chocolatey
    if ([string]::IsNullOrEmpty($env:ChocolateyExitCode)) {
        $env:ChocolateyExitCode = 0
    }

    Write-ChocolateyInfo @"
Chocolatey CLI (choco.exe) is now ready.
You can call choco from anywhere, command line or powershell by typing choco.
Run choco --help for a list of functions.
You may need to shut down and restart powershell and/or consoles
 first prior to using choco.
"@

    if (-not $allowInsecureRootInstall) {
        Remove-OldChocolateyInstall -Path $script:defaultChocolateyPathOld
    }

    Remove-UnsupportedShims -Path $chocolateyExePath
}

function Set-EnvChocolateyInstall {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]
        $Path
    )

    Write-Debug "Set-ChocolateyInstallPath"

    $environmentTarget = [System.EnvironmentVariableTarget]::User

    # Remove old user-scope variable
    Install-ChocolateyEnvironmentVariable -variableName $script:chocoInstallVariableName -variableValue $null -variableType $environmentTarget
    if (Test-ProcessAdminRights) {
        Write-Debug "Administrator installing so using Machine environment variable target instead of User."
        $environmentTarget = [System.EnvironmentVariableTarget]::Machine

        # Remove old machine-scope variable
        Install-ChocolateyEnvironmentVariable -variableName $script:chocoInstallVariableName -variableValue $null -variableType $environmentTarget
    }
    else {
        Write-ChocolateyWarning "Setting ChocolateyInstall Environment Variable on USER and not SYSTEM variables. This is due to either non-administrator install OR the process you are running is not being run as an Administrator."
    }

    Write-ChocolateyInfo "Creating $script:chocoInstallVariableName as an environment variable (targeting '$environmentTarget') `n  Setting $script:chocoInstallVariableName to '$Path'"
    Write-ChocolateyWarning "It's very likely you will need to close and reopen your shell before you can use choco."
    Install-ChocolateyEnvironmentVariable -variableName $script:chocoInstallVariableName -variableValue $Path -variableType $environmentTarget
}

function Get-ChocolateyInstallPath {
    Write-Debug "Get-ChocolateyInstallPath"
    [Environment]::GetEnvironmentVariable($script:chocoInstallVariableName)
}

function Initialize-Directory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [string]
        $Path
    )
    process {
        Write-Debug "Initialize-Directory"
        if (-not (Test-Path $Path)) {
            $null = New-Item -Path $Path -ItemType Directory
        }
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

function Set-AccessRule {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        $Principal,

        [Parameter(Mandatory = $true)]
        [System.Security.AccessControl.FileSystemRights]
        $Rights,

        [Parameter(Mandatory = $true)]
        [System.Security.AccessControl.FileSystemSecurity]
        $Acl,

        [Parameter()]
        [System.Security.AccessControl.InheritanceFlags]
        $InheritanceFlags = [Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit,

        [Parameter()]
        [System.Security.AccessControl.PropagationFlags]
        $PropagationFlags = [Security.AccessControl.PropagationFlags]::None
    )

    $rule = New-Object -TypeName 'System.Security.AccessControl.FileSystemAccessRule' -ArgumentList @($Principal, $Rights, $InheritanceFlags, $PropagationFlags, "Allow")
    $Acl.SetAccessRule($rule)
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

    if ($Path -ne $defaultInstallPath) {
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
        $acl = Get-Acl -Path $Path

        Write-Debug "Removing existing permissions."
        $acl.Access | ForEach-Object { $acl.RemoveAccessRuleAll($_) }


        $rightsFullControl = [Security.AccessControl.FileSystemRights]::FullControl
        $rightsModify = [Security.AccessControl.FileSystemRights]::Modify

        Write-ChocolateyInfo "Restricting write permissions to Administrators"

        $builtinAdmins = Get-LocalizedWellKnownPrincipalName -WellKnownSidType ([Security.Principal.WellKnownSidType]::BuiltinAdministratorsSid)
        $localSystem = Get-LocalizedWellKnownPrincipalName -WellKnownSidType ([Security.Principal.WellKnownSidType]::LocalSystemSid)
        $builtinUsers = Get-LocalizedWellKnownPrincipalName -WellKnownSidType ([Security.Principal.WellKnownSidType]::BuiltinUsersSid)

        Set-AccessRule -Principal $builtinAdmins -Rights $rightsFullControl -Acl $acl
        Set-AccessRule -Principal $localSystem -Rights $rightsFullControl -Acl $acl
        Set-AccessRule -Principal $builtinUsers -Rights $rightsFullControl -Acl $acl

        $allowCurrentUser = $env:ChocolateyInstallAllowCurrentUser -eq 'true'
        if ($allowCurrentUser) {
            # get current user
            $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()

            if ($currentUser.Name -ne $localSystem) {
                Write-ChocolateyWarning 'Adding Modify permission for current user due to $env:ChocolateyInstallAllowCurrentUser. This could lead to escalation of privilege attacks. Consider not allowing this.'
                Set-AccessRule -Principal $currentUser.Name -Rights $rightsModify -Acl $acl
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
        Set-Acl -Path $Path -AclObject $acl

        Initialize-LogDirectory -Path "$Path\logs"
    }
    catch {
        Write-ChocolateyWarning "Not able to set permissions for $Path."
    }
    $ErrorActionPreference = $currentEA
}

function Initialize-LogDirectory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]
        $Path
    )

    Write-Debug 'Initialize-LogDirectory'

    # set an explicit append permission on the logs folder
    Write-Debug "Granting users Append permission for log files."
    Initialize-Directory -Path $Path

    $logsAcl = Get-Acl -Path $Path
    $rightsWrite = [Security.AccessControl.FileSystemRights]::Write

    Set-AccessRule -Principal $builtinUsers -Rights $rightsWrite -InheritanceFlags ObjectInherit -PropagationFlags InheritOnly -Acl $logsAcl
    $logsAcl.SetAccessRuleProtection($false, $true)
    Set-Acl -Path $Path -AclObject $logsAcl
}

function Move-OldChocolateyInstall {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]
        $Path = "$env:SystemDrive\Chocolatey",

        [Parameter()]
        [string]
        $Destination = "$($env:ALLUSERSPROFILE)\chocolatey"
    )

    Write-Debug "Move-OldChocolateyInstall"

    if (Test-Path $Path) {
        Write-ChocolateyInfo "Attempting to upgrade `'$Path`' to `'$Destination`'."
        Write-ChocolateyWarning "Copying the contents of `'$Path`' to `'$Destination`'.`n This step may fail if you have anything in this folder running or locked."
        Write-ChocolateyInfo 'If it fails, just manually copy the rest of the items out and then delete the folder.'
        Write-ChocolateyWarning "NOTE: YOU WILL NEED TO CLOSE AND REOPEN YOUR SHELL"

        $chocolateyExePathOld = Join-Path -Path $Path -ChildPath 'bin'

        foreach ($scope in 'Machine', 'User') {
            $envPath = Get-EnvironmentVariable -Name 'PATH' -Scope $scope

            # TODO: use -replace once we have at least PSv3 as a minimum
            $updatedPath = [System.Text.RegularExpressions.Regex]::Replace(
                $envPath,
                [System.Text.RegularExpressions.Regex]::Escape($chocolateyExePathOld) + '(?>;)?',
                [string]::Empty,
                [System.Text.RegularExpressions.RegexOptions]::IgnoreCase
            )

            if ($updatedPath -ne $envPath) {
                Write-ChocolateyInfo "Updating '$scope' PATH to reflect removal of '$Path'."
                try {
                    Set-EnvironmentVariable -Name 'Path' -Value $updatedPath -Scope $_ -ErrorAction Stop
                }
                catch {
                    Write-ChocolateyWarning "Was not able to remove the old environment variable from PATH. You will need to do this manually"
                }
            }
        }

        Copy-Item -Path "$Path\lib\*" -Destination "$Destination\lib" -Force -Recurse

        $from = "$Path\bin"
        $to = "$Destination\bin"

        $exclude = @("choco.exe", "RefreshEnv.cmd")
        Get-ChildItem -Path $from -Recurse -Exclude $exclude |
            ForEach-Object {
                Write-Debug "Copying $_ `n to $to"
                if ($_.PSIsContainer) {
                    Copy-Item $_ -Destination (Join-Path -Path $to -ChildPath $_.Parent.FullName.Substring($from.length)) -Force -ErrorAction SilentlyContinue
                }
                else {
                    $fileToMove = (Join-Path -Path $to -ChildPath $_.FullName.Substring($from.length))
                    try {
                        Copy-Item -Path $_ -Destination $fileToMove -Exclude $exclude -Force -ErrorAction Stop
                    }
                    catch {
                        Write-ChocolateyWarning "Was not able to move '$fileToMove'. You may need to reinstall the shim"
                    }
                }
            }
    }
}

function Remove-OldChocolateyInstall {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]
        $Path = "$env:SystemDrive\Chocolatey"
    )

    Write-Debug "Remove-OldChocolateyInstall"

    if (Test-Path $Path) {
        Write-ChocolateyWarning "This action will result in Log Errors, you can safely ignore those.`n You may need to finish removing '$Path' manually."
        try {
            Get-ChildItem -Path $Path | ForEach-Object {
                if (Test-Path $_.FullName) {
                    Write-Debug "Removing $_ unless matches .log"
                    Remove-Item -Path $_.FullName -Exclude *.log -Recurse -Force -ErrorAction SilentlyContinue
                }
            }

            Write-ChocolateyInfo "Attempting to remove '$Path'. This may fail if something in the folder is being used or locked."
            Remove-Item -Path $Path -Force -Recurse -ErrorAction Stop
        }
        catch {
            Write-ChocolateyWarning "Was not able to remove '$Path'. You will need to manually remove it."
        }
    }
}

function Install-ChocolateyFiles {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]
        $Path
    )

    Write-Debug "Install-ChocolateyFiles"

    Write-Debug "Removing install files in chocolateyInstall, helpers, redirects, and tools"

    "$Path\chocolateyInstall", "$Path\helpers", "$Path\redirects", "$Path\tools" |
        Where-Object { Test-Path $_ } |
        Get-ChildItem |
        ForEach-Object {
            Write-Debug "Removing '$_' and children except those matching *.log"
            Remove-Item -Path $_.FullName -Exclude *.log -Recurse -Force -ErrorAction SilentlyContinue
        }

    # rename the currently running process / it will be locked if it exists
    Write-Debug "Attempting to move choco.exe to choco.exe.old"
    $chocoExe = Join-Path $Path 'choco.exe'
    if (Test-Path $chocoExe) {
        Write-Debug "Renaming '$chocoExe' to '$chocoExe.old'"
        try {
            Remove-Item -Path "$chocoExe.old" -Force -ErrorAction SilentlyContinue
            Move-Item -Path $chocoExe "$chocoExe.old" -Force -ErrorAction Stop
        }
        catch {
            Write-ChocolateyWarning "Was not able to rename '$chocoExe' to '$chocoExe.old'."
        }
    }

    # remove pdb file if it is found
    $chocoPdb = Join-Path -Path $Path -ChildPath 'choco.pdb'
    if (Test-Path $chocoPdb) {
        Remove-Item -Path $chocoPdb -Force -ErrorAction SilentlyContinue
    }

    Write-Debug "Unpacking files required for Chocolatey."
    $chocoInstallFolder = Join-Path -Path $script:thisScriptFolder -ChildPath "chocolateyInstall"
    $chocoExe = Join-Path -Path $chocoInstallFolder -ChildPath 'choco.exe'
    $chocoExeDest = Join-Path -Path $Path -ChildPath 'choco.exe'

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
            Copy-Item -Path $chocoZipFile -Destination "$chocoPkgDirectory\chocolatey.nupkg" -Force -ErrorAction SilentlyContinue
        }

        if (-not (Test-Path "$chocoPkgDirectory\chocolatey.nupkg")) {
            $chocoPkg = Get-ChildItem -Path "$script:thisScriptFolder/../../" |
                Where-Object { $_.Name -match "^chocolatey.*nupkg" } |
                Sort-Object -Property Name -Descending |
                Select-Object -First 1 -ExpandProperty FullName

            $chocoZipFile, $chocoPkg |
                Where-Object { $_ -and (Test-Path $_) } |
                ForEach-Object {
                    Write-Debug "Copying '$_' to '$chocoPkgDirectory\chocolatey.nupkg'."
                    Copy-Item -Path $_ -Destination "$chocoPkgDirectory\chocolatey.nupkg" -Force -ErrorAction SilentlyContinue
                }
        }
    }
}

function Install-ChocolateyBinFiles {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]
        $Path,

        [Parameter(Mandatory = $true)]
        [string]
        $ChocolateyPath
    )

    Write-Debug "Install-ChocolateyBinFiles"
    Write-Debug "Installing the bin file redirects"

    $redirectsPath = Join-Path -Path $ChocolateyPath -ChildPath 'redirects'

    if (-not (Test-Path $redirectsPath)) {
        Write-ChocolateyWarning "$redirectsPath does not exist"
        return
    }

    $exeFiles = Get-ChildItem -Path $redirectsPath -Include @("*.exe", "*.cmd") -Recurse
    foreach ($exeFile in $exeFiles) {
        Install-Redirect -Path $exeFile -ChocolateyPath $ChocolateyPath -ChocolateyBinPath $Path
    }
}

function Install-Redirect {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]
        $Path,

        [Parameter(Mandatory = $true)]
        [string]
        $ChocolateyPath,

        [Parameter(Mandatory = $true)]
        [string]
        $ChocolateyBinPath
    )

    $file = Get-Item $Path
    $exeFilePath = $file.FullName
    $exeFileName = $file.Name

    $binFilePath = Join-Path -Path $ChocolateyBinPath -ChildPath $exeFileName
    $oldBinFilePath = "$binFilePath.old"

    $batchFilePath = $binFilePath.Replace(".exe", ".bat")
    $bashFilePath = $binFilePath.Replace(".exe", "")

    $batchFilePath, $bashFilePath |
        Where-Object { Test-Path $_ } |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

    if (Test-Path ($oldBinFilePath)) {
        try {
            Write-Debug "Attempting to remove $oldBinFilePath"
            Remove-Item -Path $oldBinFilePath -Force -ErrorAction Stop
        }
        catch {
            Write-ChocolateyWarning "Unable to remove '$oldBinFilePath'. This may cause errors."
        }
    }

    if (Test-Path ($binFilePath)) {
        try {
            Write-Debug "Attempting to rename $binFilePath to $oldBinFilePath"
            Move-Item -Path $binFilePath -Destination $oldBinFilePath -Force -ErrorAction Stop
        }
        catch {
            Write-ChocolateyWarning "Unable to move '$binFilePath' to '$oldBinFilePath'."
        }
    }

    try {
        Write-Debug "Attempting to copy $exeFilePath to $binFilePath"
        Copy-Item -Path $exeFilePath -Destination $binFilePath -Force -ErrorAction Stop
    }
    catch {
        Write-ChocolateyWarning "Unable to replace '$binFilePath' with '$exeFilePath'. You may need to do this manually."
    }

    $commandShortcut = [System.IO.Path]::GetFileNameWithoutExtension($exeFilePath)
    Write-Debug "Added command $commandShortcut"
}

function Add-ChocolateyToPath {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]
        $Path = "$env:ALLUSERSPROFILE\chocolatey\bin"
    )

    Write-Debug "Add-ChocolateyToPath"
    Write-Debug "Initializing Chocolatey Path if required"

    $environmentTarget = [System.EnvironmentVariableTarget]::User
    if (Test-ProcessAdminRights) {
        Write-Debug "Administrator installing so using Machine environment variable target instead of User."
        $environmentTarget = [System.EnvironmentVariableTarget]::Machine
    }
    else {
        Write-ChocolateyWarning "Setting ChocolateyInstall Path on USER PATH and not SYSTEM Path.`n  This is due to either non-administrator install OR the process you are running is not being run as an Administrator."
    }

    Install-ChocolateyPath -pathToInstall $Path -pathType $environmentTarget
}

function Edit-ChocolateyBinFiles {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]
        $Path = "$($env:ALLUSERSPROFILE)\chocolatey\bin"
    )

    Write-Debug "Process-ChocolateyBinFiles"

    $processedMarkerFile = Join-Path -Path $Path -ChildPath '_processed.txt'
    if (Test-Path $processedMarkerFile) {
        return
    }

    $binFiles = Get-ChildItem $Path -Include *.bat -Recurse
    if ($binFiles -and $binFiles.Count -gt 0) {
        Write-Debug "Processing Bin files"
        foreach ($file in $binFiles) {
            Write-ChocolateyInfo "Processing $($file.Name) to make it portable"

            try {
                $fileStream = [System.IO.File]::Open($file, 'Open', 'Read', 'ReadWrite')
                $reader = New-Object -TypeName 'System.IO.StreamReader' -ArgumentList @($fileStream)
                $fileText = $reader.ReadToEnd()
            }
            finally {
                $reader.Close()
                $fileStream.Close()
            }

            $fileText = $fileText.ToLower().Replace('"' + $chocolateyPath.ToLower(), "SET DIR=%~dp0%`n""%DIR%..\").Replace("\\", "\")

            Set-Content -Path $file -Value $fileText -Encoding Ascii
        }
    }

    Set-Content -Path $processedMarkerFile -Value "$([System.DateTime]::Now.Date)" -Encoding Ascii
}

# Adapted from http://www.west-wind.com/Weblog/posts/197245.aspx
function Get-FileEncoding {
    [CmdletBinding()]
    param(
        [Parameter()]
        $Path
    )

    [byte[]] $bytes = if ($PSVersionTable.PSVersion.Major -lt 6) {
        Write-Debug "Detected Powershell version < 6 ; Using -Encoding byte parameter"
        Get-Content $Path -Encoding byte -ReadCount 4 -TotalCount 4
    }
    else {
        Write-Debug "Detected Powershell version >= 6 ; Using -AsByteStream parameter"
        Get-Content $Path -AsByteStream -ReadCount 4 -TotalCount 4
    }

    if (-not $bytes -or $bytes.Length -lt 4) {
        return 'utf8'
    }

    $hexBytes = '{0:x2}{1:x2}{2:x2}{3:x2}' -f $bytes[0], $bytes[1], $bytes[2], $bytes[3]
    switch -regex ($hexBytes) {
        '^efbbbf' {
            'utf8'
        }
        '^2b2f76' {
            'utf7'
        }
        '^fffe' {
            'unicode'
        }
        '^feff' {
            'bigendianunicode'
        }
        '^0000feff' {
            'utf32'
        }
        default {
            'ascii'
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

        if ($env:ChocolateyNoProfile) {
            Write-Warning "Not setting tab completion: Environment variable "ChocolateyNoProfile" is set."
            return
        }

        $localSystem = Get-LocalizedWellKnownPrincipalName -WellKnownSidType ([Security.Principal.WellKnownSidType]::LocalSystemSid)
        $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()

        if ($currentUser.Name -eq $localSystem) {
            Write-Warning "Not setting tab completion: Current user is SYSTEM user."
            return
        }

        $profileDirectory = Split-Path -Parent $profileFile

        if (-not (Test-Path $profileDirectory)) {
            Write-Debug "Creating '$profileDirectory'"
            $null = New-Item $profileDirectory -Type Directory -Force -ErrorAction SilentlyContinue
        }

        if (-not (Test-Path $profileFile)) {
            Write-Warning "Not setting tab completion: Profile file does not exist at '$profileFile'."
            return
        }

        # Check authenticode, but only if file is greater than 5 bytes
        $profileFileInfo = Get-Item $profileFile
        if ($profileFileInfo.Length -gt 5) {
            $signature = Get-AuthenticodeSignature $profile
            if ($signature.Status -ne 'NotSigned') {
                Write-Warning "Not setting tab completion: File is Authenticode signed at '$profile'."
                return
            }
        }

        $profileScript = @'

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
        $profileScript | Add-Content -Path $profileFile -Encoding (Get-FileEncoding $profileFile)
        Write-ChocolateyWarning 'Chocolatey profile installed. Reload your profile - type . $profile'

        if ($PSVersionTable.PSVersion.Major -lt 3) {
            Write-ChocolateyWarning "Tab completion does not currently work in PowerShell v2. `n Please upgrade to a more recent version of PowerShell to take advantage of tab completion."
        }
    }
    catch {
        Write-ChocolateyWarning "Unable to add Chocolatey to the profile. You will need to do it manually. Error was '$_'"
        Write-ChocolateyInfo @"
To add the Chocolatey Profile manually:
Find your `$profile file. Then add the following lines to it:
$profileScript
"@
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

    if (-not $Force) {
        # Skip if .NET 4.8 is already installed
        $net4Version = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -ErrorAction SilentlyContinue).Release
        if ($net4Version -ge 528040) {
            return
        }
    }

    $NetFx48Url = 'https://download.visualstudio.microsoft.com/download/pr/2d6bb6b2-226a-4baa-bdec-798822606ff1/8494001c276a4b96804cde7829c04d7f/ndp48-x86-x64-allos-enu.exe'
    $NetFx48Path = $script:tempDir
    $NetFx48InstallerFile = 'ndp48-x86-x64-allos-enu.exe'
    $NetFx48Installer = Join-Path -Path $NetFx48Path -ChildPath $NetFx48InstallerFile

    Write-ChocolateyInfo "The registry key for .Net 4.8 was not found or this is forced"

    if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending") {
        Write-Warning "A reboot is required. `n If you encounter errors, reboot the system and attempt the operation again"
    }

    $script:netFx48InstallTries += 1

    if (-not (Test-Path $NetFx48Installer)) {
        Write-ChocolateyInfo "Downloading '$NetFx48Url' to '$NetFx48Installer' - the installer is 100+ MBs, so this could take a while on a slow connection."
        (New-Object -TypeName 'System.Net.WebClient').DownloadFile($NetFx48Url, $NetFx48Installer)
    }

    $startInfo = New-Object -TypeName 'System.Diagnostics.ProcessStartInfo'
    $startInfo.WorkingDirectory = $NetFx48Path
    $startInfo.FileName = $NetFx48InstallerFile
    # https://msdn.microsoft.com/library/ee942965(v=VS.100).aspx#command_line_options
    # http://blogs.msdn.com/b/astebner/archive/2010/05/12/10011664.aspx
    # For the actual setup.exe (if you want to unpack first) - /repair /x86 /x64 /ia64 /parameterfolder Client /q /norestart
    $startInfo.Arguments = "/q /norestart"

    Write-ChocolateyInfo "Installing '$NetFx48Installer' - this may take a while with no output."
    $installProcess = [System.Diagnostics.Process]::Start($startInfo)
    $installProcess.WaitForExit()

    $rebootNeededExitCodes = 1641, 3010
    if ($rebootNeededExitCodes -contains $installProcess.ExitCode) {
        throw ".NET Framework 4.8 was installed, but a reboot is required. `n Please reboot the system and try to install/upgrade Chocolatey again."
    }

    if ($installProcess.ExitCode -ne 0) {
        if ($script:netFx48InstallTries -ge 2) {
            Write-ChocolateyError ".NET Framework install failed with exit code '$($installProcess.ExitCode)'. `n This will cause the rest of the install to fail."
            throw "Error installing .NET Framework 4.8 (exit code $($installProcess.ExitCode)). `n Please install the .NET Framework 4.8 manually and reboot the system `n and then try to install/upgrade Chocolatey again. `n Download at '$NetFx48Url'"
        }

        Write-ChocolateyWarning "Attempt #$script:netFx48InstallTries of .NET framework install failed with exit code '$($installProcess.ExitCode)'. Trying again."
        Install-DotNet48IfMissing -Force
    }
}

function Invoke-Chocolatey {
    [CmdletBinding()]
    param()

    Write-Debug "Initializing Chocolatey files, etc by running Chocolatey"

    try {
        $chocoInstallationFolder = Get-ChocolateyInstallPath
        $chocoExe = Join-Path -Path $chocoInstallationFolder -ChildPath "choco.exe"
        & $chocoExe --version
        Write-Debug "Chocolatey execution completed successfully."
    }
    catch {
        Write-ChocolateyWarning "Unable to run Chocolatey at this time.  It is likely that .Net Framework installation requires a system reboot"
    }
}

Export-ModuleMember -Function Initialize-Chocolatey
