using FluentAssertions;
using Moq;
using System.IO.Abstractions;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Kuju63.WorkTree.CommandLine.Utils;
using Xunit;

namespace Kuju63.WorkTree.Tests.Services.Worktree;

public class WorktreeServiceTests
{
    private readonly Mock<IGitService> _mockGitService;
    private readonly Mock<IPathHelper> _mockPathHelper;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly WorktreeService _worktreeService;

    public WorktreeServiceTests()
    {
        _mockGitService = new Mock<IGitService>();
        _mockFileSystem = new Mock<IFileSystem>();
        _mockPathHelper = new Mock<IPathHelper>();
        _worktreeService = new WorktreeService(_mockGitService.Object, _mockPathHelper.Object);
    }

    [Fact]
    public async Task CreateWorktreeAsync_WithValidOptions_CreatesWorktreeSuccessfully()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main"
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.BranchExistsAsync("feature-x", default))
            .ReturnsAsync(CommandResult<bool>.Success(false));

        _mockGitService
            .Setup(x => x.CreateBranchAsync("feature-x", "main", default))
            .ReturnsAsync(CommandResult<BranchInfo>.Success(new BranchInfo("feature-x", "main", true, false)));

        _mockPathHelper
            .Setup(x => x.ResolvePath("../wt-feature-x", It.IsAny<string>()))
            .Returns("/Users/dev/project/../wt-feature-x");

        _mockPathHelper
            .Setup(x => x.NormalizePath("/Users/dev/project/../wt-feature-x"))
            .Returns("/Users/dev/wt-feature-x");

        _mockPathHelper
            .Setup(x => x.ValidatePath("/Users/dev/wt-feature-x"))
            .Returns(new PathValidationResult(true));

        _mockGitService
            .Setup(x => x.AddWorktreeAsync("/Users/dev/wt-feature-x", "feature-x", default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Branch.Should().Be("feature-x");
        result.Data.BaseBranch.Should().Be("main");
        result.Data.Path.Should().Be("/Users/dev/wt-feature-x");
    }

    [Fact]
    public async Task CreateWorktreeAsync_WhenNotInGitRepo_ReturnsError()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main"
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(default))
            .ReturnsAsync(CommandResult<bool>.Success(false));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotGitRepository);
    }

    [Fact]
    public async Task CreateWorktreeAsync_WhenBranchAlreadyExists_ReturnsError()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main"
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.BranchExistsAsync("feature-x", default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.BranchAlreadyExists);
    }

    [Fact]
    public async Task CreateWorktreeAsync_WhenInvalidPath_ReturnsError()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main"
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.BranchExistsAsync("feature-x", default))
            .ReturnsAsync(CommandResult<bool>.Success(false));

        _mockGitService
            .Setup(x => x.CreateBranchAsync("feature-x", "main", default))
            .ReturnsAsync(CommandResult<BranchInfo>.Success(new BranchInfo("feature-x", "main", true, false)));

        _mockPathHelper
            .Setup(x => x.ResolvePath("../wt-feature-x", It.IsAny<string>()))
            .Returns("/Users/dev/wt-feature-x");

        _mockPathHelper
            .Setup(x => x.NormalizePath("/Users/dev/wt-feature-x"))
            .Returns("/Users/dev/wt-feature-x");

        _mockPathHelper
            .Setup(x => x.ValidatePath("/Users/dev/wt-feature-x"))
            .Returns(new PathValidationResult(false, "Invalid path"));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.InvalidPath);
    }

    [Fact]
    public async Task CreateWorktreeAsync_WithCustomPath_UsesProvidedPath()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main",
            WorktreePath = "/custom/path/feature-x"
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.BranchExistsAsync("feature-x", default))
            .ReturnsAsync(CommandResult<bool>.Success(false));

        _mockGitService
            .Setup(x => x.CreateBranchAsync("feature-x", "main", default))
            .ReturnsAsync(CommandResult<BranchInfo>.Success(new BranchInfo("feature-x", "main", true, false)));

        _mockPathHelper
            .Setup(x => x.ResolvePath("/custom/path/feature-x", It.IsAny<string>()))
            .Returns("/custom/path/feature-x");

        _mockPathHelper
            .Setup(x => x.NormalizePath("/custom/path/feature-x"))
            .Returns("/custom/path/feature-x");

        _mockPathHelper
            .Setup(x => x.ValidatePath("/custom/path/feature-x"))
            .Returns(new PathValidationResult(true));

        _mockGitService
            .Setup(x => x.AddWorktreeAsync("/custom/path/feature-x", "feature-x", default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Path.Should().Be("/custom/path/feature-x");
    }
}
