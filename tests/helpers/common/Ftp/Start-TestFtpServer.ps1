function Start-TestFtpServer {
    <#
        .SYNOPSIS
            Starts a local FTP server that serves files from a table.

        .DESCRIPTION
            Starts a TCP listener on a free loopback port, served from a background runspace.
            The server speaks just enough passive mode FTP to download files: logging in, changing directory,
            SIZE, and RETR over a passive data connection. It handles one client at a time.

            Files are keyed by their absolute path on the server (for example '/file.zip' or '/folder/file.zip').
            The value is a byte array, or a string that is written as UTF-8.
            A path without a file is answered with 550.

            Every command that a client sends is recorded in the Commands list of the returned object,
            so that tests can assert on what a command sent, including the credentials it logged in with.

            Stop the server with Stop-TestFtpServer.

        .OUTPUTS
            An object with Url (the server root, without a trailing slash), Commands, and the internals needed by Stop-TestFtpServer.
    #>
    [CmdletBinding()]
    param(
        # The files to serve, keyed by absolute path on the server (for example '/file.zip').
        [Parameter(Mandatory)]
        [hashtable]$Files,

        # When given with Password, the only user name that can log in. Without it any user can log in.
        [string]$UserName,

        # When given with UserName, the only password that can log in.
        [string]$Password,

        # Leaves the size of the file out of the reply that starts a transfer.
        [switch]$OmitSize
    )
    end {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
        $listener.Start()
        $port = $listener.LocalEndpoint.Port

        $commands = [System.Collections.ArrayList]::Synchronized([System.Collections.ArrayList]::new())

        $serve = {
            param($listener, $files, $commands, $requiredUser, $requiredPassword, $omitSize)

            $ascii = [System.Text.Encoding]::ASCII

            function Resolve-ServerPath($current, $path) {
                if (-not $path.StartsWith('/')) {
                    $path = $current.TrimEnd('/') + '/' + $path
                }

                $segments = [System.Collections.ArrayList]::new()
                foreach ($segment in $path.Split('/')) {
                    if ($segment -eq '' -or $segment -eq '.') {
                        continue
                    }

                    if ($segment -eq '..') {
                        if ($segments.Count -gt 0) {
                            $segments.RemoveAt($segments.Count - 1)
                        }
                        continue
                    }

                    $null = $segments.Add($segment)
                }

                '/' + ($segments -join '/')
            }

            while ($true) {
                try {
                    $client = $listener.AcceptTcpClient()
                }
                catch {
                    break
                }

                $dataListener = $null
                try {
                    $client.ReceiveTimeout = 10000
                    $client.SendTimeout = 10000
                    $stream = $client.GetStream()
                    $reader = [System.IO.StreamReader]::new($stream, $ascii)
                    $writer = [System.IO.StreamWriter]::new($stream, $ascii)
                    $writer.NewLine = "`r`n"
                    $writer.AutoFlush = $true

                    $writer.WriteLine('220 Test FTP server ready.')

                    $current = '/'
                    $user = $null
                    $loggedIn = $false
                    $done = $false

                    while (-not $done -and $null -ne ($line = $reader.ReadLine())) {
                        $null = $commands.Add($line)

                        $space = $line.IndexOf(' ')
                        $verb = if ($space -lt 0) { $line } else { $line.Substring(0, $space) }
                        $argument = if ($space -lt 0) { '' } else { $line.Substring($space + 1) }

                        if (-not $loggedIn -and $verb.ToUpperInvariant() -notin 'USER', 'PASS', 'QUIT', 'OPTS') {
                            $writer.WriteLine('530 Please log in first.')
                            continue
                        }

                        switch ($verb.ToUpperInvariant()) {
                            'USER' {
                                $user = $argument
                                $writer.WriteLine('331 Password required.')
                            }
                            'PASS' {
                                if ($requiredUser -and ($user -ne $requiredUser -or $argument -ne $requiredPassword)) {
                                    $writer.WriteLine('530 Login incorrect.')
                                }
                                else {
                                    $loggedIn = $true
                                    $writer.WriteLine('230 Logged in.')
                                }
                            }
                            'OPTS' { $writer.WriteLine('200 Option accepted.') }
                            'TYPE' { $writer.WriteLine('200 Type set.') }
                            { $_ -in 'PWD', 'XPWD' } { $writer.WriteLine("257 `"$current`" is the current directory.") }
                            'CWD' {
                                $target = Resolve-ServerPath $current $argument
                                $prefix = $target.TrimEnd('/') + '/'
                                if ($target -eq '/' -or @($files.Keys | Where-Object { $_.StartsWith($prefix) }).Count -gt 0) {
                                    $current = $target
                                    $writer.WriteLine('250 Directory changed.')
                                }
                                else {
                                    $writer.WriteLine('550 No such directory.')
                                }
                            }
                            'SIZE' {
                                $target = Resolve-ServerPath $current $argument
                                if ($files.ContainsKey($target)) {
                                    $value = $files[$target]
                                    $length = if ($value -is [string]) { [System.Text.Encoding]::UTF8.GetByteCount($value) } else { ([byte[]]$value).Length }
                                    $writer.WriteLine("213 $length")
                                }
                                else {
                                    $writer.WriteLine('550 No such file.')
                                }
                            }
                            { $_ -in 'PASV', 'EPSV' } {
                                if ($dataListener) {
                                    $dataListener.Stop()
                                }

                                $dataListener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
                                $dataListener.Start()
                                $dataPort = $dataListener.LocalEndpoint.Port

                                if ($verb.ToUpperInvariant() -eq 'EPSV') {
                                    $writer.WriteLine("229 Entering Extended Passive Mode (|||$dataPort|)")
                                }
                                else {
                                    $writer.WriteLine("227 Entering Passive Mode (127,0,0,1,$([math]::Floor($dataPort / 256)),$($dataPort % 256))")
                                }
                            }
                            'RETR' {
                                $target = Resolve-ServerPath $current $argument
                                if (-not $files.ContainsKey($target)) {
                                    $writer.WriteLine("550 $argument`: No such file.")
                                }
                                elseif (-not $dataListener) {
                                    $writer.WriteLine('425 Use PASV first.')
                                }
                                else {
                                    $value = $files[$target]
                                    $bytes = [byte[]]::new(0)
                                    if ($value -is [string]) {
                                        $bytes = [System.Text.Encoding]::UTF8.GetBytes($value)
                                    }
                                    else {
                                        $bytes = [byte[]]$value
                                    }

                                    $sizeText = if ($omitSize) { '' } else { " ($($bytes.Length) bytes)" }
                                    $writer.WriteLine("150 Opening BINARY mode data connection for $argument$sizeText.")

                                    $dataClient = $dataListener.AcceptTcpClient()
                                    try {
                                        $dataStream = $dataClient.GetStream()
                                        $dataStream.Write($bytes, 0, $bytes.Length)
                                        $dataStream.Flush()
                                    }
                                    finally {
                                        $dataClient.Close()
                                        $dataListener.Stop()
                                        $dataListener = $null
                                    }

                                    $writer.WriteLine('226 Transfer complete.')
                                }
                            }
                            'QUIT' {
                                $writer.WriteLine('221 Goodbye.')
                                $done = $true
                            }
                            default { $writer.WriteLine('502 Command not implemented.') }
                        }
                    }
                }
                catch {
                    # A failure with one client must not stop the server from answering the next.
                }
                finally {
                    if ($dataListener) {
                        $dataListener.Stop()
                    }

                    $client.Close()
                }
            }
        }

        $normalizedFiles = @{}
        foreach ($key in $Files.Keys) {
            $normalizedFiles[$(if ($key.StartsWith('/')) { $key } else { "/$key" })] = $Files[$key]
        }

        $powerShell = [powershell]::Create()
        $null = $powerShell.AddScript($serve).
            AddArgument($listener).
            AddArgument($normalizedFiles).
            AddArgument($commands).
            AddArgument($(if ($UserName) { $UserName } else { '' })).
            AddArgument($(if ($UserName) { $Password } else { '' })).
            AddArgument([bool]$OmitSize)
        $handle = $powerShell.BeginInvoke()

        [pscustomobject]@{
            Url        = "ftp://127.0.0.1:$port"
            Commands   = $commands
            Listener   = $listener
            PowerShell = $powerShell
            Handle     = $handle
        }
    }
}
