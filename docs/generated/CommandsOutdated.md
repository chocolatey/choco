# Chocolatey Outdated (choco outdated)
***NOTE***: New in 0.9.9.6.

Returns a list of outdated packages

## Usage

    choco outdated [<options/switches>]

## Examples

    choco outdated
    choco outdated -s "https://somewhere/out/there"
    choco outdated -s "https://somewhere/protected" -u user -p pass

If you use `--source=https://somewhere/out/there`, it is going to look for outdated packages only based on that source.

## Options and Switches

**NOTE**: Options and switches apply to all items passed, so if you are installing multiple packages, and you use `--version=1.0.0`, it is going to look for and try to install version 1.0.0 of every package passed. So please split out multiple package calls when wanting to pass specific options.

Includes [[default options/switches|CommandsReference#default-options-and-switches]]

```
 -s, --source=VALUE
     Source - The source to find the package(s) updates. Defaults to
       default feeds.

 -u, --user=VALUE
     User - used with authenticated feeds. Defaults to empty.

 -p, --password=VALUE
     Password - the user's password to the source. Defaults to empty.
```

## See It In Action

![choco outdated](https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_outdated.gif)
