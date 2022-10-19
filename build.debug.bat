@echo off
set psscript="%~dp0build.ps1"
echo ==================================================
echo ============= WRAP POWERSHELL SCRIPT =============
echo ==================================================

echo calling %psscript% with args %*
PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& '%psscript%' -Configuration Debug %*"
set buildstatus=%ERRORLEVEL%
echo ==================================================
exit /b %buildstatus%
