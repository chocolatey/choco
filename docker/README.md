Building Docker Image
=====================

This directory contains the necessary files for building a Docker Images. There is a Windows based image which runs choco on .NET and a Linux based image that builds and runs choco with Mono.

To build the Windows image yourself, follow these steps:

1. Clone down the repository on a Windows system using `git clone https://github.com/chocolatey/choco.git`.
1. Change directories to the root of the repository.
1. Either build choco on the host or put a pre-built chocolatey `.nupkg` in `.\code_drop\Packages\Chocolatey`.
    * See the README at the root of the repository for instructions on how to build choco.
1. Run the docker build command. `docker build -t choco:latest-windows -f docker/Dockerfile.windows .` (the trailing . is important)
    * To change the version of the servercore image used, add the argument `--build-arg tagversion=servercore-tag`
    * To build a official version, put an official `.nupkg` in `.\code_drop\Packages\Chocolatey` and change the image name to `chocolatey/choco:latest-windows`
    * If you get messages similar to "Can't add file x to tar: archive/tar: missed writing 794 bytes", make sure Visual Studio is closed.
1. Run your new image using the command `docker run -ti --rm choco:latest-windows cmd.exe`
1. Test choco by running `choco -h`. You should see the help message from choco.exe.

To build the Linux image yourself, follow these steps:

1. Clone down the repository using `git clone https://github.com/chocolatey/choco.git`.
1. Change directories to the root of the repository.
1. Run the docker build command. `docker build -t choco:latest-linux -f docker/Dockerfile.linux .` (the trailing . is important)
    * To build a official version, use this command: `docker build -t chocolatey/choco:latest-linux -f docker/Dockerfile.linux . --build-arg buildscript=build.official.sh`
    * To build a debug version, add the argument `--build-arg buildscript=build.debug.sh`
    * To change the version of mono used, add the argument `--build-arg monoversion=mono-tag`
1. Run your new image using the command `docker run -ti --rm choco:latest-linux /bin/bash`
1. Test choco by running `choco -h`. You should see the help message from choco.exe.

