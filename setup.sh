#!/bin/bash

unset $modName

createConfig() {
  folderName=$1
  nameSuffix=$2
  fullNameSuffix=$3
  cd "$thunderstoreDir"
  mkdir -p "$folderName"
  cp * "$folderName"
  cd "$folderName"
  versionKey="version_number"
  versionEntry="\"$versionKey\": \"[0-9]+\.[0-9]+\.[0-9]+\""
  sed -i -E "s/$versionEntry/\"$versionKey\": \"$modVersion\"/" manifest.json
  nameEntry="\"name\": \".*\""
  sed -i -e "s/$nameEntry/\"name\": \"$modName$nameSuffix\"/" manifest.json
  fullNameBase="JngoCreates-$modName"
  fullNameEntry="\"FullName\": \"$fullNameBase\""
  sed -i -e "s/$fullNameEntry/\"FullName\": \"$fullNameBase-$fullNameSuffix\"/" manifest.json
  cd "$scriptDir"
}

scriptPath=$(realpath $0)
scriptDir=$(dirname $scriptPath)
thunderstoreDir="$scriptDir/thunderstore"
target=$1
modName=$2
modVersion=$3
cd "$thunderstoreDir"
if [[ $target == "windows" ]]; then
  createConfig "$target" "Windows" "Windows"
elif [[ $target == "osx" ]]; then
  createConfig "$target" "Mac" "macOS"
fi