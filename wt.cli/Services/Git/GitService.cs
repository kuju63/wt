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

    /// <summary>
    /// Lists all worktrees in the repository asynchronously. This overload does not accept a cancellation token.
    /// </summary>
    /// <returns>A <see cref="CommandResult{T}"/> containing a list of worktree information.</returns>
    public Task<CommandResult<List<WorktreeInfo>>> ListWorktreesAsync()
        => ListWorktreesAsync(CancellationToken.None);

    /// <summary>
    /// Lists all worktrees in the repository asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing a list of worktree information.</returns>
    public async Task<CommandResult<List<WorktreeInfo>>> ListWorktreesAsync(CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync("git", "worktree list --porcelain", null, cancellationToken);

        if (result.ExitCode != 0)
        {
            return CommandResult<List<WorktreeInfo>>.Failure(
                ErrorCodes.GitCommandFailed,
                "Failed to list worktrees",
                result.StandardError);
        }

        var worktrees = ParseWorktreesFromPorcelain(result.StandardOutput);
        return CommandResult<List<WorktreeInfo>>.Success(worktrees);
    }

    private List<WorktreeInfo> ParseWorktreesFromPorcelain(string porcelainOutput)
    {
        var worktrees = new List<WorktreeInfo>();
        var lines = porcelainOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var currentWorktree = new WorktreeData();

        foreach (var line in lines)
        {
            if (line.StartsWith("worktree "))
            {
                AddWorktreeIfValid(worktrees, currentWorktree);
                currentWorktree = ParseWorktreeLine(line);
            }
            else
            {
                ParseWorktreeAttribute(line, ref currentWorktree);
            }
        }

        AddWorktreeIfValid(worktrees, currentWorktree);
        return worktrees;
    }

    private WorktreeData ParseWorktreeLine(string line)
    {
        if (string.IsNullOrEmpty(line) || line.Length <= 9)
        {
            return new WorktreeData();
        }

        var rawPath = line.Substring(9);
        // Normalize the path for consistent comparisons.
        // Note: Path.GetFullPath resolves relative segments, and on Windows it also
        // resolves certain symlinks/junctions. On Unix-like systems it does not fully
        // resolve symlinks (e.g., it may leave /var instead of /private/var on macOS).
        // Tests or callers that use GetRealPath will typically see more fully resolved
        // paths than this method returns, so any comparisons should account for this.
        var normalizedPath = Path.GetFullPath(rawPath);

        return new WorktreeData
        {
            Path = normalizedPath
        };
    }

    private void ParseWorktreeAttribute(string line, ref WorktreeData worktree)
    {
        if (line.StartsWith("HEAD "))
        {
            if (line.Length >= 5)
            {
                worktree.Head = line.Substring(5);
            }
        }
        else if (line.StartsWith("branch "))
        {
            if (line.Length >= 7)
            {
                worktree.Branch = NormalizeBranchName(line.Substring(7));
            }
        }
        else if (line.Trim() == "detached")
        {
            worktree.IsDetached = true;
        }
    }

    private string NormalizeBranchName(string branch)
    {
        return branch.StartsWith("refs/heads/") ? branch.Substring(11) : branch;
    }

    private void AddWorktreeIfValid(List<WorktreeInfo> worktrees, WorktreeData data)
    {
        if (data.Path != null && data.Head != null)
        {
            worktrees.Add(CreateWorktreeInfo(
                data.Path,
                data.Branch ?? data.Head,
                data.IsDetached,
                data.Head));
        }
    }

    private class WorktreeData
    {
        public string? Path { get; set; }
        public string? Head { get; set; }
        public string? Branch { get; set; }
        public bool IsDetached { get; set; }
    }

    private WorktreeInfo CreateWorktreeInfo(string path, string branch, bool isDetached, string commitHash)
    {
        // Get creation time from .git/worktrees/<name>/gitdir file
        var createdAt = DateTime.Now; // Default value
        var exists = Directory.Exists(path);

        try
        {
            // Resolve the actual git directory, handling the case where .git is a file pointing elsewhere
            var gitDir = ".git";
            if (File.Exists(gitDir))
            {
                try
                {
                    var lines = File.ReadAllLines(gitDir);
                    if (lines.Length > 0)
                    {
                        var firstLine = lines[0];
                        const string prefix = "gitdir:";
                        if (firstLine.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            var gitDirPath = firstLine.Substring(prefix.Length).Trim();
                            if (!string.IsNullOrWhiteSpace(gitDirPath))
                            {
                                // Resolve to absolute path
                                if (!Path.IsPathRooted(gitDirPath))
                                {
                                    gitDirPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), gitDirPath));
                                }
                                else
                                {
                                    gitDirPath = Path.GetFullPath(gitDirPath);
                                }

                                // Security: Validate that the resolved path points to a valid git directory
                                // This helps prevent path traversal attacks from malicious .git files
                                if (Directory.Exists(gitDirPath) && 
                                    (Directory.Exists(Path.Combine(gitDirPath, "worktrees")) || 
                                     File.Exists(Path.Combine(gitDirPath, "config"))))
                                {
                                    gitDir = gitDirPath;
                                }
                            }
                        }
                    }
                }
                catch (IOException ex)
                {
                    System.Console.Error.WriteLine($"[GitService] Error reading .git file: {ex.Message}");
                    // Continue with default ".git" value
                }
                catch (UnauthorizedAccessException ex)
                {
                    System.Console.Error.WriteLine($"[GitService] Access denied reading .git file: {ex.Message}");
                    // Continue with default ".git" value
                }
            }

            // Get worktree name from path
            var worktreeName = Path.GetFileName(path);
            var gitWorktreePath = Path.Combine(gitDir, "worktrees", worktreeName, "gitdir");

            if (File.Exists(gitWorktreePath))
            {
                createdAt = File.GetCreationTime(gitWorktreePath);
            }
        }
        catch (System.IO.IOException ex)
        {
            System.Console.Error.WriteLine($"[GitService] Error reading creation time for worktree at '{path}': {ex.Message}");
        }
        catch (System.UnauthorizedAccessException ex)
        {
            System.Console.Error.WriteLine($"[GitService] Access denied when reading creation time for worktree at '{path}': {ex.Message}");
        }

        return new WorktreeInfo(path, branch, isDetached, commitHash, createdAt, exists);
    }
}

