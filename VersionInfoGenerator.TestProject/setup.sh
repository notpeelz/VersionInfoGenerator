#!/usr/bin/env bash

set -e

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"

rm -rf "$DIR/nuget-pkgs"

dotnet build -c Release "$DIR/../VersionInfoGenerator"
dotnet pack "$DIR/../VersionInfoGenerator" \
  -o "$DIR/nuget-pkgs/src" \
  -c Release \
  -p:DebugType=pdbonly \
  -p:DebugSymbols=true
