# Tasks: Remove Git Worktree

**Input**: Design documents from `/specs/005-remove-worktree/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: TDD approach required per project constitution. Tests are written FIRST and must FAIL before implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `wt.cli/` (Commands/, Services/, Models/, Utils/)
- **Tests**: `wt.tests/` (Commands/, Services/)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create model classes and interface definitions needed by all user stories

- [ ] T001 [P] Create RemovalValidationError enum in wt.cli/Models/RemovalValidationError.cs
- [ ] T002 [P] Create DeletionFailure class in wt.cli/Models/DeletionFailure.cs
- [ ] T003 [P] Create RemoveWorktreeOptions class in wt.cli/Models/RemoveWorktreeOptions.cs
- [ ] T004 [P] Create RemoveWorktreeResult and RemoveWorktreeData classes in wt.cli/Models/RemoveWorktreeResult.cs
- [ ] T005 Add ValidateForRemoval and RemoveWorktreeAsync method signatures to wt.cli/Services/Worktree/IWorktreeService.cs
- [ ] T006 Add HasUncommittedChanges and RemoveWorktreeAsync method signatures to wt.cli/Services/Git/IGitService.cs
- [ ] T007 Add IsWorktreeLocked method signature to wt.cli/Services/Git/IGitService.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before user story implementation

**⚠️ CRITICAL**: No user story implementation can begin until this phase is complete

- [ ] T008 Implement HasUncommittedChanges method in wt.cli/Services/Git/GitService.cs (calls `git -C <path> status --porcelain`, filters out `??` lines)
- [ ] T009 Implement IsWorktreeLocked method in wt.cli/Services/Git/GitService.cs (checks `.git/worktrees/<name>/locked` file existence)
- [ ] T010 Implement RemoveWorktreeAsync in wt.cli/Services/Git/GitService.cs (calls `git worktree remove [--force] <path>`)
- [ ] T011 Add stub implementations for ValidateForRemoval and RemoveWorktreeAsync in wt.cli/Services/Worktree/WorktreeService.cs (throw NotImplementedException)
- [ ] T012 Create RemoveCommand class skeleton in wt.cli/Commands/Worktree/RemoveCommand.cs (argument/option definitions only, no handler)
- [ ] T013 Register RemoveCommand in wt.cli/Program.cs

**Checkpoint**: Foundation ready - command is wired but returns not-implemented. User story implementation can now begin.

---

## Phase 3: User Story 1 - Remove a Worktree Safely (Priority: P1) 🎯 MVP

**Goal**: Developers can remove a worktree with no uncommitted changes and have the working directory deleted from disk.

**Independent Test**: Create a worktree, run `wt remove <branch>`, verify worktree is gone from git and directory is deleted.

### Tests for User Story 1 (TDD - Write First, Must FAIL)

- [ ] T014 [P] [US1] Create WorktreeServiceRemoveTests.cs in wt.tests/Services/Worktree/ with test class skeleton and required using statements
- [ ] T015 [P] [US1] Write test: ValidateForRemoval_WhenWorktreeNotFound_ReturnsNotFound in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [ ] T016 [P] [US1] Write test: ValidateForRemoval_WhenMainWorktree_ReturnsIsMainWorktree in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [ ] T017 [P] [US1] Write test: ValidateForRemoval_WhenCurrentWorktree_ReturnsIsCurrentWorktree in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [ ] T018 [P] [US1] Write test: ValidateForRemoval_WhenUncommittedChanges_ReturnsUncommittedChanges in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [ ] T019 [P] [US1] Write test: ValidateForRemoval_WhenValid_ReturnsNone in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [ ] T020 [P] [US1] Write test: RemoveWorktreeAsync_WhenValidationFails_ReturnsError in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [ ] T021 [P] [US1] Write test: RemoveWorktreeAsync_WhenSuccess_RemovesWorktreeAndDirectory in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [ ] T022 [P] [US1] Create RemoveCommandTests.cs in wt.tests/Commands/Worktree/ with test class skeleton
- [ ] T023 [P] [US1] Write test: RemoveCommand_ParsesWorktreeArgument in wt.tests/Commands/Worktree/RemoveCommandTests.cs
- [ ] T024 [P] [US1] Write test: RemoveCommand_HumanOutput_DisplaysSuccessMessage in wt.tests/Commands/Worktree/RemoveCommandTests.cs
- [ ] T025 [P] [US1] Write test: RemoveCommand_JsonOutput_ReturnsStructuredResult in wt.tests/Commands/Worktree/RemoveCommandTests.cs

### Implementation for User Story 1

- [ ] T026 [US1] Implement ValidateForRemoval method in wt.cli/Services/Worktree/WorktreeService.cs (check exists, main, current, uncommitted)
- [ ] T027 [US1] Implement RemoveWorktreeAsync core logic in wt.cli/Services/Worktree/WorktreeService.cs (validate → git remove → delete directory)
- [ ] T028 [US1] Implement DeleteWorktreeDirectoryAsync helper method in wt.cli/Services/Worktree/WorktreeService.cs (recursive delete with IFileSystem)
- [ ] T029 [US1] Implement RemoveCommand handler in wt.cli/Commands/Worktree/RemoveCommand.cs (call service, format output)
- [ ] T030 [US1] Add human output formatting in RemoveCommand (success/error messages with checkmark/cross symbols)
- [ ] T031 [US1] Add JSON output formatting in RemoveCommand (serialize RemoveWorktreeResult)

**Checkpoint**: At this point, User Story 1 should be fully functional. Run `wt remove <branch>` and verify worktree and directory are removed.

---

## Phase 4: User Story 2 - Force Remove a Locked Worktree (Priority: P2)

**Goal**: Developers can remove a locked worktree or one with uncommitted changes using the `--force` flag.

**Independent Test**: Create a worktree with uncommitted changes or lock file, run `wt remove <branch> --force`, verify worktree is removed.

### Tests for User Story 2 (TDD - Write First, Must FAIL)

- [ ] T032 [P] [US2] Write test: ValidateForRemoval_WhenLockedWorktree_ReturnsLocked in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [ ] T033 [P] [US2] Write test: ValidateForRemoval_WhenUncommittedChangesWithForce_ReturnsNone in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [ ] T034 [P] [US2] Write test: ValidateForRemoval_WhenLockedWithForce_ReturnsNone in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [ ] T035 [P] [US2] Write test: RemoveWorktreeAsync_WhenForce_BypassesUncommittedChangesCheck in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [ ] T036 [P] [US2] Write test: RemoveWorktreeAsync_WhenPartialDeletion_ReportsUndeleteableFiles in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [ ] T037 [P] [US2] Write test: RemoveCommand_ParsesForceFlag in wt.tests/Commands/Worktree/RemoveCommandTests.cs
- [ ] T038 [P] [US2] Write test: RemoveCommand_WithForce_RemovesLockedWorktree in wt.tests/Commands/Worktree/RemoveCommandTests.cs

### Implementation for User Story 2

- [ ] T039 [US2] Update ValidateForRemoval in wt.cli/Services/Worktree/WorktreeService.cs to check for lock file (IsWorktreeLocked)
- [ ] T040 [US2] Update ValidateForRemoval in wt.cli/Services/Worktree/WorktreeService.cs to respect Force flag for uncommitted changes and locks
- [ ] T041 [US2] Update RemoveWorktreeAsync in wt.cli/Services/Worktree/WorktreeService.cs to pass --force flag to git when Force=true
- [ ] T042 [US2] Implement partial deletion handling in DeleteWorktreeDirectoryAsync (catch UnauthorizedAccessException, IOException; track failures)
- [ ] T043 [US2] Update output formatting to display partial failure messages with undeleteable file list

**Checkpoint**: Both User Stories should work independently. Test normal removal (US1) and forced removal (US2).

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, code quality, and final validation

- [ ] T044 [P] Add XML documentation comments to all public methods in RemoveCommand.cs
- [ ] T045 [P] Add XML documentation comments to ValidateForRemoval, RemoveWorktreeAsync in WorktreeService.cs
- [ ] T046 [P] Add verbose output mode support (--verbose flag) in wt.cli/Commands/Worktree/RemoveCommand.cs
- [ ] T047 Build project and run DocGenerator: `dotnet run --project Tools/DocGenerator/DocGenerator/DocGenerator.csproj`
- [ ] T048 Verify generated documentation includes wt remove command reference
- [ ] T049 Update CHANGELOG.md with new feature entry
- [ ] T050 Run all tests and verify >80% coverage: `dotnet test --collect:"XPlat Code Coverage"`
- [ ] T051 Code cleanup: ensure all methods are <50 LOC per constitution
- [ ] T052 [P] Create ADR for force flag behavior in docs/adr/NNNN-force-flag-removal-behavior.md (per constitution IV: document decision for --force bypassing uncommitted changes and lock checks)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational phase completion - MVP
- **User Story 2 (Phase 4)**: Depends on Foundational phase completion - can start in parallel with US1
- **Polish (Phase 5)**: Depends on both user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - Core removal functionality
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Extends US1 with force flag support
  - **Note**: US2 builds on US1 implementation but tests are independent

### Within Each User Story

1. Tests MUST be written FIRST and FAIL before implementation
2. Service layer before command layer
3. Core implementation before output formatting
4. Story complete when all tests pass

### Parallel Opportunities

**Phase 1 (Setup)**:
- T001, T002, T003, T004 can run in parallel (different model files)

**Phase 3 (US1 Tests)**:
- T014-T025 can run in parallel (different test methods/files)

**Phase 4 (US2 Tests)**:
- T032-T038 can run in parallel (different test methods)

**Phase 5 (Polish)**:
- T044, T045, T046, T052 can run in parallel (different concerns)

---

## Parallel Example: User Story 1 Tests

```bash
# Launch all US1 tests together (TDD - must fail initially):
Task: "Write test: ValidateForRemoval_WhenWorktreeNotFound_ReturnsNotFound"
Task: "Write test: ValidateForRemoval_WhenMainWorktree_ReturnsIsMainWorktree"
Task: "Write test: ValidateForRemoval_WhenCurrentWorktree_ReturnsIsCurrentWorktree"
Task: "Write test: ValidateForRemoval_WhenUncommittedChanges_ReturnsUncommittedChanges"
Task: "Write test: ValidateForRemoval_WhenValid_ReturnsNone"
Task: "Write test: RemoveWorktreeAsync_WhenValidationFails_ReturnsError"
Task: "Write test: RemoveWorktreeAsync_WhenSuccess_RemovesWorktreeAndDirectory"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (create models and interfaces)
2. Complete Phase 2: Foundational (wire up skeleton)
3. Complete Phase 3: User Story 1 (safe removal)
4. **STOP and VALIDATE**: Test `wt remove <branch>` on clean worktree
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Command wired but not implemented
2. Add User Story 1 → Normal removal works → Deploy/Demo (MVP!)
3. Add User Story 2 → Force removal works → Deploy/Demo
4. Polish → Documentation, coverage, cleanup → Final release

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 tests → User Story 1 implementation
   - Developer B: User Story 2 tests (can write in parallel, will fail until US1 is done)
3. Stories complete and integrate

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- TDD required: tests fail → implement → tests pass → refactor
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Use Moq for mocking IGitService, IFileSystem in tests
- Follow existing CreateCommand.cs patterns for RemoveCommand
