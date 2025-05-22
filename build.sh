#!/bin/bash

unset $modName

getMSBuildFile() {
  csproj=$(find . -iname *.csproj | head -1)
}

getModName() {
  prevPath=$(pwd)
  cd "$scriptDir"
  tag=AssemblyName
  getMSBuildFile
  modName=$(grep --only-matching --perl-regexp "<$tag>\K.*(?=</$tag>)" $csproj)
  cd "$prevPath"
}

getModVersion() {
  prevPath=$(pwd)
  cd "$scriptDir"
  tag=Version
  getMSBuildFile
  versionNumber=$(grep --only-matching --perl-regexp "<$tag>\K.*(?=</$tag>)" $csproj)
  cd "$prevPath"
}

modifyBaseConfig() {
  getModVersion
  versionKey="version_number"
  versionEntry="\"$versionKey\": \"[0-9]+\.[0-9]+\.[0-9]+\""
  sed --in-place --expression "s/$versionEntry$/\"$versionKey\": \"$versionNumber\"/" manifest.json
}

createConfig() {
  folderName=$1
  nameSuffix=$2
  fullNameSuffix=$3
  cd "$thunderstoreDir"
  mkdir -p "$folderName"
  cp * "$folderName"
  getModName
  nameEntry="\"name\": \".*\""
  cd "$folderName"
  sed --in-place --expression "s/$nameEntry/\"name\": \"$modName$nameSuffix\"/" manifest.json
  fullNameBase="JngoCreates-$modName"
  fullNameEntry="\"FullName\": \"$fullNameBase\""
  sed --in-place --expression "s/$fullNameEntry/\"FullName\": \"$fullNameBase-$fullNameSuffix\"/" manifest.json
  cd "$scriptDir"
}

build() {
  cd "$scriptDir"
  dotnet restore
  version=$(cat $PWD/libs-stripped/versions.txt | tail -1)
  echo "Checking $version..."
  dotnet build --no-restore -p:DllPath="$PWD/libs-stripped/$version" --configuration Release
  done
}

scriptPath=$(realpath $0)
scriptDir=$(dirname $scriptPath)
thunderstoreDir="$scriptDir/thunderstore"
target=$1
cd "$thunderstoreDir"
modifyBaseConfig
if [[ $target == "windows" ]]; then
  createConfig "$target" "Windows" "Windows"
elif [[ $target == "osx" ]]; then
  createConfig "$target" "Mac" "macOS"
fi
build