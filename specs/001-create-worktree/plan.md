# Implementation Plan: Git Worktree 作成コマンド

**Branch**: `001-create-worktree` | **Date**: 2026-01-03 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-create-worktree/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Git worktree の作成を1コマンドで実行できるCLIツールを開発します。開発者はブランチ名を指定するだけで、新しいブランチが作成され、自動的に git worktree として登録されます（`wt create <branch-name>`）。オプションでエディターの自動起動や worktree パスのカスタマイズが可能です。これにより、複雑な git worktree コマンドを覚える必要がなくなり、AIコードエージェントとの並列開発や手作業との併用が容易になります。

**Technical Approach**: C# .NET 10を使用したクロスプラットフォームCLIツールとして実装。System.Diagnostics.Processを使用してgitコマンドを実行し、標準ライブラリのみで実装します（最小限の依存関係）。

## Technical Context

**Language/Version**: C# .NET 10 (既存プロジェクトに統合)  
**Primary Dependencies**: System.CommandLine (CLI フレームワーク), System.IO.Abstractions (パス操作の抽象化), Newtonsoft.Json または System.Text.Json (JSON出力)  
**Storage**: N/A (ファイルシステムのみ使用)  
**Testing**: xUnit (既存プロジェクトで使用中), FluentAssertions (アサーション), Moq (モック)  
**Target Platform**: Windows, macOS, Linux (クロスプラットフォーム)  
**Project Type**: single (CLIツール)  
**Performance Goals**:

- コマンド実行開始から完了まで 5秒以内（通常のGit操作含む）
- worktree作成 + エディター起動まで 30秒以内（ユーザー体験目標）
- メモリ使用量 < 100MB（CLIツールとして軽量）

**Constraints**:

- Git 2.5以上が必要（git worktree サポート）
- クロスプラットフォーム対応必須（憲章 II）
- 外部依存関係は最小限（憲章 V）
- TDDアプローチ必須（憲章 VI）

**Scale/Scope**:

- 単一ユーザー・単一リポジトリでの使用
- 同時実行想定なし（ローカルCLI）
- worktree数の制限なし（Gitの制限に依存）

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle                   | Requirement              | Status     | Notes                                                                               |
| --------------------------- | ------------------------ | ---------- | ----------------------------------------------------------------------------------- |
| **I. Developer Usability**  | CLI優先、明瞭な操作      | ✅ PASS    | コマンド構文はシンプル(`wt create <branch>`形式)、ヘルプ完備、JSON/人間可読出力対応 |
| **I. Developer Usability**  | エラーメッセージに解決策 | ✅ PASS    | FR-010で明記、verboseモードで詳細診断                                               |
| **II. Cross-Platform**      | Windows/macOS/Linux対応  | ✅ PASS    | .NET 10はクロスプラットフォーム、System.IO.Abstractionsでパス抽象化                 |
| **II. Cross-Platform**      | OS固有機能に非依存       | ✅ PASS    | Gitコマンド実行のみ、プラットフォーム固有処理を分離                                 |
| **III. Clean & Secure**     | セキュアなコード         | ✅ PASS    | 機密情報ハードコードなし、入力バリデーション実装（FR-008）                          |
| **III. Clean & Secure**     | 静的解析                 | ✅ PASS    | .NETの標準アナライザー使用                                                          |
| **IV. Documentation**       | 日本語ドキュメント優先   | ✅ PASS    | README、ガイドは日本語で作成                                                        |
| **IV. Documentation**       | ADRで技術決定記録        | ✅ PASS    | 技術選択（C#、System.CommandLine等）をADRとして記録予定                             |
| **V. Minimal Dependencies** | 最小限の依存             | ✅ PASS    | System.CommandLine、System.IO.Abstractions、JSONライブラリのみ（3つ）               |
| **V. Minimal Dependencies** | 標準ライブラリ優先       | ✅ PASS    | System.Diagnostics.Process等の標準ライブラリを最大限活用                            |
| **VI. Testing**             | TDD必須                  | ✅ PASS    | xUnit + FluentAssertions + Moq でテスト先行開発                                     |
| **VI. Testing**             | CI/CDでテスト自動化      | ⚠️ DEFERRED| CI/CDパイプラインは別タスクで設定（プロジェクト全体の設定）                         |

**Overall Status**: ✅ **PASS** - All gates passed. 1 item deferred to project-level task.

**Justifications**: None required - no violations.

## Project Structure

### Documentation (this feature)

```text
specs/001-create-worktree/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── cli-interface.md # CLIコマンドインターフェース仕様
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
wt.cli/                          # 既存CLIプロジェクト
├── Program.cs                   # エントリーポイント
├── Commands/                    # 新規: コマンド実装
│   ├── Worktree/               # worktreeコマンドグループ
│   │   ├── CreateCommand.cs    # 'worktree create' コマンド
│   │   ├── WorktreeOptions.cs  # コマンドオプション定義
│   │   └── WorktreeHandler.cs  # コマンドハンドラー
│   └── BaseCommand.cs          # 共通コマンド基底クラス
├── Services/                    # 新規: ビジネスロジック
│   ├── Git/
│   │   ├── IGitService.cs      # Gitサービスインターフェース
│   │   ├── GitService.cs       # Git操作実装
│   │   └── GitCommandExecutor.cs # Gitコマンド実行
│   ├── Worktree/
│   │   ├── IWorktreeService.cs # Worktreeサービスインターフェース
│   │   ├── WorktreeService.cs  # Worktree作成ロジック
│   │   └── WorktreeValidator.cs # バリデーション
│   ├── Editor/
│   │   ├── IEditorService.cs   # エディターサービスインターフェース
│   │   ├── EditorService.cs    # エディター起動ロジック
│   │   └── EditorPresets.cs    # エディタープリセット定義
│   └── Output/
│       ├── IOutputFormatter.cs # 出力フォーマッターIF
│       ├── JsonFormatter.cs    # JSON出力
│       └── HumanFormatter.cs   # 人間可読出力
├── Models/                      # 新規: データモデル
│   ├── WorktreeInfo.cs         # Worktree情報
│   ├── BranchInfo.cs           # ブランチ情報
│   ├── EditorConfig.cs         # エディター設定
│   └── CommandResult.cs        # コマンド実行結果
└── Utils/                       # 新規: ユーティリティ
    ├── PathHelper.cs           # パス操作ヘルパー
    └── ValidationHelper.cs     # バリデーションヘルパー

wt.tests/                        # 既存テストプロジェクト
├── Commands/                    # 新規: コマンドテスト
│   └── Worktree/
│       └── CreateCommandTests.cs
├── Services/                    # 新規: サービステスト
│   ├── Git/
│   │   └── GitServiceTests.cs
│   ├── Worktree/
│   │   ├── WorktreeServiceTests.cs
│   │   └── WorktreeValidatorTests.cs
│   ├── Editor/
│   │   └── EditorServiceTests.cs
│   └── Output/
│       └── OutputFormatterTests.cs
├── Integration/                 # 新規: 統合テスト
│   └── WorktreeE2ETests.cs     # E2Eテスト
└── TestHelpers/                 # 新規: テストヘルパー
    ├── MockGitRepository.cs    # Gitリポジトリモック
    └── TestFixtures.cs         # テストフィクスチャ
```

**Structure Decision**: 既存の単一プロジェクト構造を拡張します。Commands、Services、Models、Utilsディレクトリを追加し、機能ごとに明確に分離します。テストプロジェクトも同様の構造で対応するテストを配置します。この構造により、TDDアプローチを実践しやすく、将来的な拡張も容易になります。

## Phase 0: Research (Complete)

**Status**: ✅ Complete  
**Output**: [research.md](./research.md)

### Research Areas Covered

1. **CLI Framework Selection**: System.CommandLine選定理由とメリット
2. **Git Command Execution**: Process wrapperパターン
3. **Cross-Platform Path Handling**: System.IO.Abstractionsの活用
4. **Editor Detection Strategy**: プリセット + PATH検索
5. **Output Formatting**: JSON/Human-readable出力
6. **Error Handling Pattern**: `Result<T>`パターン
7. **Testing Strategy**: Unit/Integration/E2E 3層アプローチ
8. **Cross-Platform Considerations**: プラットフォーム固有処理の分離
9. **Performance Optimization**: 同期処理、最小限のディスク操作
10. **Security Considerations**: 入力サニタイゼーション、コマンドインジェクション対策

**Key Decisions**:

- System.CommandLineを採用（Microsoft公式、強力なヘルプ生成）
- Git実行はProcessRunnerラッパークラス（テスト容易性）
- `Result<T>`パターンでエラーハンドリング（null安全、明示的）
- 3層テスト戦略（Unit 80%, Integration 15%, E2E 5%）

## Phase 1: Design & Contracts (Complete)

**Status**: ✅ Complete  
**Outputs**:

- [data-model.md](./data-model.md)
- [contracts/cli-interface.md](./contracts/cli-interface.md)
- [quickstart.md](./quickstart.md)

### Core Data Model

5つのコアエンティティを定義:

1. **WorktreeInfo**: Worktree情報（パス、ブランチ、作成日時）
2. **BranchInfo**: ブランチ情報（名前、ベース、SHA）
3. **EditorConfig**: エディター設定（タイプ、コマンド、引数）
4. **`CommandResult<T>`**: 実行結果（成功/失敗、データ、エラー詳細）
5. **CreateWorktreeOptions**: コマンドオプション（全パラメータ）

### Validation Rules

- **ブランチ名**: 正規表現 `^[a-zA-Z0-9][a-zA-Z0-9/_-]*$`
- **禁止パターン**: `..`, `@{`, 空白、制御文字
- **パス検証**: 親ディレクトリ存在、書き込み権限、ディスク容量

### Error Code Taxonomy

11のエラーコード定義（5カテゴリ）:

- **GIT系** (GIT001-003): Git実行エラー
- **BR系** (BR001-002): ブランチエラー
- **WT系** (WT001-002): Worktreeエラー
- **FS系** (FS001-003): ファイルシステムエラー
- **ED系** (ED001): エディターエラー

### CLI Interface

コマンド構文:

```bash
wt create <branch-name> [OPTIONS]
```

**Design Philosophy**: `wt`自体が「worktree tool」を意味するため、`worktree`サブコマンドは省略。シンプルで覚えやすいCLIを実現します。

主要オプション:

- `--base-branch, -b`: ベースブランチ指定
- `--path, -p`: カスタムパス指定
- `--editor, -e`: エディター起動
- `--checkout-existing`: 既存ブランチをチェックアウト
- `--output, -o`: 出力形式 (human|json)
- `--verbose, -v`: 詳細診断出力

終了コード: 0-1 (一般), 10-12 (Git), 20-21 (ブランチ), 30-32 (Worktree), 40 (ディスク), 50 (エディター)

### Quickstart Guide

開発者向けガイド作成済み:

- 5フェーズの実装ワークフロー
- Test-First実装例
- 統合テスト・E2Eテスト戦略
- トラブルシューティング

## Constitution Re-Check (Post-Design)

*Final validation after Phase 1 design completion.*

| Principle                   | Requirement              | Status     | Notes                                                             |
| --------------------------- | ------------------------ | ---------- | ----------------------------------------------------------------- |
| **I. Developer Usability**  | CLI優先、明瞭な操作      | ✅ PASS    | CLI仕様確定、全7オプション定義、usage例8件作成                    |
| **I. Developer Usability**  | エラーメッセージに解決策 | ✅ PASS    | 11エラーコード定義、全てにソリューション記載                      |
| **II. Cross-Platform**      | Windows/macOS/Linux対応  | ✅ PASS    | プラットフォーム別考慮事項を明記、パス処理抽象化                  |
| **II. Cross-Platform**      | OS固有機能に非依存       | ✅ PASS    | 標準.NETライブラリのみ使用                                        |
| **III. Clean & Secure**     | セキュアなコード         | ✅ PASS    | バリデーション規則定義、入力サニタイゼーション戦略策定            |
| **III. Clean & Secure**     | 静的解析                 | ✅ PASS    | .NET標準アナライザー適用予定                                      |
| **IV. Documentation**       | 日本語ドキュメント優先   | ✅ PASS    | 全ドキュメント日本語（4文書作成済み）                             |
| **IV. Documentation**       | ADRで技術決定記録        | ✅ PASS    | research.mdに10分野の技術決定を記録                               |
| **V. Minimal Dependencies** | 最小限の依存             | ✅ PASS    | 3依存のみ（System.CommandLine, System.IO.Abstractions, JSON lib） |
| **V. Minimal Dependencies** | 標準ライブラリ優先       | ✅ PASS    | System.Diagnostics.Process等の標準ライブラリ中心                  |
| **VI. Testing**             | TDD必須                  | ✅ PASS    | Quickstartに5フェーズTDDワークフロー記載                          |
| **VI. Testing**             | CI/CDでテスト自動化      | ⚠️ DEFERRED| プロジェクト全体タスクとして別途設定                              |

**Final Status**: ✅ **PASS** - All gates remain passed after design phase.

**Design Impact**: 設計によりすべての憲章要件を満たすことが確認されました。依存関係は計画通り最小限（3つ）、ドキュメントは日本語で充実、TDD戦略も具体的に定義されています。

## Agent Context Update

**Status**: ✅ Complete

- **File**: `.github/agents/copilot-instructions.md`
- **Changes**:
  - Added C# .NET 10 as active language
  - Added System.CommandLine, System.IO.Abstractions, JSON library as frameworks
  - Recorded feature 001-create-worktree

## Next Steps

### Immediate Actions

1. **Run `/speckit.tasks`**: 実装タスクを生成

   ```bash
   /speckit.tasks
   ```

2. **Review Generated Artifacts**: 以下のファイルをレビュー
   - [x] `plan.md` - この実装計画書
   - [x] `research.md` - 技術リサーチ（10分野）
   - [x] `data-model.md` - データモデル定義（5エンティティ）
   - [x] `contracts/cli-interface.md` - CLI仕様（完全定義）
   - [x] `quickstart.md` - 開発者ガイド（5フェーズ）

### Implementation Preparation

1. **Setup Development Environment**:
   - .NET 10 SDK インストール確認
   - Git 2.5+ インストール確認
   - IDE/エディター設定

2. **Create Feature Branch**:

   ```bash
   git checkout -b 001-create-worktree
   ```

3. **Install Dependencies**:

   ```bash
   dotnet add wt.cli package System.CommandLine
   dotnet add wt.cli package System.IO.Abstractions
   dotnet add wt.cli package System.Text.Json
   dotnet add wt.tests package xUnit
   dotnet add wt.tests package FluentAssertions
   dotnet add wt.tests package Moq
   ```

4. **Follow Quickstart Guide**: [quickstart.md](./quickstart.md)を参照し、5フェーズのワークフローに従って実装

### Quality Gates

実装時に以下を確認:

- ✅ すべてのテストが通過（Green）
- ✅ コードカバレッジ80%以上
- ✅ 憲章の全要件を満たす
- ✅ ドキュメントが最新

## Artifacts Summary

| Document                               | Status       | Purpose                  | Lines |
| -------------------------------------- | ------------ | ------------------------ | ----- |
| plan.md                                | ✅ Complete  | 実装計画書               | ~240  |
| research.md                            | ✅ Complete  | 技術リサーチ             | ~350  |
| data-model.md                          | ✅ Complete  | データモデル定義         | ~280  |
| contracts/cli-interface.md             | ✅ Complete  | CLI仕様                  | ~420  |
| quickstart.md                          | ✅ Complete  | 開発者ガイド             | ~480  |
| .github/agents/copilot-instructions.md | ✅ Updated   | エージェントコンテキスト | ~35   |

**Total Documentation**: ~1,800 lines across 6 files

## Complexity Tracking

> **No violations detected** - All constitution requirements are met without compromise.
