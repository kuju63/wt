#!/usr/bin/env bash
# Build script for Linux ARM (OPTIONAL platform)
# Usage: build-linux-arm.sh <version>

set -euo pipefail

VERSION="${1:-v0.1.0}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

"$SCRIPT_DIR/build.sh" "$VERSION" "linux-arm" "Linux ARM" "false"
