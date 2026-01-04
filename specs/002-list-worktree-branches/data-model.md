# Data Model: Worktreeとブランチ情報

**Phase**: 1 - Design & Contracts  
**Date**: 2026-01-04  
**Feature**: [spec.md](spec.md) | [plan.md](plan.md) | [research.md](research.md)

## Purpose

このドキュメントは、worktreeとブランチ一覧表示機能のデータモデルを定義します。仕様書（spec.md）の主要エンティティを具体的なC#クラスとして設計し、プロパティ、バリデーションルール、関係性を明確にします。

## Core Entities

### WorktreeInfo

worktreeインスタンスの情報を表現するモデル。

```csharp
namespace Kuju63.WorkTree.CommandLine.Models
{
    /// <summary>
    /// Git worktreeの情報を表現します。
    /// </summary>
    public class WorktreeInfo
    {
        /// <summary>
        /// Worktreeのファイルシステム上のパス
        /// </summary>
        /// <remarks>
        /// 絶対パスである必要があります。
        /// 存在しないパスの場合でも格納可能（警告表示用）。
        /// </remarks>
        public required string Path { get; init; }

        /// <summary>
        /// チェックアウトされているブランチ名
        /// </summary>
        /// <remarks>
        /// Detached HEAD状態の場合は短縮コミットハッシュを含む。
        /// 例: "main", "feature/new-feature", "abc1234"
        /// </remarks>
        public required string Branch { get; init; }

        /// <summary>
        /// Detached HEAD状態かどうか
        /// </summary>
        public bool IsDetached { get; init; }

        /// <summary>
        /// 現在のHEADのコミットハッシュ（完全形）
        /// </summary>
        /// <remarks>
        /// 40文字の16進数文字列。
        /// 短縮形が必要な場合はSubstring(0, 7)を使用。
        /// </remarks>
        public required string CommitHash { get; init; }

        /// <summary>
        /// Worktreeの作成日時
        /// </summary>
        /// <remarks>
        /// ローカルタイムゾーン。
        /// .git/worktrees/<name>/gitdirファイルの作成日時から取得。
        /// </remarks>
        public DateTime CreatedAt { get; init; }

        /// <summary>
        /// Worktreeがファイルシステム上に存在するか
        /// </summary>
        /// <remarks>
        /// Directory.Exists()の結果をキャッシュ。
        /// falseの場合、警告メッセージを表示する必要がある。
        /// </remarks>
        public bool Exists { get; init; }

        /// <summary>
        /// 表示用のブランチ文字列を取得します
        /// </summary>
        /// <returns>
        /// 通常のブランチ: ブランチ名
        /// Detached HEAD: "abc1234 (detached)"
        /// </returns>
        public string GetDisplayBranch()
        {
            if (IsDetached)
            {
                var shortHash = CommitHash.Length >= 7 
                    ? CommitHash.Substring(0, 7) 
                    : CommitHash;
                return $"{shortHash} (detached)";
            }
            return Branch;
        }

        /// <summary>
        /// 表示用のステータス文字列を取得します
        /// </summary>
        /// <returns>
        /// 存在する: "active"
        /// 存在しない: "missing"
        /// </returns>
        public string GetDisplayStatus()
        {
            return Exists ? "active" : "missing";
        }
    }
}
```

### BranchInfo

ブランチ参照の情報を表現するモデル（将来の拡張用）。

```csharp
namespace Kuju63.WorkTree.CommandLine.Models
{
    /// <summary>
    /// Gitブランチの情報を表現します。
    /// </summary>
    /// <remarks>
    /// 現在のフェーズでは使用しませんが、将来の拡張（ブランチ一覧表示等）のために定義します。
    /// </remarks>
    public class BranchInfo
    {
        /// <summary>
        /// ブランチ名（refs/heads/を除いた形式）
        /// </summary>
        /// <example>main, feature/new-feature</example>
        public required string Name { get; init; }

        /// <summary>
        /// ブランチのHEADコミットハッシュ
        /// </summary>
        public required string CommitHash { get; init; }

        /// <summary>
        /// いずれかのworktreeでチェックアウトされているか
        /// </summary>
        public bool IsCheckedOut { get; init; }

        /// <summary>
        /// チェックアウトしているworktreeのパス（複数の場合は最初の1つ）
        /// </summary>
        /// <remarks>
        /// IsCheckedOut=trueの場合のみ有効。
        /// nullの場合、どのworktreeでもチェックアウトされていない。
        /// </remarks>
        public string? WorktreePath { get; init; }
    }
}
```

## Validation Rules

### WorktreeInfo Validation

| Property   | Rule                  | Error Message                    |
| ---------- | --------------------- | -------------------------------- |
| Path       | 必須、空文字列不可    | "Worktree path is required"      |
| Path       | 絶対パスであること    | "Worktree path must be absolute" |
| Branch     | 必須、空文字列不可    | "Branch name is required"        |
| Branch     | 有効なGit参照名       | "Invalid branch name format"     |
| CommitHash | 必須、40文字の16進数  | "Invalid commit hash format"     |
| CreatedAt  | DateTime.MinValue以外 | "Invalid creation date"          |

**実装例**:

```csharp
public static class WorktreeInfoValidator
{
    public static ValidationResult Validate(WorktreeInfo info)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(info.Path))
            errors.Add("Worktree path is required");
        else if (!System.IO.Path.IsPathRooted(info.Path))
            errors.Add("Worktree path must be absolute");

        if (string.IsNullOrWhiteSpace(info.Branch))
            errors.Add("Branch name is required");

        if (string.IsNullOrWhiteSpace(info.CommitHash) || 
            !IsValidCommitHash(info.CommitHash))
            errors.Add("Invalid commit hash format");

        if (info.CreatedAt == DateTime.MinValue)
            errors.Add("Invalid creation date");

        return errors.Count == 0 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(errors);
    }

    private static bool IsValidCommitHash(string hash)
    {
        return hash.Length == 40 && 
               hash.All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f'));
    }
}
```

## Entity Relationships

```text
WorktreeInfo (1) --- (0..1) BranchInfo
  │
  │ 集約: Worktreeは単一のブランチまたはdetached HEADを持つ
  │ 方向: WorktreeInfo.Branch → BranchInfo.Name
  │
  └─ IsDetached=false の場合のみBranchInfoと関連
```

**注意**: 現在の実装（Phase 1）では、BranchInfoは使用せず、WorktreeInfoのみを使用します。将来、ブランチ一覧機能を追加する際にBranchInfoを活用します。

## State Transitions

WorktreeInfoには明示的な状態遷移はありませんが、以下のような不変の状態があります：

```text
[Created] ─┬─> [Exists=true, IsDetached=false]  通常のブランチ
           ├─> [Exists=true, IsDetached=true]   Detached HEAD
           ├─> [Exists=false, IsDetached=false] 削除された（ブランチ）
           └─> [Exists=false, IsDetached=true]  削除された（Detached）
```

すべてのWorktreeInfoインスタンスは不変（immutable）です。状態変更は新しいインスタンスの生成により行います。

## Data Flow

```text
1. GitService.ListWorktreesAsync()
   ↓ git worktree list --porcelain 実行
   ↓ ポーセラン出力をパース
   ↓
2. WorktreeInfo インスタンス生成
   ├─ Path: ポーセラン出力の"worktree"行から取得
   ├─ Branch: "branch"行から取得（detachedの場合は"HEAD"行から）
   ├─ IsDetached: "detached"行の有無で判定
   ├─ CommitHash: "HEAD"行から取得
   ├─ CreatedAt: .git/worktrees/<name>/gitdirのファイルタイムスタンプ
   └─ Exists: Directory.Exists(Path)の結果
   ↓
3. ソート: CreatedAt降順
   ↓
4. TableFormatter.Format(worktrees)
   ↓ テーブル文字列生成
   ↓
5. Console出力
```

## Persistence

このモデルはメモリ上のみに存在し、永続化は行いません。すべての情報はGitリポジトリとファイルシステムから動的に取得します。

## Serialization

将来の拡張（P3: 代替出力フォーマット）のために、JSON シリアライゼーションをサポートする設計にします。

```csharp
// System.Text.Json 互換
[JsonPropertyName("path")]
public required string Path { get; init; }

[JsonPropertyName("branch")]
public required string Branch { get; init; }

[JsonPropertyName("is_detached")]
public bool IsDetached { get; init; }

[JsonPropertyName("commit_hash")]
public required string CommitHash { get; init; }

[JsonPropertyName("created_at")]
public DateTime CreatedAt { get; init; }

[JsonPropertyName("exists")]
public bool Exists { get; init; }
```

**JSON出力例**:

```json
[
  {
    "path": "/Users/dev/project/wt",
    "branch": "main",
    "is_detached": false,
    "commit_hash": "abc1234567890abcdef1234567890abcdef1234",
    "created_at": "2026-01-04T10:30:00+09:00",
    "exists": true
  },
  {
    "path": "/Users/dev/project/feature-wt",
    "branch": "feature-branch",
    "is_detached": false,
    "commit_hash": "def5678901234567890abcdef1234567890abcde",
    "created_at": "2026-01-04T11:00:00+09:00",
    "exists": true
  }
]
```

## Testing Considerations

### ユニットテスト

- `GetDisplayBranch()`メソッドのテスト:
  - 通常のブランチ名が正しく返される
  - Detached HEADの場合、短縮ハッシュ + "(detached)"が返される
  - コミットハッシュが7文字未満の場合でも動作する

- `GetDisplayStatus()`メソッドのテスト:
  - Exists=trueの場合 "active"
  - Exists=falseの場合 "missing"

- バリデーションのテスト:
  - 各プロパティのバリデーションルールが正しく機能する
  - 複数のエラーが同時に検出される

### 統合テスト

- Gitリポジトリから実際にWorktreeInfoを生成するテスト
- 存在しないworktreeの検出テスト
- Detached HEAD状態のworktreeの処理テスト

## Summary

データモデルは以下の原則に従って設計されています：

- **不変性**: すべてのプロパティはinit-onlyで不変
- **明示性**: 各プロパティの用途と制約が明確
- **拡張性**: 将来の機能追加（JSON出力、ブランチ一覧）に対応可能
- **テスト可能性**: バリデーションとビジネスロジックが分離
- **型安全性**: required修飾子とnull許容型で安全性を確保

次のステップ: contracts/cli-interface.mdでCLIインターフェース仕様を定義します。
