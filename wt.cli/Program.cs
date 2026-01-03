using System.CommandLine;
using System.IO.Abstractions;
using wt.cli.Commands.Worktree;
using wt.cli.Services.Editor;
using wt.cli.Services.Git;
using wt.cli.Services.Worktree;
using wt.cli.Utils;

// Setup Dependency Injection
var fileSystem = new FileSystem();
var processRunner = new ProcessRunner();
var pathHelper = new PathHelper(fileSystem);
var gitService = new GitService(processRunner);
var editorService = new EditorService(processRunner);
var worktreeService = new WorktreeService(gitService, pathHelper, editorService);

// Setup root command
var rootCommand = new RootCommand("Git worktree management CLI tool");

// Add commands
var createCommand = new CreateCommand(worktreeService);
rootCommand.Subcommands.Add(createCommand);

// Parse and execute
ParseResult parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
