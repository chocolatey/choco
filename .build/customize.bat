@echo on

::Project UppercuT - http://uppercut.googlecode.com
::No edits to this file are required - http://uppercut.pbwiki.com

if '%1' == '' goto usage
if '%2' == '' goto usage
if '%3' NEQ '' goto usage
if '%1' == '/?' goto usage
if '%1' == '-?' goto usage
if '%1' == '?' goto usage
if '%1' == '/help' goto usage

SET step.name=%1
SET step.type=%2


SET DIR=%cd%
SET BUILD_DIR=%~d0%~p0%
SET NANT="%DIR%\lib\Nant\nant.exe"
SET build.config.settings="%DIR%\.uppercut"

%NANT% /logger:"NAnt.Core.DefaultLogger" /quiet /nologo /f:"%BUILD_DIR%customize.build" -D:build.config.settings=%build.config.settings% -D:customize.step.name=%step.name% -D:customize.step.type=%step.type%

if %ERRORLEVEL% NEQ 0 goto errors

goto finish

:usage
echo.
echo Usage: .build\customize.bat stepName customizeType
echo stepName is the name of the item
echo customizeType is "pre" "post" or "replace"
echo .
echo Example: customize package.step post
echo.
goto finish

:errors
EXIT /B %ERRORLEVEL%

:finish
