﻿$toolsDir = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"

"simple file" | Out-File "$toolsDir\simplefile.txt" -Force

Write-Output "This is $packageName v$packageVersion being installed to `n '$packageFolder'."
Write-Host "PowerShell Version is '$($PSVersionTable.PSVersion)' and CLR Version is '$($PSVersionTable.CLRVersion)'."
Write-Host "Execution Policy is '$(Get-ExecutionPolicy)'."
Write-Host "PSScriptRoot is '$PSScriptRoot'."
Write-Debug "A debug message."
Write-Verbose "Yo!"
Write-Warning "A warning!"
Write-Error "Oh no! An error"
throw "We had an error captain!"
