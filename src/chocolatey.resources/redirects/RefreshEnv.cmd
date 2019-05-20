@echo off
setlocal enableextensions enabledelayedexpansion
REM RefreshEnv.cmd

REM Batch file to read environment variables from registry and
REM set session variables to these values.

REM Running this batch file propagates modifications to existing -- or additions
REM to new -- variables set in the Windows Registry or System Environment
REM Variable Editor. If using PowerShell, do not use this script. Use refreshenv
REM from the Chocolatey Profile PowerShell module.
REM Note: this script does not erase variables not in the registry.
REM It also sets "convenience" PATH_HKCU and PATH_HKLM environment variables.

echo Refreshing environment variables from registry to active cmd.exe session.
echo ** WARNING **
echo If using PowerShell, do not use this script. Use refreshenv from Chocolatey (see
echo chocolatey.org/docs/troubleshooting#i-cant-get-the-powershell-tab-completion-working)

GOTO main

REM CALLd procedures.
REM Set one environment variable from registry key
:SetFromReg
    "%WinDir%\System32\Reg" QUERY "%~1" /v "%~2" > "%TEMP%\_envset.tmp" 2>NUL
    for /f "usebackq skip=2 tokens=2,*" %%A IN ("%TEMP%\_envset.tmp") do (
        REM If blank, don't erase the variable. Maybe force-set to space? Alt-255?
        IF /I NOT "%%~B"=="" (
            IF /I "%%~A"=="REG_EXPAND_SZ" (
                echo/call set "%~3=%%B"
            ) ELSE (
                set "tempvar=%%B"
                echo/call set "%~3=!tempvar:%%=%%%%!"
            )
        )
    )
    goto :EOF

REM Get a list of environment variables from registry
:GetRegEnv
    "%WinDir%\System32\Reg" QUERY "%~1" > "%TEMP%\_envget.tmp"
    for /f "usebackq skip=2" %%A IN ("%TEMP%\_envget.tmp") do (
        if /I not "%%~A"=="Path" (
            call :SetFromReg "%~1" "%%~A" "%%~A"
        )
    )
    goto :EOF

REM Processing begin
:main
    echo/@echo off >"%TEMP%\_env.cmd"

    REM Generate batch file, one line at a time first from System then User
    call :GetRegEnv "HKLM\System\CurrentControlSet\Control\Session Manager\Environment" >> "%TEMP%\_env.cmd"
    call :GetRegEnv "HKCU\Environment" >> "%TEMP%\_env.cmd"

    REM Special handling for PATH - mix both User and System
    call :SetFromReg "HKLM\System\CurrentControlSet\Control\Session Manager\Environment" Path Path_HKLM >> "%TEMP%\_env.cmd"
    call :SetFromReg "HKCU\Environment" Path Path_HKCU >> "%TEMP%\_env.cmd"
    call set "EXPATH=%%%%Path_HKLM%%%%;%%%%Path_HKCU%%%%"
    echo/call set "Path=!EXPATH!">> "%TEMP%\_env.cmd"

    REM Remove EXPATH, TEMPVAR, etc
    endlocal

    REM Capture user / architecture
    SET "OriginalUserName=%USERNAME%"
    SET "OriginalArchitecture=%PROCESSOR_ARCHITECTURE%"

    REM Re-set the variables
    call "%TEMP%\_env.cmd"

    REM Cleanup
    del /f /q "%TEMP%\_env.cmd" 2>nul
    del /f /q "%TEMP%\_envset.tmp" 2>nul
    del /f /q "%TEMP%\_envget.tmp" 2>nul

    REM reset user / architecture and exit
    SET "USERNAME=%OriginalUserName%"
    SET "PROCESSOR_ARCHITECTURE=%OriginalArchitecture%"
    echo Finished re-setting environment variables.
    echo/
