#!/bin/bash
# stty -echo

# ::Project UppercuT - http://uppercut.googlecode.com
# ::No edits to this file are required - http://uppercut.pbwiki.com

function usage
{
	echo ""
	echo "Usage: test.sh - to run unit tests"
	echo "Usage: test.sh integration - run integration tests"   
	echo "Usage: test.sh all - to run all tests"
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

mono --runtime=v4.0.30319 ./lib/NAnt/NAnt.exe /logger:"NAnt.Core.DefaultLogger" /nologo /quiet /f:"$(cd $(dirname "$0"); pwd)/.build/compile.step" /D:build.config.settings="$(cd $(dirname "$0"); pwd)/.uppercut" /D:microsoft.framework="mono-4.5" /D:run.ilmerge="false" /D:run.nuget="false"

mono --runtime=v4.0.30319 ./lib/NAnt/NAnt.exe /logger:"NAnt.Core.DefaultLogger" /nologo /f:"$(cd $(dirname "$0"); pwd)/.build/analyzers/test.step" $* /D:build.config.settings="$(cd $(dirname "$0"); pwd)/.uppercut" /D:microsoft.framework="mono-4.5" /D:run.ilmerge="false" /D:run.nuget="false" 
