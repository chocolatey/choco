# circulardependency1

## Purpose

The purpose of this package is for testing of circular dependencies. Currently Chocolatey prevents them on a `choco install`, but choco-licensed `convert` and `push` commands currently don't prevent it.

Current (as of October 1 2021) behaviour:

* `choco install circulardependency1` results in an error indicating that a circular dependency has been detected.
* `choco convert circulardependency1.0.0.1.nupkg --to intune` results in `intunewin` files being created
* `choco push circulardependency1.0.0.1.intunewin` results in a circular loop indicating `circulardepency[12] is not in Intune` until the Intune authentication token expires at which point you'll be told it's expired.
