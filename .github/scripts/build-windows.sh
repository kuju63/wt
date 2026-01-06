#!/usr/bin/env bash
# Build script for Windows x64 (MANDATORY platform)
# Usage: build-windows.sh <version>

set -euo pipefail

VERSION="${1:-v0.1.0}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

"$SCRIPT_DIR/build.sh" "$VERSION" "win-x64" "Windows x64" "true"
