FROM mono:3.12.1

MAINTAINER Justin Phelps <linuturk@onitato.com>

COPY . /usr/local/src/choco/

WORKDIR /usr/local/src/choco
RUN chmod +x build.sh
RUN chmod +x zip.sh
RUN ./build.sh

WORKDIR /usr/local/bin
RUN ln -s /usr/local/src/choco/build_output/chocolatey

COPY docker/choco_wrapper /usr/local/bin/choco

WORKDIR /root
