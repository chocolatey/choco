These packages can be used to test the installation or upgrading of packages that require an existing package to downgrade.

Each version is available as `upgradedowngradesdependency` and `downgradesdependency`. This is to allow testing of scenarios where `choco upgrade all` would process the dependency before and after the parent package.

- Version 1.0.0 contains a range that can be used in an upgrade scenario and has a dependency on `isdependency 1.0.0 or greater`
- Version 2.0.0 contains an exact dependency on `isdependency` with a version of `1.0.0`
