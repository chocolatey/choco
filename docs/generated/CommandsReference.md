# Command Reference

This is a listing of all of the different things you can pass to choco.

## Commands

 * [[list|Commandslist]] - lists remote or local packages
 * [[search|Commandssearch]] - searches remote or local packages (alias for list)
 * [[info|Commandsinfo]] - retrieves package information. Shorthand for choco search pkgname --exact --verbose
 * [[install|Commandsinstall]] - installs packages from various sources
 * [[pin|Commandspin]] - suppress upgrades for a package
 * [[outdated|Commandsoutdated]] - retrieves packages that are outdated. Similar to upgrade all --noop
 * [[upgrade|Commandsupgrade]] - upgrades packages from various sources
 * [[uninstall|Commandsuninstall]] - uninstalls a package
 * [[pack|Commandspack]] - packages up a nuspec to a compiled nupkg
 * [[push|Commandspush]] - pushes a compiled nupkg
 * [[new|Commandsnew]] - generates files necessary for a chocolatey package from a template
 * [[sources|Commandssources]] - view and configure default sources (alias for source)
 * [[source|Commandssource]] - view and configure default sources
 * [[config|Commandsconfig]] - Retrieve and configure config file settings
 * [[feature|Commandsfeature]] - view and configure choco features
 * [[features|Commandsfeatures]] - view and configure choco features (alias for feature)
 * [[apikey|Commandsapikey]] - retrieves or saves an apikey for a particular source
 * [[setapikey|Commandssetapikey]] - retrieves or saves an apikey for a particular source (alias for apikey)
 * [[unpackself|Commandsunpackself]] - have chocolatey set it self up
 * [[version|Commandsversion]] - [DEPRECATED] will be removed in v1 - use [[`choco outdated`|Commandsoutdated]] or `cup <pkg|all> -whatif` instead
 * [[update|Commandsupdate]] - [DEPRECATED] RESERVED for future use (you are looking for upgrade, these are not the droids you are looking for)
 * [[download|Commandsdownload]] - downloads packages - optionally downloading and internalizing all remote resources (recompiling)


Please run chocolatey with `choco command -help` for specific help on
 each command.

## How To Pass Options / Switches

You can pass options and switches in the following ways:

 * Unless stated otherwise, an option/switch should only be passed one
   time. Otherwise you may find weird/non-supported behavior.
 * `-`, `/`, or `--` (one character switches should not use `--`)
 * **Option Bundling / Bundled Options**: One character switches can be
   bundled. e.g. `-d` (debug), `-f` (force), `-v` (verbose), and `-y`
   (confirm yes) can be bundled as `-dfvy`.
 * **NOTE:** If `debug` or `verbose` are bundled with local options
   (not the global ones above), some logging may not show up until after
   the local options are parsed.
 * **Use Equals**: You can also include or not include an equals sign
   `=` between options and values.
 * **Quote Values**: When you need to quote an entire argument, such as
   when using spaces, please use a combination of double quotes and
   apostrophes (`"'value'"`). In cmd.exe you can just use double quotes
   (`"value"`) but in powershell.exe you should use backticks
   (`` `"value`" ``) or apostrophes (`'value'`). Using the combination
   allows for both shells to work without issue, except for when the next
   section applies.
 * **Pass quotes in arguments**: When you need to pass quoted values to
   to something like a native installer, you are in for a world of fun. In
   cmd.exe you must pass it like this: `-ia "/yo=""Spaces spaces"""`. In
   PowerShell.exe, you must pass it like this: `-ia '/yo=""Spaces spaces""'`.
   No other combination will work. In PowerShell.exe if you are on version
   v3+, you can try `--%` before `-ia` to just pass the args through as is,
   which means it should not require any special workarounds.
 * Options and switches apply to all items passed, so if you are
   installing multiple packages, and you use `--version=1.0.0`, choco
   is going to look for and try to install version 1.0.0 of every
   package passed. So please split out multiple package calls when
   wanting to pass specific options.

## See Help Menu In Action

![choco help in action](https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_help.gif)

## Default Options and Switches

**NOTE:** Options and switches apply to all items passed, so if you are
 running a command like install that allows installing multiple
 packages, and you use `--version=1.0.0`, it is going to look for and
 try to install version 1.0.0 of every package passed. So please split
 out multiple package calls when wanting to pass specific options.

~~~

 -?, --help, -h
     Prints out the help menu.

 -d, --debug
     Debug - Show debug messaging.

 -v, --verbose
     Verbose - Show verbose messaging.

     --acceptlicense, --accept-license
     AcceptLicense - Accept license dialogs automatically. Reserved for 
       future use.

 -y, --yes, --confirm
     Confirm all prompts - Chooses affirmative answer instead of prompting. 
       Implies --accept-license

 -f, --force
     Force - force the behavior. Do not use force during normal operation - 
       it subverts some of the smart behavior for commands.

     --noop, --whatif, --what-if
     NoOp / WhatIf - Don't actually do anything.

 -r, --limitoutput, --limit-output
     LimitOutput - Limit the output to essential information

     --timeout, --execution-timeout=VALUE
     CommandExecutionTimeout (in seconds) - The time to allow a command to 
       finish before timing out. Overrides the default execution timeout in the 
       configuration of 2700 seconds.

 -c, --cache, --cachelocation, --cache-location=VALUE
     CacheLocation - Location for download cache, defaults to %TEMP% or value 
       in chocolatey.config file.

     --allowunofficial, --allow-unofficial, --allowunofficialbuild, --allow-unofficial-build
     AllowUnofficialBuild - When not using the official build you must set 
       this flag for choco to continue.

     --failstderr, --failonstderr, --fail-on-stderr, --fail-on-standard-error, --fail-on-error-output
     FailOnStandardError - Fail on standard error output (stderr), typically 
       received when running external commands during install providers. This 
       overrides the feature failOnStandardError.

     --use-system-powershell
     UseSystemPowerShell - Execute PowerShell using an external process 
       instead of the built-in PowerShell host. Should only be used when 
       internal host is failing. Available in 0.9.10+.

~~~



***NOTE:*** This documentation has been automatically generated from `choco -h`. 

