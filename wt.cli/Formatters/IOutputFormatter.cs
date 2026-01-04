using Kuju63.WorkTree.CommandLine.Models;

namespace Kuju63.WorkTree.CommandLine.Formatters;

/// <summary>
/// Defines methods for formatting worktree output in different formats.
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Formats a collection of worktree information into a string.
    /// </summary>
    /// <param name="worktrees">The collection of worktree information to format.</param>
    /// <returns>A formatted string representation of the worktrees.</returns>
    string Format(IEnumerable<WorktreeInfo> worktrees);
}
