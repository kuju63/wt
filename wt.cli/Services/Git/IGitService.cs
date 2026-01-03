using wt.cli.Models;

namespace wt.cli.Services.Git;

public interface IGitService
{
    /// <summary>
    /// Check if the current directory is a Git repository
    /// </summary>
    Task<CommandResult<bool>> IsGitRepositoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current branch name
    /// </summary>
    Task<CommandResult<string>> GetCurrentBranchAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a branch exists (local or remote)
    /// </summary>
    Task<CommandResult<bool>> BranchExistsAsync(string branchName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new branch from a base branch
    /// </summary>
    Task<CommandResult<BranchInfo>> CreateBranchAsync(string branchName, string baseBranch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new worktree
    /// </summary>
    Task<CommandResult<bool>> AddWorktreeAsync(string path, string branchName, CancellationToken cancellationToken = default);
}
