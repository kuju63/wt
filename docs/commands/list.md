# list

List all worktrees with their branch information.

## Syntax

```bash
wt list
```

## Description

Display all Git worktrees in the repository with their paths, checked-out branches, and status in a table format. Missing worktrees (registered but not existing on disk) are highlighted with warnings.

## Arguments

None.

## Options

**`-h, --help`**

Show help and usage information.

## Output

The command displays a table with the following columns:

- **Path**: File system path to the worktree
- **Branch**: Currently checked-out branch name (or commit hash for detached HEAD)
- **Status**: `active` (exists on disk) or `missing` (registered but directory not found)

## Example

### Basic usage

```bash
wt list
```

Output:
```
┌─────────────────────────────────────────────┬──────────────────────┬─────────┐
│ Path                                        │ Branch               │ Status  │
├─────────────────────────────────────────────┼──────────────────────┼─────────┤
│ /Users/dev/project/wt                       │ main                 │ active  │
│ /Users/dev/project/wt-feature-login         │ feature-login        │ active  │
│ /Users/dev/project/wt-bugfix-123            │ bugfix-123           │ active  │
└─────────────────────────────────────────────┴──────────────────────┴─────────┘
```

### With detached HEAD

Output:
```
┌─────────────────────────────────────────────┬──────────────────────┬─────────┐
│ Path                                        │ Branch               │ Status  │
├─────────────────────────────────────────────┼──────────────────────┼─────────┤
│ /Users/dev/project/wt                       │ main                 │ active  │
│ /Users/dev/project/wt-hotfix                │ abc1234 (detached)   │ active  │
└─────────────────────────────────────────────┴──────────────────────┴─────────┘
```

### With missing worktree

Output:
```
Warning: Worktree at '/Users/dev/project/wt-old-feature' does not exist on disk

┌─────────────────────────────────────────────┬──────────────────────┬─────────┐
│ Path                                        │ Branch               │ Status  │
├─────────────────────────────────────────────┼──────────────────────┼─────────┤
│ /Users/dev/project/wt                       │ main                 │ active  │
│ /Users/dev/project/wt-old-feature           │ old-feature          │ missing │
└─────────────────────────────────────────────┴──────────────────────┴─────────┘
```

### No worktrees

Output:
```
No worktrees found in this repository.
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0    | Success |
| 1    | Git not found |
| 2    | Not a Git repository |
| 10   | Git command failed |
| 99   | Unexpected error |

## Common Errors

### Not a Git repository

```
Error: Not a git repository. Please run this command from within a git repository.
```

**Solution**: Navigate to a directory that is part of a Git repository.

### Git command failed

```
Error: Git command failed. <details>
```

**Solution**: Check that Git is properly installed and the repository is not corrupted.

## Tips

### Clean up missing worktrees

If you see worktrees with `missing` status, clean them up:

```bash
git worktree prune
```

### Automation

Combine with other Unix tools for automation:

```bash
# Count active worktrees
wt list | grep "active" | wc -l

# Find worktrees for a specific branch pattern
wt list | grep "feature-"
```

## See Also

- [create](create.md) - Create a new worktree
- [Quick Start Guide](../guides/quickstart.md) - Learn worktree workflows
