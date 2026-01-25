using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Kuju63.WorkTree.CommandLine.Utils;
using Moq;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Services.Worktree;

/// <summary>
/// Tests for WorktreeService remove functionality.
/// </summary>
public class WorktreeServiceRemoveTests
{
    private readonly Mock<IGitService> _mockGitService;
    private readonly Mock<IPathHelper> _mockPathHelper;
    private readonly MockFileSystem _mockFileSystem;
    private readonly WorktreeService _worktreeService;

    public WorktreeServiceRemoveTests()
    {
        _mockGitService = new Mock<IGitService>();
        _mockPathHelper = new Mock<IPathHelper>();
        _mockFileSystem = new MockFileSystem();
        _worktreeService = new WorktreeService(_mockGitService.Object, _mockPathHelper.Object);
    }

    #region ValidateForRemovalAsync Tests

    [Fact]
    public async Task ValidateForRemovalAsync_WhenWorktreeNotFound_ReturnsNotFound()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new("/repo", "main", false, "abc123", DateTime.UtcNow, true)
        };
        _mockGitService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));

        // Act
        var result = await _worktreeService.ValidateForRemovalAsync("non-existent", false);

        // Assert
        result.ShouldBe(RemovalValidationError.NotFound);
    }

    [Fact]
    public async Task ValidateForRemovalAsync_WhenMainWorktree_ReturnsIsMainWorktree()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new("/repo", "main", false, "abc123", DateTime.UtcNow, true)
        };
        _mockGitService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));

        // Act
        var result = await _worktreeService.ValidateForRemovalAsync("main", false);

        // Assert
        result.ShouldBe(RemovalValidationError.IsMainWorktree);
    }

    [Fact]
    public async Task ValidateForRemovalAsync_WhenCurrentWorktree_ReturnsIsCurrentWorktree()
    {
        // Arrange
        var currentDir = Environment.CurrentDirectory;
        var worktrees = new List<WorktreeInfo>
        {
            new("/repo", "main", false, "abc123", DateTime.UtcNow, true),
            new(currentDir, "feature", false, "def456", DateTime.UtcNow, true)
        };
        _mockGitService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));

        // Act
        var result = await _worktreeService.ValidateForRemovalAsync("feature", false);

        // Assert
        result.ShouldBe(RemovalValidationError.IsCurrentWorktree);
    }

    [Fact]
    public async Task ValidateForRemovalAsync_WhenUncommittedChanges_ReturnsHasUncommittedChanges()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new("/repo", "main", false, "abc123", DateTime.UtcNow, true),
            new("/repo-feature", "feature", false, "def456", DateTime.UtcNow, true)
        };
        _mockGitService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));
        _mockGitService
            .Setup(x => x.HasUncommittedChangesAsync("/repo-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));
        _mockGitService
            .Setup(x => x.IsWorktreeLockedAsync("/repo-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(false));

        // Act
        var result = await _worktreeService.ValidateForRemovalAsync("feature", false);

        // Assert
        result.ShouldBe(RemovalValidationError.HasUncommittedChanges);
    }

    [Fact]
    public async Task ValidateForRemovalAsync_WhenNormalRemoval_ReturnsNone()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new("/repo", "main", false, "abc123", DateTime.UtcNow, true),
            new("/repo-feature", "feature", false, "def456", DateTime.UtcNow, true)
        };
        _mockGitService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));
        _mockGitService
            .Setup(x => x.HasUncommittedChangesAsync("/repo-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(false));
        _mockGitService
            .Setup(x => x.IsWorktreeLockedAsync("/repo-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(false));

        // Act
        var result = await _worktreeService.ValidateForRemovalAsync("feature", false);

        // Assert
        result.ShouldBe(RemovalValidationError.None);
    }

    #endregion

    #region RemoveWorktreeAsync Tests

    [Fact]
    public async Task RemoveWorktreeAsync_WhenValidationFails_ReturnsErrorWithSolution()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new("/repo", "main", false, "abc123", DateTime.UtcNow, true)
        };
        _mockGitService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));

        var options = new RemoveWorktreeOptions
        {
            WorktreeIdentifier = "non-existent",
            Force = false
        };

        // Act
        var result = await _worktreeService.RemoveWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("WT-RM-001");
        result.Solution.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task RemoveWorktreeAsync_WhenSuccess_RemovesWorktreeAndDeletesDirectory()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new("/repo", "main", false, "abc123", DateTime.UtcNow, true),
            new("/repo-feature", "feature", false, "def456", DateTime.UtcNow, true)
        };
        _mockGitService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));
        _mockGitService
            .Setup(x => x.HasUncommittedChangesAsync("/repo-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(false));
        _mockGitService
            .Setup(x => x.IsWorktreeLockedAsync("/repo-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(false));
        _mockGitService
            .Setup(x => x.RemoveWorktreeAsync("/repo-feature", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        var options = new RemoveWorktreeOptions
        {
            WorktreeIdentifier = "feature",
            Force = false
        };

        // Act
        var result = await _worktreeService.RemoveWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Success.ShouldBeTrue();
        result.Data.WorktreeId.ShouldBe("feature");
        result.Data.RemovedPath.ShouldBe("/repo-feature");
        result.Data.WorktreeMetadataRemoved.ShouldBeTrue();
    }

    #endregion

    #region User Story 2: Force Removal Tests

    [Fact]
    public async Task ValidateForRemovalAsync_WhenLockedWorktree_ReturnsIsLocked()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new("/repo", "main", false, "abc123", DateTime.UtcNow, true),
            new("/repo-feature", "feature", false, "def456", DateTime.UtcNow, true)
        };
        _mockGitService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));
        _mockGitService
            .Setup(x => x.HasUncommittedChangesAsync("/repo-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(false));
        _mockGitService
            .Setup(x => x.IsWorktreeLockedAsync("/repo-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        // Act
        var result = await _worktreeService.ValidateForRemovalAsync("feature", false);

        // Assert
        result.ShouldBe(RemovalValidationError.IsLocked);
    }

    [Fact]
    public async Task ValidateForRemovalAsync_WhenUncommittedChangesWithForce_ReturnsNone()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new("/repo", "main", false, "abc123", DateTime.UtcNow, true),
            new("/repo-feature", "feature", false, "def456", DateTime.UtcNow, true)
        };
        _mockGitService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));

        // Act - force=true should skip uncommitted changes check
        var result = await _worktreeService.ValidateForRemovalAsync("feature", true);

        // Assert
        result.ShouldBe(RemovalValidationError.None);

        // Verify HasUncommittedChangesAsync was never called when force=true
        _mockGitService.Verify(
            x => x.HasUncommittedChangesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateForRemovalAsync_WhenLockedWithForce_ReturnsNone()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new("/repo", "main", false, "abc123", DateTime.UtcNow, true),
            new("/repo-feature", "feature", false, "def456", DateTime.UtcNow, true)
        };
        _mockGitService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));

        // Act - force=true should skip lock check
        var result = await _worktreeService.ValidateForRemovalAsync("feature", true);

        // Assert
        result.ShouldBe(RemovalValidationError.None);

        // Verify IsWorktreeLockedAsync was never called when force=true
        _mockGitService.Verify(
            x => x.IsWorktreeLockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveWorktreeAsync_WhenForce_BypassesUncommittedChangesAndLockCheck()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new("/repo", "main", false, "abc123", DateTime.UtcNow, true),
            new("/repo-feature", "feature", false, "def456", DateTime.UtcNow, true)
        };
        _mockGitService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));
        _mockGitService
            .Setup(x => x.RemoveWorktreeAsync("/repo-feature", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        var options = new RemoveWorktreeOptions
        {
            WorktreeIdentifier = "feature",
            Force = true
        };

        // Act
        var result = await _worktreeService.RemoveWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data!.Success.ShouldBeTrue();

        // Verify force flag was passed to GitService
        _mockGitService.Verify(
            x => x.RemoveWorktreeAsync("/repo-feature", true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
