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
  Write-Debug "Running 'System-GetBits'"

  #TODO: This is trivial at the moment, but Get-WmiObject Win32_Processor IS SLOW.
  #      Can we cache this?

  # Get the address width
    $proc = Get-WmiObject Win32_Processor
    $procCount = (Get-WmiObject Win32_ComputerSystem).NumberofProcessors
    if ($procCount -eq '1') {
        $bits = $proc.AddressWidth
    } else {
        $bits = $proc[0].AddressWidth
    }

  # Return bool|int
  if ("$compare" -ne '' -and $compare -eq $bits) {
    return $True
  } elseif ("$compare" -ne '') {
    return $False
  } else {
    return $bits
  }
}
