# Chocolatey Config (choco config)
***NOTE***: New in 0.9.9.9.

Chocolatey will allow you to interact with the configuration file settings.

## Usage

    choco config [list]|get|set [<options/switches>]

## Examples

    choco config
    choco config list
    choco config get cacheLocation
    choco config get --name cacheLocation
    choco config set cacheLocation c:\temp\choco
    choco config set --name cacheLocation --value c:\temp\choco

## Options and Switches

**NOTE**: Options and switches apply to all items passed, so if you are installing multiple packages, and you use `--version=1.0.0`, it is going to look for and try to install version 1.0.0 of every package passed. So please split out multiple package calls when wanting to pass specific options.

Includes [[default options/switches|CommandsReference#default-options-and-switches]]

```
     --name=VALUE
     Name - the name of the config setting. Required with some actions.
       Defaults to empty.

     --value=VALUE
     Value - the value of the config setting. Required with some actions.
       Defaults to empty.

```

## See It In Action

![Config shown in action](https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_config.gif)
