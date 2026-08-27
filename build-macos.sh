#!/usr/bin/env bash

execd() {
  echo '>' "$@" >&2
  "$@" || exit $?
}

fail() {
  echo "$@" >&2
  exit 1
}

cd -- "$(dirname -- "${BASH_SOURCE[0]}")"
mkdir -p bin

export DOTNET_CLI_TELEMETRY_OPTOUT=1

execd zig build-lib -dynamic ./src/lib_api.zig -O ReleaseFast -target native-macos -lc -femit-bin=bin/apollo.dylib

execd cp bin/apollo.dylib src/cli/play/Apollo/

case "$(uname -m)" in
  arm*) RUNTIME=osx-arm64 ;;
  x86*) RUNTIME=osx-x64 ;;
  *) fail "unsupported architecture: $(uname -p)" ;;
esac

execd dotnet publish src/cli/play/SpcProgram/SpcProgram.csproj \
  -r "$RUNTIME" \
  -p:PublishAot=true \
  -p:WarningLevel=0 \
  --output ./bin

# dotnet on macos is fun and doesn't embed paths to dylibs in a way that
# respects CFLAGS, LDFLAGS, etc.
# so we manually use pkg-config to grab the -L flags and add them to rpath.
pkg-config --libs-only-L --newlines SDL2 | while read L; do
  # strip 2 characters to remove the -L
  p="${L:2}"
  execd install_name_tool -add_rpath "$p" bin/apollo-spc-program
done
