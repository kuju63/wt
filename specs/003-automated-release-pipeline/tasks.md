# Tasks: 自動化されたバイナリリリースパイプライン

**Feature**: `001-automated-release-pipeline`  
**Date**: 2026-01-04  
**Plan**: [plan.md](plan.md) | **Spec**: [spec.md](spec.md)

## Task Workflow

This feature is organized by user story (independent implementation and testing). Each phase must be completed and tested before moving to the next.

**Phases**:

1. **Phase 0: Foundation** - Build infrastructure and prerequisites
2. **Phase 1: P1 Stories** - Core user stories (binary distribution + security)
3. **Phase 2: P2 Stories** - Supporting stories (automation + quality)
4. **Phase 3: Integration & Polish** - Cross-cutting concerns and final validation

---

## Phase 0: Foundation & Prerequisites

Phase 0 establishes the base infrastructure needed by all user stories.

### Setup & Configuration

- [X] T001 Create GitHub Actions workflow directory structure in `.github/workflows/`
- [X] T002 Create shell script directory in `.github/scripts/`
- [X] T003 Create GitHub repository secrets configuration document (list required secrets: GH_RELEASE_TOKEN, CODACY_TOKEN, etc.)
- [X] T004 Document Conventional Commits naming convention in `.github/` (for team reference)
- [X] T005 Create semantic versioning decision log in `specs/001-automated-release-pipeline/research.md`

### Dependency & Tool Verification

- [X] T006 Research and document CycloneDX integration options for .NET projects (dotnet-sbom, etc.)
- [X] T007 Research and document Conventional Commits parser options (semantic-release, commitlint, custom)
- [X] T008 Verify GitHub Actions concurrency limits for parallel platform builds
- [X] T009 Verify Codacy integration status and required workflow configuration
- [X] T010 Document platform-specific compiler availability in GitHub-hosted runners

---

## Phase 1: P1 Stories - Core Binary Distribution & Security

### User Story 1: Download and Install Compiled Binary

This story delivers the ability to download pre-built binaries for each platform.

**Story Goal**: Binaries are available on GitHub Releases for download, testable, and runnable on target platforms.

**Independent Test**: Download a binary for each platform, verify it runs and displays version info.

#### Build Infrastructure (Prerequisite for US1)

**Platform Matrix**: Mandatory (Windows x64, Linux x64, Mac ARM64) - build failure blocks release. Optional (Linux ARM) - build failure logged but does not block release (see T078 for error handling).

- [X] T011 [P] Implement cross-platform build matrix in `.github/workflows/build.yml` (Windows x64, Linux x64, Linux ARM, Mac ARM64) with platform status tracking
- [X] T012 [P] Create `.github/scripts/build-windows.sh` to build Windows x64 binary (MANDATORY)
- [X] T013 [P] Create `.github/scripts/build-linux-x64.sh` to build Linux x64 binary (MANDATORY)
- [X] T014 [P] Create `.github/scripts/build-linux-arm.sh` to build Linux ARM binary (OPTIONAL - failure allowed)
- [X] T015 [P] Create `.github/scripts/build-macos-arm64.sh` to build Mac ARM64 binary (MANDATORY)
- [X] T016 [P] Update `wt.cli.csproj` to support multi-platform publishing (RID-specific builds)
- [ ] T017 Test local builds for all 4 platform combinations manually before workflow automation

#### Binary Release to GitHub Releases

- [X] T018 Implement `.github/workflows/release.yml` to trigger on main branch push
- [X] T019 Add release workflow step to upload all platform binaries as GitHub Release assets
- [X] T020 Verify binary naming convention: `wt-<version>-<platform>-<arch>.<ext>` (e.g., `wt-v1.0.0-windows-x64.exe`)
- [ ] T021 Test release workflow: merge a PR to main and verify binaries appear on GitHub Releases
- [ ] T022 Document release manual download process in `specs/001-automated-release-pipeline/quickstart.md`

---

### User Story 2: Verify Software Supply Chain Security

This story delivers file hashes and SBOM for binary integrity and dependency verification.

**Story Goal**: Every release includes SHA256SUMS and CycloneDX SBOM files alongside binaries.

**Independent Test**: Download SBOM, download checksum file, verify hash matches computed hash.

#### Hash Generation

- [X] T023 Create `.github/scripts/generate-checksums.sh` to compute SHA256 hashes for all binaries
- [X] T024 Add workflow step in `build.yml` to generate SHA256SUMS file after all builds complete
- [X] T025 Upload SHA256SUMS file to GitHub Release as asset
- [ ] T026 Test hash generation: verify downloaded binary hash matches SHA256SUMS entry

#### SBOM Generation

- [X] T027 Integrate CycloneDX tool into build workflow (research result from Phase 0 used here)
- [X] T028 Create `.github/scripts/generate-sbom.sh` to generate CycloneDX SBOM for binary
- [X] T029 Add workflow step in `build.yml` to generate SBOM for each platform build
- [X] T030 Generate single aggregated SBOM listing all dependencies across platforms
- [X] T031 Upload aggregated SBOM (JSON format) to GitHub Release as asset
- [ ] T032 Test SBOM: download and verify SBOM contains expected dependency list

#### Digital Signature Generation

- [X] T032a Research and select signature tool (GPG, cosign, or GitHub Actions built-in) during Phase 0
- [X] T032b Create `.github/scripts/sign-artifacts.sh` to generate signatures for SBOM and SHA256SUMS files
- [X] T032c Add workflow step in `build.yml` to sign SBOM.json and SHA256SUMS files after generation
- [X] T032d Upload signature files (SBOM.json.sig, SHA256SUMS.sig) to GitHub Release as assets
- [ ] T032e Test signature generation: verify signature files are present and properly formatted
- [ ] T032f Document signature verification process in `quickstart.md` (public key distribution, verification command)

#### Supply Chain Documentation

- [X] T033 Add file hash verification instructions to `quickstart.md`
- [X] T034 Add SBOM interpretation guide to `quickstart.md`
- [X] T035 Create ADR (Architecture Decision Record) explaining SBOM format choice (CycloneDX JSON/XML) and signature tool selection
- [X] T035a Add signature verification guide to `quickstart.md` (download .sig files, verify with public key)

---

## Phase 2: P2 Stories - Automation & Quality Assurance

### User Story 3: Receive Automated Releases on Main Branch Merge

This story delivers automatic version management and release notes.

**Story Goal**: Releases are published automatically with correct version numbers and descriptive notes when PR merges to main.

**Independent Test**: Merge a feature PR to main, verify release is created with incremented version and release notes.

#### Version Management

- [X] T036 Implement version number parsing from latest Git tag
- [X] T037 Create `.github/scripts/calculate-version.sh` to determine next version based on Conventional Commits
- [X] T038 Implement logic: feat: → minor, fix: → patch, BREAKING CHANGE: → major
- [X] T039 Add workflow step to create Git tag for new version
- [ ] T040 Test version calculation: commit feat/fix/BREAKING CHANGE and verify version increments correctly

#### Release Notes Generation

- [X] T041 Create `.github/scripts/generate-release-notes.sh` to extract Conventional Commits since last tag
- [X] T042 Implement categorization: features (feat:), fixes (fix:), breaking changes (BREAKING CHANGE:)
- [X] T043 Add workflow step to generate release notes and include in GitHub Release body
- [ ] T044 Test release notes: verify notes accurately reflect commits since last release

#### Release Workflow Automation

- [X] T045 Update `release.yml` workflow to automatically trigger on main push (not manual)
- [X] T046 Add version calculation and git tag creation to release workflow
- [X] T047 Add release notes generation to release workflow
- [ ] T048 Verify release appears on GitHub Releases within 30 minutes of main merge
- [ ] T049 Test release end-to-end: merge PR to main, monitor workflow, verify release created

#### Release Documentation

- [X] T050 Document versioning strategy in ADR (Conventional Commits → SemVer)
- [X] T051 Add release notification setup guide to `quickstart.md`

---

### User Story 4: Verify All Changes Are Tested with Coverage

This story delivers automated testing, coverage reporting, and quality gates.

**Story Goal**: All branches are tested, coverage is reported to Codacy, and failures block main merges.

**Independent Test**: Create feature branch, push failing test, verify merge is blocked; push passing test, verify merge is allowed and Codacy reports coverage.

#### Test Automation on All Branches

- [X] T052 Create `.github/workflows/test.yml` to run all tests on every push/PR
- [X] T053 Add test execution step to run `dotnet test` with code coverage collection
- [X] T054 Configure test failure reporting (exit code check)
- [ ] T055 Add branch protection rule: require passing tests before main merge
- [ ] T056 Test: push failing test to feature branch, verify CI fails and merge is blocked

#### Coverage Reporting to Codacy

- [ ] T057 Add Codacy token secret configuration in GitHub repository settings
- [X] T058 Integrate Codacy coverage upload in `test.yml` workflow
- [ ] T059 Configure coverage threshold: 80% project-wide goal (from spec)
- [X] T060 Add coverage report comment on pull requests
- [ ] T061 Test: push change with coverage drop, verify Codacy warning appears on PR

#### Quality Gates

- [ ] T062 Configure Codacy check to warn (not block) on coverage below 80%
- [ ] T063 Configure test failure check to block merge
- [X] T064 Document quality gate policy in ADR
- [ ] T065 Test: verify test failure blocks merge, coverage drop shows warning but allows merge

#### Release Trigger on Quality Gate Pass

- [ ] T066 Update `release.yml` to run only if test workflow succeeds
- [ ] T067 Add check: all required status checks passed before release
- [ ] T068 Test: merge PR to main with failing tests, verify release is NOT triggered
- [ ] T069 Test: merge PR to main with passing tests, verify release IS triggered

#### Release Performance SLA Configuration

- [X] T048a Configure GitHub Actions job timeout in `release.yml` to 25 minutes (buffer before 30-minute SLA from SC-003)
- [X] T048b Document timeout rationale in ADR (5-minute buffer for cleanup, upload finalization)
- [ ] T048c Test: verify workflow kills gracefully at timeout and logs clear error message

#### Testing Documentation

- [ ] T070 Document testing requirements in `quickstart.md`
- [ ] T071 Document Codacy integration in `quickstart.md`
- [X] T072 Create testing ADR explaining TDD expectations and coverage goals

---

## Phase 3: Integration & Polish

### Cross-Cutting Concerns

- [ ] T073 [P] Create comprehensive testing guide for workflow validation (manual test scenarios)
- [ ] T074 [P] Document troubleshooting guide for common workflow failures
- [ ] T075 [P] Create workflow status monitoring dashboard documentation
- [ ] T076 [P] Implement error logging/reporting from all shell scripts

### Error Handling & Robustness

- [ ] T077 Add retry logic to Codacy upload (transient failure handling)
- [ ] T078 Add graceful failure if optional platform build fails (log warning, skip asset)
- [ ] T079 Add graceful failure if SBOM generation fails (fail release - security critical)
- [ ] T080 Test error scenarios: simulate Codacy outage, simulate single platform failure

### Documentation & Handoff

- [X] T081 Complete `research.md` with all Phase 0 research findings
- [X] T082 Complete `data-model.md` with release pipeline data model (versions, tags, releases)
- [X] T083 Complete `contracts/` directory with detailed workflow specifications
- [X] T084 Complete `quickstart.md` with end-to-end user journey walkthrough
- [X] T085 Create troubleshooting guide in `specs/001-automated-release-pipeline/troubleshooting.md`

### Final Validation & Testing

- [ ] T086 End-to-end test: create feature branch with test + code, push, verify all workflows run
- [ ] T087 End-to-end test: merge to main, verify release created with version/notes/binaries/hashes/sbom
- [ ] T088 Verify all 4 platform binaries present in release
- [ ] T089 Verify SHA256SUMS file is present and checksums are correct
- [ ] T090 Verify SBOM file is present and contains expected dependencies
- [ ] T091 Manual download test: download each binary, verify it runs and shows version
- [ ] T092 Verify release notes are accurate and well-formatted
- [ ] T093 Performance test: measure time from main merge to release availability (goal: < 30 min)
- [ ] T094 Performance test: measure binary download time (goal: < 2 min for typical connection)
- [ ] T095 Security test: verify no secrets leaked in workflows or scripts
- [ ] T096 Accessibility test: verify documentation is clear for new developers

---

## Task Execution Strategy

### Phase 0 (Foundation)

**Duration**: ~1 week  
**Parallelization**: T001-T005 can run in parallel, followed by T006-T010 in parallel  
**Blocking**: Phase 1 blocks on Phase 0 completion

### Phase 1 (P1 Stories)

**Duration**: ~2 weeks  
**Parallelization**:

- Build infrastructure (T011-T017) can run in parallel across platforms
- Hash generation (T023-T026) and SBOM generation (T027-T032) can run in parallel
- Documentation (T033-T035) can run in parallel

### Phase 2 (P2 Stories)

**Duration**: ~2 weeks  
**Parallelization**:

- Version management (T036-T040) and release notes (T041-T044) can run in parallel
- Test automation (T052-T056) and coverage reporting (T057-T061) can run in parallel
- Quality gates (T062-T065) are sequential (depend on earlier steps)

### Phase 3 (Integration & Polish)

**Duration**: ~1 week  
**Parallelization**: T073-T076 can run in parallel, followed by T077-T080, then final validation (T086-T096)

### Total Estimated Timeline

**~6 weeks** with efficient parallelization and team coordination

---

## Dependency Map

```text
Phase 0: Foundation (T001-T010)
  ↓
Phase 1a: Build Infrastructure (T011-T017)
  ├→ Phase 1b: Release to GitHub (T018-T022)
  ├→ Phase 1c: Hash & SBOM (T023-T035)
  ↓
Phase 2a: Version Management (T036-T040)
  ├→ Phase 2b: Release Notes (T041-T044)
  ├→ Phase 2c: Test Automation (T052-T056)
  ├→ Phase 2d: Coverage Reporting (T057-T061)
  ├→ Phase 2e: Quality Gates (T062-T065)
  ├→ Phase 2f: Release Trigger (T066-T069)
  ↓
Phase 3: Integration & Polish (T073-T096)
```

---

## Success Criteria per Phase

**Phase 0**: All prerequisites verified, research completed, team ready  
**Phase 1**: Binaries downloadable, hashes verifiable, SBOM accessible  
**Phase 2**: Releases automatic, version increments correct, quality gates enforced  
**Phase 3**: All workflows robust, documentation complete, end-to-end validated
