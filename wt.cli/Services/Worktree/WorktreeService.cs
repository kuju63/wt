using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Editor;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Utils;

namespace Kuju63.WorkTree.CommandLine.Services.Worktree;

/// <summary>
/// Represents a unit type (void) for generic command results.
/// </summary>
internal sealed class Unit
{
    /// <summary>
    /// Gets the singleton instance of Unit.
    /// </summary>
    public static Unit Value => new();

    private Unit()
    {
    }
}

/// <summary>
/// Represents the result of path preparation.
/// </summary>
internal sealed class PathPrepareResult
{
    /// <summary>
    /// Gets a value indicating whether the path is valid.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the error message if the path is invalid.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the prepared path if valid.
    /// </summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>
    /// Creates an invalid result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>An invalid PathPrepareResult.</returns>
    public static PathPrepareResult Invalid(string errorMessage)
        => new() { IsValid = false, ErrorMessage = errorMessage };

    /// <summary>
    /// Creates a valid result with a path.
    /// </summary>
    /// <param name="path">The prepared path.</param>
    /// <returns>A valid PathPrepareResult.</returns>
    public static PathPrepareResult Valid(string path)
        => new() { IsValid = true, Path = path };
}

/// <summary>
/// Provides functionality for creating and managing Git worktrees.
/// </summary>
public class WorktreeService : IWorktreeService
{
    private readonly IGitService _gitService;
    private readonly IPathHelper _pathHelper;
    private readonly IEditorService? _editorService;

    public WorktreeService(IGitService gitService, IPathHelper pathHelper, IEditorService? editorService)
    {
        _gitService = gitService;
        _pathHelper = pathHelper;
        _editorService = editorService;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="WorktreeService"/> without an <see cref="IEditorService"/>.
    /// </summary>
    /// <param name="gitService">The <see cref="IGitService"/> instance.</param>
    /// <param name="pathHelper">The <see cref="IPathHelper"/> instance.</param>
    public WorktreeService(IGitService gitService, IPathHelper pathHelper)
        : this(gitService, pathHelper, null)
    {
    }

    /// <summary>
    /// Creates a new worktree with the specified options. This overload does not accept a cancellation token.
    /// </summary>
    /// <param name="options">The options for creating the worktree.</param>
    /// <returns>A <see cref="CommandResult{WorktreeInfo}"/> representing the result.</returns>
    public Task<CommandResult<WorktreeInfo>> CreateWorktreeAsync(CreateWorktreeOptions options)
        => CreateWorktreeAsync(options, CancellationToken.None);

    /// <summary>
    /// Creates a new worktree with the specified options asynchronously.
    /// </summary>
    /// <param name="options">The options for creating the worktree.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{WorktreeInfo}"/> representing the result.</returns>
    public async Task<CommandResult<WorktreeInfo>> CreateWorktreeAsync(CreateWorktreeOptions options, CancellationToken cancellationToken)
    {
        // Validate options
        var validationResult = options.Validate();
        if (!validationResult.IsValid)
        {
            return CommandResult<WorktreeInfo>.Failure(
                ErrorCodes.InvalidBranchName,
                "Invalid options",
                validationResult.ErrorMessage);
        }

        // Check if in Git repository
        var isGitRepoResult = await _gitService.IsGitRepositoryAsync(cancellationToken);
        if (!isGitRepoResult.IsSuccess || !isGitRepoResult.Data)
        {
            return CommandResult<WorktreeInfo>.Failure(
                ErrorCodes.NotGitRepository,
                "Not in a Git repository",
                ErrorCodes.GetSolution(ErrorCodes.NotGitRepository));
        }

        return await CreateWorktreeInternalAsync(options, cancellationToken);
    }

    private async Task<CommandResult<WorktreeInfo>> CreateWorktreeInternalAsync(CreateWorktreeOptions options, CancellationToken cancellationToken)
    {
        var baseBranchResult = await GetBaseBranchAsync(options, cancellationToken);
        if (!baseBranchResult.IsSuccess)
        {
            return ToWorktreeFailure(baseBranchResult);
        }

        var branchResult = await EnsureBranchExistsAsync(options.BranchName, baseBranchResult.Data!, cancellationToken);
        if (!branchResult.IsSuccess)
        {
            return ToWorktreeFailure(branchResult);
        }

        var pathResult = PrepareWorktreePath(options);
        if (!pathResult.IsValid)
        {
            return CommandResult<WorktreeInfo>.Failure(ErrorCodes.InvalidPath, "Invalid worktree path", pathResult.ErrorMessage);
        }

        var addWorktreeResult = await _gitService.AddWorktreeAsync(pathResult.Path, options.BranchName, cancellationToken);
        if (!addWorktreeResult.IsSuccess)
        {
            return CommandResult<WorktreeInfo>.Failure(addWorktreeResult.ErrorCode!, addWorktreeResult.ErrorMessage!, addWorktreeResult.Solution);
        }

        return await CreateAndLaunchWorktreeAsync(options, pathResult.Path, baseBranchResult.Data!, cancellationToken);
    }

    private CommandResult<WorktreeInfo> ToWorktreeFailure<T>(CommandResult<T> result)
    {
        return CommandResult<WorktreeInfo>.Failure(
            result.ErrorCode ?? ErrorCodes.GitCommandFailed,
            result.ErrorMessage ?? "Operation failed",
            result.Solution);
    }

    private async Task<CommandResult<WorktreeInfo>> CreateAndLaunchWorktreeAsync(
        CreateWorktreeOptions options,
        string normalizedPath,
        string baseBranch,
        CancellationToken cancellationToken)
    {
        var worktreeInfo = new WorktreeInfo(
            normalizedPath,
            options.BranchName,
            baseBranch,
            DateTime.UtcNow);

        return await LaunchEditorIfSpecifiedAsync(options, worktreeInfo, cancellationToken);
    }

    private async Task<CommandResult<string>> GetBaseBranchAsync(CreateWorktreeOptions options, CancellationToken cancellationToken)
    {
        var baseBranch = options.BaseBranch;
        if (!string.IsNullOrEmpty(baseBranch))
        {
            return CommandResult<string>.Success(baseBranch);
        }

        var currentBranchResult = await _gitService.GetCurrentBranchAsync(cancellationToken);
        if (!currentBranchResult.IsSuccess)
        {
            return CommandResult<string>.Failure(
                currentBranchResult.ErrorCode!,
                currentBranchResult.ErrorMessage!,
                currentBranchResult.Solution);
        }

        return CommandResult<string>.Success(currentBranchResult.Data ?? string.Empty);
    }

    private async Task<CommandResult<Unit>> EnsureBranchExistsAsync(string branchName, string baseBranch, CancellationToken cancellationToken)
    {
        var branchExistsResult = await _gitService.BranchExistsAsync(branchName, cancellationToken);
        if (!branchExistsResult.IsSuccess)
        {
            return CommandResult<Unit>.Failure(
                branchExistsResult.ErrorCode!,
                branchExistsResult.ErrorMessage!,
                branchExistsResult.Solution);
        }

        if (branchExistsResult.Data)
        {
            return CommandResult<Unit>.Failure(
                ErrorCodes.BranchAlreadyExists,
                $"Branch '{branchName}' already exists",
                ErrorCodes.GetSolution(ErrorCodes.BranchAlreadyExists));
        }

        var createBranchResult = await _gitService.CreateBranchAsync(branchName, baseBranch, cancellationToken);
        if (!createBranchResult.IsSuccess)
        {
            return CommandResult<Unit>.Failure(
                createBranchResult.ErrorCode!,
                createBranchResult.ErrorMessage!,
                createBranchResult.Solution);
        }

        return CommandResult<Unit>.Success(Unit.Value);
    }

    private PathPrepareResult PrepareWorktreePath(CreateWorktreeOptions options)
    {
        var worktreePath = options.WorktreePath ?? $"../wt-{options.BranchName}";
        var resolvedPath = _pathHelper.ResolvePath(worktreePath, Environment.CurrentDirectory);
        var normalizedPath = _pathHelper.NormalizePath(resolvedPath);

        var pathValidation = _pathHelper.ValidatePath(normalizedPath);
        if (!pathValidation.IsValid)
        {
            return PathPrepareResult.Invalid(pathValidation.ErrorMessage ?? "Invalid path");
        }

        try
        {
            _pathHelper.EnsureParentDirectoryExists(normalizedPath);
        }
        catch (Exception ex)
        {
            return PathPrepareResult.Invalid(ex.Message ?? "Failed to create parent directory");
        }

        return PathPrepareResult.Valid(normalizedPath);
    }

    private async Task<CommandResult<WorktreeInfo>> LaunchEditorIfSpecifiedAsync(
        CreateWorktreeOptions options,
        WorktreeInfo worktreeInfo,
        CancellationToken cancellationToken)
    {
        if (!options.EditorType.HasValue || _editorService == null)
        {
            return CommandResult<WorktreeInfo>.Success(worktreeInfo);
        }

        var editorResult = await _editorService.LaunchEditorAsync(
            worktreeInfo.Path,
            options.EditorType.Value,
            cancellationToken);

        if (!editorResult.IsSuccess)
        {
            return CommandResult<WorktreeInfo>.Success(
                worktreeInfo,
                new List<string> { $"Warning: {editorResult.ErrorMessage ?? "Failed to launch editor"}" });
        }

        return CommandResult<WorktreeInfo>.Success(worktreeInfo);
    }
}
