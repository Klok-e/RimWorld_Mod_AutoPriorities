RIMWORLD_DEPLOY_PATH=$(HOME)/.steam/steam/steamapps/common/RimWorld/Mods
export RIMWORLD_ASSEMBLIES_DIR=$(HOME)/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed
export RIMWORLD_VERSION=1.6

ifdef DEBUG
export BUILD_CONFIGURATION=Debug
else
export BUILD_CONFIGURATION=Release
endif

all: cleanup deploy

copy-assemblies:
	echo copying Rimworld assemblies...
	cp -r $(RIMWORLD_ASSEMBLIES_DIR) ./Source/RimManaged

build: cleanup copy-assemblies
	set -e;

	echo selected $(BUILD_CONFIGURATION) configuration
	echo compiling...
	msbuild Source/AutoPriorities/AutoPriorities.sln -verbosity:quiet -p:Configuration=$(BUILD_CONFIGURATION)

	# copy assemblies to correct places
	mkdir -p "$(RIMWORLD_VERSION)/Assemblies/"
	cp -Rf "Source/AutoPriorities/AutoPriorities/bin/$(BUILD_CONFIGURATION)/." "$(RIMWORLD_VERSION)/Assemblies/"

	mkdir -p "ConditionalAssemblies/$(RIMWORLD_VERSION)/"
	cp -Rf "Source/AutoPriorities/FluffyWorktabPatch/bin/$(BUILD_CONFIGURATION)/." "ConditionalAssemblies/$(RIMWORLD_VERSION)/"
	cp -Rf "Source/AutoPriorities/BetterPawnControlPatch/bin/$(BUILD_CONFIGURATION)/." "ConditionalAssemblies/$(RIMWORLD_VERSION)/"

	mkdir ./Build
	echo building mod to $(realpath ./Build)
	for dir in 1.* Textures About LICENSE ConditionalAssemblies; do \
		cp -r ./$$dir "./Build/$$dir"; \
	done
	echo build complete.

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
