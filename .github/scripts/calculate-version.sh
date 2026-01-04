#!/usr/bin/env bash
# Calculate next semantic version based on Conventional Commits
# Usage: calculate-version.sh

set -euo pipefail

echo "========================================="
echo "Calculating next semantic version"
echo "========================================="

# Get the latest tag, default to v0.0.0 if no tags exist
LAST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0")
echo "Latest tag: $LAST_TAG"

# Parse current version (remove 'v' prefix)
CURRENT_VERSION="${LAST_TAG#v}"
IFS='.' read -r MAJOR MINOR PATCH <<< "$CURRENT_VERSION"

echo "Current version: $MAJOR.$MINOR.$PATCH"

# Get commits since last tag
if [ "$LAST_TAG" == "v0.0.0" ]; then
  # If no previous tag, get all commits
  COMMITS=$(git log --pretty=format:%s)
else
  # Get commits since last tag
  COMMITS=$(git log "${LAST_TAG}..HEAD" --pretty=format:%s)
fi

if [ -z "$COMMITS" ]; then
  echo "No new commits since last tag"
  echo "version=${LAST_TAG}" >> "$GITHUB_OUTPUT"
  exit 0
fi

echo ""
echo "Commits since last tag:"
echo "$COMMITS"
echo ""

# Check for breaking changes
if echo "$COMMITS" | grep -qi "BREAKING CHANGE:"; then
  echo "🚨 BREAKING CHANGE detected → MAJOR version bump"
  MAJOR=$((MAJOR + 1))
  MINOR=0
  PATCH=0
elif echo "$COMMITS" | grep -qiE "^feat(\(|:)"; then
  echo "✨ feat: detected → MINOR version bump"
  MINOR=$((MINOR + 1))
  PATCH=0
elif echo "$COMMITS" | grep -qiE "^fix(\(|:)"; then
  echo "🐛 fix: detected → PATCH version bump"
  PATCH=$((PATCH + 1))
else
  echo "📝 No version-relevant commits (docs, style, refactor, test, chore, ci)"
  echo "version=${LAST_TAG}" >> "$GITHUB_OUTPUT"
  exit 0
fi

NEW_VERSION="v${MAJOR}.${MINOR}.${PATCH}"
echo ""
echo "========================================="
echo "New version: $NEW_VERSION"
echo "========================================="

# Output to GitHub Actions
echo "version=${NEW_VERSION}" >> "$GITHUB_OUTPUT"
echo "previous-version=${LAST_TAG}" >> "$GITHUB_OUTPUT"
