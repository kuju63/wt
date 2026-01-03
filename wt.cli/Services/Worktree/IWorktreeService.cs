using wt.cli.Models;

namespace wt.cli.Services.Worktree;

public interface IWorktreeService
{
    /// <summary>
    /// Create a new worktree with the specified options
    /// </summary>
    Task<CommandResult<WorktreeInfo>> CreateWorktreeAsync(CreateWorktreeOptions options, CancellationToken cancellationToken = default);
}
