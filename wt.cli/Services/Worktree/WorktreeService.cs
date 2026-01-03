using wt.cli.Models;
using wt.cli.Services.Git;
using wt.cli.Utils;

namespace wt.cli.Services.Worktree;

public class WorktreeService : IWorktreeService
{
    private readonly IGitService _gitService;
    private readonly IPathHelper _pathHelper;

    public WorktreeService(IGitService gitService, IPathHelper pathHelper)
    {
        _gitService = gitService;
        _pathHelper = pathHelper;
    }

    public async Task<CommandResult<WorktreeInfo>> CreateWorktreeAsync(CreateWorktreeOptions options, CancellationToken cancellationToken = default)
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

        return CommandResult<WorktreeInfo>.Success(worktreeInfo);
    }
}
