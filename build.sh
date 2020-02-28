RIMWORLD_ASSEMBLIES_DIR=~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed
RIMWORLD_DEPLOY_PATH=~/.steam/steam/steamapps/common/RimWorld/Mods

echo removing old Rimworld assemblies...
rm -r -f ./Source/RimManaged

echo copying Rimworld assemblies...
cp -r $RIMWORLD_ASSEMBLIES_DIR ./Source/RimManaged

echo removing old build...
rm -r -f ./1.1/Assemblies/
echo compiling...
cd ./Source/AutoPriorities/AutoPriorities/
msbuild AutoPriorities.csproj -p:Configuration=Release
cd ../../../

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