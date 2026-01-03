using System.CommandLine;
using wt.cli.Models;
using wt.cli.Services.Worktree;

namespace wt.cli.Commands.Worktree;

public class CreateCommand : Command
{
    public CreateCommand(IWorktreeService worktreeService)
        : base("create", "Create a new worktree with a new branch")
    {
        // Arguments
        var branchArgument = new Argument<string>("branch", "Name of the branch to create");

        // Options
        var baseOption = new Option<string?>(new[] { "--base", "-b" }, "Base branch to branch from (default: current branch)");
        var pathOption = new Option<string?>(new[] { "--path", "-p" }, "Path where the worktree will be created (default: ../wt-<branch>)");
        var editorOption = new Option<EditorType?>(new[] { "--editor", "-e" }, "Editor to launch after creating worktree");
        var outputOption = new Option<OutputFormat>(new[] { "--output" }, () => OutputFormat.Human, "Output format (human or json)");
        var verboseOption = new Option<bool>(new[] { "--verbose", "-v" }, () => false, "Show detailed diagnostic information");

        // Add
        this.Add(branchArgument);
        this.Add(baseOption);
        this.Add(pathOption);
        this.Add(editorOption);
        this.Add(outputOption);
        this.Add(verboseOption);

        // Handler
        this.SetHandler(async (branch, baseBranch, path, editor, output, verbose) =>
        {
            var options = new CreateWorktreeOptions
            {
                BranchName = branch,
                BaseBranch = baseBranch,
                WorktreePath = path,
                EditorType = editor,
                OutputFormat = output,
                Verbose = verbose
            };

            var result = await worktreeService.CreateWorktreeAsync(options);

            if (result.IsSuccess)
            {
                DisplaySuccess(result.Data!, output);
            }
            else
            {
                DisplayError(result, verbose);
                Environment.ExitCode = 1;
            }
        }, branchArgument, baseOption, pathOption, editorOption, outputOption, verboseOption);
    }

    private static void DisplaySuccess(WorktreeInfo worktreeInfo, OutputFormat format)
    {
        if (format == OutputFormat.Json)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                success = true,
                worktree = new
                {
                    path = worktreeInfo.Path,
                    branch = worktreeInfo.Branch,
                    baseBranch = worktreeInfo.BaseBranch,
                    createdAt = worktreeInfo.CreatedAt
                }
            }, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            Console.WriteLine(json);
        }
        else
        {
            Console.WriteLine($"✓ Worktree created successfully");
            Console.WriteLine($"  Path:   {worktreeInfo.Path}");
            Console.WriteLine($"  Branch: {worktreeInfo.Branch}");
            Console.WriteLine($"  Base:   {worktreeInfo.BaseBranch}");
        }
    }

    private static void DisplayError(CommandResult<WorktreeInfo> result, bool verbose)
    {
        Console.Error.WriteLine($"✗ {result.ErrorMessage}");

        if (!string.IsNullOrEmpty(result.Solution))
        {
            Console.Error.WriteLine($"  Solution: {result.Solution}");
        }

        if (verbose && !string.IsNullOrEmpty(result.ErrorCode))
        {
            Console.Error.WriteLine($"  Error Code: {result.ErrorCode}");
        }
    }
}
