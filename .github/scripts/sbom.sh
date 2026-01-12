#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: sbom.sh [options]
  --drop-path PATH          Path containing the files to be shipped (sbom-tool -b)
  --component-path PATH     Path to scan for build components (sbom-tool -bc)
  --package-name NAME       Package name to embed in the SBOM (sbom-tool -pn)
  --package-version VERSION Package version to embed in the SBOM (sbom-tool -pv)
  --package-supplier NAME   Supplier string, e.g., "Organization: example" (sbom-tool -ps)
  --namespace-base URI      Base URI for document namespace (sbom-tool -nsb)
  --manifest-info VALUE     Manifest info, e.g., SPDX:2.2 or SPDX:3.0 (default: SPDX:2.2)
  --target-path PATH        Optional path to copy the generated manifest to
  --validation-output PATH  Optional path for validation output (default: <drop-path>/sbom-validation.json)
  --skip-validate           Skip sbom-tool validate
  -h, --help                Show this help
EOF
}

DROP_PATH=""
COMPONENT_PATH=""
PACKAGE_NAME=""
PACKAGE_VERSION=""
PACKAGE_SUPPLIER=""
NAMESPACE_BASE=""
MANIFEST_INFO="SPDX:2.2"
TARGET_PATH=""
VALIDATION_OUTPUT=""
SKIP_VALIDATE=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --drop-path)
      DROP_PATH="$2"; shift 2 ;;
    --component-path)
      COMPONENT_PATH="$2"; shift 2 ;;
    --package-name)
      PACKAGE_NAME="$2"; shift 2 ;;
    --package-version)
      PACKAGE_VERSION="$2"; shift 2 ;;
    --package-supplier)
      PACKAGE_SUPPLIER="$2"; shift 2 ;;
    --namespace-base)
      NAMESPACE_BASE="$2"; shift 2 ;;
    --manifest-info)
      MANIFEST_INFO="$2"; shift 2 ;;
    --target-path)
      TARGET_PATH="$2"; shift 2 ;;
    --validation-output)
      VALIDATION_OUTPUT="$2"; shift 2 ;;
    --skip-validate)
      SKIP_VALIDATE=1; shift ;;
    -h|--help)
      usage; exit 0 ;;
    *)
      echo "Unknown option: $1" >&2
      usage
      exit 1 ;;
  esac
done

for var in DROP_PATH COMPONENT_PATH PACKAGE_NAME PACKAGE_VERSION PACKAGE_SUPPLIER NAMESPACE_BASE; do
  if [[ -z "${!var}" ]]; then
    echo "Missing required option: ${var,,}" >&2
    usage
    exit 1
  fi
done

if ! command -v sbom-tool >/dev/null 2>&1; then
  echo "sbom-tool not found on PATH. Install Microsoft.Sbom.DotNetTool first." >&2
  exit 1
fi

mkdir -p "$DROP_PATH"

manifest_folder=$(echo "$MANIFEST_INFO" | tr '[:upper:]' '[:lower:]' | tr ':' '_')
generated_file="$DROP_PATH/_manifest/${manifest_folder}/manifest.spdx.json"

if [[ -z "$VALIDATION_OUTPUT" ]]; then
  VALIDATION_OUTPUT="$DROP_PATH/sbom-validation.json"
fi

sbom-tool generate \
  -b "$DROP_PATH" \
  -bc "$COMPONENT_PATH" \
  -pn "$PACKAGE_NAME" \
  -pv "$PACKAGE_VERSION" \
  -ps "$PACKAGE_SUPPLIER" \
  -nsb "$NAMESPACE_BASE" \
  -mi "$MANIFEST_INFO" \
  -v Information

action_target="$generated_file"
if [[ ! -f "$generated_file" ]]; then
  echo "SBOM manifest not found at $generated_file" >&2
  exit 1
fi

if [[ -n "$TARGET_PATH" ]]; then
  mkdir -p "$(dirname "$TARGET_PATH")"
  cp "$generated_file" "$TARGET_PATH"
  action_target="$TARGET_PATH"
fi

if [[ $SKIP_VALIDATE -eq 0 ]]; then
  sbom-tool validate \
    -b "$DROP_PATH" \
    -o "$VALIDATION_OUTPUT" \
    -mi "$MANIFEST_INFO"
fi

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  echo "sbom_file=$action_target" >> "$GITHUB_OUTPUT"
  echo "generated_file=$generated_file" >> "$GITHUB_OUTPUT"
  echo "validation_report=$VALIDATION_OUTPUT" >> "$GITHUB_OUTPUT"
fi

echo "SBOM generated: $action_target"
[[ $SKIP_VALIDATE -eq 0 ]] && echo "Validation report: $VALIDATION_OUTPUT"
