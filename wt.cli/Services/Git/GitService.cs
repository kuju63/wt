using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Utils;

namespace Kuju63.WorkTree.CommandLine.Services.Git;

public class GitService : IGitService
{
    private readonly IProcessRunner _processRunner;

    public GitService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task<CommandResult<bool>> IsGitRepositoryAsync(CancellationToken cancellationToken = default)
    {
        var result = await _processRunner.RunAsync("git", "rev-parse --git-dir", null, cancellationToken);
        return CommandResult<bool>.Success(result.ExitCode == 0);
    }

    public async Task<CommandResult<string>> GetCurrentBranchAsync(CancellationToken cancellationToken = default)
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

    public async Task<CommandResult<bool>> BranchExistsAsync(string branchName, CancellationToken cancellationToken = default)
    {
        var result = await _processRunner.RunAsync("git", $"rev-parse --verify {branchName}", null, cancellationToken);
        return CommandResult<bool>.Success(result.ExitCode == 0);
    }

    public async Task<CommandResult<BranchInfo>> CreateBranchAsync(string branchName, string baseBranch, CancellationToken cancellationToken = default)
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

    public async Task<CommandResult<bool>> AddWorktreeAsync(string path, string branchName, CancellationToken cancellationToken = default)
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
