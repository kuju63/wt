# CLI Interface Contract: Worktree List コマンド

**Phase**: 1 - Design & Contracts  
**Date**: 2026-01-04  
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md)

## Purpose

このドキュメントは、worktree一覧表示コマンドのCLIインターフェース契約を定義します。コマンド構文、引数、オプション、出力形式、エラーコード、動作仕様を明確にします。

## Command Structure

### Base Command

```bash
wt list
```

### Aliases

将来的に以下のエイリアスを検討（初期実装では未サポート）:

- `wt ls`

## Arguments

### Positional Arguments

なし。このコマンドは引数を取りません。

## Options

### --format, -f

**説明**: 出力フォーマットを指定します（将来の拡張、P3）

**型**: string  
**デフォルト**: `table`  
**許可値**: `table`, `json`, `csv`（初期実装では`table`のみサポート）

**例**:

```bash
wt list --format table
wt list -f json  # 将来の実装
```

### --verbose, -v

**説明**: 詳細情報を表示します（将来の拡張）

**型**: flag（boolean）  
**デフォルト**: `false`

**例**:

```bash
wt list --verbose
wt list -v
```

詳細モードでは以下の追加情報を表示:

- 完全なコミットハッシュ（40文字）
- 最終更新日時
- ブランチのアップストリーム情報

**注意**: 初期実装（P1、P2）では未サポート

## Output Formats

### Table Format (デフォルト)

```text
┌─────────────────────────────────┬──────────────────┬──────────┐
│ Path                            │ Branch           │ Status   │
├─────────────────────────────────┼──────────────────┼──────────┤
│ /Users/dev/project/wt           │ main             │ active   │
│ /Users/dev/project/feature-wt   │ feature-branch   │ active   │
│ /Users/dev/project/hotfix-wt    │ abc1234 (detached)│ active  │
└─────────────────────────────────┴──────────────────┴──────────┘
```

**列の説明**:

| Column | Description                   | Example                      |
| ------ | ----------------------------- | ---------------------------- |
| Path   | Worktreeの絶対パス            | `/Users/dev/project/wt`      |
| Branch | ブランチ名またはdetached HEAD | `main`, `abc1234 (detached)` |
| Status | worktreeの状態                | `active`, `missing`          |

**テーブル仕様**:

- ヘッダー行: 列名を表示
- 罫線: Unicode Box Drawing文字（┌─┬─┐ ├─┼─┤ └─┴─┘ │）
- 列幅: 内容に応じて動的に調整（最小幅: ヘッダー幅）
- 配置: すべて左揃え
- ソート: 作成日時の新しい順（最新のworktreeが最初）

### Worktree Not Found Case

worktreeが1つも存在しない場合:

```text
No worktrees found in this repository.
```

### Warning Messages

ディスク上に存在しないworktreeがある場合（FR-008）:

```text
Warning: Worktree at '/Users/dev/project/missing-wt' does not exist on disk
┌─────────────────────────────────┬──────────────────┬──────────┐
│ Path                            │ Branch           │ Status   │
├─────────────────────────────────┼──────────────────┼──────────┤
│ /Users/dev/project/wt           │ main             │ active   │
│ /Users/dev/project/feature-wt   │ feature-branch   │ active   │
└─────────────────────────────────┴──────────────────┴──────────┘
```

警告メッセージは標準エラー出力（stderr）に出力し、テーブルは標準出力（stdout）に出力します。

## Exit Codes

| Code | Condition            | Description                                         |
| ---- | -------------------- | --------------------------------------------------- |
| 0    | Success              | コマンドが正常に完了                                |
| 1    | Git not found        | Gitが実行できない（git worktreeコマンド未対応含む） |
| 2    | Not a git repository | 現在のディレクトリがGitリポジトリではない           |
| 10   | Git command failed   | `git worktree list`の実行が失敗                     |
| 99   | Unexpected error     | 予期しないエラー                                    |

**重要**: 警告（存在しないworktree）が発生してもexit code 0を返します。

## Error Messages

### Git Not Found

```shell
Error: Git command not found. Please ensure Git is installed and available in PATH.
```

Exit code: 1

### Not a Git Repository

```shell
Error: Not a git repository. Please run this command from within a git repository.
```

Exit code: 2

### Git Command Failed

```shell
Error: Failed to list worktrees.
Git output:
fatal: this operation must be run in a work tree

Run with --verbose for more details.
```

Exit code: 10

## Behavior Specifications

### Pre-conditions

1. コマンドはGitリポジトリ内で実行される必要がある
2. Git 2.5以上がインストールされている必要がある
3. `git worktree list`コマンドが使用可能である必要がある

### Execution Flow

```text
1. カレントディレクトリがGitリポジトリか確認
   ├─ No → Error (exit code 2)
   └─ Yes → 次へ

2. `git worktree list --porcelain` を実行
   ├─ 失敗 → Error (exit code 10)
   └─ 成功 → 次へ

3. ポーセラン出力をパース
   ├─ worktreeなし → "No worktrees found" メッセージ (exit code 0)
   └─ worktreeあり → 次へ

4. 各worktreeの情報を収集
   ├─ パス、ブランチ、detached状態、コミットハッシュ
   ├─ 作成日時 (.git/worktrees/<name>/gitdir のタイムスタンプ)
   └─ 存在確認 (Directory.Exists)

5. 作成日時の降順でソート

6. テーブル整形
   └─ 列幅を動的計算

7. 出力
   ├─ 警告メッセージ (stderr) - 存在しないworktreeがある場合
   └─ テーブル (stdout)

8. Exit (code 0)
```

### Post-conditions

- 標準出力にテーブルが表示される
- 警告がある場合、標準エラー出力に警告が表示される
- 正常終了時のexit codeは0

## Examples

### 基本的な使用例

```bash
$ wt list
┌─────────────────────────────────┬──────────────────┬──────────┐
│ Path                            │ Branch           │ Status   │
├─────────────────────────────────┼──────────────────┼──────────┤
│ /Users/dev/project/main-wt      │ main             │ active   │
│ /Users/dev/project/feature-wt   │ feature-new      │ active   │
└─────────────────────────────────┴──────────────────┴──────────┘
```

### Detached HEAD状態を含む例

```bash
$ wt list
┌─────────────────────────────────┬────────────────────┬──────────┐
│ Path                            │ Branch             │ Status   │
├─────────────────────────────────┼────────────────────┼──────────┤
│ /Users/dev/project/main-wt      │ main               │ active   │
│ /Users/dev/project/hotfix-wt    │ abc1234 (detached) │ active   │
└─────────────────────────────────┴────────────────────┴──────────┘
```

### 存在しないworktreeがある例

```bash
$ wt list
Warning: Worktree at '/Users/dev/project/deleted-wt' does not exist on disk
┌─────────────────────────────────┬──────────────────┬──────────┐
│ Path                            │ Branch           │ Status   │
├─────────────────────────────────┼──────────────────┼──────────┤
│ /Users/dev/project/main-wt      │ main             │ active   │
│ /Users/dev/project/feature-wt   │ feature-new      │ active   │
└─────────────────────────────────┴──────────────────┴──────────┘

$ echo $?
0
```

### Worktreeが存在しない例

```bash
$ wt list
No worktrees found in this repository.

$ echo $?
0
```

### エラー例: Gitリポジトリではない

```bash
$ cd /tmp
$ wt list
Error: Not a git repository. Please run this command from within a git repository.

$ echo $?
2
```

## Implementation Notes

### System.CommandLine統合

```csharp
var listCommand = new Command("list", "List all worktrees with their branches");

// 将来のオプション（初期実装では未使用）
var formatOption = new Option<string>(
    aliases: new[] { "--format", "-f" },
    getDefaultValue: () => "table",
    description: "Output format (table, json, csv)");
listCommand.AddOption(formatOption);

listCommand.SetHandler(async (format) =>
{
    // ハンドラー実装
    var service = GetWorktreeService();
    var worktrees = await service.ListWorktreesAsync();
    var formatter = new TableFormatter();
    var output = formatter.Format(worktrees);
    Console.WriteLine(output);
}, formatOption);
```

### テーブル整形

`Formatters/TableFormatter.cs`クラスが以下の責務を持ちます:

```csharp
namespace Kuju63.WorkTree.CommandLine.Formatters;

public class TableFormatter
{
    public string Format(IEnumerable<WorktreeInfo> worktrees)
    {
        // 1. 列幅の計算
        // 2. ヘッダー行の生成
        // 3. 罫線の生成
        // 4. データ行の生成
        // 5. 結合して返す
    }

    private int CalculateColumnWidth(string header, IEnumerable<string> values)
    {
        // ヘッダーとすべての値の最大幅を返す
    }

    private string GenerateSeparator(int[] columnWidths, string left, string mid, string right)
    {
        // 罫線を生成（┌─┬─┐ 等）
    }
}
```

## Testing Strategy

### ユニットテスト

- コマンドハンドラーのテスト（モックサービス使用）
- TableFormatterの各メソッドのテスト
- エラーケースのテスト（Git未検出、非リポジトリ等）

### 統合テスト

- 実際のGitリポジトリでのエンドツーエンドテスト
- 複数worktreeのシナリオテスト
- Detached HEAD状態のテスト
- 存在しないworktreeの警告テスト

### 契約テスト

- 出力形式の正確性検証（テーブル罫線、列配置等）
- Exit codeの検証
- エラーメッセージの検証

## Compliance

このCLI契約は以下の仕様要件を満たしています:

- **FR-001**: すべてのアクティブなworktreeのリストを表示
- **FR-002**: 各worktreeの現在のブランチを表示
- **FR-003**: テーブル形式で出力を表示
- **FR-004**: Detached HEAD状態を処理
- **FR-005**: worktreeパスとブランチ情報を表示
- **FR-006**: コマンドラインインターフェースから呼び出し可能
- **FR-007**: 作成日時順（新しい順）でソート
- **FR-008**: 存在しないworktreeに対して警告を表示し、処理を継続

## Future Extensions (P3)

### JSON Format (将来)

```bash
$ wt list --format json
[
  {
    "path": "/Users/dev/project/main-wt",
    "branch": "main",
    "is_detached": false,
    "commit_hash": "abc1234567890abcdef1234567890abcdef1234",
    "created_at": "2026-01-04T10:30:00+09:00",
    "exists": true
  }
]
```

### CSV Format (将来)

```bash
$ wt list --format csv
Path,Branch,Status,Created At
/Users/dev/project/main-wt,main,active,2026-01-04T10:30:00+09:00
/Users/dev/project/feature-wt,feature-new,active,2026-01-04T11:00:00+09:00
```

## Summary

CLI Interface契約は以下の点で明確に定義されています:

- **コマンド構文**: シンプルで覚えやすい
- **出力形式**: テーブル形式で視覚的に明確
- **エラーハンドリング**: 適切なexit codeと明確なエラーメッセージ
- **拡張性**: 将来のフォーマット追加に対応可能
- **互換性**: System.CommandLineの標準パターンに準拠

この契約に基づいて、実装とテストを進めることができます。
