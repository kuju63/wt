using System.Text;
using Kuju63.WorkTree.CommandLine.Models;

namespace Kuju63.WorkTree.CommandLine.Formatters;

/// <summary>
/// Formats worktree information as a table with Unicode box-drawing characters.
/// </summary>
public class TableFormatter : IOutputFormatter
{
    private static class BoxDrawing
    {
        public const char TopLeft = '┌';
        public const char TopRight = '┐';
        public const char BottomLeft = '└';
        public const char BottomRight = '┘';
        public const char Horizontal = '─';
        public const char Vertical = '│';
        public const char TopJunction = '┬';
        public const char BottomJunction = '┴';
        public const char MiddleJunction = '┼';
        public const char LeftJunction = '├';
        public const char RightJunction = '┤';
    }

    private readonly TableColumn[] _columns;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableFormatter"/> class with default columns.
    /// </summary>
    public TableFormatter()
        : this(GetDefaultColumns())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableFormatter"/> class with custom columns.
    /// </summary>
    /// <param name="columns">The columns to display in the table.</param>
    public TableFormatter(params TableColumn[] columns)
    {
        _columns = columns ?? throw new ArgumentNullException(nameof(columns));
        if (_columns.Length == 0)
        {
            throw new ArgumentException("At least one column is required.", nameof(columns));
        }
    }

    private static TableColumn[] GetDefaultColumns()
    {
        return new[]
        {
            new TableColumn("Path", w => w.Path),
            new TableColumn("Branch", w => w.GetDisplayBranch()),
            new TableColumn("Status", w => w.GetDisplayStatus())
        };
    }

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

        // Pre-calculate all cell values in a single pass to avoid redundant calls
        var rows = worktreeList.Select(w =>
            _columns.Select(col => col.ValueSelector(w)).ToArray()
        ).ToList();

        // Calculate column widths
        var columnWidths = new int[_columns.Length];
        for (int i = 0; i < _columns.Length; i++)
        {
            columnWidths[i] = Math.Max(
                _columns[i].Header.Length,
                rows.Max(row => row[i].Length)
            );
        }

        var sb = new StringBuilder();

        // Top border: ┌─┬─┬─┐
        AppendHorizontalBorder(sb, BoxDrawing.TopLeft, BoxDrawing.TopJunction, BoxDrawing.TopRight, columnWidths);

        // Header row: │ Path │ Branch │ Status │
        AppendRow(sb, _columns.Select(c => c.Header).ToArray(), columnWidths);

        // Separator line: ├─┼─┼─┤
        AppendHorizontalBorder(sb, BoxDrawing.LeftJunction, BoxDrawing.MiddleJunction, BoxDrawing.RightJunction, columnWidths);

        // Data rows
        foreach (var row in rows)
        {
            AppendRow(sb, row, columnWidths);
        }

        // Bottom border: └─┴─┴─┘
        AppendHorizontalBorder(sb, BoxDrawing.BottomLeft, BoxDrawing.BottomJunction, BoxDrawing.BottomRight, columnWidths);

        return sb.ToString();
    }

    private void AppendHorizontalBorder(StringBuilder sb, char left, char junction, char right, int[] columnWidths)
    {
        sb.Append(left);
        for (int i = 0; i < columnWidths.Length; i++)
        {
            sb.Append(BoxDrawing.Horizontal, columnWidths[i] + 2);
            if (i < columnWidths.Length - 1)
            {
                sb.Append(junction);
            }
        }
        sb.Append(right);
        sb.AppendLine();
    }

    private void AppendRow(StringBuilder sb, string[] values, int[] columnWidths)
    {
        sb.Append(BoxDrawing.Vertical);
        sb.Append(' ');
        for (int i = 0; i < values.Length; i++)
        {
            sb.Append(values[i].PadRight(columnWidths[i]));
            sb.Append(' ');
            sb.Append(BoxDrawing.Vertical);
            if (i < values.Length - 1)
            {
                sb.Append(' ');
            }
        }
        sb.AppendLine();
    }
}
