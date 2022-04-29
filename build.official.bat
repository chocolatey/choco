@echo off
setlocal enableextensions enabledelayedexpansion
set psscript="%~dp0/build.ps1"
echo ==================================================
echo ============= WRAP POWERSHELL SCRIPT =============
echo ==================================================

echo calling %psscript% with args %*
PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& '%psscript%' -Configuration ReleaseOfficial %*"

echo ==================================================
endlocal