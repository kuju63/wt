#!/usr/bin/env bash
# Build script for Linux x64 (MANDATORY platform)
# Usage: build-linux-x64.sh <version>

set -euo pipefail

VERSION="${1:-v0.1.0}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

"$SCRIPT_DIR/build.sh" "$VERSION" "linux-x64" "Linux x64" "true"
