using System.CommandLine;
using FluentAssertions;
using Kuju63.WorkTree.CommandLine.Commands.Worktree;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Kuju63.WorkTree.CommandLine.Utils;
using Xunit;

namespace Kuju63.WorkTree.Tests.Integration;

[Collection("Sequential Integration Tests")]
public class WorktreeE2ETests : IDisposable
{
    private readonly string _testRepoPath;
    private readonly string _originalDirectory;

    public WorktreeE2ETests()
    {
        // Ensure we start from a valid directory
        try
        {
            var currentDir = Environment.CurrentDirectory;
            if (!Directory.Exists(currentDir))
            {
                Environment.CurrentDirectory = Path.GetTempPath();
            }
        }
        catch
        {
            Environment.CurrentDirectory = Path.GetTempPath();
        }

        // Save original directory
        _originalDirectory = Environment.CurrentDirectory;

        // Create temporary test repository
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"wt-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRepoPath);

        // Initialize git repository
        RunGitCommand("init");
        RunGitCommand("config user.email \"test@example.com\"");
        RunGitCommand("config user.name \"Test User\"");
        RunGitCommand("checkout -b main"); // Ensure we're on main branch

        // Create initial commit
        var readmePath = Path.Combine(_testRepoPath, "README.md");
        File.WriteAllText(readmePath, "# Test Repository\n");
        RunGitCommand("add README.md");
        RunGitCommand("commit -m \"Initial commit\"");
    }

    public void Dispose()
    {
        // Restore original directory first
        try
        {
            if (Directory.Exists(_originalDirectory))
            {
                Environment.CurrentDirectory = _originalDirectory;
            }
        }
        catch
        {
            // If original directory no longer exists, change to temp
            Environment.CurrentDirectory = Path.GetTempPath();
        }

        // Cleanup: Remove all worktrees first
        try
        {
            Environment.CurrentDirectory = _testRepoPath;
            var worktreesOutput = RunGitCommand("worktree list --porcelain");
            var lines = worktreesOutput.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("worktree "))
                {
                    var path = line.Substring("worktree ".Length).Trim();
                    if (path != _testRepoPath && Directory.Exists(path))
                    {
                        try
                        {
                            RunGitCommand($"worktree remove \"{path}\" --force");
                        }
                        catch { }
                    }
                }
            }

            // Delete branches
            var branchesOutput = RunGitCommand("branch --format=\"%(refname:short)\"");
            var branches = branchesOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var branch in branches)
            {
                if (branch.Trim() != "main" && branch.Trim() != "master")
                {
                    try
                    {
                        RunGitCommand($"branch -D \"{branch.Trim()}\"");
                    }
                    catch { }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        // Delete worktree directories that may not have been removed
        try
        {
            var parentDir = Path.GetDirectoryName(_testRepoPath);
            if (parentDir != null && Directory.Exists(parentDir))
            {
                var worktreeDirs = Directory.GetDirectories(parentDir, "wt-*");
                foreach (var dir in worktreeDirs)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch { }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        // Delete test repository
        try
        {
            if (Directory.Exists(_testRepoPath))
            {
                Directory.Delete(_testRepoPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task E2E_CreateWorktree_WithDefaultPath_CreatesWorktreeSuccessfully()
    {
        // Arrange
        var fileSystem = new System.IO.Abstractions.FileSystem();
        var processRunner = new ProcessRunner();
        var pathHelper = new PathHelper(fileSystem);
        var gitService = new GitService(processRunner);
        var worktreeService = new WorktreeService(gitService, pathHelper);
        var command = new CreateCommand(worktreeService);

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(command);

        var outputWriter = new StringWriter();
        var errorWriter = new StringWriter();
        var invocationConfig = new InvocationConfiguration
        {
            Output = outputWriter,
            Error = errorWriter
        };

        // Change to test repository directory
        var originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testRepoPath);

        try
        {
            // Act
            var parseResult = rootCommand.Parse(new[] { "create", "feature-test" });
            var exitCode = await parseResult.InvokeAsync(invocationConfig);

            // Assert
            if (exitCode != 0)
            {
                var errorOutput = errorWriter.ToString();
                var output = outputWriter.ToString();
                throw new Exception($"Command failed with exit code {exitCode}. Output: {output}\nError: {errorOutput}");
            }
            exitCode.Should().Be(0, "command should succeed");
            outputWriter.ToString().Should().Contain("Worktree created successfully");

            // Verify worktree was created
            var worktreePath = Path.Combine(Path.GetDirectoryName(_testRepoPath)!, "wt-feature-test");
            Directory.Exists(worktreePath).Should().BeTrue("worktree directory should exist");

            // Verify branch was created and checked out
            var gitDirPath = Path.Combine(worktreePath, ".git");
            File.Exists(gitDirPath).Should().BeTrue("worktree should have .git file");

            // Verify branch exists in main repository
            var branchesOutput = RunGitCommand("branch --list feature-test");
            branchesOutput.Should().Contain("feature-test", "branch should be created");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    [Fact]
    public async Task E2E_CreateWorktree_WithBaseBranch_CreatesFromSpecifiedBase()
    {
        // Arrange - Create a base branch first
        RunGitCommand("checkout -b develop");
        var devReadmePath = Path.Combine(_testRepoPath, "DEVELOP.md");
        File.WriteAllText(devReadmePath, "# Development\n");
        RunGitCommand("add DEVELOP.md");
        RunGitCommand("commit -m \"Add develop file\"");
        RunGitCommand("checkout main");

        var fileSystem = new System.IO.Abstractions.FileSystem();
        var processRunner = new ProcessRunner();
        var pathHelper = new PathHelper(fileSystem);
        var gitService = new GitService(processRunner);
        var worktreeService = new WorktreeService(gitService, pathHelper);
        var command = new CreateCommand(worktreeService);

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(command);

        var outputWriter = new StringWriter();
        var errorWriter = new StringWriter();
        var invocationConfig = new InvocationConfiguration
        {
            Output = outputWriter,
            Error = errorWriter
        };

        var originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testRepoPath);

        try
        {
            // Act
            var parseResult = rootCommand.Parse(new[] { "create", "feature-from-dev", "--base", "develop" });
            var exitCode = await parseResult.InvokeAsync(invocationConfig);

            // Assert
            exitCode.Should().Be(0, "command should succeed");

            // Verify worktree was created
            var worktreePath = Path.Combine(Path.GetDirectoryName(_testRepoPath)!, "wt-feature-from-dev");
            Directory.Exists(worktreePath).Should().BeTrue("worktree directory should exist");

            // Verify DEVELOP.md exists in the new worktree (inherited from develop branch)
            var devFileInWorktree = Path.Combine(worktreePath, "DEVELOP.md");
            File.Exists(devFileInWorktree).Should().BeTrue("file from base branch should exist in worktree");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    [Fact]
    public async Task E2E_CreateWorktree_WithCustomPath_CreatesAtSpecifiedLocation()
    {
        // Arrange
        var customPath = Path.Combine(Path.GetTempPath(), $"custom-wt-{Guid.NewGuid()}");

        var fileSystem = new System.IO.Abstractions.FileSystem();
        var processRunner = new ProcessRunner();
        var pathHelper = new PathHelper(fileSystem);
        var gitService = new GitService(processRunner);
        var worktreeService = new WorktreeService(gitService, pathHelper);
        var command = new CreateCommand(worktreeService);

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(command);

        var outputWriter = new StringWriter();
        var errorWriter = new StringWriter();
        var invocationConfig = new InvocationConfiguration
        {
            Output = outputWriter,
            Error = errorWriter
        };

        var originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testRepoPath);

        try
        {
            // Act
            var parseResult = rootCommand.Parse(new[] { "create", "custom-location", "--path", customPath });
            var exitCode = await parseResult.InvokeAsync(invocationConfig);

            // Assert
            exitCode.Should().Be(0, "command should succeed");

            // Verify worktree was created at custom location
            Directory.Exists(customPath).Should().BeTrue("worktree should be created at custom path");

            // Verify it's a valid worktree
            var gitDirPath = Path.Combine(customPath, ".git");
            File.Exists(gitDirPath).Should().BeTrue("worktree should have .git file");

            // Cleanup custom path
            if (Directory.Exists(customPath))
            {
                RunGitCommand($"worktree remove \"{customPath}\" --force");
            }
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    [Fact]
    public async Task E2E_CreateWorktree_WhenBranchExists_ReturnsError()
    {
        // Arrange - Create a branch first
        RunGitCommand("branch existing-branch");

        var fileSystem = new System.IO.Abstractions.FileSystem();
        var processRunner = new ProcessRunner();
        var pathHelper = new PathHelper(fileSystem);
        var gitService = new GitService(processRunner);
        var worktreeService = new WorktreeService(gitService, pathHelper);
        var command = new CreateCommand(worktreeService);

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(command);

        var outputWriter = new StringWriter();
        var errorWriter = new StringWriter();
        var invocationConfig = new InvocationConfiguration
        {
            Output = outputWriter,
            Error = errorWriter
        };

        var originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testRepoPath);

        try
        {
            // Act
            var parseResult = rootCommand.Parse(new[] { "create", "existing-branch" });
            var exitCode = await parseResult.InvokeAsync(invocationConfig);

            // Assert
            exitCode.Should().NotBe(0, "command should fail");
            errorWriter.ToString().Should().Contain("already exists", "error message should indicate branch exists");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    [Fact]
    public async Task E2E_CreateWorktree_OutsideGitRepository_ReturnsError()
    {
        // Arrange - Use a non-git directory
        var nonGitDir = Path.Combine(Path.GetTempPath(), $"non-git-{Guid.NewGuid()}");
        Directory.CreateDirectory(nonGitDir);

        var fileSystem = new System.IO.Abstractions.FileSystem();
        var processRunner = new ProcessRunner();
        var pathHelper = new PathHelper(fileSystem);
        var gitService = new GitService(processRunner);
        var worktreeService = new WorktreeService(gitService, pathHelper);
        var command = new CreateCommand(worktreeService);

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(command);

        var outputWriter = new StringWriter();
        var errorWriter = new StringWriter();
        var invocationConfig = new InvocationConfiguration
        {
            Output = outputWriter,
            Error = errorWriter
        };

        var originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(nonGitDir);

        try
        {
            // Act
            var parseResult = rootCommand.Parse(new[] { "create", "test-branch" });
            var exitCode = await parseResult.InvokeAsync(invocationConfig);

            // Assert
            exitCode.Should().NotBe(0, "command should fail");
            errorWriter.ToString().Should().Contain("Git repository", "error message should mention git repository");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            if (Directory.Exists(nonGitDir))
            {
                Directory.Delete(nonGitDir, true);
            }
        }
    }

    private string RunGitCommand(string arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = _testRepoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start git process");
        }

        process.WaitForExit();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git command failed: {error}");
        }

        return output;
    }
}
