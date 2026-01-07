# Implementation Plan: Complete SBOM Generation in Release Pipeline

**Branch**: `004-complete-sbom-generation` | **Date**: 2026-01-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-complete-sbom-generation/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

現在のリリースパイプラインは、SBOM生成前にプロジェクトの依存関係を復元していないため、推移的依存関係が欠落した不完全なSBOMが生成されています。この機能は、依存関係の完全な復元プロセスを追加し、SPDX 2.3+形式の完全なSBOMを生成し、GitHubのDependency Submission APIに送信してDependabotおよびRenovateとの統合を有効にします。

技術的アプローチ:

1. リリースパイプラインに`dotnet restore`ステップを追加（SBOM生成前）
2. Microsoft SBOM Toolを使用してSPDX 2.3+形式のSBOMを生成
3. GitHub Dependency Submission APIを使用してSBOMを依存関係グラフに送信
4. SBOMファイルをGitHubリリースの成果物として添付
5. 15分のタイムアウトと依存関係キャッシングを実装

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: System.CommandLine 2.0.1, System.IO.Abstractions 22.1.0  
**Storage**: N/A (CLI tool, no persistent storage)  
**Testing**: xUnit (wt.tests project)  
**Target Platform**: Multi-platform (win-x64, linux-x64, linux-arm, osx-arm64)  
**Project Type**: Single project (CLI application)  
**Performance Goals**: SBOM生成完了時間 - 一般的なプロジェクト（50依存関係）で5分以内、大規模（200依存関係）で15分以内  
**Constraints**:

- タイムアウト: 15分以内でSBOM生成完了必須
- SBOM形式: SPDX 2.3+ (ISO/IEC 5962:2021) 準拠
- GitHubインテグレーション: Dependency Submission API必須
- 失敗時の動作: SBOM生成またはAPI送信失敗時はパイプライン全体を失敗させる
**Scale/Scope**:

- 対象: .NETソリューション（単一または複数プロジェクト）
- 依存関係数: 最大200の直接・推移的依存関係をサポート
- リリース頻度: mainブランチへのプッシュまたは手動トリガー

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Developer Usability (開発者のユーザビリティ)

✅ **PASS** - リリースパイプラインの変更はCLIツール自体には影響せず、CI/CD設定のみを変更。ワークフローファイルは明確で理解しやすい構造を維持。

### II. Cross-Platform (クロスプラットフォーム)

✅ **PASS** - Microsoft SBOM ToolはWindows、Linux、macOSで動作。GitHub Actionsはクロスプラットフォーム。パイプラインは既存のマルチプラットフォームビルドに影響を与えない。

### III. Clean & Secure Code (クリーンでセキュアなコード)

✅ **PASS** - SBOM生成により依存関係の脆弱性が可視化される。セキュリティベストプラクティスに従い:

- GitHubトークン権限を最小限（contents: write）に制限
- SBOM生成失敗時はパイプライン全体を失敗させる（不完全な成果物の防止）
- 秘密情報はGitHub Secretsで管理
- Dependency Submission APIによりDependabotアラートを自動有効化

### IV. Documentation Clarity (ドキュメントの明瞭性)

✅ **PASS** - 日本語の仕様書、ADR、実装計画を作成。ワークフロー変更は明確にコメントされ、SBOMの目的と使用方法が文書化される。

### V. Minimal Dependencies (最小限の依存関係)

✅ **PASS** - 新規依存関係:

- Microsoft SBOM Tool: Microsoft公式ツール、SPDX生成に必須
- GitHub Dependency Submission API: GitHubネイティブ機能、追加パッケージ不要
両方とも要件を満たすために不可欠で、代替手段なし。

### VI. Comprehensive Testing (テストの充実と自動化)

✅ **PASS** - パイプライン変更のテスト戦略:

- ワークフロー検証: 実際のリリースプロセスでのE2Eテスト
- SBOM検証: 生成されたSBOMをSPDXスキーマバリデーターで検証
- 統合テスト: GitHub依存関係グラフとDependency Submission APIの統合を確認
- タイムアウトテスト: 大規模ソリューションでの15分制限を検証

**Constitution Check結果**: ✅ **全項目PASS** - 追加の正当化不要

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
# 既存プロジェクト構造（変更なし）
wt.cli/
├── Commands/            # コマンド実装（変更不要）
├── Models/              # データモデル（変更不要）
├── Services/            # サービス層（変更不要）
└── Utils/               # ユーティリティ（変更不要）

wt.tests/                # テスト（変更不要）

# 変更対象
.github/workflows/
├── release.yml          # 📝 SBOM生成ステップを追加
└── sbom-test.yml        # 📝 新規: PR時のSBOM生成テストワークフロー

# Phase 1で追加予定
docs/
├── adr/
│   └── 004-sbom-generation.md  # Architecture Decision Record
└── guides/
    └── sbom-usage.md           # SBOM使用ガイド
```

**Structure Decision**:

- ソースコード変更は不要（wt.cli、wt.testsともに既存のまま）
- 変更はCI/CDパイプライン（.github/workflows/release.yml）のみ
- Microsoft SBOM Toolはワークフロー内で実行（インストール不要）
- GitHub Dependency Submission APIはGitHub Actions標準機能で利用

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**該当なし** - Constitution Checkは全項目PASSのため、正当化が必要な違反は存在しません。

---

## Phase 0: Research (完了)

✅ **research.md作成完了**

調査結果:

- Microsoft SBOM Tool: dotnet toolとして簡単に統合可能
- SPDX 2.3+: ISO標準準拠、JSONフォーマット
- GitHub Dependency Submission API: actions/dependency-submission@v3で簡単に統合
- パフォーマンス最適化: キャッシュ戦略と並列化で目標達成可能
- エラーハンドリング: すべての失敗でパイプライン停止

すべての「NEEDS CLARIFICATION」が解決されました。

---

## Phase 1: Design & Contracts (完了)

✅ **data-model.md作成完了**: SPDX Document、Dependency Snapshot、Workflow Configの構造を定義  
✅ **contracts/作成完了**: GitHub Dependency Submission API v1の詳細な契約を文書化  
✅ **quickstart.md作成完了**: ユーザーと開発者向けのSBOM使用ガイド  
✅ **agent context更新完了**: Copilotエージェントに技術スタック追加  
✅ **テスト戦略追加**: PR時テストワークフロー（sbom-test.yml）を設計

### Constitution Check再評価（Phase 1後）

#### I. Developer Usability (開発者のユーザビリティ)

✅ **PASS (変更なし)** - quickstart.mdで明確なガイドを提供。開発者はローカルでもSBOM生成可能。

#### II. Cross-Platform (クロスプラットフォーム)

✅ **PASS (変更なし)** - すべてのツールがマルチプラットフォームで動作確認済み。

#### III. Clean & Secure Code (クリーンでセキュアなコード)

✅ **PASS (強化)** - 契約定義により、APIセキュリティ要件が明確化。トークン権限、エラーハンドリング、レート制限対策を文書化。

#### IV. Documentation Clarity (ドキュメントの明瞭性)

✅ **PASS (強化)** - 包括的なドキュメント（research.md、data-model.md、contracts/、quickstart.md）により、実装の透明性が向上。

#### V. Minimal Dependencies (最小限の依存関係)

✅ **PASS (変更なし)** - 追加依存関係なし。Microsoft SBOM ToolはCI/CD内でのみ使用。

#### VI. Comprehensive Testing (テストの充実と自動化)

✅ **PASS (強化)** - **PR時テストワークフローにより早期不具合発見を実現**。SPDX検証、必須パッケージ確認、パフォーマンスベンチマークを自動実行。リリース前に品質を保証。

**再評価結果**: ✅ **全項目PASS（Phase 1により強化）** - 設計により憲法原則がさらに強化されました。特にテスト自動化が大幅に改善。

---

## 次のステップ

Phase 2（タスク分解）は `/speckit.tasks` コマンドで実行してください。

### Phase 2で作成されるもの

- **tasks.md**: 実装タスクの詳細な分解
- 各タスクの優先順位、見積もり、依存関係
- テストタスクとドキュメントタスクの定義

### 実装準備完了

以下のドキュメントがすべて完成しました：

✅ spec.md - 機能仕様（**FR-019/FR-020、SC-011/SC-012追加: PR時テスト要件**）  
✅ research.md - 技術調査（**PR時テスト戦略、sbom-test.ymlワークフロー設計追加**）  
✅ data-model.md - データモデル  
✅ contracts/ - API契約  
✅ quickstart.md - 使用ガイド（**PR時テストとトラブルシューティング追加**）  
✅ plan.md（本ファイル） - 実装計画

すべての不明点が解決され、実装に必要な情報がすべて揃っています。

### 📝 重要な追加機能（ユーザー要望対応）

**問題**: リリースパイプライン実行まで不具合が発見できない  
**解決**: PR時自動テストワークフローを追加

新規ワークフローファイル:

- `.github/workflows/sbom-test.yml` - PR作成時に自動実行されるSBOM生成テスト

テスト内容:

1. ✅ 依存関係リストア成功確認
2. ✅ SBOM生成成功確認
3. ✅ SPDX 2.3フォーマット検証
4. ✅ 必須パッケージ存在確認（System.CommandLine等）
5. ✅ パフォーマンスベンチマーク（15分制限）
6. ❌ **GitHub API送信なし**（Dry-runモード、本番環境を汚染しない）

**重要な制約**:

- PR時は**GitHub Dependency Graphにアップロードしません**
- PR時は**Dependency Submission APIを呼び出しません**
- 実際のAPI送信とDependency Graph更新は**リリース時のみ**実行

これにより、**リリース前に不具合を検出**し、開発サイクルを高速化しつつ、本番環境の整合性を保ちます。
