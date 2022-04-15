This package can be used for testing both downloading remote MSI installers from a known location (GitHub),
as well as testing whether the new functionality in 0.10.16 that adds the ability to run a before install block
works as expected.

Additionally, this package can be used to test against different type of checksums as well.
The parameter '/Algorithm' can be used to test the actual algorithm, and the parameter '/Checksum' can be used
to test with a different checksum value.