#!/usr/bin/env bash
# Build script for macOS ARM64 (MANDATORY platform)
# Usage: build-macos-arm64.sh <version>

set -euo pipefail

VERSION="${1:-v0.1.0}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

"$SCRIPT_DIR/build.sh" "$VERSION" "osx-arm64" "macOS ARM64" "true"
