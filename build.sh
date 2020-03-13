RIMWORLD_DEPLOY_PATH=~/.steam/steam/steamapps/common/RimWorld/Mods

cd "${0%/*}"

if [ "$1" = "Release" ]
then
    BUILD_CONFIGURATION=Release
elif [ "$1" = "Debug" ]
then
    BUILD_CONFIGURATION=Debug
else
    BUILD_CONFIGURATION=Release
fi
echo selected $BUILD_CONFIGURATION configuration

echo removing old build...
rm -r -f ./1.1/Assemblies/
echo compiling...
msbuild Source/AutoPriorities/AutoPriorities.sln -verbosity:quiet -p:Configuration=$BUILD_CONFIGURATION

rm -r -f ./Build
mkdir ./Build
echo building mod to $(realpath ./Build)
for dir in /1.1 /Textures /Defs /Assemblies /About /LICENSE
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
    rm -r -f $RIMWORLD_DEPLOY_PATH/AutoPriorities
    cp -r ./Build $RIMWORLD_DEPLOY_PATH/AutoPriorities
    echo deployed to $RIMWORLD_DEPLOY_PATH
fi
