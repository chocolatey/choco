function Get-TempDirectory {
  <#
      .SYNOPSIS
          Returns the temporary directory.
  #>
  [System.IO.Path]::GetTempPath()
}
