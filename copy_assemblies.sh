RIMWORLD_ASSEMBLIES_DIR=~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed

echo removing old Rimworld assemblies...
rm -r -f ./Source/RimManaged

echo copying Rimworld assemblies...
cp -r $RIMWORLD_ASSEMBLIES_DIR ./Source/RimManaged
