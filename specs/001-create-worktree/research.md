# Research: Git Worktree 作成コマンド

**Feature**: 001-create-worktree  
**Date**: 2026-01-03  
**Purpose**: 技術選択とベストプラクティスのリサーチ結果

## Research Tasks

本フェーズでは、Technical Contextで特定された技術スタックとアプローチの妥当性を検証し、ベストプラクティスを調査しました。

## 1. CLIフレームワークの選択

### Decision: System.CommandLine

**Rationale**:

- Microsoft公式のCLIフレームワーク
- 強力なコマンドパース機能とヘルプ生成
- 型安全なオプション定義
- .NETエコシステムとの統合

**Alternatives Considered**:

- **CommandLineParser**: 成熟しているが、System.CommandLineより機能が少ない
- **McMaster.Extensions.CommandLineUtils**: 良い選択肢だが、Microsoftの公式サポートが System.CommandLine に移行

**Best Practices**:

- コマンドとハンドラーを分離（CQRS的アプローチ）
- オプションクラスで型安全性を確保
- ヘルプテキストを詳細に記述

## 2. Gitコマンド実行パターン

### Decision: System.Diagnostics.Process + ラッパークラス

**Rationale**:

- 標準ライブラリで実現可能（依存関係最小化）
- クロスプラットフォーム対応
- テストしやすい抽象化

**Alternatives Considered**:

- **LibGit2Sharp**: 強力だが、依存関係が大きい。憲章Vに反する
- **Git CLI直接呼び出し**: 抽象化なしではテストが困難

**Best Practices**:

- `IGitService` インターフェースで抽象化
- 標準出力/エラー出力を適切にキャプチャ
- タイムアウト設定を実装
- エラーハンドリングを徹底

**Implementation Pattern**:

```csharp
public interface IGitService
{
    Task<GitResult> ExecuteAsync(string command, string[]args, CancellationToken ct);
    Task<bool> IsGitRepositoryAsync(string path);
    Task<string> GetCurrentBranchAsync();
    Task<bool> BranchExistsAsync(string branchName);
}
```

## 3. パス操作の抽象化

### Decision: System.IO.Abstractions

**Rationale**:

- クロスプラットフォームのパス操作を抽象化
- テスト時にファイルシステムのモックが容易
- .NET標準のPath/Directory/Fileクラスと互換性

**Best Practices**:

- `IFileSystem` を依存性注入
- パス区切り文字はOSに応じて自動処理
- 絶対パスと相対パスの明確な区別

**Path Handling Strategy**:

```text
入力: branch-name
デフォルトパス生成: Path.Combine(repoParent, "worktrees", branch-name)
カスタムパス: ユーザー指定の絶対パス or カレントディレクトリからの相対パス
```

## 4. エディター検出とプリセット

### Decision: エディタープリセット + PATH検索

**Rationale**:

- 一般的なエディターの標準コマンドをプリセットとして定義
- 環境変数PATHから実行可能ファイルを検索
- ユーザーカスタムコマンドもサポート

**Editor Presets**:

```csharp
public static class EditorPresets
{
    public static Dictionary<string, EditorConfig> KnownEditors = new()
    {
        ["vscode"] = new("code", "{path}"),
        ["vim"] = new("vim", "{path}"),
        ["emacs"] = new("emacs", "{path}"),
        ["nano"] = new("nano", "{path}"),
        ["idea"] = new("idea", "{path}"),
    };
}
```

**Best Practices**:

- エディターが見つからない場合は警告のみ（worktree作成は継続）
- verboseモードで検索パスを表示
- Windows: .exe拡張子を自動追加

## 5. 出力フォーマット戦略

### Decision: Strategyパターン + System.Text.Json

**Rationale**:

- JSON出力には標準の System.Text.Json を使用（追加依存なし）
- 人間可読出力は構造化されたテキスト形式
- フォーマッターを切り替え可能

**Output Formats**:

```json
// JSON format
{
  "success": true,
  "worktree": {
    "path": "/path/to/worktrees/feature-x",
    "branch": "feature-x",
    "baseBranch": "main"
  },
  "editorLaunched": true
}
```

```shell
// Human-readable format
✓ Created branch 'feature-x' from 'main'
✓ Added worktree at: /path/to/worktrees/feature-x
✓ Launched VS Code
→ Next: cd /path/to/worktrees/feature-x
```

## 6. エラーハンドリング戦略

### Decision: Result パターン + カスタム例外

**Rationale**:

- Resultパターンで成功/失敗を明示的に表現
- カスタム例外で詳細なエラー情報を提供
- 各エラーに解決策を含める

**Error Categories**:

1. **Git関連**: GitNotFound, NotGitRepository, BranchExists
2. **ファイルシステム**: PathNotWritable, DiskSpaceLow
3. **Worktree関連**: WorktreeExists, InvalidBranchName
4. **エディター関連**: EditorNotFound (警告レベル)

**Best Practices**:

- すべてのエラーメッセージに解決策を含める
- verboseモードでスタックトレースと診断情報を出力
- エラーコードで種類を識別可能にする

## 7. テスト戦略

### Decision: xUnit + FluentAssertions + Moq + TestContainers (Git)

**Rationale**:

- xUnit: 既存プロジェクトで使用中
- FluentAssertions: 読みやすいアサーション
- Moq: インターフェースのモック
- テストでは実際のGitリポジトリを一時的に作成してE2Eテスト

**Test Layers**:

1. **Unit Tests**: サービス、バリデーター、フォーマッター
2. **Integration Tests**: Git操作の実際の実行
3. **E2E Tests**: コマンド全体の動作確認

**Best Practices**:

- Given-When-Then パターン
- 各テストは独立して実行可能
- テスト用の一時ディレクトリを使用
- クリーンアップを確実に実行

## 8. クロスプラットフォーム対応

### Decision: .NET 10 + 条件付きコンパイル

**Rationale**:

- .NET 10はWindows、macOS、Linuxをネイティブサポート
- 必要に応じて `#if` ディレクティブで分岐

**Platform-Specific Considerations**:

- **Windows**: パス区切り `\`、実行ファイル `.exe`
- **macOS/Linux**: パス区切り `/`、実行ファイル拡張子なし
- **共通**: System.IO.Abstractionsで抽象化

**Testing Strategy**:

- CI/CDで3プラットフォームでテスト実行
- プラットフォーム固有の動作は個別にテスト

## 9. パフォーマンス最適化

### Decision: 非同期パターン + キャンセレーション

**Rationale**:

- Git操作は時間がかかる可能性がある
- async/awaitで非ブロッキング実行
- CancellationTokenでユーザーによる中断をサポート

**Performance Goals** (from Technical Context):

- コマンド実行: < 5秒
- worktree作成 + エディター起動: < 30秒
- メモリ使用: < 100MB

**Optimization Strategies**:

- Git操作の並列化（可能な場合）
- 不要なGit情報の取得を避ける
- 出力バッファリングの最適化

## 10. セキュリティ考慮事項

### Decision: 入力バリデーション + コマンドインジェクション防止

**Rationale**:

- ブランチ名の厳格なバリデーション
- Gitコマンドのパラメータ化
- パストラバーサル攻撃の防止

**Security Measures**:

1. ブランチ名のホワイトリスト検証（英数字、`-`、`_`のみ）
2. パスのサニタイズ
3. ユーザー入力をGitコマンドに直接渡さない
4. エディターコマンドのバリデーション

## Summary

すべてのリサーチタスクが完了し、技術選択の妥当性が確認されました。Technical Contextで「NEEDS CLARIFICATION」とマークされた項目はすべて解決されています。Phase 1（Design & Contracts）に進む準備が整いました。

**Key Technologies Confirmed**:

- CLI Framework: System.CommandLine
- Git Interaction: System.Diagnostics.Process + カスタムラッパー
- Path Operations: System.IO.Abstractions
- JSON: System.Text.Json
- Testing: xUnit + FluentAssertions + Moq

**No Blockers**: すべての技術選択は憲章の要件を満たしています。
