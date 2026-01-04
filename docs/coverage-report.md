# Code Coverage Report

**Generated:** 2026-01-04  
**Branch:** copilot/sub-pr-2  
**Commits Tested:** 813360c (fix: resolve git directory from .git file for worktrees) and 79b7ac5 (refactor: remove redundant Directory.Exists check)

## Overall Coverage Summary

- **Line Coverage:** 81.8% (728/889 lines)
- **Branch Coverage:** 64.2% (167/260 branches)
- **Method Coverage:** 97.5% (121/124 methods)
- **Full Method Coverage:** 83% (103/124 fully covered methods)

## Test Results

- **Total Tests:** 171
- **Passed:** 169
- **Failed:** 1 (unrelated to changes - EditorService special characters test)
- **Skipped:** 1

## Coverage by Component

| Component | Line Coverage |
|-----------|---------------|
| ListCommand | 100% |
| CreateCommand | 79.4% |
| TableFormatter | 100% |
| **GitService** | **70.6%** |
| EditorService | 97.3% |
| WorktreeService | 77% |
| ProcessRunner | 100% |
| Validators | 100% |

## GitService Coverage Analysis

The `GitService` class has 70.6% line coverage overall. The `CreateWorktreeInfo` method, which contains the changes made in this PR, has:

- **Line Coverage:** 28.57% (12/64 lines covered)
- **Branch Coverage:** 16.66%

### Uncovered Code in Changes

The following new code added in commits 813360c and 79b7ac5 is **NOT covered by tests**:

1. **Git directory resolution logic (lines 302-323):**
   - Checking if `.git` is a file
   - Reading the `.git` file contents
   - Parsing the `gitdir:` prefix
   - Resolving relative paths to absolute paths
   - Setting the resolved `gitDir` variable

2. **File creation time reading (lines 329-332):**
   - Checking if gitWorktreePath exists
   - Reading file creation time

3. **Exception handling (lines 334-340+):**
   - IOException handling
   - UnauthorizedAccessException handling
   - Error logging

### Reason for Low Coverage

The existing unit tests for `GitService` use mocked `IProcessRunner` and don't exercise the file system operations in `CreateWorktreeInfo`. The method is only called indirectly through integration tests that test the full `ListWorktreesAsync` flow, but those tests don't create scenarios where:

- The `.git` file exists as a file (worktree scenario)
- The `gitdir` file exists to read creation time
- Exceptions are thrown during file operations

## Recommendations

To improve coverage of the changes:

1. **Add unit tests** that specifically test the `.git` file parsing logic:
   - Test when `.git` is a file with valid `gitdir:` reference
   - Test when `.git` is a file with invalid content
   - Test relative path resolution
   - Test absolute path handling

2. **Add integration tests** that:
   - Create actual worktrees and verify timestamp reading
   - Test the command when run from within a worktree
   - Verify behavior when worktree metadata files are missing

3. **Consider refactoring** to make the git directory resolution logic easier to test in isolation:
   - Extract the `.git` file parsing to a separate testable method
   - Make file system operations mockable for better unit testing

## Coverage Report Files

Full coverage reports are available in:
- HTML Report: `TestResults/CoverageReport/index.html`
- Cobertura XML: `TestResults/**/coverage.cobertura.xml`
- Text Summary: `TestResults/CoverageReport/Summary.txt`

## How to Reproduce

```bash
# Run tests with coverage collection
dotnet test wt.tests/wt.tests.csproj --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Generate HTML report
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:"Html;TextSummary"

# View the report
# Open TestResults/CoverageReport/index.html in a browser
```
