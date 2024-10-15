Describe 'Get-ChocolateyPath helper function tests' -Tags Cmdlets, GetChocolateyPath {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"

        if (-not $env:ChocolateyInstall) {
            $env:ChocolateyInstall = $testLocation
        }
    }

    Context '-PathType InstallPath (Path exists)' {
        BeforeAll {
            $programData = $env:ProgramData
            $systemDrive = $env:SystemDrive
        }

        AfterAll {
            $env:ChocolateyInstall = $testLocation
            $env:ProgramData = $programData
            $env:SystemDrive = $systemDrive
        }

        It 'Returns the value of $env:ChocolateyInstall if set' {
            $env:ChocolateyInstall | Should -Not -BeNullOrEmpty -Because 'it should be set for this test'
            Get-ChocolateyPath -PathType InstallPath | Should -BeExactly $env:ChocolateyInstall
        }

        # Skip this if the system choco install path does not exist, it will return null in that case, and we shouldn't be creating paths
        # at this level for a test.
        It 'Falls back to $env:ProgramData\chocolatey if $env:ChocolateyInstall is not set' -Skip:(-not (Test-Path "$env:ProgramData\chocolatey")) {
            $env:ChocolateyInstall = ''
            Get-ChocolateyPath -PathType InstallPath | Should -Be "$env:ProgramData\chocolatey"
        }

        # Skip this if the system choco install path does not exist, it will return null in that case, and we shouldn't be creating paths
        # at this level for a test.
        It 'Falls back to $env:SystemDrive\ProgramData\chocolatey if $env:ChocolateyInstall and $env:ProgramData are not set' -Skip:(-not (Test-Path "$env:SystemDrive\ProgramData\chocolatey")) {
            $env:ChocolateyInstall = ''
            $env:ProgramData = ''
            Get-ChocolateyPath -PathType InstallPath | Should -Be "$env:SystemDrive\ProgramData\chocolatey"
        }

        It 'Falls back to a path relative to the DLL location if none of the environment variables are set' {
            $env:SystemDrive = ''
            $env:ChocolateyInstall = ''
            $env:ProgramData = ''
            $expectedPath = [Chocolatey.PowerShell.Helpers.PSHelper].Assembly.Location | Split-Path -Parent | Split-Path -Parent
            Get-ChocolateyPath -PathType InstallPath | Should -Be $expectedPath
        }
    }

    Context '-PathType InstallPath (Path does not exist)' {
        BeforeAll {
            $programData = $env:ProgramData
            $systemDrive = $env:SystemDrive
        }

        AfterAll {
            $env:ChocolateyInstall = $testLocation
            $env:ProgramData = $programData
            $env:SystemDrive = $systemDrive
        }

        It 'Returns null if $env:ChocolateyInstall path does not exist' {
            $env:ChocolateyInstall = "$env:TEMP\DummyFolderDoesNotExist"
            Get-ChocolateyPath -PathType InstallPath | Should -BeNullOrEmpty
        }

        It 'Returns null if $env:ProgramData\chocolatey does not exist and $env:ChocolateyInstall is not set' {
            $env:ChocolateyInstall = ''
            $env:ProgramData = "$env:TEMP\DummyFolderDoesNotExist"
            Get-ChocolateyPath -PathType InstallPath | Should -BeNullOrEmpty
        }
    }

    Context '-PathType PackagePath (Path exists)' {
        BeforeAll {
            $TestPackageFolder = "$env:TEMP\test"
            New-Item -ItemType Directory -Path $TestPackageFolder -Force > $null

            $env:ChocolateyPackageName = 'test'

            $installPath = Get-ChocolateyPath -PathType InstallPath
            $expectedLibPath = Join-Path -Path $installPath -ChildPath "lib\$env:ChocolateyPackageName"
            New-Item -ItemType Directory -Path $expectedLibPath > $null
        }

        AfterEach {
            $env:ChocolateyPackageFolder = ''
            $env:PackageFolder = ''
        }

        AfterAll {
            $env:ChocolateyPackageName = ''
            Remove-Item -Path $expectedLibPath -Force -Recurse
        }

        It 'Returns the value of $env:ChocolateyPackageFolder if set' {
            $env:ChocolateyPackageFolder = $TestPackageFolder
            Get-ChocolateyPath -PathType PackagePath | Should -BeExactly $env:ChocolateyPackageFolder
        }

        It 'Returns the value of $env:PackageFolder if set and $env:ChocolateyPackageFolder is not set' {
            $env:PackageFolder = $TestPackageFolder
            Get-ChocolateyPath -PathType PackagePath | Should -BeExactly $env:PackageFolder
        }

        It 'Falls back to "{InstallPath}\lib\$env:ChocolateyPackageName" if neither of the PackageFolder variables are set' {
            Get-ChocolateyPath -PathType PackagePath | Should -Be $expectedLibPath
        }
    }

    Context '-PathType PackagePath (Path does not exist)' {
        AfterEach {
            $env:ChocolateyPackageFolder = ''
            $env:PackageFolder = ''
            $env:ChocolateyPackageName = ''
        }

        It 'Returns null if $env:ChocolateyPackageFolder does not exist' {
            $env:ChocolateyPackageFolder = "$env:TEMP\DummyFolderDoesNotExist"
            Get-ChocolateyPath -PathType PackagePath | Should -BeNullOrEmpty
        }

        It 'Returns null if $env:PackageFolder does not exist and $env:ChocolateyPackageFolder is not set' {
            $env:PackageFolder = 'C:\DummyFolderDoesNotExist'
            Get-ChocolateyPath -PathType PackagePath | Should -BeNullOrEmpty
        }

        It 'Returns null if "{InstallPath}\lib\$env:ChocolateyPackageName" does not exist and neither of the PackageFolder variables are set' {
            $env:ChocolateyPackageName = 'test'
            $installPath = Get-ChocolateyPath -PathType InstallPath

            $expectedPath = Join-Path -Path $installPath -ChildPath "lib\$env:ChocolateyPackageName"
            Get-ChocolateyPath -PathType PackagePath | Should -BeNullOrEmpty
        }
    }
}