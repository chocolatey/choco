This package can be used to test the installation or upgrading of packages that have out-of-range dependencies.

- Version 1.0.0 Contains a valid range that can be used in an upgrade scenario and has an exact version dependency on `hasdependency 1.0.0`
- Version 2.0.0 Contains an invalid maximum dependency on `hasdependency` with a version lower than `1.0.0`
- Version 2.0.1 Contains an invalid dependency on `hasdependency` with versions between `1.1.1` and `1.4.0`
- Version 2.0.2 Contains an invalid dependency on `hasdependency` with an exact version of `1.3.0`
- Version 2.0.3 Contains an invalid minimum dependency on `hasdependency` with a version higher than `2.2.0`
