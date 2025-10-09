#!/bin/bash
set -euo pipefail

# Move to the directory where this script resides
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)"
cd "$SCRIPT_DIR"

zig build-exe cli_main.zig -femit-bin="$SCRIPT_DIR/../bin/apollo-spc-program"