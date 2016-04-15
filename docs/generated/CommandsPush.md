# Chocolatey Push (choco push)
***NOTE***: 100% compatible with older chocolatey client (0.9.8.32 and below) with options and switches. Default push location is deprecated and will be removed by v1. In most cases you can still pass options and switches with one dash (`-`). See [[how to pass arguments|CommandsReference#how-to-pass-options--switches]] for more details.

Chocolatey will attempt to push a compiled nupkg to a package feed.
 Some may prefer to use `cpush` as a shortcut for `choco push`.

A feed can be a local folder, a file share, the community feed
 (`https://chocolatey.org/`), or a custom/private feed. For web
 feeds, it has a requirement that it implements the proper OData
 endpoints required for NuGet packages.

## Usage

    choco push [<path to nupkg>] [<options/switches>]
    cpush [<path to nupkg>] [<options/switches>]

**NOTE**: If there is more than one nupkg file in the folder, the command
 will require specifying the path to the file.

## Examples

    choco push --source https://chocolatey.org/
    choco push --source "https://chocolatey.org/" -t 500
    choco push --source "https://chocolatey.org/" -k="123-123123-123"

## Troubleshooting

To use this command, you must have your API key saved for the community
 feed (chocolatey.org) or the source you want to push to. Or you can
 explicitly pass the apikey to the command. See [[apikey command|CommandsApiKey]] help 
 for instructions on saving your key:

    choco apikey -?

A common error is `Failed to process request. 'The specified API key
 does not provide the authority to push packages.' The remote server
 returned an error: (403) Forbidden..` This means the package already
 exists with a different user (API key). The package could be unlisted.
 You can verify by going to https://chocolatey.org/packages/packageName.
 Please contact the administrators of https://chocolatey.org/ if you see this
 and you don't see a good reason for it.


## Options and Switches

Includes [[default options/switches|CommandsReference#default-options-and-switches]]

```
-s, --source=VALUE
  Source (REQUIRED) - The source we are pushing the package to. Use https://chocolatey.org/
  to push to community feed.

-k, --key, --apikey, --api-key=VALUE
  ApiKey - The api key for the source. If not specified (and not local
  file source), does a lookup. If not specified and one is not found for
  an https source, push will fail.

-t, --timeout=VALUE
  Timeout (in seconds) - The time to allow a package push to occur
  before timing out. Defaults to 300 seconds (5 minutes).
```
