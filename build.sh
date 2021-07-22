#!/usr/bin/env bash

set -e
# set -x

echo selected $BUILD_CONFIGURATION configuration

echo compiling...
msbuild Source/AutoPriorities/AutoPriorities.sln -verbosity:quiet -p:Configuration=$BUILD_CONFIGURATION

mkdir ./Build
echo building mod to $(realpath ./Build)
for dir in 1.* Textures About LICENSE ConditionalAssemblies
do
    cp -r ./$dir "./Build/$dir"
done
echo build complete.
