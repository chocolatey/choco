# Contributing Guidelines for `Chocolatey.PowerShell`

This document outlines some guidelines and design practices followed in the `Chocolatey.PowerShell` project.
It also highlights any important differences from how cmdlets are implemented here compared to the standard patterns for PowerShell cmdlets.

## Naming conventions

Cmdlet classes should be named `VerbNounCommand` and placed in the `Commands` folder and `Chocolatey.PowerShell.Commands` namespace.

## Inherit from `ChocolateyCmdlet` and **not** `PSCmdlet` or `Cmdlet`

`ChocolateyCmdlet` affords some additional helper methods and establishes patterns which can be easily reused across all Chocolatey cmdlets.
Note that you will still need to apply the standard `[Cmdlet(Verb, Noun)]` attribute on all cmdlet classes for PowerShell to recognise them.

### Overrides

Note that unlike `PSCmdlet`, `ChocolateyCmdlet` requires cmdlets to override the methods `Begin()`, `Process()`, and `End()`. These correspond to `begin {}`, `process {}`, and `end {}` blocks in a PowerShell function.

If unsure, follow these guidelines:
- Commands that do pipeline processing (declaring a parameter with `[Parameter(ValueFromPipeline = true)]`) will need to handle that input in `Process()`
- Commands not using pipeline input often handle the bulk of their processing in `End()`
- If there is any setup that may need to be handled in the command prior to processing any pipeline input, that can go in `Begin()`.

### Logging

Cmdlets inheriting from `ChocolateyCmdlet` will log their parameter values to debug logs when called by default. If any parameters may contain sensitive information, override the `Logging` property and set it to `false` to disable this behaviour.

### Output

By default, `ChocolateyCmdlet`'s `WriteObject(obj)` method will enumerate collections when outputting them, similar to how PowerShell's `Write-Output` works by default.
If you need to disable this, use the `WriteObject(obj, enumerateCollection: false)` overload.

### Helpers

`ChocolateyCmdlet` provides some helper methods for common operations that might be needed for many cmdlets.
Some of these (and many more) are also available on the `PSHelper` class.

## Place core logic in helper classes

For more general-purpose PowerShell helpers, add methods to the `PSHelper` so that these can remain in a centralised place.

For other more task-specific helpers:
- If there is a relevant helper class already present in `src/Chocolatey.PowerShell/Helpers`, add any needed methods to it and have the cmdlet call that method.
- If there is not already a relevant helper class, add a new one into this folder (and the `Chocolatey.PowerShell.Helpers` namespace).

Unlike in PowerShell functions, C# cmdlets cannot directly call into each other as easily (there is no supported way to instantiate one cmdlet from another in the C# PowerShell API without starting a new subshell and pipeline, which is excessively expensive).
As such, we need to ensure we leave methods that may need to be shared _not_ on the classes inheriting from `Cmdlet`/`PSCmdlet`/`ChocolateyCmdlet`.
To work around this, the majority of the core logic of a cmdlet should be placed in a helper class, so it can be easily called from other cmdlets (or helper classes) that may need to reuse the logic.

> :info: **Example**
> 
> Take the commands `Install-ChocolateyPackage` and `Install-ChocolateyInstallPackage` for example.
> `Install-ChocolateyPackage` needs to call `Install-ChocolateyInstallPackage` after downloading its installation files.
> To facilitate this, we can put the core logic of `Install-ChocolateyInstallPackage` into a helper class, and the actual `InstallChocolateyInstallPackageCommand` class we would write would only define parameters, then call into the helper class.
> Then, when we write the `InstallChocolateyPackageCommand`, it can download files and then call into the same helper class to run the other cmdlet's logic seamlessly.

### Helpers should return data or throw exceptions, not write output or errors

To avoid unexpected side effects, helper class methods should typically return the data to the calling cmdlet, and classes inheriting from `ChocolateyCmdlet` should be the only ones calling `WriteOutput()`.
This ensures that cmdlets are always aware of when output is written, and no unexpected output is written before the cmdlet is ready to write it.

Where possible, helpers should throw a standard or custom `Exception` type in cases of error, and leave constructing the `ErrorRecord` and calling `WriteError()` or `ThrowTerminatingError()` to the calling cmdlets.
Custom exception types or specific applicable .NET exception types should be used to give more specific error data where possible.
A common pattern for this would look like the following:

```csharp
// In a given GetSomethingCommand.cs class
try
{
    var result = HelperClass.CoreLogicMethod();
    WriteObject(result);
}
// Standard .NET ecosystem exceptions, where applicable
catch (FileNotFoundException error)
{
    var errorRecord = new ErrorRecord(error, $"{ErrorId}.ItemNotFound", ErrorCategory.ObjectNotFound, targetObject: PathParameter);
    WriteError(errorRecord);
}
// Custom exception types, where applicable
catch (CustomException error)
{
    var errorRecord = new ErrorRecord(error, $"{ErrorId}.CustomError", ErrorCategory.InvalidResult, targetObject: SomeParameter);
    WriteError(errorRecord);
}
// It's good practice to also include a generic catch to wrap any other error types,
// otherwise PowerShell re-wraps these as RuntimeException when they surface.
catch (Exception error)
{
    var errorRecord = new ErrorRecord(error, $"{ErrorId}.Unknown", ErrorCategory.NotSpecified, targetObject: PathParameter);
    WriteError(errorRecord);
}
```

Error records should always include:
- The underlying `Exception` object.
- A specific error ID, including the cmdlet's own `ErrorId` property, which will always name the cmdlet type.
- An appropriate `ErrorCategory`. See [here](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.errorcategory) for a list of valid category values.
- The cmdlet's target object reference. This may be an `InputObject` parameter for pipeline cmdlets, a `Path` parameter, or any other primary data that indicates the "target" of the command. In some very unusual cases, this _may_ be `null`, if we know the error being thrown does not correspond to any input data.

Note that `WriteError()` _does not_ terminate execution.
If you need to throw a terminating error and terminate the cmdlet's operations (including preventing any further calls to `Process()` for pipeline cmdlets), use `ThrowTerminatingError()` as an alternative to `WriteError()`.

## Add `ShouldProcess` support where applicable

As a _very_ brief and reductive overview, if a cmdlet is making changes to the user's machine, it should implement `ShouldProcess` support.
This enables the built-in PowerShell functionality for `-WhatIf` and `-Confirm` parameters on the cmdlet.

### Implementing `ShouldProcess`

To implement `ShouldProcess` correctly, the following conditions must be met:

1. The cmdlet class should be decorated with `[Cmdlet(SupportsShouldProcess = true)]` (in other words: add `SupportsShouldProcess = true` to the existing property declarations in the `Cmdlet` attribute)
2. In any logic paths where we might be making changes to the user's machine (installing, uninstalling, modifying the registry, making changes to the user or machine environment variables, creating or deleting non-temporary files, running external applications, and so on) we need to be wrapping those code paths in an `if` check like so:
  ```csharp
  if (ShouldProcess("target item (path, object, env var name, description of what is being actually modified)", "description of the action to be performed"))
  {
	  // Code to run that does the described action.
  }
  ```

> :memo: **Note**
>
> If this check takes place in a helper class rather than the main cmdlet class, remember that the `ShouldProcess` method only exists on the `PSCmdlet` base class.
> To call `ShouldProcess` from a helper class, a `PSCmdlet` parameter can be passed to the method, and cmdlets calling into it can simply pass `this` for that parameter value: `HelperClass.MethodName(this)`
> Then in the helper class, it can be called on the parameter, similar to this: `cmdlet.ShouldProcess(...)`

Ideally these checks should take place at the deepest or most narrow point that is sensible in the code path, bypassing _only_ the code which actually makes changes to the system where possible.
This pattern enables us to use `-WhatIf` to verify code paths when invoked from a cmdlet, without risking making any permanent changes to the testing environment.

For more information on ShouldProcess, see [Everything you wanted to know about ShouldProcess](https://learn.microsoft.com/en-us/powershell/scripting/learn/deep-dives/everything-about-shouldprocess?view=powershell-7.4).

## No dependency injection

In most areas of the Chocolatey CLI codebase, we use a SimpleInjector framework for dependency injection.
This is not available in the Chocolatey.PowerShell library, as it is intentionally decoupled from the other CLI projects and should never depend on any of the other CLI projects.

Additionally, since all cmdlets are initialised by the PowerShell runtime, we cannot use a dependency injection framework to make things work.
As a result, many of the helpers will be a little more bare-bones and not make as heavy use of interfaces as other code areas in this repository.

## Interop with Chocolatey CLI

There are some cases where certain data (such as configuration values, or enabled features, and so on) need to be communicated to the PowerShell cmdlets.
For this, the typical pattern is to:

1. Add an environment variable name to the `Chocolatey.PowerShell.Shared.EnvironmentVariables` class, typically prefixed with `Chocolatey` as part of its name.
2. Add a corresponding name to the `chocolatey.infrastructure.app.ApplicationParameters.Environment` class.
3. Amend the `PreparePowerShellEnvironment()` method in the `chocolatey.infrastructure.app.services.PowerShellService` class to set the value of the new environment variable appropriately.
4. In the cmdlet code or helper class, retrieve the environment variable value using the `Chocolatey.PowerShell.Helpers.EnvironmentHelper.GetVariable(cmdlet, EnvironmentVariables.ChocolateyVariableName, EnvironmentVariableTarget.Process)` method.

When relying on interop with Chocolatey CLI, ensure the default behaviour if the environment variable cannot be found is sensible.
For example, if it requires an opt-in feature, the default behaviour should be to assume the feature is not enabled.
If it requires certain configuration data to function, assume the functionality should be disabled or an error thrown if there is not a sensible default configuration set.

## Deprecating old command names

For deprecating an old command name, we have the following process:
1. Add an alias to the `chocolateyInstaller.psm1` file in the `chocolatey.resources` project, pointing to the new command name.
2. Rename the command and its class appropriately (cmdlet classes are named after the command's name, with a `Command` suffix, so `Get-FileHash` would be `GetFileHashCommand`).
3. Add an entry to the `_deprecatedCommandNames` list/dictionary provided on `ChocolateyCmdlet`, listing the old command name and the new.
4. File an issue targeted at the next major version to remove the alias, completing the deprecation cycle.
	1. Issues should also be added to the Package Validator and its associated extension repositories to add warning rules for package maintainers to be made aware of the impending removal of the old command name.