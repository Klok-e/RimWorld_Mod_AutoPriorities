#!/usr/bin/env bash

set -e
# set -x

echo removing old Rimworld assemblies...
rm -rf ./Source/RimManaged

echo copying Rimworld assemblies...
cp -r $RIMWORLD_ASSEMBLIES_DIR ./Source/RimManaged
