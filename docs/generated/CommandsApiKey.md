# Chocolatey Upgrade (choco apikey)
This lists api keys that are a set, or sets an api key, for a particular
 source so it doesn't need to be specified every time.

Anything that doesn't contain source and key will list api keys.

## Usage

    choco apikey [<options/switches>]
    choco setapikey [<options/switches>]

## Examples

    choco apikey
    choco apikey -s"https://somewhere/out/there"
    choco apikey -s"https://somewhere/out/there/" -k="value"
    choco apikey -s"https://chocolatey.org/" -k="123-123123-123"

## Connecting to Chocolatey.org

In order to save your API key for https://chocolatey.org/,
 log in (or register, confirm and then log in) to
 https://chocolatey.org/, go to https://chocolatey.org/account,
 copy the API Key, and then use it in the following command:

    choco apikey -k <your key here> -s https://chocolatey.org/

## Options and Switches

Includes [[default options/switches|CommandsReference#default-options-and-switches]]

```
-s, --source=VALUE
  Source [REQUIRED] - The source location for the key

-k, --key, --apikey, --api-key=VALUE
  ApiKey - The api key for the source.
```
