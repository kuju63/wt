using Kuju63.WorkTree.CommandLine.Models;

namespace Kuju63.WorkTree.CommandLine.Services.Worktree;

/// <summary>
/// Defines methods for creating and managing Git worktrees.
/// </summary>
public interface IWorktreeService
{
    /// <summary>
    /// Creates a new worktree with the specified options asynchronously.
    /// </summary>
    /// <param name="options">The options for creating the worktree.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the created worktree information.</returns>
    /// <summary>
    /// Creates a new worktree with the specified options asynchronously.
    /// </summary>
    /// <param name="options">The options for creating the worktree.</param>
    /// <returns>A <see cref="CommandResult{WorktreeInfo}"/> containing the created worktree information.</returns>
    Task<CommandResult<WorktreeInfo>> CreateWorktreeAsync(CreateWorktreeOptions options);

    /// <summary>
    /// Creates a new worktree with the specified options asynchronously.
    /// </summary>
    /// <param name="options">The options for creating the worktree.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{WorktreeInfo}"/> containing the created worktree information.</returns>
    Task<CommandResult<WorktreeInfo>> CreateWorktreeAsync(CreateWorktreeOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all worktrees in the repository asynchronously.
    /// </summary>
    /// <returns>A <see cref="CommandResult{T}"/> containing a list of worktree information, sorted by creation date (newest first).</returns>
    Task<CommandResult<List<WorktreeInfo>>> ListWorktreesAsync();

    /// <summary>
    /// Lists all worktrees in the repository asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing a list of worktree information, sorted by creation date (newest first).</returns>
    Task<CommandResult<List<WorktreeInfo>>> ListWorktreesAsync(CancellationToken cancellationToken);
}
