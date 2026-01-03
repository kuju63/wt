# CLI Interface Contract: Worktree Create Command

**Feature**: 001-create-worktree  
**Date**: 2026-01-03  
**Version**: 1.0.0

## Command Syntax

```bash
wt create <branch-name> [OPTIONS]
```

**Note**: `wt`自体が「worktree tool」を意味するため、`worktree`サブコマンドは省略しています。シンプルで使いやすいCLIを実現します。

## Arguments

### Required

- `<branch-name>`: 作成する新しいブランチ名
  - **Type**: string
  - **Validation**: Git命名規則に準拠（英数字、`-`、`_`、`/`のみ）
  - **Example**: `feature-x`, `bugfix/issue-123`, `user_story_1`

## Options

### --base, -b

ベースブランチの指定（デフォルト: 現在のブランチ）

- **Type**: string
- **Default**: 現在のブランチ
- **Example**: `--base main` または `-b develop`

### --path, -p

Worktreeを作成するパス（デフォルト: `../worktrees/<branch-name>`）

- **Type**: string (絶対パスまたは相対パス)
- **Default**: `../worktrees/<branch-name>`
- **Example**: `--path ~/projects/my-worktree` または `-p ../feature-branch`

### --editor, -e

Worktree作成後に起動するエディター

- **Type**: string (enum: vscode|vim|emacs|nano|idea)
- **Default**: なし（エディターを起動しない）
- **Example**: `--editor vscode` または `-e vim`

### --checkout-existing

既存ブランチが存在する場合、それをチェックアウトする

- **Type**: boolean flag
- **Default**: false
- **Example**: `--checkout-existing`

### --output, -o

出力形式の指定

- **Type**: string (enum: human|json)
- **Default**: human
- **Example**: `--output json` または `-o json`

### --verbose, -v

詳細な診断情報を出力

- **Type**: boolean flag
- **Default**: false
- **Example**: `--verbose` または `-v`

### --help, -h

ヘルプメッセージを表示

- **Type**: boolean flag
- **Example**: `--help` または `-h`

## Usage Examples

### Example 1: 基本的な使用方法

```bash
$ wt create feature-login
✓ Created branch 'feature-login' from 'main'
✓ Added worktree at: /Users/dev/projects/worktrees/feature-login
→ Next: cd /Users/dev/projects/worktrees/feature-login
```

### Example 2: ベースブランチを指定

```bash
$ wt create hotfix-123 --base production
✓ Created branch 'hotfix-123' from 'production'
✓ Added worktree at: /Users/dev/projects/worktrees/hotfix-123
→ Next: cd /Users/dev/projects/worktrees/hotfix-123
```

### Example 3: エディターを起動

```bash
$ wt create feature-ui --editor vscode
✓ Created branch 'feature-ui' from 'main'
✓ Added worktree at: /Users/dev/projects/worktrees/feature-ui
✓ Launched VS Code
```

### Example 4: カスタムパスを指定

```bash
$ wt create experiment --path ~/experiments/test-feature
✓ Created branch 'experiment' from 'main'
✓ Added worktree at: /Users/dev/experiments/test-feature
→ Next: cd /Users/dev/experiments/test-feature
```

### Example 5: JSON出力

```bash
$ wt create api-v2 --output json
{
  "success": true,
  "worktree": {
    "path": "/Users/dev/projects/worktrees/api-v2",
    "branch": "api-v2",
    "baseBranch": "main",
    "createdAt": "2026-01-03T13:15:30Z"
  },
  "editorLaunched": false
}
```

### Example 6: 既存ブランチをチェックアウト

```bash
$ wt create feature-x
✗ Error: Branch 'feature-x' already exists
→ Solution: Use --checkout-existing to checkout the existing branch

$ wt create feature-x --checkout-existing
✓ Checked out existing branch 'feature-x'
✓ Added worktree at: /Users/dev/projects/worktrees/feature-x
→ Next: cd /Users/dev/projects/worktrees/feature-x
```

### Example 7: Verbose モード

```bash
$ wt create test --verbose
[DEBUG] Checking for Git repository...
[DEBUG] Found Git repository at: /Users/dev/projects/my-repo
[DEBUG] Current branch: main
[DEBUG] Checking if branch 'test' exists...
[DEBUG] Branch 'test' does not exist
[DEBUG] Creating branch 'test' from 'main'...
[DEBUG] Executing: git branch test main
[DEBUG] Creating worktree at: /Users/dev/projects/worktrees/test
[DEBUG] Executing: git worktree add /Users/dev/projects/worktrees/test test
✓ Created branch 'test' from 'main'
✓ Added worktree at: /Users/dev/projects/worktrees/test
→ Next: cd /Users/dev/projects/worktrees/test
```

## Exit Codes

| Code | Meaning | Description |
|------|---------|-------------|
| 0 | Success | Worktree created successfully |
| 1 | General Error | Unspecified error occurred |
| 2 | Invalid Arguments | Command-line arguments are invalid |
| 10 | Git Not Found | Git is not installed or not in PATH |
| 11 | Not Git Repository | Current directory is not a Git repository |
| 12 | Git Command Failed | Git command execution failed |
| 20 | Invalid Branch Name | Branch name doesn't follow Git naming rules |
| 21 | Branch Exists | Branch already exists and --checkout-existing not specified |
| 30 | Worktree Exists | Worktree with same name already exists |
| 31 | Invalid Path | Specified path is invalid |
| 32 | Path Not Writable | Don't have write permissions to target directory |
| 40 | Disk Space Low | Insufficient disk space |
| 50 | Editor Not Found | Specified editor is not found (warning, not fatal) |

## Error Handling

### Standard Error Format (Human)

```
✗ Error: <Error Message>
→ Solution: <Suggested Solution>

Additional Details:
<Optional Details>
```

### JSON Error Format

```json
{
  "success": false,
  "error": {
    "code": "BR002",
    "message": "Branch 'feature-x' already exists",
    "solution": "Use --checkout-existing to checkout the existing branch",
    "details": "Branch 'feature-x' was created on 2026-01-02 at commit abc123"
  },
  "warnings": []
}
```

## Environment Variables

### WT_DEFAULT_WORKTREE_PATH

デフォルトのworktreeパスを設定

- **Type**: string
- **Default**: `../worktrees`
- **Example**: `export WT_DEFAULT_WORKTREE_PATH=~/my-worktrees`

### WT_DEFAULT_EDITOR

デフォルトのエディターを設定

- **Type**: string
- **Default**: なし
- **Example**: `export WT_DEFAULT_EDITOR=vscode`

## Validation Rules

### Branch Name Validation

1. 空文字列ではない
2. 以下の文字のみ: `a-zA-Z0-9`, `-`, `_`, `/`
3. 以下で開始しない: `-`, `.`
4. 以下を含まない: `..`, `@{`, `\`, スペース、制御文字

### Path Validation

1. 空文字列ではない
2. 親ディレクトリが存在する
3. 書き込み権限がある
4. 十分なディスク容量がある（最低100MB）

### Editor Validation

1. プリセットまたはカスタムコマンドのいずれか
2. カスタムコマンドは実行可能ファイルとしてPATHに存在

## Performance Expectations

| Operation | Target Time | Notes |
|-----------|-------------|-------|
| バリデーション | < 100ms | 入力チェック |
| Gitリポジトリ確認 | < 200ms | `.git`ディレクトリ検索 |
| ブランチ作成 | < 1s | Gitコマンド実行 |
| Worktree追加 | < 2s | ファイルコピー含む |
| エディター起動 | < 1s | プロセス起動 |
| **Total** | **< 5s** | 通常のケース |

## Compatibility

### Minimum Requirements

- Git 2.5+ (git worktree サポート)
- 100MB ディスク空き容量

**Note**: Self-contained deployment（ワンバイナリ）として配布するため、.NET Runtimeのインストールは不要です。

### Supported Platforms

- ✅ Windows 10+
- ✅ macOS 11+
- ✅ Linux (Ubuntu 20.04+, その他主要ディストリビューション)

### Supported Editors

- ✅ Visual Studio Code
- ✅ Vim
- ✅ Emacs
- ✅ Nano
- ✅ IntelliJ IDEA

## Security Considerations

1. **Command Injection Prevention**: ユーザー入力はサニタイズされ、Gitコマンドに直接渡されない
2. **Path Traversal Prevention**: パスは正規化され、許可されたディレクトリ内に制限される
3. **Validation**: すべての入力は厳格にバリデーションされる

## Future Extensions (Out of Scope for v1.0)

- `wt list`: 既存worktreeの一覧表示
- `wt remove <branch>`: Worktreeの削除
- `wt switch <branch>`: Worktree間の切り替え
- `wt status`: すべてのworktreeのステータス表示

**Design Philosophy**: `wt`自体がworktree管理ツールであるため、`worktree`サブコマンドグループは不要。すべてのコマンドはトップレベルに配置し、シンプルで覚えやすいCLIを実現します。

## Changelog

### 1.0.0 (2026-01-03)

- Initial specification
- Basic worktree creation
- Editor integration
- Cross-platform support
