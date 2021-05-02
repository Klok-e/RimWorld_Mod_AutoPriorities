export RIMWORLD_DEPLOY_PATH=$(HOME)/.steam/steam/steamapps/common/RimWorld/Mods
export RIMWORLD_ASSEMBLIES_DIR=$(HOME)/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed

ifeq ("$1", "Release")
export BUILD_CONFIGURATION=Release
else
export BUILD_CONFIGURATION=Debug
endif

all: build

copy_assemblies:
	./copy_assemblies.sh

build: copy_assemblies
	./build.sh