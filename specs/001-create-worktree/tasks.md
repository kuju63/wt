# Tasks: Git Worktree 作成コマンド

**Feature**: 001-create-worktree  
**Branch**: `001-create-worktree`  
**Date**: 2026-01-03  
**Input**: Design documents from `/specs/001-create-worktree/`

**Prerequisites**: ✅ All design documents complete
- [plan.md](./plan.md) - 実装計画書
- [spec.md](./spec.md) - 機能仕様（3 User Stories）
- [data-model.md](./data-model.md) - 5 Core Entities
- [contracts/cli-interface.md](./contracts/cli-interface.md) - CLI仕様
- [quickstart.md](./quickstart.md) - 開発ガイド

**Tests**: TDD approach required per constitution. All tests must be written FIRST and FAIL before implementation.

## Format: `- [ ] [ID] [P?] [Story?] Description with file path`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story label (US1, US2, US3) - only for user story phases
- Include exact file paths

---

## Phase 1: Setup (Project Initialization)

**Purpose**: プロジェクト初期化と基本構造

- [X] T001 既存プロジェクト構造を確認し、specs/001-create-worktree/plan.mdのProject Structureに記載された新規ディレクトリを作成（wt.cli/Commands, Services, Models, Utils）
- [X] T002 NuGetパッケージをwt.cliに追加: System.CommandLine, System.IO.Abstractions, System.Text.Json
- [X] T003 [P] NuGetパッケージをwt.testsに追加: xUnit, FluentAssertions, Moq
- [X] T004 [P] .editorconfig と静的解析設定を確認（既存設定を維持）

**Checkpoint**: ✅ プロジェクト構造とパッケージ準備完了

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: 全User Storyで共通利用する基盤実装。この Phase が完了するまで User Story の実装は開始できません。

**⚠️ CRITICAL**: この Phase 完了まで User Story 実装は不可

### Foundational Tests (Write FIRST, ensure they FAIL)

- [X] T005 [P] ProcessRunner のユニットテストを作成: wt.tests/Utils/ProcessRunnerTests.cs（Gitコマンド実行の成功・失敗ケース）
- [X] T006 [P] PathHelper のユニットテストを作成: wt.tests/Utils/PathHelperTests.cs（パス正規化、検証）
- [X] T007 [P] Validators のユニットテストを作成: wt.tests/Utils/ValidatorsTests.cs（ブランチ名バリデーション）

### Foundational Implementation

- [X] T008 [P] ProcessRunner を実装: wt.cli/Utils/ProcessRunner.cs（System.Diagnostics.Process wrapper、Git コマンド実行）
- [X] T009 [P] PathHelper を実装: wt.cli/Utils/PathHelper.cs（System.IO.Abstractions 使用、パス正規化・検証）
- [X] T010 [P] Validators を実装: wt.cli/Utils/Validators.cs（ブランチ名バリデーション、正規表現: `^[a-zA-Z0-9][a-zA-Z0-9/_.-]*$`）
- [X] T011 [P] CommandResult<T> モデルを実装: wt.cli/Models/CommandResult.cs（Result パターン、Success/Error/Warnings）
- [X] T012 [P] エラーコード定数クラスを実装: wt.cli/Models/ErrorCodes.cs（GIT001-ED001 の11コード定義）

**Checkpoint**: ✅ 基盤完了 - User Story 実装開始可能

---

## Phase 3: User Story 1 - Git Worktree 作成の基本機能 (Priority: P1) 🎯 MVP

**Goal**: ブランチ名を指定してコマンド実行すると、ブランチが作成され、自動的に git worktree として登録される

**Independent Test**: `wt create feature-test` を実行し、../worktrees/feature-test に worktree が作成され、feature-test ブランチにチェックアウトされていることを確認

### Tests for US1 (Write FIRST, ensure they FAIL) ⚠️

- [X] T013 [P] [US1] WorktreeInfo モデルのユニットテストを作成: wt.tests/Models/WorktreeInfoTests.cs（プロパティ検証）
- [X] T014 [P] [US1] BranchInfo モデルのユニットテストを作成: wt.tests/Models/BranchInfoTests.cs（バリデーション規則）
- [X] T015 [P] [US1] CreateWorktreeOptions モデルのユニットテストを作成: wt.tests/Models/CreateWorktreeOptionsTests.cs
- [X] T016 [P] [US1] GitService のユニットテストを作成: wt.tests/Services/Git/GitServiceTests.cs（IsGitRepository, GetCurrentBranch, BranchExists, CreateBranch, AddWorktree の各メソッド）
- [X] T017 [P] [US1] WorktreeService のユニットテストを作成: wt.tests/Services/Worktree/WorktreeServiceTests.cs（CreateWorktreeAsync の成功・失敗ケース）
- [X] T018 [US1] WorktreeCreateCommand のユニットテストを作成: wt.tests/Commands/Worktree/CreateCommandTests.cs（コマンド解析、実行フロー、System.CommandLine 2.0対応）
- [X] T019 [US1] E2E統合テストを作成: wt.tests/Integration/WorktreeE2ETests.cs（テスト用Gitリポジトリ作成、worktree作成、クリーンアップ、5テスト成功）

### Implementation for US1

- [X] T020 [P] [US1] WorktreeInfo モデルを実装: wt.cli/Models/WorktreeInfo.cs（Path, Branch, BaseBranch, CreatedAt プロパティ）
- [X] T021 [P] [US1] BranchInfo モデルを実装: wt.cli/Models/BranchInfo.cs（Name, BaseBranch, Exists, IsRemote プロパティ + バリデーション）
- [X] T022 [P] [US1] CreateWorktreeOptions モデルを実装: wt.cli/Models/CreateWorktreeOptions.cs（BranchName, BaseBranch, Path, Editor, CheckoutExisting, OutputFormat, Verbose）
- [X] T023 [P] [US1] IGitService インターフェースを定義: wt.cli/Services/Git/IGitService.cs（IsGitRepositoryAsync, GetCurrentBranchAsync, BranchExistsAsync, CreateBranchAsync, AddWorktreeAsync）
- [X] T024 [US1] GitService を実装: wt.cli/Services/Git/GitService.cs（ProcessRunner 使用、全メソッド実装、エラーハンドリング）
- [X] T025 [P] [US1] IWorktreeService インターフェースを定義: wt.cli/Services/Worktree/IWorktreeService.cs（CreateWorktreeAsync）
- [X] T026 [US1] WorktreeService を実装: wt.cli/Services/Worktree/WorktreeService.cs（GitService, PathHelper, Validators 依存、CreateWorktreeAsync 実装）
- [X] T027 [US1] WorktreeCreateCommand を実装: wt.cli/Commands/Worktree/CreateCommand.cs（System.CommandLine 2.0 使用、引数・オプション定義、handler 実装）
- [X] T028 [US1] Program.cs を更新: wt.cli/Program.cs（WorktreeCreateCommand を RootCommand に追加、DI設定）
- [X] T029 [US1] エラー処理とログ出力を追加: wt.cli/Commands/Worktree/CreateCommand.cs（CommandResult に基づくエラー表示、✓/✗記号、解決策表示）

**Checkpoint**: US1 完了 - `wt create <branch>` で worktree 作成が動作

---

## Phase 4: User Story 2 - エディター自動起動機能 (Priority: P2)

**Goal**: worktree 作成後、指定したエディター（VS Code など）を自動起動できる

**Independent Test**: `wt create feature-ui --editor vscode` を実行し、worktree 作成後に VS Code が自動起動することを確認

### Tests for US2 (Write FIRST, ensure they FAIL) ⚠️

- [X] T030 [P] [US2] EditorConfig モデルのユニットテストを作成: wt.tests/Models/EditorConfigTests.cs（EditorType, Command, Arguments, IsAvailable）
- [X] T031 [P] [US2] EditorService のユニットテストを作成: wt.tests/Services/Editor/EditorServiceTests.cs（LaunchEditorAsync, ResolveEditorCommand の各ケース）
- [X] T032 [US2] エディター起動のE2Eテストを作成: wt.tests/Integration/EditorLaunchTests.cs（VS Code起動の統合テスト）

### Implementation for US2

- [X] T033 [P] [US2] EditorConfig モデルを実装: wt.cli/Models/EditorConfig.cs（EditorType enum, Command, Arguments, IsAvailable プロパティ）
- [X] T034 [P] [US2] EditorPresets を実装: wt.cli/Services/Editor/EditorPresets.cs（5エディター: vscode, vim, emacs, nano, idea のプリセット定義）
- [X] T035 [P] [US2] IEditorService インターフェースを定義: wt.cli/Services/Editor/IEditorService.cs（LaunchEditorAsync, ResolveEditorCommand）
- [X] T036 [US2] EditorService を実装: wt.cli/Services/Editor/EditorService.cs（EditorPresets 使用、PATH検索、ProcessRunner でエディター起動）
- [X] T037 [US2] CreateCommand に --editor オプションを追加: wt.cli/Commands/Worktree/CreateCommand.cs（-e エイリアス、EditorType enum、handler でエディター起動）
- [X] T038 [US2] WorktreeService にエディター起動統合: wt.cli/Services/Worktree/WorktreeService.cs（CreateWorktreeAsync 完了後に EditorService.LaunchEditorAsync 呼び出し）
- [X] T039 [US2] エディターが見つからない場合の警告表示: wt.cli/Services/Editor/EditorService.cs（ED001 エラーコード、worktree 作成は継続）

**Checkpoint**: US2 完了 - `wt create <branch> --editor <type>` でエディター自動起動が動作

---

## Phase 5: User Story 3 - Worktree パスのカスタマイズ (Priority: P3)

**Goal**: worktree を作成する場所（パス）をカスタマイズできる

**Independent Test**: `wt create experiment --path ~/custom/path` を実行し、指定したパスに worktree が作成されることを確認

### Tests for US3 (Write FIRST, ensure they FAIL) ⚠️

- [X] T040 [P] [US3] カスタムパスバリデーションのユニットテストを追加: wt.tests/Utils/PathHelperTests.cs（無効パス、権限エラー、ディスク容量チェック）
- [X] T041 [US3] カスタムパスのE2Eテストを作成: wt.tests/Integration/CustomPathTests.cs（絶対パス、相対パス、無効パスのテスト）

### Implementation for US3

- [X] T042 [US3] PathHelper にカスタムパス検証を追加: wt.cli/Utils/PathHelper.cs（親ディレクトリ存在チェック、書き込み権限チェック、ディスク容量チェック）
- [X] T043 [US3] CreateCommand に --path オプションを追加: wt.cli/Commands/Worktree/CreateCommand.cs（-p エイリアス、デフォルト: ../worktrees/<branch>）
- [X] T044 [US3] WorktreeService でカスタムパス処理: wt.cli/Services/Worktree/WorktreeService.cs（options.Path が null の場合デフォルトパス、非 null の場合カスタムパス使用）
- [X] T045 [US3] パスエラーハンドリングを追加: wt.cli/Services/Worktree/WorktreeService.cs（FS001-FS003 エラーコード、解決策表示）

**Checkpoint**: US3 完了 - `wt create <branch> --path <custom>` でカスタムパス指定が動作

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: 全User Storyに影響する改善と仕上げ

- [X] T046 [P] --checkout-existing オプションを実装: wt.cli/Commands/Worktree/CreateCommand.cs（既存ブランチをチェックアウト、BR002エラー時の提案）
- [X] T047 [P] --output オプションを実装: wt.cli/Commands/Worktree/CreateCommand.cs（human|json 出力形式、JsonFormatter/HumanFormatter）
- [X] T048 [P] --verbose オプションを実装: wt.cli/Commands/Worktree/CreateCommand.cs（詳細診断情報、Git コマンド実行ログ）
- [X] T049 [P] --base オプションを実装: wt.cli/Commands/Worktree/CreateCommand.cs（-b エイリアス、ベースブランチ指定）
- [X] T050 IOutputFormatter インターフェースを定義: wt.cli/Services/Output/IOutputFormatter.cs（Format メソッド）
- [X] T051 [P] JsonFormatter を実装: wt.cli/Services/Output/JsonFormatter.cs（System.Text.Json 使用）
- [X] T052 [P] HumanFormatter を実装: wt.cli/Services/Output/HumanFormatter.cs（✓/✗記号、色付き出力）
- [X] T053 プログレス表示を追加: wt.cli/Commands/Worktree/CreateCommand.cs（"Creating branch...", "Adding worktree..." メッセージ）
- [ ] T054 [P] クロスプラットフォーム対応を検証: wt.tests/Integration/CrossPlatformTests.cs（Windows/macOS/Linux でのパス処理、改行コード）
- [ ] T055 [P] パフォーマンステストを追加: wt.tests/Performance/PerformanceTests.cs（5秒以内の実行時間、メモリ100MB以下）
- [X] T056 [P] README.md を更新: README.md（インストール、使用方法、例、トラブルシューティング）
- [ ] T057 [P] 日本語ユーザーガイドを作成: docs/ja/user-guide.md（全オプション説明、使用例）
- [X] T058 quickstart.md のバリデーションを実行（5フェーズワークフローが正しく動作することを確認）
- [X] T059 全テストを実行してカバレッジ80%以上を確認: `dotnet test --collect:"XPlat Code Coverage"`
- [X] T060 憲章チェックリストを最終確認: 全12項目 PASS を検証

**Checkpoint**: 機能完成 - すべてのオプションが動作し、品質基準を満たす

---

## Dependencies & Execution Order

### Phase Dependencies

1. **Setup (Phase 1)**: 依存なし - 即開始可能
2. **Foundational (Phase 2)**: Setup 完了後 - **全 User Story をブロック**
3. **User Story 1 (Phase 3)**: Foundational 完了後 - MVP
4. **User Story 2 (Phase 4)**: Foundational 完了後 - US1と並行可能（別ファイル）
5. **User Story 3 (Phase 5)**: Foundational 完了後 - US1/US2と並行可能（別ファイル）
6. **Polish (Phase 6)**: 全 User Story 完了後

### User Story Dependencies

- **US1 (P1)**: Foundational 完了後に開始可能 - 他 Story への依存なし
- **US2 (P2)**: Foundational 完了後に開始可能 - US1 の WorktreeService に統合するが、独立してテスト可能
- **US3 (P3)**: Foundational 完了後に開始可能 - US1 の PathHelper を拡張するが、独立してテスト可能

### Within Each User Story (TDD Workflow)

1. **Tests FIRST** (必ず失敗することを確認)
2. **Models** (依存なし)
3. **Services** (Models に依存)
4. **Commands** (Services に依存)
5. **Integration** (全コンポーネントに依存)

### Parallel Opportunities

**Setup Phase**:
- T002, T003, T004 は並行実行可能

**Foundational Phase**:
- Tests: T005, T006, T007 は並行実行可能
- Implementation: T008, T009, T010, T011, T012 は並行実行可能（異なるファイル）

**User Story 1**:
- Tests: T013, T014, T015, T016, T017 は並行実行可能
- Models: T020, T021, T022 は並行実行可能
- Interfaces: T023, T025 は並行実行可能

**User Story 2**:
- Tests: T030, T031 は並行実行可能
- Models/Presets: T033, T034, T035 は並行実行可能

**User Story 3**:
- Tests: T040 と T041 は並行実行可能

**Polish Phase**:
- T046, T047, T048, T049 は並行実行可能（同一ファイルだが別オプション）
- T051, T052 は並行実行可能
- T054, T055, T056, T057 は並行実行可能

**全 User Story の並行実行**:
- Foundational (Phase 2) 完了後、Phase 3, 4, 5 は並行開始可能（異なる開発者、異なるファイル）

---

## Parallel Example: User Story 1

```bash
# テストを並行で作成（Phase 3 開始時）
Task T013: WorktreeInfo model tests
Task T014: BranchInfo model tests
Task T015: CreateWorktreeOptions model tests
Task T016: GitService tests
Task T017: WorktreeService tests

# モデルを並行で実装（テスト失敗確認後）
Task T020: WorktreeInfo model
Task T021: BranchInfo model
Task T022: CreateWorktreeOptions model

# インターフェースを並行で定義
Task T023: IGitService interface
Task T025: IWorktreeService interface
```

---

## Implementation Strategy

### MVP First (User Story 1 のみ実装)

1. ✅ Phase 1: Setup (T001-T004)
2. ✅ Phase 2: Foundational (T005-T012) - **CRITICAL**
3. ✅ Phase 3: User Story 1 (T013-T029)
4. **STOP and VALIDATE**: `wt create feature-test` を実行して動作確認
5. **MVP Ready**: US1 のみでデプロイ可能

この時点で以下が動作:
- `wt create <branch>` で worktree 作成
- デフォルトパス `../worktrees/<branch>` に作成
- エラー時の解決策表示

### Incremental Delivery (優先度順)

1. Setup + Foundational → 基盤完成
2. **US1 → 独立テスト → デプロイ可能** (MVP!)
3. US2 追加 → 独立テスト → デプロイ可能（エディター起動機能付き）
4. US3 追加 → 独立テスト → デプロイ可能（カスタムパス対応）
5. Polish → 全オプション完備

各 Story が前の Story を壊さずに価値を追加

### Parallel Team Strategy

複数開発者の場合:

1. チーム全員で Setup + Foundational を完成（T001-T012）
2. Foundational 完了後:
   - **Developer A**: US1 (T013-T029) - コア機能
   - **Developer B**: US2 (T030-T039) - エディター起動
   - **Developer C**: US3 (T040-T045) - カスタムパス
3. 各 Story は独立して完成し、最後に統合

---

## Quality Gates

各フェーズ完了時にチェック:

- ✅ 全テストが GREEN（失敗なし）
- ✅ コードカバレッジ 80% 以上
- ✅ 憲章の全要件を満たす（12項目）
- ✅ 各 User Story が独立してテスト可能
- ✅ ドキュメント最新（README, user-guide）

---

## Task Summary

| Phase                 | Task Count | Can Parallelize | Description          |
| --------------------- | ---------- | --------------- | -------------------- |
| Phase 1: Setup        | 4          | 3 tasks         | プロジェクト初期化   |
| Phase 2: Foundational | 8          | 7 tasks         | 基盤実装（BLOCKING） |
| Phase 3: US1 (P1)     | 17         | 11 tasks        | コア機能 - MVP       |
| Phase 4: US2 (P2)     | 10         | 6 tasks         | エディター起動       |
| Phase 5: US3 (P3)     | 6          | 2 tasks         | カスタムパス         |
| Phase 6: Polish       | 15         | 11 tasks        | 仕上げ               |
| **Total**             | **60**     | **40**          | **全タスク**         |

**Parallel Efficiency**: 67% のタスクが並行実行可能（40/60）

**Estimated Timeline** (1 developer):
- Phase 1: 0.5 day
- Phase 2: 2 days
- Phase 3 (US1): 3 days → **MVP Ready** (5.5 days total)
- Phase 4 (US2): 2 days
- Phase 5 (US3): 1.5 days
- Phase 6 (Polish): 2 days
- **Total**: ~11 days for full feature

**MVP Timeline**: 5.5 days (Setup + Foundational + US1)

---

## Notes

- **[P]** = 並行実行可能（異なるファイル、依存関係なし）
- **[Story]** = User Story ラベル（US1, US2, US3）でタスクをトレース
- **TDD必須**: 全テストを実装前に作成し、RED（失敗）を確認
- 各 User Story は独立して完成・テスト可能
- Checkpoint で独立動作を検証
- 小さく頻繁にコミット（各タスクまたは論理グループごと）
- 避けるべき: 曖昧なタスク、同一ファイルの競合、Story 間の強い依存関係

---

## Suggested MVP Scope

**MVP = User Story 1 のみ**

含まれる機能:
- ✅ `wt create <branch>` で worktree 作成
- ✅ デフォルトパス `../worktrees/<branch>`
- ✅ 現在のブランチをベースに新規ブランチ作成
- ✅ エラーメッセージ + 解決策表示
- ✅ ブランチ名バリデーション
- ✅ Git リポジトリチェック

含まれない機能（将来追加可能）:
- ❌ エディター自動起動（US2）
- ❌ カスタムパス指定（US3）
- ❌ JSON 出力
- ❌ ベースブランチ指定

**MVP で検証すること**:
1. 開発者が worktree を簡単に作成できるか？
2. git worktree の複雑なコマンドを隠蔽できているか？
3. エラーメッセージは理解しやすいか？

MVP が成功したら、US2, US3 を追加して機能拡張。
