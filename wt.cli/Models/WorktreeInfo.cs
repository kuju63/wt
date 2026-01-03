namespace wt.cli.Models;

/// <summary>
/// Worktree情報を表すレコード
/// </summary>
public record WorktreeInfo(
    string Path,
    string Branch,
    string BaseBranch,
    DateTime CreatedAt
);
