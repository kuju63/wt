using System.Text;
using Kuju63.WorkTree.CommandLine.Models;

namespace Kuju63.WorkTree.CommandLine.Formatters;

/// <summary>
/// Formats worktree information as a table with Unicode box-drawing characters.
/// </summary>
public class TableFormatter : IOutputFormatter
{
    /// <summary>
    /// Formats a collection of worktree information into a table string.
    /// </summary>
    /// <param name="worktrees">The collection of worktree information to format.</param>
    /// <returns>A formatted table string with headers, separators, and rows.</returns>
    public string Format(IEnumerable<WorktreeInfo> worktrees)
    {
        var worktreeList = worktrees.ToList();
        if (!worktreeList.Any())
        {
            return string.Empty;
        }

        // Column headers
        const string headerPath = "Path";
        const string headerBranch = "Branch";
        const string headerStatus = "Status";

        // Calculate column widths based on content
        var pathWidth = Math.Max(headerPath.Length, worktreeList.Max(w => w.Path.Length));
        var branchWidth = Math.Max(headerBranch.Length, worktreeList.Max(w => w.GetDisplayBranch().Length));
        var statusWidth = Math.Max(headerStatus.Length, worktreeList.Max(w => w.GetDisplayStatus().Length));

        var sb = new StringBuilder();

        // Top border: ┌─┬─┬─┐
        sb.Append('┌');
        sb.Append('─', pathWidth + 2);
        sb.Append('┬');
        sb.Append('─', branchWidth + 2);
        sb.Append('┬');
        sb.Append('─', statusWidth + 2);
        sb.AppendLine("┐");

        // Header row: │ Path │ Branch │ Status │
        sb.Append("│ ");
        sb.Append(headerPath.PadRight(pathWidth));
        sb.Append(" │ ");
        sb.Append(headerBranch.PadRight(branchWidth));
        sb.Append(" │ ");
        sb.Append(headerStatus.PadRight(statusWidth));
        sb.AppendLine(" │");

        // Separator line: ├─┼─┼─┤
        sb.Append('├');
        sb.Append('─', pathWidth + 2);
        sb.Append('┼');
        sb.Append('─', branchWidth + 2);
        sb.Append('┼');
        sb.Append('─', statusWidth + 2);
        sb.AppendLine("┤");

        // Data rows
        foreach (var worktree in worktreeList)
        {
            sb.Append("│ ");
            sb.Append(worktree.Path.PadRight(pathWidth));
            sb.Append(" │ ");
            sb.Append(worktree.GetDisplayBranch().PadRight(branchWidth));
            sb.Append(" │ ");
            sb.Append(worktree.GetDisplayStatus().PadRight(statusWidth));
            sb.AppendLine(" │");
        }

        // Bottom border: └─┴─┴─┘
        sb.Append('└');
        sb.Append('─', pathWidth + 2);
        sb.Append('┴');
        sb.Append('─', branchWidth + 2);
        sb.Append('┴');
        sb.Append('─', statusWidth + 2);
        sb.Append("┘");

        return sb.ToString();
    }
}
