Describe 'Ensuring <Command> honours page size settings' -ForEach @(
    @{
        Command = 'search'
    }
) -Tag PageSize {
    BeforeAll {
        Initialize-ChocolateyTestInstall
    }

    Context 'Page size <ProvidedSize>' -ForEach @(
        @{
            ExpectedExitCode = 1
            ProvidedSize = 0
        }
        @{
            ExpectedExitCode = 1
            ProvidedSize = 101
        }
        @{
            ExpectedExitCode = 0
            ProvidedSize = 1
        }
        @{
            ExpectedExitCode = 0
            ProvidedSize = 30
        }
        @{
            ExpectedExitCode = 0
            ProvidedSize = 40
        }
        @{
            ExpectedExitCode = 0
            ProvidedSize = 100
        }
    ) {
        BeforeAll {
            Disable-ChocolateySource
            Enable-ChocolateySource -Name hermes
            $Output = Invoke-Choco $Command --page-size $ProvidedSize
        }

        It 'Exits correctly (<ExpectedExitCode>)' {
            $Output.ExitCode | Should -Be $ExpectedExitCode -Because $Output.String
        }

        It 'Outputs expected messages' {
            $ExpectedMessage = if ($ExpectedExitCode -eq 1) {
                "The page size has been specified to be $ProvidedSize packages. The page size cannot be lower than 1 package, and no larger than 100 packages."
            } else {
                if ($ProvidedSize -ne 30) {
                    "The page size has been specified to be $ProvidedSize packages. There are known issues with some repositories when you use a page size other than 30."
                }
                # There are currently 43 test packages in the repository.
                # Any number above this amount will not result in the below message.
                if ($ProvidedSize -le 40) {
                    "The threshold of $ProvidedSize packages, or package versions, per source has been met. Please refine your search, or specify a page number to retrieve more results."
                }
            }
            foreach ($message in $ExpectedMessage) {
                $Output.Lines | Should -Contain $message -Because $Output.String
            }
        }
    }
}
