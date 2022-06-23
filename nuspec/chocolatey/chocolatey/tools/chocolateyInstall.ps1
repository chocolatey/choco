$toolsPath = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Ensure module loading preference is on
$PSModuleAutoLoadingPreference = "All"

$licensedAssembly = [System.AppDomain]::CurrentDomain.GetAssemblies() |
    Where-Object { $_.GetName().Name -eq 'chocolatey.licensed' } |
    Select-Object -First 1

if ($null -ne $licensedAssembly) {
    # The licensed assembly is installed, check its supported Chocolatey versions and/or the assembly
    # version so we can attempt to determine whether it's compatible with this version of Chocolatey.
    $attributeData = $licensedAssembly.GetCustomAttributes($true)

    $minVersion = $attributeData |
        Where-Object { $_.TypeId -like '*MinimumChocolateyVersion*' } |
        Select-Object -ExpandProperty Version -First 1

    $borderWidth = 70
    try {
        $borderWidth = [System.Console]::BufferWidth - 10
    } catch {
        # Do nothing. This means we're in a non-interactive environment without a console attached.
    }

    $messageBorder = '=' * $borderWidth
    $extensionVersionWarning = @"
$messageBorder

You are installing a version of Chocolatey that may not be compatible with the currently installed version of the chocolatey.extension package.
Running Chocolatey with the current version of the chocolatey.extension package is an unsupported configuration.
See https://ch0.co/compatibility for more information.

If you are also modifying the chocolatey.extension package, you can ignore this warning.

$messageBorder
"@

    if ($null -ne $minVersion) {
        # Found an explicit attribute declaring what version(s) of Chocolatey the current licensed
        # assembly is known to work with.
        # Check what Chocolatey version we're installing in Major.Minor.Patch form, stripping off any prerelease suffix
        $packageVersion = $env:ChocolateyPackageVersion -replace '-.+$' -as [System.Version]

        if ($packageVersion -lt $minVersion) {
            Write-Warning $extensionVersionWarning
        }
    }
    else {
        $version = $attributeData |
            Where-Object { $_.TypeId -like '*AssemblyInformationalVersion*' } |
            Select-Object -ExpandProperty InformationalVersion -First 1

        # Strip off quotes and prerelease suffix if present, that's not critical for this check.
        $version = $version -replace '-.+$' -as [System.Version]

        if ($version.Major -lt 4) {
            Write-Warning $extensionVersionWarning
        }
    }
}

$modules = Get-ChildItem $toolsPath -Filter *.psm1
$modules | ForEach-Object {
	  $psm1File = $_.FullName
	  $moduleName = [System.IO.Path]::GetFileNameWithoutExtension($psm1File)

	  if (Get-Module $moduleName) {
        Remove-Module $moduleName -ErrorAction SilentlyContinue
    }

	  Import-Module -Name $psm1File
}

Initialize-Chocolatey
