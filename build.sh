#!/bin/bash
set -euo pipefail

# Build zig app
src/zig-build.sh
rm -f bin/*.o

# Generate additional hash from the output of zig build
hashval=$(sha256sum bin/apollo-spc-program)
hashval="${hashval%% *}"
cp -f src/cli/play/Play/_AdditionalHashes-Template.txt src/cli/play/Play/AdditionalHashes.cs
sed -i "s|\"\\[\\[\\[C#___play___apollo-spc-program___C735A0F9___!GenFromCode!\\]\\]\\]\"|\"$hashval\"|g" src/cli/play/Play/AdditionalHashes.cs

# Build dotnet app
dotnet clean src/cli/play/Play/Play.csproj -r linux-x64 -c Release
dotnet restore src/cli/play/Play/Play.csproj -r linux-x64
dotnet build src/cli/play/Play/Play.csproj -r linux-x64 -c Release
dotnet publish src/cli/play/Play/Play.csproj -r linux-x64 -p:PublishAot=true --output bin/

# Cleanup
cp -f src/cli/play/Play/_AdditionalHashes-Template.txt src/cli/play/Play/AdditionalHashes.cs