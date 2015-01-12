$thisScriptFolder = (Split-Path -parent $MyInvocation.MyCommand.Definition)
$chocInstallVariableName = "ChocolateyInstall"
$sysDrive = $env:SystemDrive
$tempDir = $env:TEMP

function Initialize-Chocolatey {
<#
  .DESCRIPTION
    This will initialize the Chocolatey tool by
      a) setting up the "nugetPath" (the location where all chocolatey nuget packages will be installed)
      b) Installs chocolatey into the "nugetPath"
            c) Instals .net 4.0 if needed
      d) Adds chocolaty to the PATH environment variable so you have access to the chocolatey|cinst commands.
  .PARAMETER  NuGetPath
    Allows you to override the default path of (C:\Chocolatey\) by specifying a directory chocolaty will install nuget packages.

  .EXAMPLE
    C:\PS> Initialize-Chocolatey

    Installs chocolatey into the default C:\Chocolatey\ directory.

  .EXAMPLE
    C:\PS> Initialize-Chocolatey -nugetPath "D:\ChocolateyInstalledNuGets\"

    Installs chocolatey into the custom directory D:\ChocolateyInstalledNuGets\

#>
param(
  [Parameter(Mandatory=$false)][string]$chocolateyPath = ''
)

  $installModule = Join-Path $thisScriptFolder 'chocolateyInstall\helpers\chocolateyInstaller.psm1'
  Import-Module $installModule -Force

  if ($chocolateyPath -eq '') {
    $programData = [Environment]::GetFolderPath("CommonApplicationData")
    $chocolateyPath = Join-Path "$programData" 'chocolatey'
  }

  #if we have an already environment variable path, use it.
  $alreadyInitializedNugetPath = Get-ChocolateyInstallFolder
  if($alreadyInitializedNugetPath -and $alreadyInitializedNugetPath -ne $chocolateyPath){
    $chocolateyPath = $alreadyInitializedNugetPath
  }
  else {
    Set-ChocolateyInstallFolder $chocolateyPath
  }
  Create-DirectoryIfNotExists $chocolateyPath

  #set up variables to add
  $chocolateyExePath = Join-Path $chocolateyPath 'bin'
  $chocolateyLibPath = Join-Path $chocolateyPath 'lib'
  $chocolateyInstallPath = Join-Path $chocolateyPath 'chocolateyinstall'

  if ($tempDir -eq $null) {
    $tempDir = Join-Path $chocolateyPath 'temp'
    Create-DirectoryIfNotExists $tempDir
  }

  $yourPkgPath = [System.IO.Path]::Combine($chocolateyLibPath,"yourPackageName")
@"
We are setting up the Chocolatey repository for NuGet packages that should be at the machine level. Think executables/application packages, not library packages.
That is what Chocolatey NuGet goodness is for. The repository is set up at `'$chocolateyPath`'.
The packages themselves go to `'$chocolateyLibPath`' (i.e. $yourPkgPath).
A shim file for the command line goes to `'$chocolateyExePath`' and points to an executable in `'$yourPkgPath`'.

Creating Chocolatey NuGet folders if they do not already exist.

"@ | Write-Host

  #create the base structure if it doesn't exist
  Create-DirectoryIfNotExists $chocolateyExePath
  Create-DirectoryIfNotExists $chocolateyLibPath
  Create-DirectoryIfNotExists $chocolateyInstallPath
  Install-ChocolateyFiles $chocolateyPath

  $chocolateyExePathVariable = $chocolateyExePath.ToLower().Replace($chocolateyPath.ToLower(), "%DIR%..\").Replace("\\","\")
  Install-ChocolateyBinFiles $chocolateyInstallPath $chocolateyExePath
  Initialize-ChocolateyPath $chocolateyExePath $chocolateyExePathVariable
  Process-ChocolateyBinFiles $chocolateyExePath $chocolateyExePathVariable
  Install-DotNet4IfMissing $chocolateyInstallPath
  $realModule = Join-Path $chocolateyInstallPath "helpers\chocolateyInstaller.psm1"
  Import-Module "$realModule" -Force

@"
Chocolatey is now ready.
You can call chocolatey from anywhere, command line or powershell by typing choco.
Run choco /? for a list of functions.
You may need to shut down and restart powershell and/or consoles first prior to using chocolatey.
"@ | write-host
}

function Set-ChocolateyInstallFolder {
param(
  [string]$folder
)
  $environmentTarget = [System.EnvironmentVariableTarget]::User
  if (Test-ProcessAdminRights) {
    Write-Debug "Administrator installing so using Machine environment variable target instead of User."
    $environmentTarget = [System.EnvironmentVariableTarget]::Machine
  }
    Write-Host "Creating $chocInstallVariableName as an Environment variable (targeting `'$environmentTarget`') and setting it to `'$folder`'"
    Install-ChocolateyEnvironmentVariable -variableName "$chocInstallVariableName" -variableValue "$folder" -variableType $environmentTarget
}

function Get-ChocolateyInstallFolder(){
  [Environment]::GetEnvironmentVariable($chocInstallVariableName)
}

function Create-DirectoryIfNotExists($folderName){
  if (![System.IO.Directory]::Exists($folderName)) {[System.IO.Directory]::CreateDirectory($folderName) | Out-Null}
}

function Install-ChocolateyBinFiles {
param(
  [string] $chocolateyInstallPath,
  [string] $chocolateyExePath
)

  $redirectsPath = Join-Path $chocolateyInstallPath 'redirects'
  $exeFiles = Get-ChildItem "$redirectsPath" -include @("*.exe","*.cmd") -recurse
  foreach ($exeFile in $exeFiles) {
    $exeFilePath = $exeFile.FullName
    $exeFileName = [System.IO.Path]::GetFileName("$exeFilePath")
    $binFilePath = Join-Path $chocolateyExePath $exeFileName
    $binFilePathRename = $binFilePath + '.old'
    $batchFilePath = $binFilePath.Replace(".exe",".bat")
    $bashFilePath = $binFilePath.Replace(".exe","")
    if (Test-Path ($batchFilePath)) {Remove-Item $batchFilePath -force}
    if (Test-Path ($bashFilePath)) {Remove-Item $bashFilePath -force}
    if (Test-Path ($binFilePathRename)) {Remove-Item $binFilePathRename -force}
    if (Test-Path ($binFilePath)) {Move-Item -path $binFilePath -destination $binFilePathRename -force}

    Copy-Item -path $exeFilePath -destination $binFilePath -force
    $commandShortcut = [System.IO.Path]::GetFileNameWithoutExtension("$exeFilePath")
    Write-Debug "Added command $commandShortcut"
  }
}

function Install-ChocolateyFiles {
param(
  [string]$chocolateyPath
)

  $chocInstallFolder = Join-Path $thisScriptFolder "chocolateyInstall"
  Write-Host "Copying the contents of `'$chocInstallFolder`' to `'$chocolateyPath`'."
  if(test-path "$chocolateyPath\chocolateyInstall\functions") {
    Remove-Item "$chocolateyPath\chocolateyInstall\functions" -recurse -force
  }
  if(test-path "$chocolateyPath\chocolateyInstall\helpers") {
    Remove-Item "$chocolateyPath\chocolateyInstall\helpers" -recurse -force
  }
  Copy-Item $chocInstallFolder $chocolateyPath -recurse -force
}

function Initialize-ChocolateyPath {
param(
  [string]$chocolateyExePath = "$sysDrive\Chocolatey\bin",
  [string]$chocolateyExePathVariable = "%$($chocInstallVariableName)%\bin"
)

  $environmentTarget = [System.EnvironmentVariableTarget]::User
  if (Test-ProcessAdminRights) {
    Write-Debug "Administrator installing so using Machine environment variable target instead of User."
    $environmentTarget = [System.EnvironmentVariableTarget]::Machine
  }

  Install-ChocolateyPath -pathToInstall "$chocolateyExePath" -pathType $environmentTarget
}

function Process-ChocolateyBinFiles {
param(
  [string]$chocolateyExePath = "$($env:ALLUSERSPROFILE)\Chocolatey\bin",
  [string]$chocolateyExePathVariable = "%$($chocInstallVariableName)%\bin"
)
  $processedMarkerFile = Join-Path $chocolateyExePath '_processed.txt'
  if (!(test-path $processedMarkerFile)) {
    $files = get-childitem $chocolateyExePath -include *.bat -recurse
    if ($files -ne $null -and $files.Count -gt 0) {
      foreach ($file in $files) {
        Write-Host "Processing $($file.Name) to make it portable"
        $fileStream = [System.IO.File]::Open("$file", 'Open', 'Read', 'ReadWrite')
        $reader = New-Object System.IO.StreamReader($fileStream)
        $fileText = $reader.ReadToEnd()
        $reader.Close()
        $fileStream.Close()

        $fileText = $fileText.ToLower().Replace("`"" + $chocolateyPath.ToLower(), "SET DIR=%~dp0%`n""%DIR%..\").Replace("\\","\")

        Set-Content $file -Value $fileText -Encoding Ascii
      }
    }

    Set-Content $processedMarkerFile -Value "$([System.DateTime]::Now.Date)" -Encoding Ascii
  }
}

function Install-DotNet4IfMissing {
param(
  [string]$chocolateyInstallPath
)
  if([IntPtr]::Size -eq 8) {$fx="framework64"} else {$fx="framework"}

  if(!(test-path "$env:windir\Microsoft.Net\$fx\v4.0.30319")) {
    $NetFx4ClientUrl = 'http://download.microsoft.com/download/5/6/2/562A10F9-C9F4-4313-A044-9C94E0A8FAC8/dotNetFx40_Client_x86_x64.exe'
    $NetFx4FullUrl = 'http://download.microsoft.com/download/9/5/A/95A9616B-7A37-4AF6-BC36-D6EA96C8DAAE/dotNetFx40_Full_x86_x64.exe'
    Install-ChocolateyPackage "NetFx4.0" 'exe' -silentArgs "/q /norestart /repair /log `'$tempDir\NetFx4Install.log`'" -url "$NetFx4ClientUrl" -url64bit "$NetFx4ClientUrl" -validExitCodes @(0, 3010)
  }
}


Export-ModuleMember -function Initialize-Chocolatey;
