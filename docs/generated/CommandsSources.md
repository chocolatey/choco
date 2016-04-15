# Chocolatey Source (choco source)
***NOTE***: Mostly compatible with older chocolatey client (0.9.8.32 and below) with options and switches. When enabling, disabling or removing a source, use `-name` in front of the option now. In most cases you can still pass options and switches with one dash (`-`). See [[how to pass arguments|CommandsReference#how-to-pass-options--switches]] for more details.

Chocolatey will allow you to interact with sources.

## Usage

    choco source [list]|add|remove|disable|enable [<options/switches>]
    choco sources [list]|add|remove|disable|enable [<options/switches>]

## Examples

    choco source
    choco source list
    choco source add -n=bob -s "https://somewhere/out/there/api/v2/"
    choco source add -n=bob -s "https://somewhere/out/there/api/v2/" -u=bob -p=12345
    choco source disable -n=bob
    choco source enable -n=bob
    choco source remove -n=bob

## Options and Switches

Includes [[default options/switches|CommandsReference#default-options-and-switches]]

```
-n, --name=VALUE
  Name - the name of the source. Required with some actions. Defaults to
  empty.

-s, --source=VALUE
  Source - The source. Defaults to empty.

-u, --user=VALUE
  User - used with authenticated feeds. Defaults to empty.

-p, --password=VALUE
  Password - the user's password to the source. Encrypted in
  chocolatey.config file.
```
