# Tasks: Worktreeとブランチ情報の一覧表示

**Input**: Design documents from `/specs/002-list-worktree-branches/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/cli-interface.md, research.md, quickstart.md

## TDD前提

**このプロジェクトはTDD (Test-Driven Development)を採用しています。**

- 各実装タスクは **Red-Green-Refactorサイクル** に従って実施してください
  1. **Red**: テストを先に書き、失敗することを確認
  2. **Green**: テストが通る最小限の実装
  3. **Refactor**: コード品質の向上
- 詳細なTDDワークフローは `quickstart.md` を参照してください
- テストタスクは実装タスクに統合されているため、個別には記載していません
- constitution.mdで定義されたテストカバレッジ基準（80%以上）を満たすこと

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

This is a single CLI project with the following structure:

- `wt.cli/` - Main CLI application source
- `wt.tests/` - Test project

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Review existing project structure and ensure it matches plan.md
- [X] T002 Verify System.CommandLine 2.0.1 and System.IO.Abstractions 22.1.0 dependencies
- [X] T003 [P] Verify xUnit, Shouldly, and Moq test dependencies are configured

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Create WorktreeInfo model with all properties and methods in wt.cli/Models/WorktreeInfo.cs
- [X] T005 [P] Create BranchInfo model (future extension placeholder) in wt.cli/Models/BranchInfo.cs
- [X] T006 [P] Create IOutputFormatter interface in wt.cli/Formatters/IOutputFormatter.cs
- [X] T007 Implement TableFormatter class in wt.cli/Formatters/TableFormatter.cs
- [X] T008 Extend IGitService interface to add ListWorktreesAsync method signature
- [X] T009 Implement GitService.ListWorktreesAsync() method to parse `git worktree list --porcelain` output in wt.cli/Services/Git/GitService.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - すべてのworktreeとブランチを表示 (Priority: P1) 🎯 MVP

**Goal**: Display all worktrees with their checked-out branches in a structured format

**Independent Test**: Execute the list command and verify all worktrees with their branch names are displayed

### Implementation for User Story 1

- [X] T010 [US1] Create ListCommand class inheriting from Command in wt.cli/Commands/ListCommand.cs
- [X] T011 [US1] Implement ListCommand constructor with IWorktreeService dependency
- [X] T012 [US1] Implement ListCommand.SetHandler to call WorktreeService and format output
- [X] T013 [US1] Extend IWorktreeService interface to add ListWorktreesAsync method signature
- [X] T014 [US1] Implement WorktreeService.ListWorktreesAsync() to call GitService.ListWorktreesAsync() in wt.cli/Services/Worktree/WorktreeService.cs
- [X] T015 [US1] Add worktree creation time retrieval logic using `.git/worktrees/<name>/gitdir` file timestamp in WorktreeService
- [X] T016 [US1] Add worktree existence validation using Directory.Exists() in WorktreeService
- [X] T017 [US1] Add sorting by creation date (newest first) in WorktreeService.ListWorktreesAsync()
- [X] T018 [US1] Register ListCommand in Program.cs with dependency injection
- [X] T019 [US1] Add warning message output to stderr for missing worktrees in ListCommand handler
- [X] T020 [US1] Add "No worktrees found" message handling in ListCommand handler
- [X] T021 [US1] Add error handling for Git not found (exit code 1) in ListCommand handler
- [X] T022 [US1] Add error handling for not a Git repository (exit code 2) in ListCommand handler
- [X] T023 [US1] Add error handling for Git command failure (exit code 10) in ListCommand handler

**Checkpoint**: At this point, User Story 1 should be fully functional - `wt list` command displays all worktrees with branches

---

## Phase 4: User Story 2 - 人間が読みやすい形式でリストを表示 (Priority: P2)

**Goal**: Display worktree and branch information in a clear, human-readable table format

**Independent Test**: Verify output is formatted as a table with Path, Branch, and Status columns

### Implementation for User Story 2

- [X] T024 [US2] Implement TableFormatter.Format() method to calculate column widths in wt.cli/Formatters/TableFormatter.cs
- [X] T025 [US2] Implement TableFormatter header row generation with Unicode box drawing characters (┌─┬─┐)
- [X] T026 [US2] Implement TableFormatter separator line generation (├─┼─┤)
- [X] T027 [US2] Implement TableFormatter data row generation with proper column alignment
- [X] T028 [US2] Implement TableFormatter footer line generation (└─┴─┘)
- [X] T029 [US2] Update ListCommand to use TableFormatter.Format() for output
- [X] T030 [US2] Ensure table displays Path, Branch (using GetDisplayBranch()), and Status (using GetDisplayStatus()) columns
- [X] T031 [US2] Verify table output handles long paths and branch names correctly with dynamic column widths

**Checkpoint**: At this point, User Stories 1 AND 2 should both work - output is in a clear table format

---

## Phase 5: User Story 3 - 代替出力フォーマットのサポート (Priority: P3)

**Goal**: Support alternative output formats (JSON, CSV) for scripting and automation

**Note**: This feature is deferred to future releases per specification

### Implementation for User Story 3 (Future Release)

- [ ] T032 [US3] Add --format option to ListCommand with values: table, json, csv
- [ ] T033 [US3] Create JsonFormatter implementing IOutputFormatter in wt.cli/Formatters/JsonFormatter.cs
- [ ] T034 [US3] Create CsvFormatter implementing IOutputFormatter in wt.cli/Formatters/CsvFormatter.cs
- [ ] T035 [US3] Update ListCommand to select formatter based on --format option
- [ ] T036 [US3] Add JSON serialization support to WorktreeInfo model with JsonPropertyName attributes
- [ ] T037 [US3] Implement JsonFormatter.Format() to output JSON array
- [ ] T038 [US3] Implement CsvFormatter.Format() to output CSV with header row

**Checkpoint**: All user stories completed - alternative formats supported

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [X] T039 [P] Update README.md with `wt list` command documentation
- [X] T040 [P] Add usage examples to getting-started.md documentation
- [ ] T041 [P] Add ADR for table formatting implementation decision (no external dependencies)
- [ ] T042 [P] Add ADR for worktree creation time retrieval approach (filesystem timestamp)
- [X] T043 Code review and refactoring for clarity and maintainability
- [X] T044 Verify all exit codes are correctly implemented (0, 1, 2, 10, 99)
- [X] T045 Verify cross-platform compatibility (Windows path separators, Unicode support)
- [ ] T046 Performance testing with 100 worktrees (should complete within 1 second)
- [ ] T047 Run quickstart.md validation following TDD workflow
- [ ] T048 [P] Update docfx documentation with API documentation for new classes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) completion
- **User Story 2 (Phase 4)**: Depends on User Story 1 (Phase 3) completion - builds on list functionality
- **User Story 3 (Phase 5)**: Deferred to future release - not blocking current implementation
- **Polish (Phase 6)**: Depends on User Stories 1 and 2 being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Depends on User Story 1 - enhances the display format of US1 output
- **User Story 3 (P3)**: Deferred to future release - no implementation required for initial MVP

### Within Each User Story

**User Story 1 (Display all worktrees):**

1. Create ListCommand and WorktreeService integration (T010-T014)
2. Add time retrieval and validation (T015-T017)
3. Register command and add error handling (T018-T023)

**User Story 2 (Human-readable table format):**

1. Implement TableFormatter core logic (T024-T028)
2. Integrate formatter with ListCommand (T029-T031)

### Parallel Opportunities

- **Within Setup (Phase 1)**: T001, T002, T003 can run in parallel
- **Within Foundational (Phase 2)**: T005, T006 can run in parallel after T004 completes
- **Within Polish (Phase 6)**: T039, T040, T041, T042, T048 can all run in parallel

---

## Parallel Example: Foundational Phase

```bash
# After T004 (WorktreeInfo model) is complete, launch these in parallel:
Task T005: "Create BranchInfo model in wt.cli/Models/BranchInfo.cs"
Task T006: "Create IOutputFormatter interface in wt.cli/Formatters/IOutputFormatter.cs"
```

---

## Parallel Example: Polish Phase

```bash
# All documentation tasks can run in parallel:
Task T039: "Update README.md with wt list command documentation"
Task T040: "Add usage examples to getting-started.md documentation"
Task T041: "Add ADR for table formatting implementation decision"
Task T042: "Add ADR for worktree creation time retrieval approach"
Task T048: "Update docfx documentation with API documentation"
```

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 2)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (basic listing functionality)
4. Complete Phase 4: User Story 2 (table formatting)
5. **STOP and VALIDATE**: Test `wt list` command with multiple worktrees
6. Deploy/demo MVP

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add User Story 1 → Basic listing works → Test independently
3. Add User Story 2 → Beautiful table format → Test independently → Deploy/Demo (MVP!)
4. User Story 3 deferred to future release

### Testing Strategy

While test tasks are not included per specification, follow TDD principles from quickstart.md:

1. Write test for each component (Red)
2. Implement minimum code to pass test (Green)
3. Refactor for quality (Refactor)
4. Verify manually with real Git repositories

---

## Notes

- [P] tasks = different files, no dependencies, can run in parallel
- [Story] label maps task to specific user story for traceability
- Each user story should be independently testable
- User Story 3 (P3) is deferred to future release - no implementation required for MVP
- Follow TDD workflow from quickstart.md: Red → Green → Refactor
- Commit after each task or logical group
- Stop at checkpoints to validate story independently
- All file paths are relative to repository root

---

## Summary

- **Total Tasks**: 48 (excluding deferred User Story 3 implementation)
- **MVP Tasks**: T001-T031 (31 tasks for User Stories 1 and 2)
- **User Story 1 Tasks**: 14 tasks (T010-T023)
- **User Story 2 Tasks**: 8 tasks (T024-T031)
- **Deferred Tasks**: 7 tasks (T032-T038) for User Story 3
- **Parallel Opportunities**: 8 tasks can be parallelized (marked with [P])
- **Suggested MVP Scope**: User Stories 1 and 2 (basic listing + table formatting)
- **Format Validation**: ✅ All tasks follow checklist format with checkbox, ID, optional labels, and file paths
