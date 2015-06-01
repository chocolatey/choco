@echo off

::Project UppercuT - http://uppercut.googlecode.com

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

SET APP_BDDDOC="%DIR%\lib\bdddoc\bdddoc.console.exe"
SET TEST_ASSEMBLY_NAME="chocolatey.tests.integration.dll"


%NANT% /logger:"NAnt.Core.DefaultLogger" /quiet /nologo /f:"%BUILD_DIR%.build\compile.step" -D:build.config.settings=%build.config.settings%

if %ERRORLEVEL% NEQ 0 goto errors

::echo "This step of running integration specs can take a long time with no output"

::%NANT% /logger:"NAnt.Core.DefaultLogger" /nologo /f:"%BUILD_DIR%.build\analyzers\test.step" all -D:build.config.settings=%build.config.settings%
%NANT% /logger:"NAnt.Core.DefaultLogger" /nologo /f:"%BUILD_DIR%.build.custom\bdddoc.build" -D:build.config.settings=%build.config.settings% -D:app.bdddoc=%APP_BDDDOC% -D:test_assembly=%TEST_ASSEMBLY_NAME%

if %ERRORLEVEL% NEQ 0 goto errors

goto finish

:usage
echo.
echo Usage: bdddoc.bat
echo.
goto finish

:errors
EXIT /B %ERRORLEVEL%

:finish