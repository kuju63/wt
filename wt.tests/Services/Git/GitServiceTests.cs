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

    [Fact]
    public async Task ListWorktreesAsync_WithValidWorktrees_ReturnsWorktreeList()
    {
        // Arrange
        var porcelainOutput = @"worktree /path/to/main
HEAD abc123def456
branch refs/heads/main

worktree /path/to/feature-a
HEAD def456789012
branch refs/heads/feature-a

";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "worktree list --porcelain", null, default))
            .ReturnsAsync(new ProcessResult(0, porcelainOutput, ""));

        // Act
        var result = await _gitService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(2);
        result.Data[0].Path.ShouldBe("/path/to/main");
        result.Data[0].Branch.ShouldBe("main");
        result.Data[1].Path.ShouldBe("/path/to/feature-a");
        result.Data[1].Branch.ShouldBe("feature-a");
    }

    [Fact]
    public async Task ListWorktreesAsync_WithInvalidWorktreeLine_IgnoresMalformedEntry()
    {
        // Arrange - Line with "worktree " prefix but too short
        var porcelainOutput = @"worktree
HEAD abc123def456
branch refs/heads/main

worktree /path/to/valid
HEAD def456789012
branch refs/heads/feature

";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "worktree list --porcelain", null, default))
            .ReturnsAsync(new ProcessResult(0, porcelainOutput, ""));

        // Act
        var result = await _gitService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(1);
        result.Data[0].Path.ShouldBe("/path/to/valid");
    }

    [Fact]
    public async Task ListWorktreesAsync_WithInvalidHeadLine_IgnoresInvalidHead()
    {
        // Arrange - Line with "HEAD " prefix but too short
        var porcelainOutput = @"worktree /path/to/test
HEAD
branch refs/heads/main

worktree /path/to/valid
HEAD def456789012
branch refs/heads/feature

";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "worktree list --porcelain", null, default))
            .ReturnsAsync(new ProcessResult(0, porcelainOutput, ""));

        // Act
        var result = await _gitService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(1);
        result.Data[0].Path.ShouldBe("/path/to/valid");
    }

    [Fact]
    public async Task ListWorktreesAsync_WithInvalidBranchLine_IgnoresInvalidBranch()
    {
        // Arrange - Line with "branch " prefix but too short
        var porcelainOutput = @"worktree /path/to/test
HEAD abc123def456
branch

worktree /path/to/valid
HEAD def456789012
branch refs/heads/feature

";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "worktree list --porcelain", null, default))
            .ReturnsAsync(new ProcessResult(0, porcelainOutput, ""));

        // Act
        var result = await _gitService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(2);
        // First worktree should use HEAD as branch since branch line was invalid
        result.Data[0].Branch.ShouldBe("abc123def456");
        result.Data[1].Branch.ShouldBe("feature");
    }

    [Fact]
    public async Task ListWorktreesAsync_WithDetachedHead_ReturnsDetachedWorktree()
    {
        // Arrange
        var porcelainOutput = @"worktree /path/to/detached
HEAD abc123def456
detached

";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "worktree list --porcelain", null, default))
            .ReturnsAsync(new ProcessResult(0, porcelainOutput, ""));

        // Act
        var result = await _gitService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(1);
        result.Data[0].IsDetached.ShouldBeTrue();
    }
}
