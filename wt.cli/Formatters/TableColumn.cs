using Kuju63.WorkTree.CommandLine.Models;

namespace Kuju63.WorkTree.CommandLine.Formatters;

/// <summary>
/// Represents a column definition in a table.
/// </summary>
/// <param name="Header">The column header text.</param>
/// <param name="ValueSelector">A function to extract the column value from a WorktreeInfo.</param>
public record TableColumn(string Header, Func<WorktreeInfo, string> ValueSelector);
