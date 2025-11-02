## About this project

The Chocolatey.PowerShell project within the Chocolatey CLI solution is the compiled PowerShell cmdlets.

## Debugging the Chocolatey.PowerShell cmdlets

Because the Chocolatey.PowerShell module is a compiled module, debugging it is a little more involved than a script based module.
If you need to debug cmdlets within a Chocolatey CLI execution, you can merely create a package that calls the Chocolatey.PowerShell function you are debugging, and it will work.
However, if you need to test the functions outside of a Chocolatey CLI invocation, that is a little bit more involved.
Fortunately, Marc-Andr� Moreau provides and [excellent guide](https://awakecoding.com/posts/debugging-powershell-binary-modules-in-visual-studio/) on debugging a binary PowerShell module from Visual Studio.
The below steps are greatly simplified and targetted specifically at the Chocolatey.PowerShell project.

1. Edit or create a `Chocolatey.PowerShell.csproj.user` file beside the `Chocolatey.PowerShell.csproj` file (located at `$ChocoSourceRepository/src/Chocolatey.PowerShell`).
1. Set it's contents to the XML following this list.
1. (Re)Launch Visual Studio to have it pick up the addition of the `Chocolatey.PowerShell.csproj.user` file.
1. Set the `Chocolatey.PowerShell` project as the startup project.
1. Start the debugger.
1. In the Windows PowerShell that launched, import the module with `Import-Module ./Chocolatey.PowerShell.dll`.
1. Now when you call any of the compiled PowerShell files, you will be able to stop at set breakpoints and debug as normal.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|AnyCPU'">
    <StartAction>Program</StartAction>
    <StartProgram>c:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe</StartProgram>
  </PropertyGroup>
</Project>
```
