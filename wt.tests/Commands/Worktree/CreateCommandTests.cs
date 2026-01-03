using FluentAssertions;
using Moq;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using wt.cli.Commands.Worktree;
using wt.cli.Models;
using wt.cli.Services.Worktree;
using Xunit;

namespace wt.tests.Commands.Worktree;

public class CreateCommandTests
{
    private readonly Mock<IWorktreeService> _mockWorktreeService;
    private readonly CreateCommand _command;
    private readonly TestConsole _console;

    public CreateCommandTests()
    {
        _mockWorktreeService = new Mock<IWorktreeService>();
        _command = new CreateCommand(_mockWorktreeService.Object);
        _console = new TestConsole();
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
            .Setup(x => x.CreateWorktreeAsync(It.IsAny<CreateWorktreeOptions>(), default))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(worktreeInfo));

        var rootCommand = new RootCommand();
        rootCommand.AddCommand(_command);
        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .Build();

        // Act
        var result = await parser.InvokeAsync("create feature-x", _console);

        // Assert
        result.Should().Be(0);
        _console.Out.ToString().Should().Contain("feature-x");
        _mockWorktreeService.Verify(
            x => x.CreateWorktreeAsync(
                It.Is<CreateWorktreeOptions>(o => o.BranchName == "feature-x"),
                default),
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
            .Setup(x => x.CreateWorktreeAsync(It.IsAny<CreateWorktreeOptions>(), default))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(worktreeInfo));

        var rootCommand = new RootCommand();
        rootCommand.AddCommand(_command);
        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .Build();

        // Act
        var result = await parser.InvokeAsync("create feature-x --base develop", _console);

        // Assert
        result.Should().Be(0);
        _mockWorktreeService.Verify(
            x => x.CreateWorktreeAsync(
                It.Is<CreateWorktreeOptions>(o =>
                    o.BranchName == "feature-x" &&
                    o.BaseBranch == "develop"),
                default),
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
            .Setup(x => x.CreateWorktreeAsync(It.IsAny<CreateWorktreeOptions>(), default))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(worktreeInfo));

        var rootCommand = new RootCommand();
        rootCommand.AddCommand(_command);
        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .Build();

        // Act
        var result = await parser.InvokeAsync("create feature-x --path /custom/path/feature-x", _console);

        // Assert
        result.Should().Be(0);
        _mockWorktreeService.Verify(
            x => x.CreateWorktreeAsync(
                It.Is<CreateWorktreeOptions>(o =>
                    o.BranchName == "feature-x" &&
                    o.WorktreePath == "/custom/path/feature-x"),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WhenServiceReturnsError_DisplaysErrorAndReturnsNonZero()
    {
        // Arrange
        _mockWorktreeService
            .Setup(x => x.CreateWorktreeAsync(It.IsAny<CreateWorktreeOptions>(), default))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Failure(
                ErrorCodes.BranchAlreadyExists,
                "Branch 'feature-x' already exists",
                ErrorCodes.GetSolution(ErrorCodes.BranchAlreadyExists)));

        var rootCommand = new RootCommand();
        rootCommand.AddCommand(_command);
        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .Build();

        // Act
        var result = await parser.InvokeAsync("create feature-x", _console);

        // Assert
        result.Should().NotBe(0);
        _console.Error.ToString().Should().Contain("Branch 'feature-x' already exists");
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
            .Setup(x => x.CreateWorktreeAsync(It.IsAny<CreateWorktreeOptions>(), default))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(worktreeInfo));

        var rootCommand = new RootCommand();
        rootCommand.AddCommand(_command);
        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .Build();

        // Act
        var result = await parser.InvokeAsync("create feature-x -b develop -p /custom/path", _console);

        // Assert
        result.Should().Be(0);
        _mockWorktreeService.Verify(
            x => x.CreateWorktreeAsync(
                It.Is<CreateWorktreeOptions>(o =>
                    o.BranchName == "feature-x" &&
                    o.BaseBranch == "develop" &&
                    o.WorktreePath == "/custom/path"),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WithoutBranchName_ReturnsError()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(_command);
        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .Build();

        // Act
        var result = await parser.InvokeAsync("create", _console);

        // Assert
        result.Should().NotBe(0);
    }
}
