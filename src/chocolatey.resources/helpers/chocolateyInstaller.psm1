# Copyright © 2017 - 2021 Chocolatey Software, Inc.
# Copyright © 2015 - 2017 RealDimensions Software, LLC
# Copyright © 2011 - 2015 RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

$helpersPath = Split-Path -Parent $MyInvocation.MyCommand.Definition

$global:DebugPreference = "SilentlyContinue"
if ($env:ChocolateyEnvironmentDebug -eq 'true') {
    $global:DebugPreference = "Continue"
}
$global:VerbosePreference = "SilentlyContinue"
if ($env:ChocolateyEnvironmentVerbose -eq 'true') {
    $global:VerbosePreference = "Continue"
    $verbosity = $true
}

$overrideArgs = $env:chocolateyInstallOverride -eq 'true'

$forceX86 = $env:chocolateyForceX86 -eq 'true'

$installArguments = $env:chocolateyInstallArguments

$packageParameters = $env:chocolateyPackageParameters

# ensure module loading preference is on
$PSModuleAutoLoadingPreference = "All"

Write-Debug "Host version is $($host.Version), PowerShell Version is '$($PSVersionTable.PSVersion)' and CLR Version is '$($PSVersionTable.CLRVersion)'."

# Import functions from files
# Explicitly export only commands added to avoid Wildcards which would fail in Constrained Language Mode
$functionsBeforeImport = @(Get-Command -CommandType Function | Select-Object -ExpandProperty Name)
$aliasesBeforeImport = @(Get-Alias | Select-Object -ExpandProperty Name)
$cmdletsBeforeImport = @(Get-Command -CommandType Cmdlet | Select-Object -ExpandProperty Name)

# Avoid wildcard file matching here and filter the discovered files explicitly.
Get-ChildItem -Path (Join-Path $helpersPath 'functions') |
    Where-Object { $_.Extension -eq '.ps1' -and -not $_.Name.Contains(".Tests.") } |
    ForEach-Object {
        . $_.FullName
    }

# Build explicit export lists from commands added after dot-sourcing the helper files.
$functionsToExport = @(Get-Command -CommandType Function | Where-Object { $functionsBeforeImport -notcontains $_.Name } | Select-Object -ExpandProperty Name)
$aliasesToExport = @(Get-Alias | Where-Object { $aliasesBeforeImport -notcontains $_.Name } | Select-Object -ExpandProperty Name)
$cmdletsToExport = @(Get-Command -CommandType Cmdlet | Where-Object { $cmdletsBeforeImport -notcontains $_.Name } | Select-Object -ExpandProperty Name)

$currentAssemblies = [System.AppDomain]::CurrentDomain.GetAssemblies()

# Import commands from Chocolatey.PowerShell.dll
$chocolateyCmdlets = @{}

$dllPath = "$helpersPath\Chocolatey.PowerShell.dll"
if (Test-Path $dllPath) { 
    # Try to import from already-loaded assembly, otherwise fallback to importing from dll
    $cmdletsAssembly = $currentAssemblies |
        Where-Object { $_.GetName().Name -eq 'Chocolatey.PowerShell' } |
        Select-Object -First 1

    if ($cmdletsAssembly) {
        Import-Module $cmdletsAssembly.Location -Force
    }
    else {
        Import-Module $dllPath
    }

    # Cache module commands for helping resolve lookups
    $chocolateyCmdlets.Default = @( (Get-Module Chocolatey.PowerShell).ExportedCmdlets.Keys )

    Write-Debug "Cmdlets exported from Chocolatey.PowerShell.dll"
    $chocolateyCmdlets.Default | Write-Debug

    # Set aliases for imported cmdlets
    Set-Alias refreshenv Update-SessionEnvironment

    # refreshenv is created after the initial alias export list is built.
    $aliasesToExport = @(Get-Alias | Where-Object { $aliasesBeforeImport -notcontains $_.Name } | Select-Object -ExpandProperty Name)

    # Chocolatey.PowerShell cmdlets are imported after the initial cmdlet export list is built.
    $cmdletsToExport = @(Get-Command -CommandType Cmdlet | Where-Object { $cmdletsBeforeImport -notcontains $_.Name } | Select-Object -ExpandProperty Name)

    # Check for & remove Chocolatey.PowerShell.dll.old left-over from an upgrade/reinstall
    $dllOldPath = "$dllPath.old"
    if (Test-Path $dllOldPath) {
        Remove-Item -Path $dllOldPath -Force -ErrorAction SilentlyContinue
    }
}

# Export built-in functions prior to loading extensions so that
# extension-specific loading behavior can be used based on built-in
# functions. This allows those overrides to be much more deterministic
# Export explicit names to work in Constrained Language Mode
Export-ModuleMember -Function $functionsToExport -Alias $aliasesToExport -Cmdlet $cmdletsToExport

# Load community extensions if they exist
$extensionsPath = Join-Path $helpersPath -ChildPath '..\extensions'
if (Test-Path $extensionsPath) {
    $licensedExtensionPath = Join-Path $extensionsPath -ChildPath 'chocolatey\chocolatey.licensed.dll'
    if (Test-Path $licensedExtensionPath) {
        Write-Debug "Importing '$licensedExtensionPath'"
        Write-Debug "Loading 'chocolatey.licensed' extension"

        try {
            # Attempt to import module via already-loaded assembly
            $licensedAssembly = $currentAssemblies |
                Where-Object { $_.GetName().Name -eq 'chocolatey.licensed' } |
                Select-Object -First 1

            if ($licensedAssembly) {
                # Import-Module -Assembly doesn't work if the parent module is reimported, so force the import by path.
                if ($licensedAssembly.Location) {
                    Import-Module $licensedAssembly.Location -Force
                } else {
                    Import-Module $licensedAssembly
                }
            }
            else {
                # Fallback: load the extension DLL from the path directly.
                Import-Module $licensedExtensionPath
            }

            # Store commands from licensed module, stripping any 'Cmdlet' suffix from the command name
            $chocolateyCmdlets.Licensed = @( (Get-Module chocolatey.licensed).ExportedCmdlets.Keys | ForEach-Object { $_ -replace "Cmdlet$" } )
            
            Write-Debug "Cmdlets exported from chocolatey.licensed"
            $chocolateyCmdlets.Licensed | Write-Debug
        }
        catch {
            # Only write a warning if the Licensed extension failed to load in some way.
            Write-Warning "Import failed for Chocolatey Licensed Extension. Error: '$_'"
        }
    }

    Write-Debug 'Loading community extensions'
    # Avoid wildcard to work in Constrained Language Mode
    Get-ChildItem -Path $extensionsPath -Recurse |
        Where-Object { $_.Extension -eq '.psm1' } |
        Select-Object -ExpandProperty FullName |
        ForEach-Object {
            Write-Debug "Importing '$_'"
            Import-Module $_
        }
}

# Exercise caution and test _thoroughly_ with AND without the licensed extension installed
# when making any changes here. And make sure to update this comment if needed when any
# changes are being made.
#
# This code overrides PowerShell's default command lookup semantics as follows:
#
# 1. If the command being looked up is available in chocolatey.licensed.dll as
#    a cmdlet with OR without a "Cmdlet" suffix in its name, resolve to this command.
#    (in other words, looking for `Get-ChocolateyThing` will _also_ accept something
#    called `Get-ChocolateyThingCmdlet` if it's from the licensed extension)
# 2. If nothing comes back from the licensed extension, then look through the cmdlets
#    exported from the Chocolatey.PowerShell.dll module. If we find a match, make sure
#    we resolve to this command from the Chocolatey.PowerShell.dll module.
# 3. Finally, if neither of the above find the command being looked up, do nothing and allow
#    PowerShell to use its default command lookup semantics.
#
# In effect we ensure that any command calls that match the name of one of our commands
# will resolve to _our_ commands (preferring licensed cmdlets in the case of a name collision),
# preventing packages from overriding them with their own commands and potentially breaking things.
#
# This functionality is only available in v3 and later, so using this in v2 will not
# work; check for the property before trying to set it.
if ($ExecutionContext.InvokeCommand.PreCommandLookupAction) {
    $ExecutionContext.InvokeCommand.PreCommandLookupAction = {
        param($command, $eventArgs)

        # Don't run this handler for stuff PowerShell is looking up internally
        if ($eventArgs.CommandOrigin -eq 'Runspace') {
            $resolvedCommand = if ($chocolateyCmdlets.Licensed -contains $command) {
                # Resolve only the two supported exact licensed cmdlet names instead of using wildcard lookup to work in Constrained Language Mode
                @($command, "$($command)Cmdlet") |
                    ForEach-Object { Get-Command $_ -Module 'chocolatey.licensed' -CommandType Cmdlet -ErrorAction SilentlyContinue } |
                    Select-Object -First 1
            }
            elseif ($chocolateyCmdlets.Default -contains $command) {
                Get-Command $command -Module "Chocolatey.PowerShell" -CommandType Cmdlet -ErrorAction SilentlyContinue
            }

            if ($resolvedCommand) {
                $eventArgs.Command = $resolvedCommand
                $eventArgs.StopSearch = $true
            }
        }
    }.GetNewClosure()
}

# Refresh explicit export lists after extension loading so extension commands imported
# into this module are also exported without using wildcard exports.
$functionsToExport = @(Get-Command -CommandType Function | Where-Object { $functionsBeforeImport -notcontains $_.Name } | Select-Object -ExpandProperty Name)
$aliasesToExport = @(Get-Alias | Where-Object { $aliasesBeforeImport -notcontains $_.Name } | Select-Object -ExpandProperty Name)
$cmdletsToExport = @(Get-Command -CommandType Cmdlet | Where-Object { $cmdletsBeforeImport -notcontains $_.Name } | Select-Object -ExpandProperty Name)

# todo: explore removing this for a future version
Export-ModuleMember -Function $functionsToExport -Alias $aliasesToExport -Cmdlet $cmdletsToExport
