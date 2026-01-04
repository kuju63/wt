using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Utils;
using Moq;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Services.Git;

public class GitServiceTests
{
    private readonly Mock<IProcessRunner> _mockProcessRunner;
    private readonly GitService _gitService;

    public GitServiceTests()
    {
        _mockProcessRunner = new Mock<IProcessRunner>();
        _gitService = new GitService(_mockProcessRunner.Object);
    }

    [Fact]
    public async Task IsGitRepositoryAsync_WhenInGitRepo_ReturnsTrue()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "rev-parse --git-dir", null, default))
            .ReturnsAsync(new ProcessResult(0, ".git", ""));

        // Act
        var result = await _gitService.IsGitRepositoryAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task IsGitRepositoryAsync_WhenNotInGitRepo_ReturnsFalse()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "rev-parse --git-dir", null, default))
            .ReturnsAsync(new ProcessResult(128, "", "fatal: not a git repository"));

        // Act
        var result = await _gitService.IsGitRepositoryAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeFalse();
    }

    [Fact]
    public async Task GetCurrentBranchAsync_WithValidRepo_ReturnsBranchName()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "branch --show-current", null, default))
            .ReturnsAsync(new ProcessResult(0, "main", ""));

        // Act
        var result = await _gitService.GetCurrentBranchAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe("main");
    }

    [Fact]
    public async Task GetCurrentBranchAsync_WhenDetachedHead_ReturnsError()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "branch --show-current", null, default))
            .ReturnsAsync(new ProcessResult(0, "", ""));

        // Act
        var result = await _gitService.GetCurrentBranchAsync();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.GitCommandFailed);
    }

    [Fact]
    public async Task BranchExistsAsync_WhenBranchExists_ReturnsTrue()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "rev-parse --verify feature-x", null, default))
            .ReturnsAsync(new ProcessResult(0, "abc123def", ""));

        // Act
        var result = await _gitService.BranchExistsAsync("feature-x");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task BranchExistsAsync_WhenBranchDoesNotExist_ReturnsFalse()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "rev-parse --verify nonexistent", null, default))
            .ReturnsAsync(new ProcessResult(128, "", "fatal: Needed a single revision"));

        // Act
        var result = await _gitService.BranchExistsAsync("nonexistent");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateBranchAsync_WithValidName_CreatesSuccessfully()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "branch feature-x main", null, default))
            .ReturnsAsync(new ProcessResult(0, "", ""));

        // Act
        var result = await _gitService.CreateBranchAsync("feature-x", "main");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Name.ShouldBe("feature-x");
        result.Data.BaseBranch.ShouldBe("main");
    }

    [Fact]
    public async Task CreateBranchAsync_WhenBranchAlreadyExists_ReturnsError()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "branch feature-x main", null, default))
            .ReturnsAsync(new ProcessResult(128, "", "fatal: A branch named 'feature-x' already exists"));

        // Act
        var result = await _gitService.CreateBranchAsync("feature-x", "main");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.BranchAlreadyExists);
    }

    [Fact]
    public async Task AddWorktreeAsync_WithValidPath_AddsSuccessfully()
    {
        // Arrange
        var worktreePath = "/Users/dev/worktrees/feature-x";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", $"worktree add \"{worktreePath}\" feature-x", null, default))
            .ReturnsAsync(new ProcessResult(0, $"Preparing worktree\nBranch 'feature-x' set up", ""));

        // Act
        var result = await _gitService.AddWorktreeAsync(worktreePath, "feature-x");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task AddWorktreeAsync_WhenWorktreeAlreadyExists_ReturnsError()
    {
        // Arrange
        var worktreePath = "/Users/dev/worktrees/feature-x";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", $"worktree add \"{worktreePath}\" feature-x", null, default))
            .ReturnsAsync(new ProcessResult(128, "", "fatal: '/Users/dev/worktrees/feature-x' already exists"));

        // Act
        var result = await _gitService.AddWorktreeAsync(worktreePath, "feature-x");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.WorktreeAlreadyExists);
    }
}
