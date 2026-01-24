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

A developer's worktree is locked (potentially due to another git process holding a lock or an interrupted operation) and cannot be removed with a standard remove command. They need a force flag to remove the worktree anyway, even if the removal cannot be performed cleanly.

**Why this priority**: High value for recovery scenarios and unblocking developers when worktrees become stuck. Provides an escape hatch for problematic situations that prevents losing work.

**Independent Test**: Can be fully tested by creating a worktree, simulating a lock condition, then removing it with the force flag, and verifying the worktree and directory are removed despite the lock.

**Acceptance Scenarios**:

1. **Given** a worktree is locked or has exclusive file locks, **When** a developer uses the force flag with the remove command, **Then** the worktree is forcibly removed from the git repository and the working directory is deleted from disk
2. **Given** multiple worktrees exist and one is locked, **When** using the force flag to remove the locked worktree, **Then** only the locked worktree is removed; other worktrees remain unaffected

---

### User Story 3 - Prevent Accidental Removal with Confirmation (Priority: P3)

A developer wants protection against accidentally removing a worktree they didn't intend to delete. The system should provide a way to confirm the removal operation before proceeding, especially when using the force flag.

**Why this priority**: Improves user experience by adding safety guardrails. While not critical for the MVP, it reduces the risk of data loss from mistakes and improves developer confidence in using the feature.

**Independent Test**: Can be fully tested by attempting to remove a worktree with confirmation enabled and verifying that either confirming or declining the operation produces the expected outcome.

**Acceptance Scenarios**:

1. **Given** a developer attempts to remove a worktree, **When** the system requests confirmation, **Then** the removal proceeds only if the user confirms the action
2. **Given** a developer declines the confirmation prompt, **When** no confirmation is provided, **Then** the worktree remains unchanged and the operation is cancelled

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
- **FR-004**: System MUST support a force flag that allows removal of locked or problematic worktrees
- **FR-005**: System MUST prevent removal of the primary/main worktree (the main working directory of the repository)
- **FR-006**: System MUST prevent removal of a worktree that is currently checked out in the active session
- **FR-007**: System MUST provide clear error messages when removal fails, indicating the reason (not found, locked, in use, etc.)
- **FR-008**: System MUST remove only the specified worktree without affecting other worktrees in the same repository
- **FR-009**: System MUST handle partial directory deletion gracefully, reporting which files could not be deleted and why
- **FR-010**: System MUST support confirmation prompts before removal to prevent accidental deletion

### Key Entities

- **Worktree**: A git worktree associated with a branch, including its metadata (name, path, branch reference, locked status) and its working directory on disk

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
