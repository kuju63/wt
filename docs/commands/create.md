# create

Create a new worktree with a new branch.

## Syntax

```bash
wt create <branch> [options]
```

## Description

Creates a new Git worktree in a separate directory with a new branch. This allows you to work on multiple branches simultaneously without switching contexts.

## Arguments

**`<branch>`** (required)

Name of the new branch to create.

## Options

**`-b, --base <base>`**

Base branch to branch from. If not specified, uses the current branch.

**`-p, --path <path>`**

Custom path where the worktree will be created. Default: `../wt-<branch>`

**`-e, --editor <type>`**

Editor to launch automatically after creating the worktree.

Supported values:
- `vscode` - Visual Studio Code
- `vim` - Vim
- `emacs` - Emacs
- `nano` - Nano
- `idea` - IntelliJ IDEA

**`--output <format>`**

Output format for the command result.

Values:
- `human` (default) - Human-readable output
- `json` - JSON format for automation

**`-v, --verbose`**

Show detailed diagnostic information including error codes.

**`-h, --help`**

Show help and usage information.

## Examples

### Example 1: Create a feature branch

```bash
wt create feature-login
```

Output:
```
✓ Worktree created successfully
  Path:   /Users/username/projects/wt-feature-login
  Branch: feature-login
```

### Example 2: Create from specific base branch

```bash
wt create feature-auth --base main
```

Creates a new `feature-auth` branch from `main` instead of the current branch.

### Example 3: Custom path

```bash
wt create bugfix-123 --path ~/bugfixes/fix-123
```

Creates the worktree at a specific location.

### Example 4: Auto-launch editor

```bash
wt create feature-ui --editor vscode
```

Creates the worktree and automatically opens it in Visual Studio Code.

### Example 5: JSON output for automation

```bash
wt create feature-api --output json
```

Output:
```json
{
  "success": true,
  "worktree": {
    "path": "/Users/username/projects/wt-feature-api",
    "branch": "feature-api",
    "createdAt": "2026-01-16T03:45:00Z"
  }
}
```

### Example 6: Combine multiple options

```bash
wt create feature-dashboard --base develop --path ~/features/dashboard --editor vscode --verbose
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0    | Success |
| 1    | General error (branch exists, path exists, etc.) |
| 2    | Not a Git repository |
| 10   | Git command failed |
| 99   | Unexpected error |

## Common Errors

### Branch already exists

```
✗ Branch 'feature-login' already exists
  Solution: Use a different branch name or delete the existing branch
```

### Path already exists

```
✗ Path '/path/to/worktree' already exists
  Solution: Specify a different path using --path option
```

### Not a Git repository

```
✗ Not a git repository
  Solution: Run this command from within a git repository
```

## See Also

- [list](list.md) - List all worktrees
- [Quick Start Guide](../guides/quickstart.md) - Learn worktree workflows
