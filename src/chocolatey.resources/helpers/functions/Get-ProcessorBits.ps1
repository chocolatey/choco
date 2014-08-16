function Get-ProcessorBits {
<#
.SYNOPSIS
Get the system architecture address width.

.DESCRIPTION
This will return the system architecture address width (probably 32 or 64 bit).

.PARAMETER compare
This optional parameter causes the function to return $True or $False, depending on wether or not the bitwidth matches.

.NOTES
When your installation script has to know what architecture it is run on, this simple function comes in handy.
#>
param(
  $compare # You can optionally pass a value to compare the system architecture and receive $True or $False in stead of 32|64|nn
)
  Write-Debug "Running 'Get-ProcessorBits'"

  $bits = 64
  if ([System.IntPtr]::Size -eq 4) {
    $bits = 32
  }

  # Return bool|int
  if ("$compare" -ne '' -and $compare -eq $bits) {
    return $true
  } elseif ("$compare" -ne '') {
    return $false
  } else {
    return $bits
  }
}
