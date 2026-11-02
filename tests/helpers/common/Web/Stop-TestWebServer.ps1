function Stop-TestWebServer {
    <#
        .SYNOPSIS
            Stops a local HTTP server started with Start-TestWebServer.
    #>
    [CmdletBinding()]
    param(
        # The server object returned by Start-TestWebServer.
        [Parameter(Mandatory, ValueFromPipeline)]
        [pscustomobject]$Server
    )
    process {
        $Server.Listener.Close()
        $null = $Server.Handle.AsyncWaitHandle.WaitOne(5000)
        $Server.PowerShell.Dispose()
    }
}
