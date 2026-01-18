# API Reference

Welcome to the **wt** API reference documentation. This section provides comprehensive documentation for all public interfaces, classes, and methods in the wt library.

## Overview

The wt library is organized into the following namespaces:

- **Kuju63.WorkTree.CommandLine.Commands** - Command implementations for worktree operations
- **Kuju63.WorkTree.CommandLine.Models** - Data models and result types
- **Kuju63.WorkTree.CommandLine.Services** - Core services for Git, worktree, and editor operations
- **Kuju63.WorkTree.CommandLine.Formatters** - Output formatting utilities
- **Kuju63.WorkTree.CommandLine.Utils** - Utility classes and helpers

## Getting Started with the API

While wt is primarily a CLI tool, its underlying API can be integrated into your own .NET applications for programmatic worktree management.

### Basic Integration Example

```csharp
using System.IO.Abstractions;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Kuju63.WorkTree.CommandLine.Utils;
using Kuju63.WorkTree.CommandLine.Models;

// Setup dependencies
var fileSystem = new FileSystem();
var processRunner = new ProcessRunner();
var pathHelper = new PathHelper(fileSystem);
var gitService = new GitService(processRunner);
var worktreeService = new WorktreeService(gitService, pathHelper, null);

// Create a worktree
var options = new CreateWorktreeOptions
{
    BranchName = "feature-branch",
    BaseBranch = "main",
    WorktreePath = null, // Uses default path
    OutputFormat = OutputFormat.Human,
    Verbose = false
};

var result = await worktreeService.CreateWorktreeAsync(options, CancellationToken.None);

if (result.IsSuccess)
{
    Console.WriteLine($"Worktree created at: {result.Data.Path}");
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

### List Worktrees Example

```csharp
// List all worktrees
var listResult = await worktreeService.ListWorktreesAsync(CancellationToken.None);

if (listResult.IsSuccess)
{
    foreach (var worktree in listResult.Data)
    {
        Console.WriteLine($"{worktree.Path} - {worktree.Branch} ({worktree.Status})");
    }
}
```

## Key Interfaces

### IWorktreeService

The primary interface for worktree operations. Provides methods for creating and listing worktrees.

**Methods:**

- `ListWorktreesAsync` - Lists all worktrees in the repository

### IGitService

Low-level Git operations interface. Provides direct access to Git commands.

**Methods:**

- `IsGitRepositoryAsync` - Checks if the current directory is a Git repository
- `BranchExistsAsync` - Checks if a branch exists
- `GetCurrentBranchAsync` - Gets the currently checked-out branch name
- `CreateBranchAsync` - Creates a new Git branch
- `CreateWorktreeAsync` - Creates a worktree using git worktree add

### IEditorService

Editor integration interface for launching editors after worktree creation.

**Methods:**

- `LaunchEditorAsync` - Launches the specified editor with the given path

## Models

### CommandResult\<T\>

Generic result type for command operations. Provides success/failure status, data, error information, and solutions.

**Properties:**

- `IsSuccess` - Indicates if the operation was successful
- `Data` - The result data (when successful)
- `ErrorCode` - Error code for debugging
- `ErrorMessage` - Human-readable error message
- `Solution` - Suggested solution for the error
- `Warnings` - List of non-critical warnings

### WorktreeInfo

Represents information about a Git worktree.

**Properties:**

- `Path` - File system path to the worktree
- `Branch` - Branch name checked out in the worktree
- `Commit` - Current commit hash
- `Status` - Worktree status (active, missing, etc.)
- `Exists` - Whether the worktree directory exists on disk

### CreateWorktreeOptions

Options for creating a new worktree.

**Properties:**

- `BranchName` - Name of the new branch to create (required)
- `BaseBranch` - Base branch to branch from (optional)
- `WorktreePath` - Custom path for the worktree (optional)
- `EditorType` - Editor to launch after creation (optional)
- `OutputFormat` - Output format (Human or Json)
- `Verbose` - Enable verbose diagnostic output

## Error Handling

All service methods return `CommandResult<T>` which includes comprehensive error information:

```csharp
var result = await worktreeService.CreateWorktreeAsync(options, cancellationToken);

if (!result.IsSuccess)
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
    Console.WriteLine($"Error Code: {result.ErrorCode}");
    
    if (!string.IsNullOrEmpty(result.Solution))
    {
        Console.WriteLine($"Solution: {result.Solution}");
    }
}
```

## Browse the API

Use the navigation on the left to explore the complete API documentation, including:

- Detailed class and interface documentation
- Method signatures and parameters
- Property descriptions
- Code examples (where available)
- Inheritance hierarchies
- Implementation details

## Support

For questions about the API or integration:

- [GitHub Issues](https://github.com/kuju63/wt/issues) - Report bugs or request features
- [Contributing Guide](../contributing.md) - Contribute to the project

---

**Note**: This API is primarily designed for internal use by the wt CLI. While it can be used in your applications, the API surface may change between versions. For stable integrations, consider using the CLI as a subprocess or waiting for an official SDK release.
