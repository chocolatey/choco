# Chocolatey List (choco list) / Search (choco search)
***NOTE***: 100% compatible with older chocolatey client (0.9.8.32 and below) with options and switches. In most cases you can still pass options and switches with one dash (`-`). See [[how to pass arguments|CommandsReference#how-to-pass-options--switches]] for more details.

Chocolatey will perform a search for a package local or remote.  Some
 may prefer to use `clist` as a shortcut for `choco list`.

## Usage

    choco search <filter> [<options/switches>]
    choco list <filter> [<options/switches>]
    clist <filter> [<options/switches>]

## Examples

    choco list --local-only
    choco list -li
    choco list -lai
    choco search git
    choco search git -s "https://somewhere/out/there"
    choco search bob -s "https://somewhere/protected" -u user -p pass

## Options and Switches

Includes [[default options/switches|CommandsReference#default-options-and-switches]]

```
-s, --source=VALUE
  Source - Source location for install. Can include special 'webpi'.
  Defaults to sources.

-l, --lo, --localonly, --local-only
  LocalOnly - Only search against local machine items.

-i, --includeprograms, --include-programs
  IncludePrograms - Used in conjuction with LocalOnly, filters out apps
  Chocolatey has listed as packages and includes those in the list.
  Defaults to false.

-a, --all, --allversions, --all-versions
  AllVersions - include results from all versions.

 -u, --user=VALUE
     User - used with authenticated feeds. Defaults to empty.

 -p, --password=VALUE
     Password - the user's password to the source. Defaults to empty.

### 0.9.10.0+

     --page=VALUE
     Page - the 'page' of results to return. Defaults to return all results.

     --page-size=VALUE
     Page Size - the amount of package results to return per page. Defaults
       to 25.

 -e, --exact
     Exact - Only return packages with this exact name.

     --by-id-only
     ByIdOnly - Only return packages where the id contains the search filter.

     --id-starts-with
     IdStartsWith - Only return packages where the id starts with the search
       filter.

     --order-by-popularity
     OrderByPopularity - Sort by package results by popularity.

     --approved-only
     ApprovedOnly - Only return approved packages - this option will filter
       out results not from the community repository.

     --download-cache, --download-cache-only
     DownloadCacheAvailable - Only return packages that have a download cache
       available - this option will filter out results not from the community
       repository.

     --not-broken
     NotBroken - Only return packages that are not failing testing - this
       option only filters out failing results from the community feed. It will
       not filter against other sources.
```

## See It In Action

![choco search](https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_search.gif)

## Alternative Sources 
0.9.10+

### WebPI
This specifies the source is Web PI (Web Platform Installer) and that we
are searching for a WebPI product, such as IISExpress. If you do not
have the Web PI command line installed, it will install that first and
then perform the search requested.
e.g. `choco list --source webpi`

### Windows Features
This specifies that the source is a Windows Feature and we should
install via the Deployment Image Servicing and Management tool (DISM) on
the local machine.
e.g. `choco list --source windowsfeatures`