RIMWORLD_DEPLOY_PATH=$(HOME)/.steam/steam/steamapps/common/RimWorld/Mods
export RIMWORLD_ASSEMBLIES_DIR=$(HOME)/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed
export RIMWORLD_VERSION=1.2

ifdef DEBUG
export BUILD_CONFIGURATION=Debug
else
export BUILD_CONFIGURATION=Release
endif

all: cleanup deploy

copy-assemblies:
	./copy_assemblies.sh

build: copy-assemblies
	./build.sh

cleanup:
	echo 'removing old build...'
	rm -f ./Build.zip
	rm -rf ./Build
	rm -rf ./$(RIMWORLD_VERSION)/
	rm -rf ./ConditionalAssemblies/$(RIMWORLD_VERSION)/

	echo 'removing deployed libs...'
	rm -rf $(RIMWORLD_DEPLOY_PATH)/AutoPriorities

	echo 'removing old Rimworld assemblies...'
	rm -rf ./Source/RimManaged

	echo 'cleanup complete'

deploy: build
	echo deploying to $(RIMWORLD_DEPLOY_PATH)
	cp -r ./Build $(RIMWORLD_DEPLOY_PATH)/AutoPriorities
	echo deployed to $(RIMWORLD_DEPLOY_PATH)

compress-to-zip:
	echo 'compressing the build to zip'
	zip -r ./Build.zip ./Build
	echo 'compressed the build to ./Build.zip'
