# Implementation Plan: Worktreeとブランチ情報の一覧表示

**Branch**: `002-list-worktree-branches` | **Date**: 2026-01-04 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-list-worktree-branches/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Git worktreeとブランチの一覧を表示するCLIコマンドを開発します。開発者は`wt list`コマンドを実行することで、すべてのworktreeとその各worktreeでチェックアウトされているブランチをテーブル形式で確認できます。出力には、worktreeのパス、ブランチ名、ステータス（通常/detached HEAD）が含まれます。これにより、複数のworktreeを使用した並列作業時に、どのworktreeでどのブランチを作業しているかを一目で把握できます。

**Technical Approach**: 既存のC# .NET 10 CLIツール（wt.cli）に新しいlistコマンドを追加。`git worktree list`コマンドの出力を解析し、各worktreeのブランチ情報を取得してテーブル形式で整形します。作成日時順ソートのために、Gitの内部情報またはファイルシステムのメタデータを活用します。

## Technical Context

**Language/Version**: C# .NET 10 (既存プロジェクトwt.cliに統合)  
**Primary Dependencies**:

- System.CommandLine 2.0.1 (CLIフレームワーク)
- System.IO.Abstractions 22.1.0 (パス操作の抽象化)
- System.Text.Json または Newtonsoft.Json (将来の代替フォーマット対応用)

**Storage**: N/A (ファイルシステムとGitの内部情報のみ使用)  
**Testing**: xUnit, Shouldly, Moq (既存プロジェクトで使用中)  
**Target Platform**: Windows, macOS, Linux (クロスプラットフォーム)  
**Project Type**: single (CLIツール - 既存プロジェクトに機能追加)  
**Performance Goals**:

- `git worktree list`実行から表示完了まで 3秒以内
- 10個のworktreeで1秒以内にテーブル表示
- メモリ使用量 < 50MB（既存の軽量性を維持）

**Constraints**:

- Git 2.5以上が必要（git worktree サポート）
- クロスプラットフォーム対応必須（憲章 II）
- 外部依存関係は最小限（憲章 V）
- TDDアプローチ必須（憲章 VI）
- P3（代替出力フォーマット）は初期リリースで未実装

**Scale/Scope**:

- 単一ユーザー・単一リポジトリでの使用
- 想定worktree数: 通常1-10個、最大100個
- 同時実行想定なし（ローカルCLI）

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle                   | Requirement              | Status  | Notes                                                                    |
| --------------------------- | ------------------------ | ------- | ------------------------------------------------------------------------ |
| **I. Developer Usability**  | CLI優先、明瞭な操作      | ✅ PASS | シンプルなコマンド構文、テーブル形式の読みやすい出力（FR-003）           |
| **I. Developer Usability**  | エラーメッセージに解決策 | ✅ PASS | 存在しないworktreeに対する明確な警告メッセージ（FR-008）                 |
| **II. Cross-Platform**      | Windows/macOS/Linux対応  | ✅ PASS | .NET 10のクロスプラットフォーム対応、System.IO.Abstractionsでパス抽象化  |
| **II. Cross-Platform**      | OS固有機能に非依存       | ✅ PASS | Gitコマンド実行のみ、プラットフォーム固有処理なし                        |
| **III. Clean & Secure**     | セキュアなコード         | ✅ PASS | 機密情報なし、Git出力の安全な解析                                        |
| **III. Clean & Secure**     | 静的解析                 | ✅ PASS | .NETの標準アナライザー使用、既存プロジェクトの品質基準を継承             |
| **IV. Documentation**       | 日本語ドキュメント優先   | ✅ PASS | 仕様書とガイドは日本語で作成                                             |
| **IV. Documentation**       | ADRで技術決定記録        | ✅ PASS | テーブル表示ライブラリ選定等の技術決定をADRとして記録予定                |
| **V. Minimal Dependencies** | 最小限の依存             | ✅ PASS | 新規依存関係なし、既存の依存関係（System.CommandLine等）のみ使用         |
| **V. Minimal Dependencies** | 標準ライブラリ優先       | ✅ PASS | テーブル整形は標準ライブラリで実装可能、必要に応じて軽量ライブラリを検討 |
| **VI. Testing**             | TDD必須                  | ✅ PASS | xUnit + Shouldly + Moq でテスト先行開発                                  |
| **VI. Testing**             | CI/CDでテスト自動化      | ✅ PASS | 既存のCI/CDパイプラインで自動テスト実行                                  |

**Overall Status**: ✅ **PASS** - All gates passed.

**Justifications**: なし - 違反なし。既存の憲章準拠プロジェクトに機能追加するため、すべての原則を自然に満たします。

## Project Structure

### Documentation (this feature)

```text
specs/002-list-worktree-branches/
├── spec.md              # 機能仕様書（完成）
├── plan.md              # This file (実装計画書)
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
├── Program.cs                   # エントリーポイント（既存）
├── Commands/                    
│   ├── Worktree/               # worktreeコマンドグループ（既存）
│   │   └── CreateCommand.cs    # 既存: 'create' コマンド
│   └── ListCommand.cs          # 新規: 'list' コマンド
├── Services/                    
│   ├── Git/
│   │   ├── IGitService.cs      # 既存: Gitサービスインターフェース
│   │   └── GitService.cs       # 拡張: worktree情報取得メソッド追加
│   └── Worktree/
│       ├── IWorktreeService.cs # 既存: Worktreeサービスインターフェース
│       └── WorktreeService.cs  # 拡張: list機能追加
├── Models/                      
│   ├── WorktreeInfo.cs         # 新規: Worktree情報モデル
│   └── BranchInfo.cs           # 新規: ブランチ情報モデル
└── Formatters/                  
    ├── IOutputFormatter.cs      # 新規: フォーマッターインターフェース（将来の拡張用）
    └── TableFormatter.cs        # 新規: テーブル整形実装

wt.tests/                        # 既存テストプロジェクト
├── Commands/
│   └── ListCommandTests.cs      # 新規: ListCommandのテスト
├── Services/
│   ├── Git/
│   │   └── GitServiceTests.cs   # 拡張: worktree情報取得のテスト追加
│   └── Worktree/
│       └── WorktreeServiceTests.cs # 拡張: list機能のテスト追加
└── Formatters/
    └── TableFormatterTests.cs   # 新規: テーブル整形のテスト
```

**Structure Decision**: 既存の単一プロジェクト構造（wt.cli）に機能を追加します。worktreeコマンドグループに新しいListCommandを追加し、既存のGitServiceとWorktreeServiceを拡張してworktree一覧取得機能を実装します。テーブル整形用の新しいユーティリティクラスを追加します。

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

N/A - すべての憲章チェックが合格しており、違反はありません。

| Violation           | Why Needed     | Simpler Alternative Rejected Because |
| ------------------- | -------------- | ------------------------------------ |
| [e.g., 4th project] | [current need] | [why 3 projects insufficient]        |

---

## Phase Summary

### Phase 0: Research ✅ Complete

すべての技術的不明点を解決しました：

- `git worktree list --porcelain`の出力形式とパース方法
- Worktree作成日時の取得方法（`.git/worktrees/<name>/gitdir`のタイムスタンプ）
- テーブル整形アプローチ（カスタムTableFormatter実装、依存関係ゼロ）
- Detached HEAD表示方法（短縮コミットハッシュ + "(detached)"）
- エラーハンドリング（存在しないworktreeの警告表示）

詳細: [research.md](research.md)

### Phase 1: Design & Contracts ✅ Complete

以下のドキュメントを作成しました：

1. **data-model.md**: WorktreeInfo、BranchInfoモデルの詳細設計
   - 不変（immutable）な設計
   - バリデーションルール
   - 表示用メソッド（GetDisplayBranch、GetDisplayStatus）

2. **contracts/cli-interface.md**: CLIコマンド仕様
   - コマンド構文: `wt worktree list`
   - テーブル形式の出力仕様
   - Exit code定義
   - エラーメッセージ仕様

3. **quickstart.md**: 開発者向けクイックスタートガイド
   - TDDサイクルの実践手順
   - Phase 1〜6の実装ステップ
   - テストファーストアプローチ

4. **Agent Context更新**: GitHub Copilot用のコンテキストファイル更新完了

### Constitution Re-check ✅ Passed

Phase 1設計完了後の再チェック結果:

| Principle                   | Status  | Notes                                            |
| --------------------------- | ------- | ------------------------------------------------ |
| **I. Developer Usability**  | ✅ PASS | テーブル形式の明確な出力、エラーメッセージも明確 |
| **II. Cross-Platform**      | ✅ PASS | .NET 10のクロスプラットフォーム対応を活用        |
| **III. Clean & Secure**     | ✅ PASS | 入力バリデーション、安全なGit出力パース          |
| **IV. Documentation**       | ✅ PASS | すべてのドキュメントを日本語で作成完了           |
| **V. Minimal Dependencies** | ✅ PASS | 新規依存関係なし、カスタムTableFormatter実装     |
| **VI. Testing**             | ✅ PASS | TDDアプローチ、quickstart.mdに詳細な手順         |

**Overall**: ✅ **PASS** - All principles maintained after design phase.

---

## Next Steps

**Phase 2: Task Breakdown** - `/speckit.tasks`コマンドを実行

実装フェーズに進むために、以下のタスク分解が必要です：

1. WorktreeInfoモデルの実装とテスト
2. BranchInfoモデルの実装（将来用）
3. GitService.ListWorktreesAsync()の実装とテスト
4. TableFormatterの実装とテスト
5. ListCommandの実装とテスト
6. Program.csへの統合
7. 統合テストの実装
8. ドキュメント更新

詳細なタスク分解は`tasks.md`に記載されます（`/speckit.tasks`コマンドで生成）。

---

## Artifacts Generated

| Artifact             | Path                       | Status                      |
| -------------------- | -------------------------- | --------------------------- |
| 実装計画書           | plan.md                    | ✅ Complete                 |
| リサーチドキュメント | research.md                | ✅ Complete                 |
| データモデル         | data-model.md              | ✅ Complete                 |
| CLI契約              | contracts/cli-interface.md | ✅ Complete                 |
| クイックスタート     | quickstart.md              | ✅ Complete                 |
| タスクリスト         | tasks.md                   | ⏳ Pending `/speckit.tasks` |

---

**Plan Status**: ✅ **COMPLETE** - Ready for task breakdown and implementation.

**Branch**: `002-list-worktree-branches`  
**Date**: 2026-01-04  
**Next Command**: `/speckit.tasks`
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
