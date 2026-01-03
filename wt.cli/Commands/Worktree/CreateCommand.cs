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
        var branchArgument = new Argument<string>("branch")
        {
            Description = "Name of the branch to create"
        };

        // Options
        var baseOption = new Option<string?>("--base", "-b")
        {
            Description = "Base branch to branch from (default: current branch)"
        };
        var pathOption = new Option<string?>("--path", "-p")
        {
            Description = "Path where the worktree will be created (default: ../wt-<branch>)"
        };
        var editorOption = new Option<EditorType?>("--editor", "-e")
        {
            Description = "Editor to launch after creating worktree"
        };
        var outputOption = new Option<OutputFormat>("--output")
        {
            Description = "Output format (human or json)",
            DefaultValueFactory = _ => OutputFormat.Human
        };
        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Show detailed diagnostic information",
            DefaultValueFactory = _ => false
        };

        // Add arguments and options
        this.Arguments.Add(branchArgument);
        this.Options.Add(baseOption);
        this.Options.Add(pathOption);
        this.Options.Add(editorOption);
        this.Options.Add(outputOption);
        this.Options.Add(verboseOption);

        // Set action using System.CommandLine 2.0 API
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            var branch = parseResult.GetValue(branchArgument);
            if (string.IsNullOrEmpty(branch))
            {
                parseResult.InvocationConfiguration.Error.WriteLine("Error: branch argument is required");
                return 1;
            }

            var options = new CreateWorktreeOptions
            {
                BranchName = branch,
                BaseBranch = parseResult.GetValue(baseOption),
                WorktreePath = parseResult.GetValue(pathOption),
                EditorType = parseResult.GetValue(editorOption),
                OutputFormat = parseResult.GetValue(outputOption),
                Verbose = parseResult.GetValue(verboseOption)
            };

            var result = await worktreeService.CreateWorktreeAsync(options, cancellationToken);

            if (result.IsSuccess)
            {
                DisplaySuccess(result.Data!, options.OutputFormat, parseResult.InvocationConfiguration.Output);
                return 0;
            }
            else
            {
                DisplayError(result, options.Verbose, parseResult.InvocationConfiguration.Error);
                return 1;
            }
        });
    }

    private static void DisplaySuccess(WorktreeInfo worktreeInfo, OutputFormat format, TextWriter output)
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
            output.WriteLine(json);
        }
        else
        {
            output.WriteLine($"✓ Worktree created successfully");
            output.WriteLine($"  Path:   {worktreeInfo.Path}");
            output.WriteLine($"  Branch: {worktreeInfo.Branch}");
            output.WriteLine($"  Base:   {worktreeInfo.BaseBranch}");
        }
    }

    private static void DisplayError(CommandResult<WorktreeInfo> result, bool verbose, TextWriter error)
    {
        error.WriteLine($"✗ {result.ErrorMessage}");

        if (!string.IsNullOrEmpty(result.Solution))
        {
            error.WriteLine($"  Solution: {result.Solution}");
        }

        if (verbose && !string.IsNullOrEmpty(result.ErrorCode))
        {
            error.WriteLine($"  Error Code: {result.ErrorCode}");
        }
    }
}
