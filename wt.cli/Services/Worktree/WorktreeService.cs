using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Editor;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Utils;

namespace Kuju63.WorkTree.CommandLine.Services.Worktree;

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

        // Get base branch (use current branch if not specified)
        var baseBranch = options.BaseBranch;
        if (string.IsNullOrEmpty(baseBranch))
        {
            var currentBranchResult = await _gitService.GetCurrentBranchAsync(cancellationToken);
            if (!currentBranchResult.IsSuccess)
            {
                return CommandResult<WorktreeInfo>.Failure(
                    currentBranchResult.ErrorCode!,
                    currentBranchResult.ErrorMessage!,
                    currentBranchResult.Solution);
            }
            baseBranch = currentBranchResult.Data;
        }

        // Check if branch already exists
        var branchExistsResult = await _gitService.BranchExistsAsync(options.BranchName, cancellationToken);
        if (!branchExistsResult.IsSuccess)
        {
            return CommandResult<WorktreeInfo>.Failure(
                branchExistsResult.ErrorCode!,
                branchExistsResult.ErrorMessage!,
                branchExistsResult.Solution);
        }

        if (branchExistsResult.Data)
        {
            return CommandResult<WorktreeInfo>.Failure(
                ErrorCodes.BranchAlreadyExists,
                $"Branch '{options.BranchName}' already exists",
                ErrorCodes.GetSolution(ErrorCodes.BranchAlreadyExists));
        }

        // Create branch
        var createBranchResult = await _gitService.CreateBranchAsync(options.BranchName, baseBranch!, cancellationToken);
        if (!createBranchResult.IsSuccess)
        {
            return CommandResult<WorktreeInfo>.Failure(
                createBranchResult.ErrorCode!,
                createBranchResult.ErrorMessage!,
                createBranchResult.Solution);
        }

        // Determine worktree path
        var worktreePath = options.WorktreePath ?? $"../wt-{options.BranchName}";
        var resolvedPath = _pathHelper.ResolvePath(worktreePath, Environment.CurrentDirectory);
        var normalizedPath = _pathHelper.NormalizePath(resolvedPath);

        // Validate path
        var pathValidation = _pathHelper.ValidatePath(normalizedPath);
        if (!pathValidation.IsValid)
        {
            return CommandResult<WorktreeInfo>.Failure(
                ErrorCodes.InvalidPath,
                "Invalid worktree path",
                pathValidation.ErrorMessage);
        }

        // Ensure parent directory exists (git worktree add requires parent to exist)
        try
        {
            _pathHelper.EnsureParentDirectoryExists(normalizedPath);
        }
        catch (Exception ex)
        {
            return CommandResult<WorktreeInfo>.Failure(
                ErrorCodes.InvalidPath,
                "Failed to create parent directory",
                ex.Message);
        }

        // Add worktree
        var addWorktreeResult = await _gitService.AddWorktreeAsync(normalizedPath, options.BranchName, cancellationToken);
        if (!addWorktreeResult.IsSuccess)
        {
            return CommandResult<WorktreeInfo>.Failure(
                addWorktreeResult.ErrorCode!,
                addWorktreeResult.ErrorMessage!,
                addWorktreeResult.Solution);
        }

        // Create WorktreeInfo
        var worktreeInfo = new WorktreeInfo(
            normalizedPath,
            options.BranchName,
            baseBranch!,
            DateTime.UtcNow);

        // Launch editor if specified
        if (options.EditorType.HasValue && _editorService != null)
        {
            var editorResult = await _editorService.LaunchEditorAsync(
                normalizedPath,
                options.EditorType.Value,
                cancellationToken);

            // Editor launch failure is a warning, not an error
            // The worktree was created successfully
            if (!editorResult.IsSuccess)
            {
                return CommandResult<WorktreeInfo>.Success(
                    worktreeInfo,
                    new List<string> { $"Warning: {editorResult.ErrorMessage ?? "Failed to launch editor"}" });
            }
        }

        return CommandResult<WorktreeInfo>.Success(worktreeInfo);
    }
}
