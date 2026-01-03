using System.CommandLine;
using System.IO.Abstractions;
using Kuju63.WorkTree.CommandLine.Commands.Worktree;
using Kuju63.WorkTree.CommandLine.Services.Editor;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Kuju63.WorkTree.CommandLine.Utils;

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
