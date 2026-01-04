#!/usr/bin/env bash
# Generate CycloneDX SBOM using Microsoft SBOM Tool
# Usage: generate-sbom.sh <version> <project-dir>

set -euo pipefail

VERSION="${1:-v0.1.0}"
PROJECT_DIR="${2:-.}"

echo "========================================="
echo "Generating CycloneDX SBOM"
echo "Version: $VERSION"
echo "Project directory: $PROJECT_DIR"
echo "========================================="

# Install Microsoft SBOM Tool if not already installed
if ! command -v sbom-tool &> /dev/null; then
  echo "Installing Microsoft SBOM Tool..."
  dotnet tool install --global Microsoft.Sbom.DotNetTool
  export PATH="$PATH:$HOME/.dotnet/tools"
fi

# Verify installation
if ! command -v sbom-tool &> /dev/null; then
  echo "❌ ERROR: sbom-tool not found after installation"
  exit 1
fi

echo "sbom-tool version:"
sbom-tool version

# Create output directory
mkdir -p sbom-output

# Generate SBOM
echo ""
echo "Generating SBOM..."
sbom-tool generate \
  -b "$PROJECT_DIR" \
  -bc "$PROJECT_DIR" \
  -pn "wt" \
  -pv "${VERSION#v}" \
  -ps "kuju63" \
  -nsb "https://github.com/kuju63/wt" \
  -m "./sbom-output" \
  -pm true \
  -v Verbose

# Find generated SBOM file
SBOM_FILE=$(find ./sbom-output -name "*sbom.json" | head -n 1)

if [ -z "$SBOM_FILE" ] || [ ! -f "$SBOM_FILE" ]; then
  echo "❌ ERROR: SBOM file not found in ./sbom-output"
  ls -la ./sbom-output
  exit 1
fi

# Copy SBOM to root with standard name
cp "$SBOM_FILE" "wt-${VERSION}-sbom.json"

echo ""
echo "✅ SBOM generated successfully"
echo "Output file: wt-${VERSION}-sbom.json"
echo ""
echo "========================================="
echo "SBOM summary:"
jq -r '.components | length' "wt-${VERSION}-sbom.json" | xargs -I {} echo "Total components: {}"
echo "========================================="
