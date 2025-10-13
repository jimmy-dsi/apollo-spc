#!/bin/bash
set -euo pipefail

# Move to the directory where this script resides
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)"
cd "$SCRIPT_DIR"

# Make sure bin directory exists before attempting to store files there
mkdir -p "$SCRIPT_DIR/../bin"

zig build-exe cli_main.zig -femit-bin="$SCRIPT_DIR/../bin/apollo-spc-program"