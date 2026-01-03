using FluentAssertions;
using Moq;
using System.CommandLine;
using Kuju63.WorkTree.CommandLine.Commands.Worktree;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Xunit;

namespace Kuju63.WorkTree.Tests.Commands.Worktree;

public class CreateCommandTests
{
    private readonly Mock<IWorktreeService> _mockWorktreeService;
    private readonly CreateCommand _command;
    private readonly StringWriter _outputWriter;
    private readonly StringWriter _errorWriter;
    private readonly InvocationConfiguration _invocationConfig;

    public CreateCommandTests()
    {
        _mockWorktreeService = new Mock<IWorktreeService>();
        _command = new CreateCommand(_mockWorktreeService.Object);
        _outputWriter = new StringWriter();
        _errorWriter = new StringWriter();
        _invocationConfig = new InvocationConfiguration
        {
            Output = _outputWriter,
            Error = _errorWriter
        };
    }

    [Fact]
    public async Task Execute_WithValidBranchName_CreatesWorktree()
    {
        // Arrange
        var worktreeInfo = new WorktreeInfo(
            "/Users/dev/wt-feature-x",
            "feature-x",
            "main",
            DateTime.UtcNow);

        _mockWorktreeService
            .Setup(x => x.CreateWorktreeAsync(It.IsAny<CreateWorktreeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(worktreeInfo));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "create", "feature-x" });
        var result = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        result.Should().Be(0);
        _outputWriter.ToString().Should().Contain("feature-x");
        _mockWorktreeService.Verify(
            x => x.CreateWorktreeAsync(
                It.Is<CreateWorktreeOptions>(o => o.BranchName == "feature-x"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WithBaseBranchOption_UsesSpecifiedBase()
    {
        // Arrange
        var worktreeInfo = new WorktreeInfo(
            "/Users/dev/wt-feature-x",
            "feature-x",
            "develop",
            DateTime.UtcNow);

        _mockWorktreeService
            .Setup(x => x.CreateWorktreeAsync(It.IsAny<CreateWorktreeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(worktreeInfo));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "create", "feature-x", "--base", "develop" });
        var result = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        result.Should().Be(0);
        _mockWorktreeService.Verify(
            x => x.CreateWorktreeAsync(
                It.Is<CreateWorktreeOptions>(o =>
                    o.BranchName == "feature-x" &&
                    o.BaseBranch == "develop"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WithCustomPath_UsesSpecifiedPath()
    {
        // Arrange
        var worktreeInfo = new WorktreeInfo(
            "/custom/path/feature-x",
            "feature-x",
            "main",
            DateTime.UtcNow);

        _mockWorktreeService
            .Setup(x => x.CreateWorktreeAsync(It.IsAny<CreateWorktreeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(worktreeInfo));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "create", "feature-x", "--path", "/custom/path/feature-x" });
        var result = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        result.Should().Be(0);
        _mockWorktreeService.Verify(
            x => x.CreateWorktreeAsync(
                It.Is<CreateWorktreeOptions>(o =>
                    o.BranchName == "feature-x" &&
                    o.WorktreePath == "/custom/path/feature-x"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WhenServiceReturnsError_DisplaysErrorAndReturnsNonZero()
    {
        // Arrange
        _mockWorktreeService
            .Setup(x => x.CreateWorktreeAsync(It.IsAny<CreateWorktreeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Failure(
                ErrorCodes.BranchAlreadyExists,
                "Branch 'feature-x' already exists",
                ErrorCodes.GetSolution(ErrorCodes.BranchAlreadyExists)));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "create", "feature-x" });
        var result = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        result.Should().NotBe(0);
        _errorWriter.ToString().Should().Contain("Branch 'feature-x' already exists");
    }

    [Fact]
    public async Task Execute_WithShortOptions_ParsesCorrectly()
    {
        // Arrange
        var worktreeInfo = new WorktreeInfo(
            "/custom/path",
            "feature-x",
            "develop",
            DateTime.UtcNow);

        _mockWorktreeService
            .Setup(x => x.CreateWorktreeAsync(It.IsAny<CreateWorktreeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(worktreeInfo));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "create", "feature-x", "-b", "develop", "-p", "/custom/path" });
        var result = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        result.Should().Be(0);
        _mockWorktreeService.Verify(
            x => x.CreateWorktreeAsync(
                It.Is<CreateWorktreeOptions>(o =>
                    o.BranchName == "feature-x" &&
                    o.BaseBranch == "develop" &&
                    o.WorktreePath == "/custom/path"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WithoutBranchName_ReturnsError()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "create" });
        var result = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        result.Should().NotBe(0);
    }
}
