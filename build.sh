#!/bin/bash
set -euo pipefail

src/zig-build.sh
rm -f bin/*.o

dotnet clean src/cli/play/Play/Play.csproj -r linux-x64 -c Release
dotnet restore src/cli/play/Play/Play.csproj -r linux-x64
dotnet build src/cli/play/Play/Play.csproj -r linux-x64 -c Release
dotnet publish src/cli/play/Play/Play.csproj -r linux-x64 -p:PublishAot=true --output bin/