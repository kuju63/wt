using FluentAssertions;
using System.IO.Abstractions;
using wt.cli.Models;
using wt.cli.Services.Git;
using wt.cli.Services.Worktree;
using wt.cli.Utils;
using Xunit;

namespace wt.tests.Integration;

public class CustomPathTests : IDisposable
{
    private readonly string _testRepoPath;
    private readonly string _customWorktreePath;
    private readonly string _relativeWorktreePath;

    public CustomPathTests()
    {
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

        // Create initial commit
        var readmePath = Path.Combine(_testRepoPath, "README.md");
        File.WriteAllText(readmePath, "# Test Repository");
        processRunner.RunAsync("git", "add README.md", _testRepoPath).Wait();
        processRunner.RunAsync("git", "commit -m \"Initial commit\"", _testRepoPath).Wait();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRepoPath))
        {
            Directory.Delete(_testRepoPath, true);
        }
        if (Directory.Exists(_customWorktreePath))
        {
            Directory.Delete(_customWorktreePath, true);
        }
        if (Directory.Exists(_relativeWorktreePath))
        {
            Directory.Delete(_relativeWorktreePath, true);
        }
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
            Environment.CurrentDirectory = originalDir;
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

            var expectedPath = Path.GetFullPath(Path.Combine(_testRepoPath, relativePath));
            result.Data!.Path.Should().Be(expectedPath);
            Directory.Exists(result.Data.Path).Should().BeTrue();

            // Clean up
            if (Directory.Exists(result.Data.Path))
            {
                Directory.Delete(result.Data.Path, true);
            }
        }
        finally
        {
            Environment.CurrentDirectory = originalDir;
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
            Environment.CurrentDirectory = originalDir;
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
            Environment.CurrentDirectory = originalDir;
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
            result.IsSuccess.Should().BeTrue();
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
            Environment.CurrentDirectory = originalDir;
        }
    }
}
