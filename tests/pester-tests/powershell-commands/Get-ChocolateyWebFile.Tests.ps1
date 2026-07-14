Describe 'Get-ChocolateyWebFile custom header tests' -Tags GetChocolateyWebFile, Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
    }

    It 'uses custom headers when retrieving web headers and downloading a file' {
        $expectedAuthorization = 'Bearer test-token'
        $responseBody = 'test response'
        $serverPortListener = New-Object System.Net.Sockets.TcpListener ([System.Net.IPAddress]::Loopback, 0)
        $serverPortListener.Start()
        $serverPort = ([System.Net.IPEndPoint]$serverPortListener.LocalEndpoint).Port
        $serverPortListener.Stop()

        $serverJob = Start-Job -ScriptBlock {
            param($prefix, $expectedAuthorization, $responseBody, $requestCount)

            $listener = New-Object System.Net.HttpListener
            $listener.Prefixes.Add($prefix)
            $listener.Start()
            Write-Output 'READY'

            $authorizations = @()
            for ($requestNumber = 0; $requestNumber -lt $requestCount; $requestNumber++) {
                $context = $listener.GetContext()
                $authorization = $context.Request.Headers['Authorization']
                $authorizations += $authorization

                $response = $context.Response
                if ($authorization -eq $expectedAuthorization) {
                    $response.StatusCode = 200
                    $body = [System.Text.Encoding]::UTF8.GetBytes($responseBody)
                    $response.ContentLength64 = $body.Length
                    $response.ContentType = 'application/octet-stream'
                    $response.Headers.Add('Content-Disposition', 'attachment; filename=downloaded.bin')
                    $response.OutputStream.Write($body, 0, $body.Length)
                }
                else {
                    $response.StatusCode = 401
                }
                $response.Close()
            }

            $listener.Stop()
            $authorizations
        } -ArgumentList "http://localhost:$serverPort/", $expectedAuthorization, $responseBody, 4

        $destination = Join-Path $testLocation 'custom-headers-test.bin'
        $downloadedFile = Join-Path $testLocation 'downloaded.bin'
        $options = @{ Headers = @{ Authorization = $expectedAuthorization } }
        $checksum = ([System.BitConverter]::ToString(([System.Security.Cryptography.SHA256]::Create()).ComputeHash([System.Text.Encoding]::UTF8.GetBytes($responseBody)))).Replace('-', '').ToLowerInvariant()

        try {
            $readyDeadline = (Get-Date).AddSeconds(10)
            do {
                $serverOutput = @(Receive-Job -Job $serverJob -Keep)
                if ($serverOutput -contains 'READY') {
                    break
                }
                Start-Sleep -Milliseconds 100
            } while ((Get-Date) -lt $readyDeadline)

            $serverOutput | Should -Contain 'READY'

            $headers = Get-WebHeaders -Url "http://localhost:$serverPort/file.bin" -Options $options
            $headers['Content-Length'] | Should -Be $responseBody.Length.ToString()

            $result = Get-ChocolateyWebFile -PackageName 'custom-header-test' -FileFullPath $destination -Url "http://localhost:$serverPort/file.bin" -Options $options -Checksum $checksum -ChecksumType sha256 -GetOriginalFileName
            $result | Should -Be $downloadedFile
            [System.IO.File]::ReadAllText($downloadedFile) | Should -BeExactly $responseBody

            Wait-Job -Job $serverJob -Timeout 30 | Should -Not -BeNullOrEmpty
            $authorizations = @(Receive-Job -Job $serverJob | Where-Object { $_ -ne 'READY' })
            $authorizations | Should -HaveCount 4
            $authorizations | Should -Be @($expectedAuthorization, $expectedAuthorization, $expectedAuthorization, $expectedAuthorization)
        }
        finally {
            Stop-Job -Job $serverJob -ErrorAction SilentlyContinue
            Remove-Job -Job $serverJob -Force -ErrorAction SilentlyContinue
            Remove-Item -Path $destination -Force -ErrorAction SilentlyContinue
            Remove-Item -Path $downloadedFile -Force -ErrorAction SilentlyContinue
        }
    }
}
