# wt - Git Worktree Manager

A modern CLI tool to simplify Git worktree management. Create worktrees with a single command and optionally launch your favorite editor.

## Features

- ✨ **Simple worktree creation**: `wt create feature-branch`
- 🎯 **Smart defaults**: Automatically creates worktrees in `../wt-<branch>` directory
- 🚀 **Editor integration**: Auto-launch VS Code, Vim, Emacs, or IntelliJ IDEA
- 🛠️ **Custom paths**: Specify where to create worktrees
- 📋 **Multiple output formats**: Human-readable or JSON for automation
- ✅ **Cross-platform**: Works on macOS, Linux, and Windows

## Installation

### Prerequisites

- .NET 10.0 SDK or later
- Git 2.5 or later

### Build from source

```bash
git clone https://github.com/yourusername/wt.git
cd wt
dotnet build
dotnet run --project wt.cli -- create --help
```

### Install globally (optional)

```bash
dotnet pack
dotnet tool install --global --add-source ./nupkg wt
```

## Quick Start

### Basic Usage

Create a new worktree with a new branch:

```bash
wt create feature-login
```

This will:
1. Create a new branch named `feature-login` from your current branch
2. Create a worktree at `../wt-feature-login`
3. Check out the new branch in the worktree

### Specify Base Branch

Create a branch from a specific base branch:

```bash
wt create feature-login --base main
```

### Custom Path

Create a worktree at a custom location:

```bash
wt create feature-login --path ~/projects/myapp-feature-login
```

### Auto-Launch Editor

Create a worktree and automatically open it in your editor:

```bash
wt create feature-login --editor vscode
```

Supported editors:
- `vscode` - Visual Studio Code
- `vim` - Vim
- `emacs` - Emacs
- `nano` - Nano
- `idea` - IntelliJ IDEA

### JSON Output

For automation and scripting:

```bash
wt create feature-login --output json
```

### Verbose Mode

Show detailed diagnostic information:

```bash
wt create feature-login --verbose
```

## Command Reference

### `wt create <branch> [options]`

Create a new worktree with a new branch.

**Arguments:**
- `<branch>` - Name of the branch to create (required)

**Options:**
- `-b, --base <base>` - Base branch to branch from (default: current branch)
- `-p, --path <path>` - Path where the worktree will be created (default: `../wt-<branch>`)
- `-e, --editor <type>` - Editor to launch after creating worktree (choices: vscode, vim, emacs, nano, idea)
- `--output <format>` - Output format: human or json (default: human)
- `-v, --verbose` - Show detailed diagnostic information
- `-h, --help` - Show help and usage information

## Examples

### Example 1: Simple Feature Branch

```bash
wt create feature-auth
```

Output:
```
✓ Created branch: feature-auth
✓ Created worktree: /Users/username/projects/wt-feature-auth
✓ Checked out: feature-auth
```

### Example 2: Bug Fix with Custom Path

```bash
wt create bugfix-123 --base main --path ~/bugfixes/fix-123
```

### Example 3: With Editor Launch

```bash
wt create feature-ui --editor vscode
```

This will create the worktree and automatically open VS Code in the new worktree directory.

### Example 4: Automation with JSON

```bash
wt create feature-api --output json | jq '.worktree.path'
```

Output:
```json
{
  "success": true,
  "worktree": {
    "path": "/Users/username/projects/wt-feature-api",
    "branch": "feature-api",
    "baseBranch": "main",
    "createdAt": "2026-01-03T12:34:56Z"
  }
}
```

## Troubleshooting

### Error: Not a git repository

**Solution**: Run `wt` from within a Git repository directory.

```bash
cd /path/to/your/git/repo
wt create my-branch
```

### Error: Branch already exists

**Solution**: Use a different branch name or delete the existing branch first.

```bash
git branch -d existing-branch
wt create existing-branch
```

### Error: Path already exists

**Solution**: Specify a different path or remove the existing directory.

```bash
wt create my-branch --path ~/different/path
```

### Error: Editor not found

**Solution**: Ensure the editor is installed and available in your PATH.

```bash
# For VS Code on macOS
code --version

# For Vim
vim --version
```

## Development

### Project Structure

```
wt/
├── wt.cli/              # CLI application
│   ├── Commands/        # Command implementations
│   ├── Models/          # Data models
│   ├── Services/        # Business logic
│   └── Utils/           # Helper utilities
├── wt.tests/            # Unit and integration tests
└── specs/               # Feature specifications
```

### Running Tests

```bash
dotnet test
```

### Test Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Contributing

Contributions are welcome! Please follow the [development guidelines](specs/001-create-worktree/quickstart.md).

## License

[MIT License](LICENSE)

## Acknowledgments

Built with:
- [System.CommandLine](https://github.com/dotnet/command-line-api) - Modern command-line parsing
- [xUnit](https://xunit.net/) - Testing framework
- [FluentAssertions](https://fluentassertions.com/) - Fluent assertion library
