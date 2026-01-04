using System.CommandLine;
using Kuju63.WorkTree.CommandLine.Formatters;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Worktree;

namespace Kuju63.WorkTree.CommandLine.Commands;

/// <summary>
/// Command for listing all Git worktrees with their branch information.
/// </summary>
public class ListCommand : Command
{
    private readonly IWorktreeService _worktreeService;
    private readonly IOutputFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListCommand"/> class.
    /// </summary>
    /// <param name="worktreeService">The worktree service for retrieving worktree information.</param>
    /// <param name="formatter">The output formatter for displaying worktree information.</param>
    public ListCommand(IWorktreeService worktreeService, IOutputFormatter formatter)
        : base("list", "List all worktrees with their branch information")
    {
        _worktreeService = worktreeService;
        _formatter = formatter;

        this.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ExecuteAsync(parseResult, cancellationToken);
        });
    }

    private async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Get worktree list
        var result = await _worktreeService.ListWorktreesAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            // Handle different error types
            if (result.ErrorCode == ErrorCodes.NotGitRepository)
            {
                await parseResult.InvocationConfiguration.Error.WriteLineAsync("Error: Not a git repository. Please run this command from within a git repository.");
                return 2;
            }
            else if (result.ErrorCode == ErrorCodes.GitCommandFailed)
            {
                await parseResult.InvocationConfiguration.Error.WriteLineAsync($"Error: Git command failed. {result.ErrorMessage}");
                return 10;
            }
            else
            {
                await parseResult.InvocationConfiguration.Error.WriteLineAsync($"Error: {result.ErrorMessage ?? "Unexpected error occurred"}");
                return 99;
            }
        }

        var worktrees = result.Data!;

        // Check if any worktrees exist
        if (worktrees.Count == 0)
        {
            parseResult.InvocationConfiguration.Output.WriteLine("No worktrees found in this repository.");
            return 0;
        }

        // Check for missing worktrees and output warnings
        var missingWorktrees = worktrees.Where(w => !w.Exists).ToList();
        foreach (var missing in missingWorktrees)
        {
            await parseResult.InvocationConfiguration.Error.WriteLineAsync($"Warning: Worktree at '{missing.Path}' does not exist on disk");
        }

        // Format and output the table
        var output = _formatter.Format(worktrees);
        parseResult.InvocationConfiguration.Output.WriteLine(output);

        return 0;
    }
}
