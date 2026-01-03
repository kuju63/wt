namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Represents information about a Git worktree.
/// </summary>
/// <param name="Path">The file system path of the worktree.</param>
/// <param name="Branch">The branch name associated with this worktree.</param>
/// <param name="BaseBranch">The base branch from which this worktree was created.</param>
/// <param name="CreatedAt">The date and time when the worktree was created.</param>
public record WorktreeInfo(
    string Path,
    string Branch,
    string BaseBranch,
    DateTime CreatedAt
);
