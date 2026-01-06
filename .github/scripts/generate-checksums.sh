#!/usr/bin/env bash
# Generate SHA256 checksums for all binaries
# Usage: generate-checksums.sh <artifacts-directory>

set -euo pipefail

ARTIFACTS_DIR="${1:-.}"

echo "========================================="
echo "Generating SHA256 checksums"
echo "Artifacts directory: $ARTIFACTS_DIR"
echo "========================================="

cd "$ARTIFACTS_DIR"

# Find all binary files
BINARIES=$(find . -type f \( -name "wt-*-windows-*.exe" -o -name "wt-*-linux-*" -o -name "wt-*-macos-*" \) ! -name "*.sha256" ! -name "SHA256SUMS*")

if [ -z "$BINARIES" ]; then
  echo "❌ ERROR: No binary files found in $ARTIFACTS_DIR"
  exit 1
fi

echo "Found binaries:"
echo "$BINARIES"
echo ""

# Generate checksums
> SHA256SUMS  # Create empty file
while IFS= read -r binary; do
  if [ -f "$binary" ]; then
    echo "Computing SHA256 for $binary..."
    sha256sum "$binary" >> SHA256SUMS
  fi
done <<< "$BINARIES"

echo ""
echo "✅ Checksums generated successfully"
echo ""
echo "========================================="
echo "SHA256SUMS content:"
echo "========================================="
cat SHA256SUMS
echo "========================================="
