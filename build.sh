#!/usr/bin/env bash

set -e
# set -x

echo selected $BUILD_CONFIGURATION configuration

echo compiling...
msbuild Source/AutoPriorities/AutoPriorities.sln -verbosity:quiet -p:Configuration=$BUILD_CONFIGURATION

# copy assemblies to correct places
mkdir -p "$RIMWORLD_VERSION/Assemblies/"
cp -Rf "Source/AutoPriorities/AutoPriorities/bin/$BUILD_CONFIGURATION/." "$RIMWORLD_VERSION/Assemblies/"

mkdir -p "ConditionalAssemblies/$RIMWORLD_VERSION/"
cp -Rf "Source/AutoPriorities/FluffyWorktabPatch/bin/$BUILD_CONFIGURATION/." "ConditionalAssemblies/$RIMWORLD_VERSION/"
cp -Rf "Source/AutoPriorities/BetterPawnControlPatch/bin/$BUILD_CONFIGURATION/." "ConditionalAssemblies/$RIMWORLD_VERSION/"

mkdir "./Build"
echo building mod to $(realpath ./Build)
for dir in 1.* Textures About LICENSE ConditionalAssemblies
do
    cp -r ./$dir "./Build/$dir"
done
echo build complete.
