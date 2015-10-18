## Get-Installed
##############################################################################################################
## Returns a list of installed applications.
## Registry based replacement for "Get-WmiObject -Class Win32_Product".
##############################################################################################################

function Get-Installed {
param(
  [switch] $GWMICompatible = $false # You may use '-GWMICompatible' switch
)

  $items = @()

  if(Test-Path "HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall") {
    $items += Get-ChildItem "HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
  }
  if(Test-Path "HKLM:SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall") {
    $items += Get-ChildItem "HKLM:SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
  }

  $productList = @()

  foreach ($app in $items)
  {
    $propitems = $app | Foreach-Object {Get-ItemProperty $_.PsPath}
    $w32item = New-Object System.Object
    $w32item | Add-Member -type NoteProperty -Name "IdentifyingNumber" -Value $app.PsChildname
    $w32item | Add-Member -type NoteProperty -Name "Name"    -Value $propitems.DisplayName
    $w32item | Add-Member -type NoteProperty -Name "Vendor"  -Value $propitems.Publisher
    $w32item | Add-Member -type NoteProperty -Name "Version" -Value $propitems.DisplayVersion
    $w32item | Add-Member -type NoteProperty -Name "Caption" -Value $propitems.DisplayName

    if($w32item.Name) {

      if(-not $GWMICompatible) {
        $w32item | Add-Member -type NoteProperty -Name "WindowsInstaller" -Value $propitems.WindowsInstaller
        $w32item | Add-Member -type NoteProperty -Name "UninstallString" -Value $propitems.UninstallString
      }

      if((-not $GWMICompatible) -or ($propitems.WindowsInstaller -eq "1")) {
        $productList += $w32item
      }

    }
  }

  return $productList
}
