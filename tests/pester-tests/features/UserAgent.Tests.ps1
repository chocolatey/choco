Import-Module helpers/common-helpers

Describe "Chocolatey User Agent" -Tag Chocolatey, UserAgent {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot

        $Output = Invoke-Choco search chocolatey --debug
        $ChocolateyVersion = Get-ChocolateyVersion

        $Processes = [System.Collections.Generic.List[string]]::new()

        # IGNORED USER AGENT PROCESSES
        # This list should match the one in CLI code for things we don't expect to see in the user agent
        # after filtering the process tree.
        # The corresponding list can be found in chocolatey.infrastructure.information.ProcessTree;
        # search the repo for the above string in caps if you have trouble finding it.
        $ExcludedProcesses = @(
            "explorer"
            "winlogon"
            "powershell"
            "pwsh"
            "cmd"
            "bash"
            "services"
            "svchost"
            "Chocolatey CLI"
            "alacritty"
            "code"
            "ConEmu64"
            "ConEmuC64"
            "conhost"
            "c3270"
            "FireCMD"
            "Hyper"
            "SecureCRT"
            "Tabby"
            "wezterm"
            "wezterm-gui"
            "WindowsTerminal"
        )
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    It 'Logs the full process tree to debug' {
        $logLine = $Output.Lines | Where-Object { $_ -match '^Process Tree' } | Select-Object -Last 1

        $logLine | Should -Not -BeNullOrEmpty -Because 'choco.exe should log the process tree to debug'
        
        Write-Host "================== PROCESS TREE =================="
        Write-Host $logLine
        
        $parentProcesses = [string[]]@($logLine -replace '^Process Tree: ' -split ' => ' | Select-Object -Skip 1)
        if ($parentProcesses.Count -gt 0) {
            $Processes.AddRange($parentProcesses)
        }
    }

    It 'Logs the final user agent to debug' {
        $logLine = $Output.Lines | Where-Object { $_ -match '^Updating User Agent' } | Select-Object -Last 1
        
        $logLine | Should -Not -BeNullOrEmpty -Because "choco.exe should log the user agent string to debug`n$($Output.Lines)"

        Write-Host "================== USER AGENT =================="
        Write-Host $logLine

        $result = $logLine -match "'(?<UserAgent>Chocolatey Command Line/[^']+)'"
        $result | Should -BeTrue -Because "the user agent string should start with Chocolatey Command Line. $logLine"

        $userAgent = $matches['UserAgent']

        $userAgent -match 'Chocolatey Command Line/(?<Version>[a-z0-9.-]+) ([a-z ]+/(?<LicensedVersion>[a-z0-9.-]+) )?\((?<RootProcess>[^,)]+)(?:, (?<ParentProcess>[^)]+))?\) via NuGet Client' |
            Should -BeTrue -Because "the user agent string should contain the choco.exe version, the licensed extension version if any, and any parent processes. $logLine"

        $matches['Version'] | Should -Be $ChocolateyVersion -Because "the user agent string should contain the currently running Chocolatey version. $logLine"

        if (Test-PackageIsEqualOrHigher -PackageName 'chocolatey.extension' -Version '6.3.0-alpha') {
            # We are not asserting the Licensed Extension version here as the Chocolatey package version often
            # mismatches the assembly version.
            $matches['LicensedVersion'] | Should -Not -BeNullOrEmpty -Because "Chocolatey Licensed Extension is installed and should be in the user agent. $logLine"
        }

        $filteredProcesses = @($Processes | Where-Object { $_ -notin $ExcludedProcesses })
        
        if ($filteredProcesses.Count -gt 1) {
            $rootProcess = $filteredProcesses[-1]
            $matches['RootProcess'] | Should -Be $rootProcess -Because "the user agent string should show the root calling process '$rootProcess'. $logLine"
        }

        if ($filteredProcesses.Count -gt 0) {
            $callingProcess = $filtered[0]
            $matches['ParentProcess'] | Should -Be $callingProcess -Because "the user agent string should show the parent process '$callingProcess'. $logLine"
        }
    }
}