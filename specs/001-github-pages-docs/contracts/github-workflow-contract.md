# Documentation Deployment Workflow Contract

**File**: `.github/workflows/release.yml` (build-and-deploy-docs job)  
**Purpose**: Defines the contract for the GitHub Actions workflow job that builds and deploys documentation to GitHub Pages as part of the release pipeline.

## Workflow Inputs

### Trigger Events
```yaml
# Part of release.yml workflow
needs: [calculate-version, create-release]
```

**Contract**:
- MUST trigger after successful release creation
- Version is obtained from `needs.calculate-version.outputs.version`
- Integrated into release pipeline (no separate workflow trigger needed)

## Workflow Outputs

### Deployment URL
```yaml
outputs:
  page_url:
    description: "URL of deployed documentation"
    value: ${{ jobs.build-and-deploy.outputs.page_url }}
```

**Contract**:
- MUST output the final GitHub Pages URL
- Format: `https://{username}.github.io/{repo}/v{major}.{minor}/`

## Workflow Steps Contract

### Step 1: Extract Version
**Input**: Version from `needs.calculate-version.outputs.version` (e.g., `v0.1.0`, `v1.2.3`)  
**Output**: Minor version string (e.g., `v0.1`, `v1.2`)  
**Contract**:
```yaml
# MUST extract minor version from calculated version
- name: Extract version
  id: version
  run: |
    TAG_NAME="${{ needs.calculate-version.outputs.version }}"
    # Extract major.minor from tag (e.g., v1.2.3 -> v1.2)
    VERSION=$(echo $TAG_NAME | grep -oE 'v?[0-9]+\.[0-9]+')
    echo "version=$VERSION" >> $GITHUB_OUTPUT
    echo "Extracted version: $VERSION"
```

**Validation**:
- Input version MUST match: `v\d+\.\d+\.\d+` (semantic versioning)
- Output MUST match: `v\d+\.\d+`
- MUST fail if version format is invalid

**Test Cases**:
| Input Version | Expected Output | Rationale |
|---------------|-----------------|-----------|
| `v0.1.0` | `v0.1` | Initial release |
| `v0.10.5` | `v0.10` | Pre-1.0 with higher minor |
| `v1.2.3` | `v1.2` | Standard semantic version |
| `v10.15.20` | `v10.15` | Large version numbers |

### Step 2: Generate Command Documentation
**Input**: CLI command definitions from `wt.cli`  
**Output**: Markdown files in `docs/commands/`  
**Contract**:
```yaml
- name: Generate command documentation
  run: dotnet run --project Tools/DocGenerator/DocGenerator -- --output docs/commands
```

**Validation**:
- MUST generate one `.md` file per command in `docs/commands/`
- MUST exit with code 0 on success
- MUST exit with non-zero if generation fails

### Step 3: Build API Documentation
**Input**: wt.cli project with XML documentation enabled  
**Output**: XML documentation file  
**Contract**:
```yaml
- name: Build API documentation
  run: |
    cd wt.cli
    dotnet build --configuration Release
```

**Validation**:
- MUST build in Release configuration only
- MUST generate XML documentation file (wt.xml)
- MUST exit with code 0 if build succeeds
- Build MUST fail if XML doc comments have errors

### Step 4: Build DocFX Site
**Input**: Markdown files, XML docs, docfx.json  
**Output**: Static HTML site in `_output/{version}/`  
**Contract**:
```yaml
- name: Build versioned documentation
  run: |
    mkdir -p _output/${{ steps.version.outputs.minor }}
    docfx build docfx.json --warningsAsErrors -o _output/${{ steps.version.outputs.minor }}
```

**Validation**:
- MUST treat all warnings as errors (`--warningsAsErrors`)
- MUST output to version-specific directory
- MUST exit with non-zero if any internal link is broken
- MUST generate HTML/CSS/JS files

### Step 5: Fetch Existing Version Manifest
**Input**: gh-pages branch (may not exist on first release)  
**Output**: `version-manifest.json` file  
**Contract**:
```yaml
- name: Fetch existing version manifest
  run: |
    git fetch origin gh-pages:gh-pages 2>/dev/null || true
    if git show gh-pages:version-manifest.json > version-manifest.json 2>/dev/null; then
      echo "✅ Existing manifest found with $(jq '.versions | length' version-manifest.json) versions"
    else
      echo '{"versions":[]}' > version-manifest.json
      echo "✅ Created new manifest (first release)"
    fi
```

**Validation**:
- MUST handle first-release case (no gh-pages branch)
- MUST create empty manifest `{"versions":[]}` if none exists
- MUST fetch existing manifest if gh-pages exists
- MUST NOT fail if gh-pages branch doesn't exist

### Step 6: Update Version Manifest
**Input**: Existing manifest, new version, release date  
**Output**: Updated `version-manifest.json`  
**Contract**:
```yaml
- name: Update version manifest
  run: |
    RELEASE_DATE="${{ github.event.release.published_at || github.event.head_commit.timestamp }}"
    
    python3 .github/scripts/update-version-manifest.py \
      --version "${{ steps.version.outputs.minor }}" \
      --date "$RELEASE_DATE"
    
    # Copy to deployment output (root and version-specific)
    cp version-manifest.json _output/
    cp version-manifest.json _output/${{ steps.version.outputs.minor }}/
```

**Validation**:
- MUST add new version to manifest
- MUST mark new version as `isLatest: true`
- MUST remove `isLatest` from all other versions
- MUST sort versions by release date (newest first)
- MUST be idempotent (re-running updates, doesn't duplicate)
- MUST exit with code 0 on success

**Python Script Contract** (`.github/scripts/update-version-manifest.py`):
```python
# MUST accept arguments: --version, --date
# MUST read input manifest (or create empty if missing)
# MUST update/add version entry
# MUST write valid JSON output
# MUST exit 0 on success, non-zero on error
```

### Step 7: Validate Links
**Input**: Built HTML site  
**Output**: Link validation report  
**Contract**:
```yaml
- name: Validate documentation links
  run: |
    pip install linkchecker
    linkchecker \
      --check-extern \
      --ignore-url="localhost" \
      --ignore-url="127.0.0.1" \
      --no-warnings \
      _output/${{ steps.version.outputs.minor }}/
```

**Validation**:
- MUST check all internal links within site
- MUST check external links (with filtering)
- MUST exit with non-zero if broken links found
- MUST ignore localhost and 127.0.0.1 URLs

### Step 8: Deploy to GitHub Pages
**Input**: `_output` directory with versioned docs  
**Output**: Deployed site on GitHub Pages  
**Contract**:
```yaml
- name: Setup GitHub Pages
  uses: actions/configure-pages@v4

- name: Upload artifact
  uses: actions/upload-pages-artifact@v3
  with:
    path: '_output'

- name: Deploy to GitHub Pages
  id: deployment
  uses: actions/deploy-pages@v4
```

**Validation**:
- MUST use OIDC authentication (no long-lived PAT)
- MUST deploy entire `_output` directory
- MUST preserve existing version directories
- MUST output `page_url` with deployed site URL

## Error Handling

| Error Condition | Required Behavior | Exit Code |
|-----------------|-------------------|-----------|
| Invalid version tag format | Fail with clear error message | 1 |
| Version extraction fails | Stop workflow immediately | 1 |
| Command doc generation fails | Stop workflow, show error | 1 |
| API build fails | Stop workflow, show build errors | 1 |
| DocFX build has warnings | Fail due to `--warningsAsErrors` | 1 |
| Broken links detected | Fail workflow, list broken links | 1 |
| Version manifest update fails | Stop workflow, show Python error | 1 |
| GitHub Pages deployment fails | Retry once, then fail | 1 |

## Performance Requirements

| Metric | Target | Maximum | Success Criteria |
|--------|--------|---------|------------------|
| Total workflow duration | <8 minutes | 10 minutes | SC-003 compliance |
| Command doc generation | <30 seconds | 1 minute | - |
| API build time | <2 minutes | 3 minutes | - |
| DocFX build time | <3 minutes | 5 minutes | - |
| Link validation time | <1 minute | 2 minutes | - |
| Deployment time | <1 minute | 2 minutes | - |

## Security Requirements

1. **Authentication**:
   - MUST use OIDC token authentication
   - MUST NOT use long-lived Personal Access Tokens (PAT)

2. **Permissions**:
   - MUST use principle of least privilege
   - Required: `contents: write`, `pages: write`, `id-token: write`
   - MUST NOT request additional permissions

3. **Secrets**:
   - MUST NOT expose secrets in workflow logs
   - MUST NOT log version manifest content (may contain dates/metadata)

4. **Input Validation**:
   - Python script MUST NOT execute arbitrary code from JSON
   - Version string MUST be validated before use in paths

## Dependencies

### External Actions
```yaml
- actions/checkout@v4
- actions/setup-dotnet@v5
- actions/configure-pages@v4
- actions/upload-pages-artifact@v3
- actions/deploy-pages@v4
```

**Contract**:
- MUST pin to specific major versions (e.g., `@v4`, not `@v4.1.0`)
- MUST NOT use `@main` or `@latest`
- MUST update via Dependabot/Renovate

### External Tools
| Tool | Version | Installation Method |
|------|---------|---------------------|
| docfx | 2.78.4 (exact) | `dotnet tool install --global docfx --version 2.78.4` |
| linkchecker | latest | `pip install linkchecker` |
| python3 | 3.x | Pre-installed on ubuntu-latest |
| jq | latest | Pre-installed on ubuntu-latest |

**Contract**:
- DocFX version MUST be pinned to 2.78.4
- Python 3 MUST be available
- LinkChecker MUST support `--check-extern` flag

## Concurrency Control

```yaml
concurrency:
  group: pages
  cancel-in-progress: false
```

**Contract**:
- MUST use `group: pages` to prevent concurrent deploys
- MUST set `cancel-in-progress: false` to allow queuing
- Multiple releases MUST queue (not run in parallel)
- In-progress deployments MUST complete before next starts

## Environment Configuration

### Required Environment
```yaml
environment:
  name: github-pages
  url: ${{ steps.deployment.outputs.page_url }}
```

**Setup Steps**:
1. Repository Settings → Environments → New environment
2. Name: `github-pages`
3. Optional: Add protection rules (approval, wait timer)

**Contract**:
- Environment MUST exist before first workflow run
- Environment MUST have Pages deployment permissions
- Environment URL is automatically set by `deploy-pages` action

### Required Permissions
```yaml
permissions:
  contents: write  # Fetch gh-pages branch
  pages: write     # Deploy to GitHub Pages
  id-token: write  # OIDC authentication
```

## Backward Compatibility

- **First Release**: Workflow MUST create gh-pages branch if it doesn't exist
- **Existing Versions**: Workflow MUST preserve existing version directories (`v1.0/`, `v1.1/`, etc.)
- **Manifest Format**: Adding fields OK, removing fields NOT OK
- **URL Structure**: Version URLs MUST remain stable (SC-006: 2+ years)

## Testing Contract

### Pre-Deployment Validation Checklist
- ✅ Version extraction produces valid `v{major}.{minor}` format
- ✅ Command documentation generated for all commands
- ✅ API build succeeds in Release configuration
- ✅ DocFX build succeeds with `--warningsAsErrors`
- ✅ Link validation passes (zero broken links)
- ✅ Version manifest is valid JSON

### Post-Deployment Verification Checklist
- ✅ Deployed URL returns HTTP 200
- ✅ Version manifest loads successfully
- ✅ New version appears in version switcher dropdown
- ✅ Version switcher navigation works
- ✅ Internal links between pages work
- ✅ API reference pages load correctly

## Example Workflow Execution

### Scenario: Release v0.2.5
```
1. Release published: v0.2.5
2. Extract version: v0.2
3. Generate command docs: docs/commands/*.md created
4. Build API: wt.xml generated
5. Build DocFX: _output/v0.2/ created with HTML
6. Fetch manifest: Found with [v0.1]
7. Update manifest: Add v0.2, mark as latest
8. Validate links: All OK (0 broken)
9. Deploy: https://username.github.io/wt/v0.2/
10. Output: page_url = https://username.github.io/wt/v0.2/
```

### Scenario: First Release v0.1.0
```
1. Release published: v0.1.0
2. Extract version: v0.1
3. Generate command docs: docs/commands/*.md created
4. Build API: wt.xml generated
5. Build DocFX: _output/v0.1/ created
6. Fetch manifest: None found, create empty {"versions":[]}
7. Update manifest: Add v0.1 (first entry, marked latest)
8. Validate links: All OK
9. Deploy: Create gh-pages branch, deploy v0.1/
10. Output: page_url = https://username.github.io/wt/v0.1/
```

---

**Next**: `docfx-config-contract.md` - Contract for DocFX configuration
