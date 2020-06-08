@echo off
SET DIR=%~d0%~p0%

::%comspec% /k ""C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat"" x86
::no need to ildasm/al

echo Make sure you have webtransform here for the merge. Continue?
pause

mkdir %DIR%\output

%DIR%..\ILMerge\ILMerge.exe NuGet.Core.dll /keyfile:%DIR%..\..\chocolatey.snk /out:%DIR%\output\NuGet.Core.dll /targetplatform:v4,"%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0" /log:%DIR%ILMerge.DELETE.log /allowDup
REM %DIR%..\ILMerge\ILMerge.exe NuGet.Core.dll /keyfile:%DIR%..\..\chocolatey.snk /out:%DIR%NuGet.Core.dll /targetplatform:v4,"%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0" /log:%DIR%ILMerge.DELETE.log /ndebug /allowDup

echo Copy the files from the output directory up to this directory and delete everything but this file, NuGet.Core.dll and NuGet.Core.pdb.
pause