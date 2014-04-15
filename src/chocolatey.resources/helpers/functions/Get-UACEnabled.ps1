function Get-UACEnabled {
  $uacRegPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"
  $uacRegValue = "EnableLUA"
  $uacEnabled = $false

  Write-Debug "Running 'Get-UACEnabled'";


  # http://msdn.microsoft.com/en-us/library/windows/desktop/ms724832(v=vs.85).aspx
  $osVersion = [Environment]::OSVersion.Version
  if ($osVersion -ge [Version]'6.0')
  {
    $uacRegSetting = Get-ItemProperty -Path $uacRegPath
    try {
      $uacValue = $uacRegSetting.EnableLUA
      if ($uacValue -eq 1) {
        $uacEnabled = $true
      }
    } catch {
      #regkey doesn't exist, so proceed with false
    }
  }

 return $uacEnabled
}
