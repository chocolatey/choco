#!/bin/bash
# stty -echo

# ::Project UppercuT - http://uppercut.googlecode.com
# ::No edits to this file are required - http://uppercut.pbwiki.com

function usage
{
	echo ""
	echo "Usage: build.sh"
	exit
}

function displayUsage
{
	case $1 in
		"/?"|"-?"|"?"|"/help") usage ;;
	esac
}

displayUsage $1

# http://www.michaelruck.de/2010/03/solving-pkg-config-and-mono-35-profile.html
# http://cloudgen.wordpress.com/2013/03/06/configure-nant-to-run-under-mono-3-06-beta-for-mac-osx/
export PKG_CONFIG_PATH=/opt/local/lib/pkgconfig:/Library/Frameworks/Mono.framework/Versions/Current/lib/pkgconfig:$PKG_CONFIG_PATH

#mono ./lib/NAnt/NAnt.exe /logger:"NAnt.Core.DefaultLogger" /nologo /quiet /f:"$(cd $(dirname "$0"); pwd)/.build/default.build" /D:build.config.settings="$(cd $(dirname "$0"); pwd)/.uppercut" /D:microsoft.framework="mono-3.5" $*
mono --runtime=v4.0.30319 ./lib/NAnt/NAnt.exe /logger:"NAnt.Core.DefaultLogger" /nologo /quiet /f:"$(cd $(dirname "$0"); pwd)/.build/default.build" /D:build.config.settings="$(cd $(dirname "$0"); pwd)/.uppercut" /D:microsoft.framework="mono-4.0" /D:run.ilmerge="false" /D:run.nuget="false" $*

#/quiet /nologo /debug /verbose /t:"mono-4.0"

