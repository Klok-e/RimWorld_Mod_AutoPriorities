#!/usr/bin/env bash

set -e
# set -x

echo selected $BUILD_CONFIGURATION configuration

echo removing old build...
rm -rf ./1.1/Assemblies/
rm -rf ./ConditionalAssemblies/1.1/
echo compiling...
msbuild Source/AutoPriorities/AutoPriorities.sln -verbosity:quiet -p:Configuration=$BUILD_CONFIGURATION

rm -r -f ./Build
mkdir ./Build
echo building mod to $(realpath ./Build)
for dir in /1.1 /Textures /Defs /About /LICENSE /ConditionalAssemblies
do
    cp -r .$dir "./Build/$dir"
done
echo build complete.

echo compressing the build to zip
rm -f ./Build.zip
zip -r ./Build.zip ./Build
echo compressed the build to ./Build.zip

if ! [[ -z "$RIMWORLD_DEPLOY_PATH" ]]
then
    echo deploying to $RIMWORLD_DEPLOY_PATH
    rm -rf $RIMWORLD_DEPLOY_PATH/AutoPriorities
    cp -r ./Build $RIMWORLD_DEPLOY_PATH/AutoPriorities
    echo deployed to $RIMWORLD_DEPLOY_PATH
fi
