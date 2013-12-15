@echo off

SET DIR=%~dp0%

%windir%\System32\WindowsPowerShell\v1.0\powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "$Error.Clear();& '%DIR%setup.ps1' %*;Exit $Error.Count + $LastExitCode"

pause