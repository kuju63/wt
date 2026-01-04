using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Utils;

namespace Kuju63.WorkTree.CommandLine.Services.Git;

/// <summary>
/// Provides functionality for interacting with Git repositories.
/// </summary>
public class GitService : IGitService
{
    private readonly IProcessRunner _processRunner;

    public GitService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    /// <summary>
    /// Checks whether the current directory is a Git repository asynchronously.
    /// This overload does not accept a cancellation token.
    /// </summary>
    /// <returns>A <see cref="CommandResult{bool}"/> containing <see langword="true"/> if in a Git repository; otherwise, <see langword="false"/>.</returns>
    public Task<CommandResult<bool>> IsGitRepositoryAsync()
        => IsGitRepositoryAsync(CancellationToken.None);

    /// <summary>
    /// Checks whether the current directory is a Git repository asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{bool}"/> containing <see langword="true"/> if in a Git repository; otherwise, <see langword="false"/>.</returns>
    public async Task<CommandResult<bool>> IsGitRepositoryAsync(CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync("git", "rev-parse --git-dir", null, cancellationToken);
        return CommandResult<bool>.Success(result.ExitCode == 0);
    }

    /// <summary>
    /// Gets the name of the current branch asynchronously. This overload does not accept a cancellation token.
    /// </summary>
    /// <returns>A <see cref="CommandResult{string}"/> containing the current branch name.</returns>
    public Task<CommandResult<string>> GetCurrentBranchAsync()
        => GetCurrentBranchAsync(CancellationToken.None);

    /// <summary>
    /// Gets the name of the current branch asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{string}"/> containing the current branch name.</returns>
    public async Task<CommandResult<string>> GetCurrentBranchAsync(CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync("git", "branch --show-current", null, cancellationToken);

        if (result.ExitCode != 0)
        {
            return CommandResult<string>.Failure(
                ErrorCodes.GitCommandFailed,
                "Failed to get current branch",
                result.StandardError);
        }

        var branchName = result.StandardOutput.Trim();
        if (string.IsNullOrEmpty(branchName))
        {
            return CommandResult<string>.Failure(
                ErrorCodes.GitCommandFailed,
                "Currently in detached HEAD state",
                "Cannot create worktree from detached HEAD. Please checkout a branch first.");
        }

        return CommandResult<string>.Success(branchName);
    }

    /// <summary>
    /// Checks whether a branch exists (local or remote) asynchronously. This overload does not accept a cancellation token.
    /// </summary>
    /// <param name="branchName">The name of the branch to check.</param>
    /// <returns>A <see cref="CommandResult{bool}"/> containing <see langword="true"/> if the branch exists; otherwise, <see langword="false"/>.</returns>
    public Task<CommandResult<bool>> BranchExistsAsync(string branchName)
        => BranchExistsAsync(branchName, CancellationToken.None);

    /// <summary>
    /// Checks whether a branch exists (local or remote) asynchronously.
    /// </summary>
    /// <param name="branchName">The name of the branch to check.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{bool}"/> containing <see langword="true"/> if the branch exists; otherwise, <see langword="false"/>.</returns>
    public async Task<CommandResult<bool>> BranchExistsAsync(string branchName, CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync("git", $"rev-parse --verify {branchName}", null, cancellationToken);
        return CommandResult<bool>.Success(result.ExitCode == 0);
    }

    /// <summary>
    /// Creates a new branch from a base branch asynchronously. This overload does not accept a cancellation token.
    /// </summary>
    /// <param name="branchName">The name of the branch to create.</param>
    /// <param name="baseBranch">The base branch to branch from.</param>
    /// <returns>A <see cref="CommandResult{BranchInfo}"/> containing information about the created branch.</returns>
    public Task<CommandResult<BranchInfo>> CreateBranchAsync(string branchName, string baseBranch)
        => CreateBranchAsync(branchName, baseBranch, CancellationToken.None);

    /// <summary>
    /// Creates a new branch from a base branch asynchronously.
    /// </summary>
    /// <param name="branchName">The name of the branch to create.</param>
    /// <param name="baseBranch">The base branch to branch from.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{BranchInfo}"/> containing information about the created branch.</returns>
    public async Task<CommandResult<BranchInfo>> CreateBranchAsync(string branchName, string baseBranch, CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync("git", $"branch {branchName} {baseBranch}", null, cancellationToken);

        if (result.ExitCode != 0)
        {
            if (result.StandardError.Contains("already exists"))
            {
                return CommandResult<BranchInfo>.Failure(
                    ErrorCodes.BranchAlreadyExists,
                    $"Branch '{branchName}' already exists",
                    result.StandardError);
            }

            return CommandResult<BranchInfo>.Failure(
                ErrorCodes.GitCommandFailed,
                "Failed to create branch",
                result.StandardError);
        }

        var branchInfo = new BranchInfo(branchName, baseBranch, true, false);
        return CommandResult<BranchInfo>.Success(branchInfo);
    }

    /// <summary>
    /// Adds a new worktree at the specified path asynchronously. This overload does not accept a cancellation token.
    /// </summary>
    /// <param name="path">The path where the worktree should be created.</param>
    /// <param name="branchName">The branch to checkout in the new worktree.</param>
    /// <returns>A <see cref="CommandResult{bool}"/> containing <see langword="true"/> if the worktree was added successfully; otherwise, <see langword="false"/>.</returns>
    public Task<CommandResult<bool>> AddWorktreeAsync(string path, string branchName)
        => AddWorktreeAsync(path, branchName, CancellationToken.None);

    /// <summary>
    /// Adds a new worktree at the specified path asynchronously.
    /// </summary>
    /// <param name="path">The path where the worktree should be created.</param>
    /// <param name="branchName">The branch to checkout in the new worktree.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{bool}"/> containing <see langword="true"/> if the worktree was added successfully; otherwise, <see langword="false"/>.</returns>
    public async Task<CommandResult<bool>> AddWorktreeAsync(string path, string branchName, CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync("git", $"worktree add \"{path}\" {branchName}", null, cancellationToken);

        if (result.ExitCode != 0)
        {
            if (result.StandardError.Contains("already exists"))
            {
                return CommandResult<bool>.Failure(
                    ErrorCodes.WorktreeAlreadyExists,
                    $"Worktree at '{path}' already exists",
                    result.StandardError);
            }

            if (result.StandardError.Contains("is already checked out"))
            {
                return CommandResult<bool>.Failure(
                    ErrorCodes.BranchAlreadyInUse,
                    $"Branch '{branchName}' is already checked out in another worktree",
                    result.StandardError);
            }

            return CommandResult<bool>.Failure(
                ErrorCodes.GitCommandFailed,
                "Failed to add worktree",
                result.StandardError);
        }

        return CommandResult<bool>.Success(true);
    }
}
