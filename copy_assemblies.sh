#!/usr/bin/env bash

set -e
# set -x

echo copying Rimworld assemblies...
cp -r $RIMWORLD_ASSEMBLIES_DIR ./Source/RimManaged
