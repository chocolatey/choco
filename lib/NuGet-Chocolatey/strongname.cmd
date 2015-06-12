@echo off
SET DIR=%~d0%~p0%

::%comspec% /k ""C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat"" x86
::no need to ildasm/al

echo Make sure you have webtransform here for the merge. Continue?
pause

%DIR%..\ILMerge\ILMerge.exe NuGet.Core.dll /keyfile:%DIR%..\..\chocolatey.snk /out:%DIR%NuGet.Core.dll /targetplatform:v4 /log:%DIR%ILMerge.DELETE.log /ndebug /allowDup