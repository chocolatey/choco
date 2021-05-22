@echo off
::
:: RefreshEnv.cmd
::
:: Batch file to read environment variables from registry and
:: set session variables to these values.
::
:: With this batch file, there should be no need to reload command
:: environment every time you want environment changes to propagate

::echo "RefreshEnv.cmd only works from cmd.exe, please install the Chocolatey Profile to take advantage of refreshenv from PowerShell"
echo | set /p dummy="Refreshing environment variables from registry for cmd.exe. Please wait..."

:: Generate unique file names
for /F "tokens=* usebackq" %%F IN (`powershell -command "[guid]::NewGuid().ToString()"`) do set REFRESHENV_GUID=%%F
set REFRESHENV_ENVSET_TMP="%TEMP%\refreshenv_envset_%REFRESHENV_GUID%.tmp"
set REFRESHENV_ENVGET_TMP="%TEMP%\refreshenv_envget_%REFRESHENV_GUID%.tmp"
set REFRESHENV_ENV_CMD="%TEMP%\refreshenv_env_%REFRESHENV_GUID%.cmd"

goto main

:: Set one environment variable from registry key
:SetFromReg
    "%WinDir%\System32\Reg" QUERY "%~1" /v "%~2" > %REFRESHENV_ENVSET_TMP% 2>NUL
    for /f "usebackq skip=2 tokens=2,*" %%A IN (%REFRESHENV_ENVSET_TMP%) do (
        echo/set "%~3=%%B"
    )
    goto :EOF

:: Get a list of environment variables from registry
:GetRegEnv
    "%WinDir%\System32\Reg" QUERY "%~1" > %REFRESHENV_ENVGET_TMP%
    for /f "usebackq skip=2" %%A IN (%REFRESHENV_ENVGET_TMP%) do (
        if /I not "%%~A"=="Path" (
            call :SetFromReg "%~1" "%%~A" "%%~A"
        )
    )
    goto :EOF

:main
    echo/@echo off >%REFRESHENV_ENV_CMD%

    :: Slowly generating final file
    call :GetRegEnv "HKLM\System\CurrentControlSet\Control\Session Manager\Environment" >> %REFRESHENV_ENV_CMD%
    call :GetRegEnv "HKCU\Environment">>%REFRESHENV_ENV_CMD% >> %REFRESHENV_ENV_CMD%

    :: Special handling for PATH - mix both User and System
    call :SetFromReg "HKLM\System\CurrentControlSet\Control\Session Manager\Environment" Path Path_HKLM >> %REFRESHENV_ENV_CMD%
    call :SetFromReg "HKCU\Environment" Path Path_HKCU >> %REFRESHENV_ENV_CMD%

    :: Caution: do not insert space-chars before >> redirection sign
    echo/set "Path=%%Path_HKLM%%;%%Path_HKCU%%" >> %REFRESHENV_ENV_CMD%

    :: Cleanup
    del /f /q %REFRESHENV_ENVSET_TMP% 2>nul
    del /f /q %REFRESHENV_ENVGET_TMP% 2>nul
    set REFRESHENV_GUID=
    set REFRESHENV_ENVSET_TMP=
    set REFRESHENV_ENVGET_TMP=

    :: capture user / architecture
    SET "OriginalUserName=%USERNAME%"
    SET "OriginalArchitecture=%PROCESSOR_ARCHITECTURE%"

    :: Set these variables
    call %REFRESHENV_ENV_CMD%

    :: Cleanup
    del /f /q %REFRESHENV_ENV_CMD% 2>nul
    set REFRESHENV_ENV_CMD=

    :: reset user / architecture
    SET "USERNAME=%OriginalUserName%"
    SET "PROCESSOR_ARCHITECTURE=%OriginalArchitecture%"

    echo | set /p dummy="Finished."
    echo .
