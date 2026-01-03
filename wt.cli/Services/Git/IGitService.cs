using Kuju63.WorkTree.CommandLine.Models;

namespace Kuju63.WorkTree.CommandLine.Services.Git;

/// <summary>
/// Defines methods for interacting with Git repositories.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Checks whether the current directory is a Git repository asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if in a Git repository; otherwise, <see langword="false"/>.</returns>
    /// <summary>
    /// Checks whether the current directory is a Git repository asynchronously.
    /// </summary>
    /// <returns>A <see cref="CommandResult{bool}"/> containing <see langword="true"/> if in a Git repository; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> IsGitRepositoryAsync();

    /// <summary>
    /// Checks whether the current directory is a Git repository asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{bool}"/> containing <see langword="true"/> if in a Git repository; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> IsGitRepositoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the name of the current branch asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the current branch name.</returns>
    /// <summary>
    /// Gets the name of the current branch asynchronously.
    /// </summary>
    /// <returns>A <see cref="CommandResult{string}"/> containing the current branch name.</returns>
    Task<CommandResult<string>> GetCurrentBranchAsync();

    /// <summary>
    /// Gets the name of the current branch asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{string}"/> containing the current branch name.</returns>
    Task<CommandResult<string>> GetCurrentBranchAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a branch exists (local or remote) asynchronously.
    /// </summary>
    /// <param name="branchName">The name of the branch to check.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if the branch exists; otherwise, <see langword="false"/>.</returns>
    /// <summary>
    /// Checks whether a branch exists (local or remote) asynchronously.
    /// </summary>
    /// <param name="branchName">The name of the branch to check.</param>
    /// <returns>A <see cref="CommandResult{bool}"/> containing <see langword="true"/> if the branch exists; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> BranchExistsAsync(string branchName);

    /// <summary>
    /// Checks whether a branch exists (local or remote) asynchronously.
    /// </summary>
    /// <param name="branchName">The name of the branch to check.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{bool}"/> containing <see langword="true"/> if the branch exists; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> BranchExistsAsync(string branchName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new branch from a base branch asynchronously.
    /// </summary>
    /// <param name="branchName">The name of the branch to create.</param>
    /// <param name="baseBranch">The base branch to branch from.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing information about the created branch.</returns>
    /// <summary>
    /// Creates a new branch from a base branch asynchronously.
    /// </summary>
    /// <param name="branchName">The name of the branch to create.</param>
    /// <param name="baseBranch">The base branch to branch from.</param>
    /// <returns>A <see cref="CommandResult{BranchInfo}"/> containing information about the created branch.</returns>
    Task<CommandResult<BranchInfo>> CreateBranchAsync(string branchName, string baseBranch);

    /// <summary>
    /// Creates a new branch from a base branch asynchronously.
    /// </summary>
    /// <param name="branchName">The name of the branch to create.</param>
    /// <param name="baseBranch">The base branch to branch from.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{BranchInfo}"/> containing information about the created branch.</returns>
    Task<CommandResult<BranchInfo>> CreateBranchAsync(string branchName, string baseBranch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new worktree at the specified path asynchronously.
    /// </summary>
    /// <param name="path">The path where the worktree should be created.</param>
    /// <param name="branchName">The branch to checkout in the new worktree.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if the worktree was added successfully; otherwise, <see langword="false"/>.</returns>
    /// <summary>
    /// Adds a new worktree at the specified path asynchronously.
    /// </summary>
    /// <param name="path">The path where the worktree should be created.</param>
    /// <param name="branchName">The branch to checkout in the new worktree.</param>
    /// <returns>A <see cref="CommandResult{bool}"/> containing <see langword="true"/> if the worktree was added successfully; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> AddWorktreeAsync(string path, string branchName);

    /// <summary>
    /// Adds a new worktree at the specified path asynchronously.
    /// </summary>
    /// <param name="path">The path where the worktree should be created.</param>
    /// <param name="branchName">The branch to checkout in the new worktree.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{bool}"/> containing <see langword="true"/> if the worktree was added successfully; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> AddWorktreeAsync(string path, string branchName, CancellationToken cancellationToken = default);
}
