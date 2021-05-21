## Get-WmiObject
##############################################################################################################
## Wrapper function. Replaces "Win32_Product" class with Get-Installer helper.
##############################################################################################################

function Get-WmiObject {

  $oc = Get-Command 'Get-WmiObject' -Module 'Microsoft.PowerShell.Management'

  if( $args -icontains "Win32_Product" ) {
    Get-Installed -GWMICompatible
  } else {
    & $oc @args
  }
}
