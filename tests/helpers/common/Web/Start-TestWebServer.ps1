function Start-TestWebServer {
    <#
        .SYNOPSIS
            Starts a local HTTP server that answers requests from a table of routes.

        .DESCRIPTION
            Starts an HttpListener on a free localhost port, served from a background runspace.
            Each route is keyed by the request path and is a hashtable with any of these keys:

            - Status: the HTTP status code to respond with (default 200).
            - ContentType: the Content-Type header (default application/octet-stream).
            - Headers: a hashtable of additional response headers, such as Content-Disposition or Location.
            - Body: a string response body, written as UTF-8.
            - BodyBytes: a byte array response body, which takes precedence over Body.

            Paths without a route respond with 404.

            Every request is recorded in the Requests list of the returned object, with its Path, Method and Headers,
            so that tests can assert on what a command sent.

            Stop the server with Stop-TestWebServer.

        .OUTPUTS
            An object with Url (the server root, without a trailing slash), Requests, and the internals needed by Stop-TestWebServer.
    #>
    [CmdletBinding()]
    param(
        # The routes to serve, keyed by request path (for example '/file.zip').
        [Parameter(Mandatory)]
        [hashtable]$Routes
    )
    end {
        $listener = $null
        foreach ($port in 18080..18180) {
            $candidate = [System.Net.HttpListener]::new()
            $candidate.Prefixes.Add("http://localhost:$port/")
            try {
                $candidate.Start()
                $listener = $candidate
                break
            }
            catch {
                $candidate.Close()
            }
        }

        if (-not $listener) {
            throw 'Unable to start a local test web server; no free port was found between 18080 and 18180.'
        }

        $requests = [System.Collections.ArrayList]::Synchronized([System.Collections.ArrayList]::new())

        $serve = {
            param($listener, $routes, $requests)

            while ($listener.IsListening) {
                try {
                    $context = $listener.GetContext()
                }
                catch {
                    break
                }

                try {
                    $request = $context.Request
                    $response = $context.Response
                    $path = $request.Url.AbsolutePath

                    $requestHeaders = @{}
                    foreach ($name in $request.Headers.AllKeys) {
                        $requestHeaders[$name] = $request.Headers[$name]
                    }
                    $null = $requests.Add([pscustomobject]@{ Path = $path; Method = $request.HttpMethod; Headers = $requestHeaders })

                    $route = $routes[$path]
                    if (-not $route) {
                        $route = @{ Status = 404; ContentType = 'text/plain'; Body = 'Not Found' }
                    }

                    $response.StatusCode = if ($route.Status) { [int]$route.Status } else { 200 }
                    $response.ContentType = if ($route.ContentType) { $route.ContentType } else { 'application/octet-stream' }
                    foreach ($name in $route.Headers.Keys) {
                        $response.AddHeader($name, $route.Headers[$name])
                    }

                    # The bytes are assigned in each branch, since an empty array assigned from an if expression becomes null.
                    $bytes = [byte[]]::new(0)
                    if ($route.BodyBytes) {
                        $bytes = [byte[]]$route.BodyBytes
                    }
                    elseif ($route.Body) {
                        $bytes = [System.Text.Encoding]::UTF8.GetBytes($route.Body)
                    }

                    $response.ContentLength64 = $bytes.Length
                    $response.OutputStream.Write($bytes, 0, $bytes.Length)
                    $response.OutputStream.Close()
                }
                catch {
                    # A failure to answer one request must not stop the server from answering the next.
                }
            }
        }

        $powerShell = [powershell]::Create()
        $null = $powerShell.AddScript($serve).AddArgument($listener).AddArgument($Routes).AddArgument($requests)
        $handle = $powerShell.BeginInvoke()

        [pscustomobject]@{
            Url        = "http://localhost:$port"
            Requests   = $requests
            Listener   = $listener
            PowerShell = $powerShell
            Handle     = $handle
        }
    }
}
