using System.IO.Abstractions;
using FluentAssertions;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Kuju63.WorkTree.CommandLine.Utils;
using Xunit;

namespace Kuju63.WorkTree.Tests.Integration;

[Collection("Sequential Integration Tests")]
public class CustomPathTests : IDisposable
{
    private readonly string _testRepoPath;
    private readonly string _customWorktreePath;
    private readonly string _relativeWorktreePath;
    private readonly string _originalDirectory;

    public CustomPathTests()
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

        _originalDirectory = Environment.CurrentDirectory;
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"test-repo-{Guid.NewGuid()}");
        _customWorktreePath = Path.Combine(Path.GetTempPath(), $"custom-worktree-{Guid.NewGuid()}");
        _relativeWorktreePath = Path.Combine(_testRepoPath, "..", $"relative-worktree-{Guid.NewGuid()}");

        // Create test repository
        Directory.CreateDirectory(_testRepoPath);
        var processRunner = new ProcessRunner();

        // Initialize git repo
        processRunner.RunAsync("git", "init", _testRepoPath).Wait();
        processRunner.RunAsync("git", "config user.email \"test@example.com\"", _testRepoPath).Wait();
        processRunner.RunAsync("git", "config user.name \"Test User\"", _testRepoPath).Wait();
        processRunner.RunAsync("git", "checkout -b main", _testRepoPath).Wait(); // Ensure we're on main branch

        // Create initial commit
        var readmePath = Path.Combine(_testRepoPath, "README.md");
        File.WriteAllText(readmePath, "# Test Repository");
        processRunner.RunAsync("git", "add README.md", _testRepoPath).Wait();
        processRunner.RunAsync("git", "commit -m \"Initial commit\"", _testRepoPath).Wait();
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

        // Now safe to delete test directories
        try
        {
            if (Directory.Exists(_testRepoPath))
            {
                Directory.Delete(_testRepoPath, true);
            }
        }
        catch { }

        try
        {
            if (Directory.Exists(_customWorktreePath))
            {
                Directory.Delete(_customWorktreePath, true);
            }
        }
        catch { }

        try
        {
            if (Directory.Exists(_relativeWorktreePath))
            {
                Directory.Delete(_relativeWorktreePath, true);
            }
        }
        catch { }
    }

    [Fact]
    public async Task CreateWorktree_WithAbsolutePath_ShouldCreateWorktreeAtSpecifiedPath()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var fileSystem = new FileSystem();
        var pathHelper = new PathHelper(fileSystem);
        var gitService = new GitService(processRunner);
        var worktreeService = new WorktreeService(gitService, pathHelper);

        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-absolute",
            WorktreePath = _customWorktreePath
        };

        // Change to test repo directory
        var originalDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _testRepoPath;

        try
        {
            // Act
            var result = await worktreeService.CreateWorktreeAsync(options);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Path.Should().Be(_customWorktreePath);
            Directory.Exists(_customWorktreePath).Should().BeTrue();
        }
        finally
        {
            try { if (Directory.Exists(originalDir)) Environment.CurrentDirectory = originalDir; else Environment.CurrentDirectory = Path.GetTempPath(); } catch { Environment.CurrentDirectory = Path.GetTempPath(); }
        }
    }

    [Fact]
    public async Task CreateWorktree_WithRelativePath_ShouldCreateWorktreeAtSpecifiedPath()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var fileSystem = new FileSystem();
        var pathHelper = new PathHelper(fileSystem);
        var gitService = new GitService(processRunner);
        var worktreeService = new WorktreeService(gitService, pathHelper);

        var relativePath = $"../relative-worktree-{Guid.NewGuid()}";
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-relative",
            WorktreePath = relativePath
        };

        // Change to test repo directory
        var originalDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _testRepoPath;

        try
        {
            // Act
            var result = await worktreeService.CreateWorktreeAsync(options);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // Verify worktree directory exists
            Directory.Exists(result.Data!.Path).Should().BeTrue();

            // Verify the directory name matches the expected pattern
            var directoryName = Path.GetFileName(result.Data.Path);
            directoryName.Should().StartWith("relative-worktree-");

            // Verify git worktree was created (should have .git file)
            var gitFile = Path.Combine(result.Data.Path, ".git");
            File.Exists(gitFile).Should().BeTrue("worktree should have .git file");

            // Clean up
            if (Directory.Exists(result.Data.Path))
            {
                Directory.Delete(result.Data.Path, true);
            }
        }
        finally
        {
            try { if (Directory.Exists(originalDir)) Environment.CurrentDirectory = originalDir; else Environment.CurrentDirectory = Path.GetTempPath(); } catch { Environment.CurrentDirectory = Path.GetTempPath(); }
        }
    }

    [Fact]
    public async Task CreateWorktree_WithInvalidPath_ShouldReturnError()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var fileSystem = new FileSystem();
        var pathHelper = new PathHelper(fileSystem);
        var gitService = new GitService(processRunner);
        var worktreeService = new WorktreeService(gitService, pathHelper);

        var invalidPath = "/nonexistent/parent/directory/worktree";
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-invalid",
            WorktreePath = invalidPath
        };

        // Change to test repo directory
        var originalDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _testRepoPath;

        try
        {
            // Act
            var result = await worktreeService.CreateWorktreeAsync(options);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be(ErrorCodes.InvalidPath);
        }
        finally
        {
            try { if (Directory.Exists(originalDir)) Environment.CurrentDirectory = originalDir; else Environment.CurrentDirectory = Path.GetTempPath(); } catch { Environment.CurrentDirectory = Path.GetTempPath(); }
        }
    }

    [Fact]
    public async Task CreateWorktree_WithExistingDirectory_ShouldReturnError()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var fileSystem = new FileSystem();
        var pathHelper = new PathHelper(fileSystem);
        var gitService = new GitService(processRunner);
        var worktreeService = new WorktreeService(gitService, pathHelper);

        // Create existing directory
        Directory.CreateDirectory(_customWorktreePath);

        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-existing",
            WorktreePath = _customWorktreePath
        };

        // Change to test repo directory
        var originalDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _testRepoPath;

        try
        {
            // Act
            var result = await worktreeService.CreateWorktreeAsync(options);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be(ErrorCodes.InvalidPath);
        }
        finally
        {
            try { if (Directory.Exists(originalDir)) Environment.CurrentDirectory = originalDir; else Environment.CurrentDirectory = Path.GetTempPath(); } catch { Environment.CurrentDirectory = Path.GetTempPath(); }
        }
    }

    [Fact]
    public async Task CreateWorktree_WithPathContainingSpaces_ShouldCreateSuccessfully()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var fileSystem = new FileSystem();
        var pathHelper = new PathHelper(fileSystem);
        var gitService = new GitService(processRunner);
        var worktreeService = new WorktreeService(gitService, pathHelper);

        var pathWithSpaces = Path.Combine(Path.GetTempPath(), $"path with spaces-{Guid.NewGuid()}");
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-spaces",
            WorktreePath = pathWithSpaces
        };

        // Change to test repo directory
        var originalDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _testRepoPath;

        try
        {
            // Act
            var result = await worktreeService.CreateWorktreeAsync(options);

            // Assert
            result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.ErrorMessage}");
            result.Data.Should().NotBeNull();
            result.Data!.Path.Should().Be(pathWithSpaces);
            Directory.Exists(pathWithSpaces).Should().BeTrue();

            // Clean up
            if (Directory.Exists(pathWithSpaces))
            {
                Directory.Delete(pathWithSpaces, true);
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(originalDir))
                {
                    try { if (Directory.Exists(originalDir)) Environment.CurrentDirectory = originalDir; else Environment.CurrentDirectory = Path.GetTempPath(); } catch { Environment.CurrentDirectory = Path.GetTempPath(); }
                }
                else
                {
                    Environment.CurrentDirectory = Path.GetTempPath();
                }
            }
            catch
            {
                Environment.CurrentDirectory = Path.GetTempPath();
            }
        }
    }
}
