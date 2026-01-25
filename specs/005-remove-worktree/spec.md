# Feature Specification: Remove Git Worktree

**Feature Branch**: `005-remove-worktree`
**Created**: 2026-01-24
**Status**: Draft
**Input**: User description: "Add the feature of removing specific branch from git worktree. Optionaly, can force remove. After remove worktree, working directory removed from disk."

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Remove a Worktree Safely (Priority: P1)

A developer has finished work on a feature branch that was checked out in a worktree. They want to clean up the worktree and remove the associated working directory from disk to free up space and maintain a clean workspace.

**Why this priority**: This is the core MVP functionality that delivers the primary value. Without this, the feature is incomplete. It allows developers to successfully clean up worktrees they no longer need.

**Independent Test**: Can be fully tested by creating a worktree, then removing it with the standard remove command, and verifying the worktree is gone and the directory is deleted from disk.

**Acceptance Scenarios**:

1. **Given** a developer has an active worktree with no uncommitted changes, **When** they execute the remove command on that worktree, **Then** the worktree is removed from the git repository and its working directory is deleted from disk
2. **Given** a developer specifies a worktree by name/path, **When** they execute the remove command, **Then** only that specific worktree is removed, and other worktrees remain unaffected

---

### User Story 2 - Force Remove a Locked Worktree (Priority: P2)

A developer's worktree is locked (either via `git worktree lock` or due to uncommitted changes) and cannot be removed with a standard remove command. They need a `--force` flag to remove the worktree anyway, even if the removal cannot be performed cleanly.

**Why this priority**: High value for recovery scenarios and unblocking developers when worktrees become stuck. Provides an escape hatch for problematic situations that prevents losing work.

**Independent Test**: Can be fully tested by creating a worktree, simulating a lock condition (either via `git worktree lock` or by creating uncommitted changes), then removing it with the `--force` flag, and verifying the worktree and directory are removed despite the lock.

**Acceptance Scenarios**:

1. **Given** a worktree has a git lock file (`.git/worktrees/<name>/locked`), **When** a developer uses the `--force` flag with the remove command, **Then** the worktree is forcibly removed from the git repository and the working directory is deleted from disk
2. **Given** a worktree has uncommitted changes (staged or unstaged modifications), **When** a developer uses the `--force` flag with the remove command, **Then** the worktree is forcibly removed despite the uncommitted changes
3. **Given** multiple worktrees exist and one is locked, **When** using the `--force` flag to remove the locked worktree, **Then** only the locked worktree is removed; other worktrees remain unaffected

---


### Edge Cases

- What happens when a developer attempts to remove a worktree that doesn't exist?
- What happens when the working directory has already been deleted but the worktree entry still exists in git?
- What happens when a developer tries to remove the main worktree or the one they're currently working in?
- How does the system handle permission errors when trying to delete the working directory?
- What happens if only part of the working directory can be deleted due to locked files or permission restrictions?

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: System MUST accept a worktree identifier (name or path) as input to the remove command
- **FR-002**: System MUST remove the specified worktree from the git worktree tracking system
- **FR-003**: System MUST delete the associated working directory from disk after removing the worktree
- **FR-004**: System MUST support a `--force` flag that allows removal of locked worktrees (see Definitions section for lock conditions)
- **FR-005**: System MUST prevent removal of the primary/main worktree (the main working directory of the repository)
- **FR-006**: System MUST prevent removal of a worktree that is currently checked out in the active session
- **FR-007**: System MUST prevent removal if the worktree has uncommitted changes, unless the `--force` flag is used
- **FR-008**: System MUST provide error messages when removal fails that include: (1) the specific reason for failure, (2) the affected worktree identifier, and (3) a suggested resolution action (e.g., "use --force to override" or "commit/stash changes first")
- **FR-009**: System MUST remove only the specified worktree without affecting other worktrees in the same repository
- **FR-010**: System MUST proceed with worktree removal even if some files in the working directory cannot be deleted; report which files failed to delete and why

### Key Entities

- **Worktree**: A git worktree associated with a branch, including its metadata (name, path, branch reference, locked status) and its working directory on disk

### Definitions

- **Locked Worktree**: A worktree is considered "locked" when a `locked` file exists at `.git/worktrees/<worktree-name>/locked` (created by `git worktree lock` command). A locked worktree will refuse standard removal and require the `--force` flag to override.

- **Uncommitted Changes**: Any of the following states in the worktree's working directory:
  1. **Staged changes**: Files added to the index but not yet committed
  2. **Unstaged modifications**: Tracked files with modifications not yet staged
  3. **Untracked files are NOT considered uncommitted changes** (consistent with git worktree remove behavior)

  A worktree with uncommitted changes will refuse standard removal (per FR-007) and require the `--force` flag to override.

- **Conditions Requiring --force**: The `--force` flag is required to remove a worktree when ANY of these conditions exist:
  1. Worktree is locked (lock file present)
  2. Worktree has uncommitted changes (staged or unstaged modifications)

- **Main/Primary Worktree**: The original working directory created when the repository was cloned or initialized. Identified by being the only worktree without a `.git` file pointing to the main repository's `.git/worktrees/` directory (i.e., its `.git` is a directory, not a file).

- **Current Worktree**: The worktree whose directory contains the current working directory (CWD) from which the command is executed. Determined by checking if CWD is equal to or a subdirectory of any worktree's path.

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: Developers can remove a worktree and have its directory deleted from disk in a single command
- **SC-002**: 100% of normal worktree removal operations complete successfully on the first attempt
- **SC-003**: Force removal successfully unblocks developers when standard removal fails due to locks
- **SC-004**: Error messages clearly communicate why a removal failed, enabling developers to fix issues without trial-and-error
- **SC-005**: No unintended worktrees are affected when removing a specific worktree from a repository with multiple worktrees

## Assumptions

- Developers have permission to delete the working directory from disk (will fail gracefully if not)
- Worktrees created by the system follow standard git worktree conventions
- The primary worktree is always protected from removal to prevent repository corruption
- Force flag behavior aligns with standard git conventions (attempts removal even with locks present)

## Clarifications

### Session 2026-01-24

- Q: Should the system prompt for confirmation before removing a worktree? → A: No confirmation prompt; rely only on explicit worktree selection and error messages
  - **Impact**: Removes User Story 3 from scope; simplifies UX by deferring safety to explicit targeting
  - **Implementation**: No interactive prompts; all removal is direct (no `--confirm` or `--interactive` flags)

- Q: What happens if a worktree has uncommitted changes? → A: Prevent removal if uncommitted changes exist; require commit/stash or `--force` to override
  - **Impact**: Adds FR-007 (uncommitted changes check); aligns with git's safety model
  - **Implementation**: Check for uncommitted changes before proceeding; `--force` bypasses this check

- Q: How should the system handle partial directory deletion (some files cannot be deleted)? → A: Proceed with removal anyway; list which files/directories failed to delete (user must manually clean up)
  - **Impact**: Clarifies FR-010 behavior; prioritizes removing worktree entry over achieving 100% disk cleanup
  - **Implementation**: Remove worktree from tracking regardless; report failures separately for user resolution

- Q: What exactly constitutes a "locked" worktree? → A: A worktree is locked when: (1) a git lock file exists at `.git/worktrees/<name>/locked`, OR (2) uncommitted changes exist (staged or unstaged modifications; untracked files do not count)
  - **Impact**: Adds Definitions section with measurable criteria for lock detection; updates FR-004 to reference definitions
  - **Implementation**: Check for lock file presence and run `git status --porcelain` to detect uncommitted changes
