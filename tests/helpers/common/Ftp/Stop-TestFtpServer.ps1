function Stop-TestFtpServer {
    <#
        .SYNOPSIS
            Stops a local FTP server started with Start-TestFtpServer.
    #>
    [CmdletBinding()]
    param(
        # The server object returned by Start-TestFtpServer.
        [Parameter(Mandatory, ValueFromPipeline)]
        [pscustomobject]$Server
    )
    process {
        $Server.Listener.Stop()
        $null = $Server.Handle.AsyncWaitHandle.WaitOne(5000)
        $Server.PowerShell.Dispose()
    }
}
