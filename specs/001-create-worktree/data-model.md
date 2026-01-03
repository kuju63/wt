# Data Model: Git Worktree 作成コマンド

**Feature**: 001-create-worktree  
**Date**: 2026-01-03  
**Source**: [spec.md](spec.md)

## Core Entities

### 1. WorktreeInfo

Worktreeの情報を表現するモデル。

**Properties**:

- `Path` (string): worktreeの絶対パス
- `Branch` (string): 関連するブランチ名
- `BaseBranch` (string): ベースブランチ名
- `CreatedAt` (DateTime): 作成日時
- `IsActive` (bool): アクティブかどうか

**Relationships**:

- 1つのWorktreeは1つのBranchに関連付けられる

**Validation Rules**:

- Pathは有効な絶対パスでなければならない
- Branchは空文字列ではない
- BaseBranchは空文字列ではない

**State Transitions**:

```
[Not Exists] --create--> [Active] --remove--> [Removed]
```

### 2. BranchInfo

Gitブランチの情報を表現するモデル。

**Properties**:

- `Name` (string): ブランチ名
- `BaseBranch` (string, optional): ベースブランチ名（nullの場合は現在のブランチ）
- `Exists` (bool): 既に存在するか
- `IsRemote` (bool): リモートブランチか

**Validation Rules**:

- Nameは Git命名規則に準拠（英数字、`-`、`_`、`/`のみ）
- Nameは以下で開始してはならない: `-`、`.`
- Nameに以下を含めてはならない: `..`、`@{`、`\`、スペース

**Validation Logic**:

```csharp
private static readonly Regex BranchNamePattern = 
    new(@"^[a-zA-Z0-9][a-zA-Z0-9/_-]*$", RegexOptions.Compiled);

public bool IsValidBranchName(string name)
{
    if (string.IsNullOrWhiteSpace(name)) return false;
    if (name.Contains("..") || name.Contains("@{")) return false;
    return BranchNamePattern.IsMatch(name);
}
```

### 3. EditorConfig

エディター設定を表現するモデル。

**Properties**:

- `EditorType` (EditorType enum): エディターの種類
- `Command` (string): 実行コマンド
- `Arguments` (string): コマンド引数テンプレート
- `IsAvailable` (bool): エディターが利用可能か

**EditorType Enum**:
```csharp
public enum EditorType
{
    VSCode,
    Vim,
    Emacs,
    Nano,
    IntelliJIDEA
}
```

**Preset Examples**:

| EditorType   | Command | Arguments |
| ------------ | ------- | --------- |
| VSCode       | `code`  | `{path}`  |
| Vim          | `vim`   | `{path}`  |
| Emacs        | `emacs` | `{path}`  |
| Nano         | `nano`  | `{path}`  |
| IntelliJIDEA | `idea`  | `{path}`  |

**Validation Rules**:

- Commandは空文字列ではない
- Argumentsには`{path}`プレースホルダーを含む必要がある

### 4. CommandResult<T>

コマンド実行結果を表現するジェネリックモデル（Resultパターン）。

**Properties**:

- `Success` (bool): 成功したか
- `Data` (T, optional): 成功時のデータ
- `Error` (ErrorInfo, optional): エラー情報
- `Warnings` (List<string>): 警告メッセージ

**ErrorInfo**:

- `Code` (string): エラーコード
- `Message` (string): エラーメッセージ
- `Solution` (string): 解決策の提案
- `Details` (string, optional): 詳細情報（verboseモード用）

**Usage Pattern**:

```csharp
var result = await worktreeService.CreateAsync(options);
if (result.Success)
{
    Console.WriteLine($"Worktree created: {result.Data.Path}");
}
else
{
    Console.Error.WriteLine($"Error: {result.Error.Message}");
    Console.Error.WriteLine($"Solution: {result.Error.Solution}");
}
```

### 5. CreateWorktreeOptions

Worktree作成オプションを表現するモデル。

**Properties**:

- `BranchName` (string, required): 作成するブランチ名
- `BaseBranch` (string, optional): ベースブランチ（nullの場合は現在のブランチ）
- `WorktreePath` (string, optional): worktreeパス（nullの場合はデフォルト）
- `EditorType` (EditorType, optional): 起動するエディター
- `CustomEditorCommand` (string, optional): カスタムエディターコマンド
- `ForceCheckout` (bool): 既存ブランチを強制チェックアウトするか
- `OutputFormat` (OutputFormat enum): 出力形式（Human/Json）
- `Verbose` (bool): 詳細出力を有効にするか

**OutputFormat Enum**:

```csharp
public enum OutputFormat
{
    Human,
    Json
}
```

**Validation Rules**:
- BranchNameは必須かつ有効な名前
- WorktreePathが指定された場合は有効なパスでなければならない
- EditorTypeとCustomEditorCommandは排他的（両方指定はエラー）

## Data Flow

### Worktree作成フロー

```
User Input (BranchName + Options)
    ↓
CreateWorktreeOptions (バリデーション)
    ↓
WorktreeService
    ├→ GitService: リポジトリチェック
    ├→ GitService: ブランチ存在確認
    ├→ BranchInfo: ブランチ情報作成
    ├→ GitService: ブランチ作成
    ├→ GitService: worktree追加
    ├→ WorktreeInfo: worktree情報作成
    └→ EditorService: エディター起動 (optional)
    ↓
CommandResult<WorktreeInfo>
    ↓
OutputFormatter (Human/Json)
    ↓
Console Output
```

## Error Codes

エラーコードの体系的な定義。

| Code     | Category   | Message                 | Solution                                                   |
| -------- | ---------- | ----------------------- | ---------------------------------------------------------- |
| `GIT001` | Git        | Git not found           | Install Git 2.5 or later                                   |
| `GIT002` | Git        | Not a git repository    | Run command in a git repository or run 'git init'          |
| `GIT003` | Git        | Git command failed      | Check git status and try again                             |
| `BR001`  | Branch     | Invalid branch name     | Use only alphanumeric, `-`, `_`, `/` characters            |
| `BR002`  | Branch     | Branch already exists   | Use `--checkout-existing` to checkout existing branch      |
| `WT001`  | Worktree   | Worktree already exists | Choose a different branch name or remove existing worktree |
| `WT002`  | Worktree   | Invalid worktree path   | Specify a valid absolute or relative path                  |
| `WT003`  | Worktree   | Path not writable       | Check directory permissions                                |
| `FS001`  | FileSystem | Disk space low          | Free up disk space (need at least 100MB)                   |
| `ED001`  | Editor     | Editor not found        | Check editor installation or use a different editor        |

## Performance Considerations

### Memory Usage

各エンティティのメモリフットプリント（概算）:
- WorktreeInfo: ~200 bytes
- BranchInfo: ~150 bytes
- EditorConfig: ~100 bytes
- CommandResult<WorktreeInfo>: ~500 bytes

**Total for single operation**: < 1KB（目標の100MB内に十分収まる）

### Caching Strategy

現時点ではキャッシングは実装しません（シンプルさ優先）。将来的な拡張として検討可能：
- エディタープリセットのキャッシュ
- Git設定のキャッシュ

## Extensibility

将来的な拡張を考慮した設計：

1. **Additional Editor Support**: EditorPresetsクラスに追加するだけ
2. **Remote Worktrees**: WorktreeInfoにRemoteInfoプロパティを追加
3. **Worktree Templates**: 新しいTemplateInfoエンティティを追加

## Summary

このデータモデルは以下の要件を満たします：
- ✅ 明確な責任分離（Single Responsibility）
- ✅ バリデーションルールの明示
- ✅ エラーハンドリングの体系化
- ✅ 拡張性の確保
- ✅ パフォーマンス目標との整合性
