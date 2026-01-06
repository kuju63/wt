#!/usr/bin/env bash
# Sign SBOM and SHA256SUMS files using GPG
# Usage: sign-artifacts.sh

set -euo pipefail

echo "========================================="
echo "Signing artifacts with GPG"
echo "========================================="

# Check required environment variables
if [ -z "${GPG_PRIVATE_KEY:-}" ]; then
  echo "❌ ERROR: GPG_PRIVATE_KEY environment variable not set"
  exit 1
fi

if [ -z "${GPG_PASSPHRASE:-}" ]; then
  echo "❌ ERROR: GPG_PASSPHRASE environment variable not set"
  exit 1
fi

# Import GPG private key
echo "Importing GPG private key..."
echo "$GPG_PRIVATE_KEY" | gpg --batch --import

# Get key ID
KEY_ID=$(gpg --list-secret-keys --keyid-format LONG | grep sec | awk '{print $2}' | cut -d'/' -f2)
echo "Using GPG key: $KEY_ID"

# Sign SBOM file
if [ -f "wt-*-sbom.json" ]; then
  SBOM_FILE=$(ls wt-*-sbom.json | head -n 1)
  echo "Signing $SBOM_FILE..."
  echo "$GPG_PASSPHRASE" | gpg \
    --batch \
    --yes \
    --passphrase-fd 0 \
    --armor \
    --detach-sign \
    --default-key "$KEY_ID" \
    "$SBOM_FILE"

  if [ -f "${SBOM_FILE}.asc" ]; then
    echo "✅ $SBOM_FILE signed successfully → ${SBOM_FILE}.asc"
  else
    echo "❌ ERROR: Failed to sign $SBOM_FILE"
    exit 1
  fi
else
  echo "⚠️ WARNING: No SBOM file found to sign"
fi

# Sign SHA256SUMS file
if [ -f "SHA256SUMS" ]; then
  echo "Signing SHA256SUMS..."
  echo "$GPG_PASSPHRASE" | gpg \
    --batch \
    --yes \
    --passphrase-fd 0 \
    --armor \
    --detach-sign \
    --default-key "$KEY_ID" \
    SHA256SUMS

  if [ -f "SHA256SUMS.asc" ]; then
    echo "✅ SHA256SUMS signed successfully → SHA256SUMS.asc"
  else
    echo "❌ ERROR: Failed to sign SHA256SUMS"
    exit 1
  fi
else
  echo "❌ ERROR: SHA256SUMS file not found"
  exit 1
fi

echo ""
echo "========================================="
echo "All artifacts signed successfully"
echo "========================================="
echo "Signature files:"
ls -lh *.asc
echo "========================================="
