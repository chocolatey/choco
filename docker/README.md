Building Docker Image
=====================

This directory contains the necessary Dockerfile and wrapper script for building a Docker Image. This is a Linux based image that builds and runs choco.exe with mono.

To build this image yourself, follow these steps:

1. Clone down the repository using `git clone https://github.com/chocolatey/choco.git`.
1. Change directories to the root of the repository.
1. Run the docker build command. `docker build -t mono-choco -f docker/Dockerfile.linux .` (the trailing . is important)
    * To build a official version, add the argument `--build-arg buildscript=build.official.sh`
    * To build a debug version, add the argument `--build-arg buildscript=build.debug.sh`
    * To change the version of mono used, add the argument `--build-arg monoversion=mono-tag`
1. Run your new image using the command `docker run -ti --rm mono-choco /bin/bash`
1. Test choco by running `choco -h`. You should see the help message from choco.exe.

