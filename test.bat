@echo off

::Project UppercuT - http://uppercut.googlecode.com
::No edits to this file are required - http://uppercut.pbwiki.com

if '%2' NEQ '' goto usage
if '%3' NEQ '' goto usage
if '%1' == '/?' goto usage
if '%1' == '-?' goto usage
if '%1' == '?' goto usage
if '%1' == '/help' goto usage

SET DIR=%cd%
SET BUILD_DIR=%~d0%~p0%
SET NANT="%BUILD_DIR%lib\Nant\nant.exe"
SET build.config.settings="%DIR%\.uppercut"

%NANT% /logger:"NAnt.Core.DefaultLogger" /quiet /nologo /f:"%BUILD_DIR%.build\compile.step" -D:build.config.settings=%build.config.settings%

if %ERRORLEVEL% NEQ 0 goto errors

%NANT% /logger:"NAnt.Core.DefaultLogger" /nologo /f:"%BUILD_DIR%.build\analyzers\test.step" %1 -D:build.config.settings=%build.config.settings%

if %ERRORLEVEL% NEQ 0 goto errors

goto finish

:usage
echo.
echo Usage: test.bat
echo Usage: test.bat all - to run all tests
echo.
goto finish

:errors
EXIT /B %ERRORLEVEL%

:finish
