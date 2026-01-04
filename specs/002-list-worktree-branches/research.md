# Research: Worktreeとブランチ情報の一覧表示

**Phase**: 0 - Outline & Research  
**Date**: 2026-01-04  
**Feature**: [spec.md](spec.md) | [plan.md](plan.md)

## Purpose

このドキュメントは、worktreeとブランチ一覧表示機能を実装するための技術調査結果をまとめています。`git worktree list`コマンドの出力形式、ブランチ情報の取得方法、テーブル整形アプローチ、ソート方法について調査しました。

## Research Tasks

### 1. Git Worktree List コマンドの出力形式

**調査内容**: `git worktree list`コマンドの標準出力とポーセラン出力の形式

**結果**:

`git worktree list`の標準出力形式:

```text
/path/to/main-worktree  abc1234 [main]
/path/to/feature-worktree  def5678 [feature-branch]
/path/to/detached-worktree  ghi9012 (detached HEAD)
```

`git worktree list --porcelain`の出力形式（機械可読）:

```text
worktree /path/to/main-worktree
HEAD abc1234567890abcdef
branch refs/heads/main

worktree /path/to/feature-worktree
HEAD def5678901234567890
branch refs/heads/feature-branch

worktree /path/to/detached-worktree
HEAD ghi9012345678901234
detached
```

**決定事項**:

- `git worktree list --porcelain`を使用して機械可読形式で情報を取得
- ポーセラン形式は安定したAPIであり、将来のGitバージョンでも互換性が保証される
- 各worktreeの情報は空行で区切られる

**代替案**:

- 標準出力を正規表現でパース: 却下（出力形式が変更される可能性があり不安定）
- `.git/worktrees/`ディレクトリを直接読む: 却下（Gitの内部実装に依存し、将来の変更に脆弱）

### 2. ブランチ情報とDetached HEAD状態の判定

**調査内容**: ブランチ名の取得方法とdetached HEAD状態の識別

**結果**:

ポーセラン出力から:

- `branch refs/heads/branch-name`行が存在する場合: 通常のブランチ
- `detached`行が存在する場合: detached HEAD状態
- detached HEAD時のコミットハッシュは`HEAD`行から取得

短縮コミットハッシュの生成:

- `git rev-parse --short HEAD`コマンドで短縮形を取得（デフォルト7文字）
- または`HEAD`行の最初の7文字を使用

**決定事項**:

- ポーセラン出力の`branch`行または`detached`行で状態を判定
- detached HEAD時は`HEAD`行のコミットハッシュの最初の7文字を使用
- フォーマット: `abc1234 (detached)`

### 3. Worktreeの作成日時取得方法

**調査内容**: worktreeを作成日時順（新しい順）でソートするための情報取得

**結果**:

オプション1: `.git/worktrees/<name>/gitdir`ファイルのタイムスタンプ

- 各worktreeには`.git/worktrees/<worktree-name>/`ディレクトリが作成される
- `gitdir`ファイルの作成日時 = worktreeの作成日時
- クロスプラットフォームで利用可能（System.IO.FileInfo.CreationTime）

オプション2: `git reflog`を使用

- worktree作成時のreflogエントリを検索
- より正確だが、パフォーマンスが低く、reflogが削除されている可能性がある

**決定事項**:

- オプション1を採用: ファイルシステムのタイムスタンプを使用
- `.git/worktrees/`ディレクトリ内の各worktreeサブディレクトリの`gitdir`ファイルの作成日時を取得
- クロスプラットフォームで動作し、パフォーマンスが良い

**理由**:

- シンプルで高速
- 依存関係なし（標準ファイルシステムAPI）
- Gitの内部実装への最小限の依存

### 4. テーブル整形アプローチ

**調査内容**: C#でテーブル形式の出力を生成する方法

**結果**:

オプション1: 標準ライブラリで自作

- 列幅の動的計算
- 罫線文字（Unicode Box Drawing）を使用
- 依存関係ゼロ

オプション2: ConsoleTables NuGetパッケージ

- 軽量なテーブル整形ライブラリ
- MIT ライセンス
- 追加の依存関係

オプション3: Spectre.Console

- 高機能なコンソールUIライブラリ
- テーブル、色、プログレスバー等をサポート
- 比較的大きな依存関係

**決定事項**:

- オプション1を採用: 標準ライブラリで自作
- `TableFormatter`ユーティリティクラスを実装
- 憲章V（最小限の依存関係）に準拠

**実装アプローチ**:

```text
┌─────────────────────┬──────────────────┬───────────┐
│ Path                │ Branch           │ Status    │
├─────────────────────┼──────────────────┼───────────┤
│ /path/to/main       │ main             │ active    │
│ /path/to/feature    │ feature-branch   │ active    │
│ /path/to/detached   │ abc1234 (detached)│ active   │
└─────────────────────┴──────────────────┴───────────┘
```

- 各列の最大幅を計算
- Unicode Box Drawing文字を使用（U+2500系）
- 左揃え/右揃えのサポート

### 5. エラーハンドリング

**調査内容**: 存在しないworktreeや破損したworktreeの検出

**結果**:

ポーセラン出力には存在しないworktreeも含まれる可能性あり:

- ファイルシステムでworktreeパスが削除された場合
- `git worktree prune`を実行していない場合

**決定事項**:

- 各worktreeパスの存在をDirectory.Exists()で確認
- 存在しない場合は警告メッセージを出力: `Warning: Worktree at '/path/to/missing' does not exist on disk`
- 警告後も処理を継続し、他のworktreeを表示（FR-008）

**実装方法**:

```csharp
if (!Directory.Exists(worktreePath))
{
    Console.WriteLine($"Warning: Worktree at '{worktreePath}' does not exist on disk");
    continue;
}
```

## Technology Stack Confirmation

| Component     | Technology                    | Justification                                       |
| ------------- | ----------------------------- | --------------------------------------------------- |
| CLI Framework | System.CommandLine 2.0.1      | 既存プロジェクトで使用中、標準的なCLIフレームワーク |
| Git実行       | System.Diagnostics.Process    | 標準ライブラリ、既存実装を再利用                    |
| パス操作      | System.IO.Abstractions 22.1.0 | 既存プロジェクトで使用中、テスト可能な抽象化        |
| テーブル整形  | カスタム実装                  | 依存関係ゼロ、要件に最適化                          |
| 日時処理      | System.IO.FileInfo            | 標準ライブラリ、クロスプラットフォーム              |

## Best Practices

### Git Worktree操作

- `--porcelain`フラグを常に使用して機械可読出力を取得
- エラー出力（stderr）を捕捉してユーザーに提示
- Git 2.5以上の存在を前提（既存機能と同じ）

### パフォーマンス

- `git worktree list`は1回のみ実行
- ファイルシステムアクセスは必要最小限
- 大量のworktree（100個）でも1秒以内に表示完了

### クロスプラットフォーム

- パスセパレーターはPath.DirectorySeparatorCharを使用
- Unicode Box Drawing文字はUTF-8コンソールで正しく表示される前提
- タイムゾーン処理はDateTimeKind.Localを使用

## Integration Patterns

### 既存コードとの統合

**GitServiceの拡張**:

```csharp
public interface IGitService
{
    // 既存メソッド
    Task<ProcessResult> CreateWorktreeAsync(...);
    
    // 新規メソッド
    Task<IEnumerable<WorktreeInfo>> ListWorktreesAsync();
}
```

**新しいモデル**:

```csharp
public class WorktreeInfo
{
    public string Path { get; set; }
    public string Branch { get; set; }
    public bool IsDetached { get; set; }
    public string CommitHash { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**コマンド実装**:

```csharp
public class ListCommand : Command
{
    // System.CommandLineパターンに従う
    // ハンドラーでWorktreeServiceを呼び出し
    // TableFormatterで整形して出力
}
```

## Unknowns Resolved

すべての主要な技術的不明点が解決されました：

| Unknown             | Resolution                                             |
| ------------------- | ------------------------------------------------------ |
| Git出力のパース方法 | `--porcelain`フラグで機械可読形式を使用                |
| 作成日時の取得      | `.git/worktrees/<name>/gitdir`のファイルタイムスタンプ |
| テーブル整形        | カスタムTableFormatterを実装（依存関係なし）           |
| Detached HEAD表示   | コミットハッシュの最初の7文字 + "(detached)"           |
| エラーハンドリング  | Directory.Exists()で確認、警告後も継続                 |

## Next Steps

Phase 1に進む準備が整いました：

- data-model.md: WorktreeInfo、BranchInfoモデルの詳細設計
- contracts/cli-interface.md: CLIコマンド仕様（引数、オプション、出力形式）
- quickstart.md: 開発者向けクイックスタートガイド
