#!/bin/bash
set -euo pipefail

# First, ensure that dotnet telemetry is disabled
telemetry_off=0
if [ -v DOTNET_CLI_TELEMETRY_OPTOUT ]; then
    if [[ "$DOTNET_CLI_TELEMETRY_OPTOUT" == 1 ]]; then
        telemetry_off=1
    fi
fi

telemetry_verified_off=0
if [[ "$telemetry_off" != 1 ]]; then
    echo -e "\033[1;93mwarning: it appears that you have not yet disabled telemetry in dotnet\033[0m"
    echo "Disabling..."
    echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1' >> ~/.bashrc || :
    export DOTNET_CLI_TELEMETRY_OPTOUT=1

    if [ -v DOTNET_CLI_TELEMETRY_OPTOUT ]; then
        if [[ "$DOTNET_CLI_TELEMETRY_OPTOUT" == 1 ]]; then
            telemetry_verified_off=1
            echo -e "\033[1;32mDisabling of dotnet telemetry succeeded 📎\033[0m"
        fi
    fi

    if [[ "$telemetry_verified_off" != 1 ]]; then
        echo -e "\033[1;91merror: unable to disable dotnet telemetry\033[0m" >&2
        echo "Please set the DOTNET_CLI_TELEMETRY_OPTOUT environment variable equal to 1 and then re-run this script"
        echo "(https://learn.microsoft.com/en-us/dotnet/core/tools/telemetry#how-to-opt-out)"
        exit 1
    fi
fi

# Make sure bin directory exists before attempting to store files there
mkdir -p bin

# Build zig app
src/zig-build.sh
rm -f bin/*.o 2> /dev/null || :

# Generate additional hash from the output of zig build
hashval=$(sha256sum bin/apollo-spc-program)
hashval="${hashval%% *}"
cp -f src/cli/play/Play/_AdditionalHashes-Template.txt src/cli/play/Play/AdditionalHashes.cs
sed -i "s|\"\\[\\[\\[C#___play___apollo-spc-program___C735A0F9___!GenFromCode!\\]\\]\\]\"|\"$hashval\"|g" src/cli/play/Play/AdditionalHashes.cs

# Build dotnet app
dotnet publish src/cli/play/Play/Play.csproj -r linux-x64 -p:PublishAot=true --output bin/

# Cleanup
cp -f src/cli/play/Play/_AdditionalHashes-Template.txt src/cli/play/Play/AdditionalHashes.cs 2> /dev/null || :

echo ""
echo "build.sh: Build successful"
echo "To run:"
echo "./bin/play <your-spc-file>"

if [[ "$telemetry_verified_off" == 1 ]]; then
    bash # Have new env variable changes inherited when user is given back control of shell
fi