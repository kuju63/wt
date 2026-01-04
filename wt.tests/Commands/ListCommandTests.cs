using System.CommandLine;
using Kuju63.WorkTree.CommandLine.Commands;
using Kuju63.WorkTree.CommandLine.Formatters;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Moq;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Commands;

public class ListCommandTests
{
    private readonly Mock<IWorktreeService> _mockWorktreeService;
    private readonly Mock<IOutputFormatter> _mockFormatter;
    private readonly ListCommand _command;
    private readonly StringWriter _outputWriter;
    private readonly StringWriter _errorWriter;
    private readonly InvocationConfiguration _invocationConfig;

    public ListCommandTests()
    {
        _mockWorktreeService = new Mock<IWorktreeService>();
        _mockFormatter = new Mock<IOutputFormatter>();
        _command = new ListCommand(_mockWorktreeService.Object, _mockFormatter.Object);
        _outputWriter = new StringWriter();
        _errorWriter = new StringWriter();
        _invocationConfig = new InvocationConfiguration
        {
            Output = _outputWriter,
            Error = _errorWriter
        };
    }

    [Fact]
    public async Task Execute_WithValidWorktrees_ReturnsSuccessAndDisplaysTable()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new WorktreeInfo("/path/to/main", "main", false, "abc123", DateTime.Now, true),
            new WorktreeInfo("/path/to/feature", "feature-branch", false, "def456", DateTime.Now, true)
        };
        var expectedOutput = "┌─────────────────┬──────────────┬────────┐\n│ Path            │ Branch       │ Status │\n└─────────────────┴──────────────┴────────┘";

        _mockWorktreeService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));
        _mockFormatter
            .Setup(x => x.Format(worktrees))
            .Returns(expectedOutput);

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "list" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(0);
        _outputWriter.ToString().ShouldContain(expectedOutput);
        _mockWorktreeService.Verify(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockFormatter.Verify(x => x.Format(worktrees), Times.Once);
    }

    [Fact]
    public async Task Execute_WithNoWorktrees_ReturnsSuccessAndDisplaysMessage()
    {
        // Arrange
        var emptyWorktrees = new List<WorktreeInfo>();

        _mockWorktreeService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(emptyWorktrees));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "list" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(0);
        _outputWriter.ToString().ShouldContain("No worktrees found in this repository.");
        _mockFormatter.Verify(x => x.Format(It.IsAny<List<WorktreeInfo>>()), Times.Never);
    }

    [Fact]
    public async Task Execute_WithMissingWorktrees_DisplaysWarningAndContinues()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new WorktreeInfo("/path/to/main", "main", false, "abc123", DateTime.Now, true),
            new WorktreeInfo("/path/to/missing", "feature-branch", false, "def456", DateTime.Now, false),
            new WorktreeInfo("/path/to/another", "hotfix", false, "ghi789", DateTime.Now, true)
        };
        var expectedOutput = "┌─────────────────┬──────────────┬────────┐\n│ Path            │ Branch       │ Status │\n└─────────────────┴──────────────┴────────┘";

        _mockWorktreeService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));
        _mockFormatter
            .Setup(x => x.Format(worktrees))
            .Returns(expectedOutput);

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "list" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(0);
        _errorWriter.ToString().ShouldContain("Warning: Worktree at '/path/to/missing' does not exist on disk");
        _outputWriter.ToString().ShouldContain(expectedOutput);
        _mockFormatter.Verify(x => x.Format(worktrees), Times.Once);
    }

    [Fact]
    public async Task Execute_WithDetachedHead_DisplaysCorrectly()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new WorktreeInfo("/path/to/main", "main", false, "abc123", DateTime.Now, true),
            new WorktreeInfo("/path/to/detached", "abc1234", true, "abc1234567890", DateTime.Now, true)
        };
        var expectedOutput = "┌─────────────────┬────────────────────┬────────┐\n│ Path            │ Branch             │ Status │\n└─────────────────┴────────────────────┴────────┘";

        _mockWorktreeService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));
        _mockFormatter
            .Setup(x => x.Format(worktrees))
            .Returns(expectedOutput);

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "list" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(0);
        _outputWriter.ToString().ShouldContain(expectedOutput);
    }

    [Fact]
    public async Task Execute_WhenNotGitRepository_ReturnsExitCode2()
    {
        // Arrange
        _mockWorktreeService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Failure(
                ErrorCodes.NotGitRepository,
                "Not a git repository"));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "list" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(2);
        _errorWriter.ToString().ShouldContain("Error: Not a git repository. Please run this command from within a git repository.");
    }

    [Fact]
    public async Task Execute_WhenGitCommandFails_ReturnsExitCode10()
    {
        // Arrange
        var errorMessage = "fatal: this operation must be run in a work tree";
        _mockWorktreeService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Failure(
                ErrorCodes.GitCommandFailed,
                errorMessage));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "list" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(10);
        _errorWriter.ToString().ShouldContain("Error: Git command failed.");
        _errorWriter.ToString().ShouldContain(errorMessage);
    }

    [Fact]
    public async Task Execute_WithUnexpectedError_ReturnsExitCode99()
    {
        // Arrange
        var errorMessage = "Unexpected error occurred";
        _mockWorktreeService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Failure(
                "UNKNOWN", // Using unknown error code
                errorMessage));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "list" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(99);
        _errorWriter.ToString().ShouldContain("Error: Unexpected error occurred");
    }

    [Fact]
    public async Task Execute_WithNullErrorMessage_DisplaysGenericMessage()
    {
        // Arrange
        _mockWorktreeService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Failure(
                "UNKNOWN", // Using unknown error code
                string.Empty));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "list" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(99);
        _errorWriter.ToString().ShouldContain("Error: Unexpected error occurred");
    }

    [Fact]
    public async Task Execute_WithMultipleMissingWorktrees_DisplaysAllWarnings()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new WorktreeInfo("/path/to/main", "main", false, "abc123", DateTime.Now, true),
            new WorktreeInfo("/path/to/missing1", "feature1", false, "def456", DateTime.Now, false),
            new WorktreeInfo("/path/to/missing2", "feature2", false, "ghi789", DateTime.Now, false),
            new WorktreeInfo("/path/to/valid", "hotfix", false, "jkl012", DateTime.Now, true)
        };
        var expectedOutput = "table output";

        _mockWorktreeService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));
        _mockFormatter
            .Setup(x => x.Format(worktrees))
            .Returns(expectedOutput);

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "list" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(0);
        var errorOutput = _errorWriter.ToString();
        errorOutput.ShouldContain("Warning: Worktree at '/path/to/missing1' does not exist on disk");
        errorOutput.ShouldContain("Warning: Worktree at '/path/to/missing2' does not exist on disk");
        _outputWriter.ToString().ShouldContain(expectedOutput);
    }

    [Fact]
    public void Constructor_ShouldSetCorrectNameAndDescription()
    {
        // Assert
        _command.Name.ShouldBe("list");
        _command.Description.ShouldBe("List all worktrees with their branch information");
    }

    [Fact]
    public async Task Execute_WithLargeNumberOfWorktrees_HandlesCorrectly()
    {
        // Arrange
        var worktrees = Enumerable.Range(1, 100)
            .Select(i => new WorktreeInfo($"/path/to/worktree{i}", $"branch{i}", false, $"hash{i}", DateTime.Now, true))
            .ToList();
        var expectedOutput = "large table output";

        _mockWorktreeService
            .Setup(x => x.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));
        _mockFormatter
            .Setup(x => x.Format(worktrees))
            .Returns(expectedOutput);

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "list" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(0);
        _outputWriter.ToString().ShouldContain(expectedOutput);
        _mockFormatter.Verify(x => x.Format(It.Is<List<WorktreeInfo>>(w => w.Count == 100)), Times.Once);
    }
}

