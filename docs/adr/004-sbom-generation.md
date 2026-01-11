# ADR 004: SBOM Generation for Release Pipeline

## Status

Accepted

## Context

As a command-line tool distributed through GitHub releases, we need to provide transparency about our software supply chain. Users need to know:

- What dependencies are included in each release
- License information for compliance
- Vulnerability tracking through automated tools
- Complete bill of materials for audit purposes

## Decision

We will implement SBOM (Software Bill of Materials) generation as part of our release pipeline with the following approach:

### Tool Selection: Microsoft SBOM Tool

**Rationale:**

- Official Microsoft tool for .NET ecosystem
- Native support for .NET project dependency analysis
- Generates SPDX 2.3+ format (ISO/IEC 5962:2021 compliant)
- Easy integration with GitHub Actions
- Multi-platform support (Windows/Linux/macOS)

**Alternatives Considered:**

- **CycloneDX Generator**: More generic but less .NET-specific → Rejected
- **OWASP Dependency-Check**: Focused on vulnerability scanning, SBOM is secondary → Rejected
- **Syft/Grype**: Container-focused, overkill for .NET projects → Rejected

### SBOM Format: SPDX 2.3 (JSON)

**Rationale:**

- ISO international standard (ISO/IEC 5962:2021)
- Required by US government and many regulations
- Microsoft's recommended format
- Strong tool ecosystem support
- Long-term maintenance by Linux Foundation

**Alternatives Considered:**

- **CycloneDX**: More developer-friendly but not an ISO standard → Rejected for regulatory compliance
- **SWID Tags**: Legacy format, not suitable for modern CI/CD → Rejected

### Implementation Approach

1. **Dependency Restoration**: Complete restore of all project dependencies before SBOM generation
2. **Multi-platform Support**: Restore for all target platforms (win-x64, linux-x64, linux-arm, osx-arm64)
3. **Validation**: Verify SBOM completeness and format compliance
4. **GitHub Integration**:
   - Submit to GitHub Dependency Graph API
   - Attach SBOM to release assets
5. **PR Testing**: Pre-release validation workflow

### Key Requirements

- **FR-001**: Complete dependency restore before SBOM generation
- **FR-002**: All direct and transitive dependencies must be included
- **FR-006**: SPDX 2.3+ format compliance
- **FR-011**: GitHub Dependency Graph integration
- **FR-019**: PR testing workflow for early detection

## Consequences

### Positive

- **Supply Chain Transparency**: Users can verify all components in the software
- **Vulnerability Management**: Automated detection via Dependabot/Renovate
- **License Compliance**: Clear license information for all dependencies
- **Regulatory Compliance**: Meets government and enterprise requirements
- **Audit Trail**: Versioned SBOM files attached to each release
- **Early Detection**: PR workflow catches issues before release

### Negative

- **Build Time**: Adds 2-5 minutes to release pipeline (acceptable within 15-minute SLA)
- **Storage**: SBOM files add ~100KB per release (minimal impact)
- **Maintenance**: Requires monitoring of Microsoft SBOM Tool updates

### Neutral

- **Learning Curve**: Team needs to understand SPDX format for troubleshooting
- **Toolchain Dependency**: Relies on Microsoft maintaining the SBOM tool

## Implementation Notes

### Workflow Integration Points

1. **Release Pipeline** (`.github/workflows/release.yml`):
   - Dependency restore with `--locked-mode`
   - Multi-platform restore
   - SBOM generation with Microsoft SBOM Tool
   - SPDX format validation
   - GitHub API submission
   - Release asset attachment

2. **PR Testing Pipeline** (`.github/workflows/sbom-test.yml`):
   - Dry-run SBOM generation
   - Format validation
   - Dependency verification
   - Performance benchmarking

### Critical Success Factors

- **Complete Dependency Capture**: Must restore all dependencies before SBOM generation
- **Format Compliance**: Must pass SPDX 2.3 validation
- **API Integration**: Must successfully submit to GitHub Dependency Graph
- **Performance**: Must complete within 15-minute workflow timeout

### Monitoring & Maintenance

- Monitor Microsoft SBOM Tool release notes for updates
- Review GitHub Dependency Graph for accuracy
- Validate SBOM files on each release
- Track workflow execution times

## References

- [Microsoft SBOM Tool](https://github.com/microsoft/sbom-tool)
- [SPDX Specification 2.3](https://spdx.github.io/spdx-spec/v2.3/)
- [ISO/IEC 5962:2021](https://www.iso.org/standard/81870.html)
- [GitHub Dependency Submission API](https://docs.github.com/en/code-security/supply-chain-security/understanding-your-software-supply-chain/using-the-dependency-submission-api)
- [Executive Order 14028 (US Cybersecurity)](https://www.whitehouse.gov/briefing-room/presidential-actions/2021/05/12/executive-order-on-improving-the-nations-cybersecurity/)

## Date

2026-01-10
