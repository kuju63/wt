using FluentAssertions;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Editor;
using Kuju63.WorkTree.CommandLine.Utils;
using Moq;

namespace Kuju63.WorkTree.Tests.Services.Editor;

public class EditorServiceTests
{
    private readonly Mock<IProcessRunner> _mockProcessRunner;
    private readonly EditorService _editorService;

    public EditorServiceTests()
    {
        _mockProcessRunner = new Mock<IProcessRunner>();
        _editorService = new EditorService(_mockProcessRunner.Object);
    }

    [Fact]
    public async Task LaunchEditorAsync_Should_Return_Success_When_Editor_Launches()
    {
        // Arrange
        var path = "/path/to/worktree";
        var editorType = EditorType.VSCode;

        // Mock 'which code' to return success
        _mockProcessRunner
            .Setup(x => x.RunAsync("which", "code", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(0, "/usr/bin/code", ""));

        // Mock 'code' launch to return success
        _mockProcessRunner
            .Setup(x => x.RunAsync("code", It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(0, "", ""));

        // Act
        var result = await _editorService.LaunchEditorAsync(path, editorType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockProcessRunner.Verify(
            x => x.RunAsync("which", "code", null, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockProcessRunner.Verify(
            x => x.RunAsync("code", It.IsAny<string>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LaunchEditorAsync_Should_Return_Error_When_Editor_Not_Found()
    {
        // Arrange
        var path = "/path/to/worktree";
        var editorType = EditorType.VSCode;

        // Mock 'which code' to return failure (command not found)
        _mockProcessRunner
            .Setup(x => x.RunAsync("which", "code", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(1, "", "command not found"));

        // Act
        var result = await _editorService.LaunchEditorAsync(path, editorType);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ED001");
        result.ErrorMessage.Should().Contain("not found in PATH");
    }

    [Theory]
    [InlineData(EditorType.VSCode, "code")]
    [InlineData(EditorType.Vim, "vim")]
    [InlineData(EditorType.Emacs, "emacs")]
    [InlineData(EditorType.Nano, "nano")]
    [InlineData(EditorType.IntelliJIDEA, "idea")]
    public async Task LaunchEditorAsync_Should_Use_Correct_Command_For_EditorType(
        EditorType editorType,
        string expectedCommand)
    {
        // Arrange
        var path = "/path/to/worktree";

        // Mock 'which' to return success
        _mockProcessRunner
            .Setup(x => x.RunAsync("which", expectedCommand, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(0, $"/usr/bin/{expectedCommand}", ""));

        // Mock editor launch to return success
        _mockProcessRunner
            .Setup(x => x.RunAsync(expectedCommand, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(0, "", ""));

        // Act
        await _editorService.LaunchEditorAsync(path, editorType);

        // Assert
        _mockProcessRunner.Verify(
            x => x.RunAsync("which", expectedCommand, null, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockProcessRunner.Verify(
            x => x.RunAsync(expectedCommand, It.IsAny<string>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LaunchEditorAsync_Should_Handle_Path_With_Spaces()
    {
        // Arrange
        var path = "/path/to/my worktree";
        var editorType = EditorType.VSCode;

        // Mock 'which code' to return success
        _mockProcessRunner
            .Setup(x => x.RunAsync("which", "code", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(0, "/usr/bin/code", ""));

        // Mock 'code' launch to return success
        _mockProcessRunner
            .Setup(x => x.RunAsync("code", It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(0, "", ""));

        // Act
        var result = await _editorService.LaunchEditorAsync(path, editorType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockProcessRunner.Verify(
            x => x.RunAsync("code", It.IsAny<string>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void ResolveEditorCommand_Should_Return_Correct_Config_For_VSCode()
    {
        // Act
        var config = _editorService.ResolveEditorCommand(EditorType.VSCode);

        // Assert
        config.Should().NotBeNull();
        config.EditorType.Should().Be(EditorType.VSCode);
        config.Command.Should().Be("code");
        config.Arguments.Should().Be("{path}");
    }

    [Fact]
    public void ResolveEditorCommand_Should_Return_Correct_Config_For_Vim()
    {
        // Act
        var config = _editorService.ResolveEditorCommand(EditorType.Vim);

        // Assert
        config.Should().NotBeNull();
        config.EditorType.Should().Be(EditorType.Vim);
        config.Command.Should().Be("vim");
        config.Arguments.Should().Be("{path}");
    }

    [Fact]
    public void ResolveEditorCommand_Should_Return_Correct_Config_For_Emacs()
    {
        // Act
        var config = _editorService.ResolveEditorCommand(EditorType.Emacs);

        // Assert
        config.Should().NotBeNull();
        config.EditorType.Should().Be(EditorType.Emacs);
        config.Command.Should().Be("emacs");
        config.Arguments.Should().Be("{path}");
    }

    [Fact]
    public void ResolveEditorCommand_Should_Return_Correct_Config_For_Nano()
    {
        // Act
        var config = _editorService.ResolveEditorCommand(EditorType.Nano);

        // Assert
        config.Should().NotBeNull();
        config.EditorType.Should().Be(EditorType.Nano);
        config.Command.Should().Be("nano");
        config.Arguments.Should().Be("{path}");
    }

    [Fact]
    public void ResolveEditorCommand_Should_Return_Correct_Config_For_IntelliJIDEA()
    {
        // Act
        var config = _editorService.ResolveEditorCommand(EditorType.IntelliJIDEA);

        // Assert
        config.Should().NotBeNull();
        config.EditorType.Should().Be(EditorType.IntelliJIDEA);
        config.Command.Should().Be("idea");
        config.Arguments.Should().Be("{path}");
    }

    [Fact]
    public async Task LaunchEditorAsync_Should_Return_Error_When_Editor_Launch_Fails()
    {
        // Arrange
        var path = "/path/to/worktree";
        var editorType = EditorType.VSCode;

        // Mock 'which code' to return success
        _mockProcessRunner
            .Setup(x => x.RunAsync("which", "code", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(0, "/usr/bin/code", ""));

        // Mock 'code' launch to return failure
        _mockProcessRunner
            .Setup(x => x.RunAsync("code", It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(1, "", "Failed to launch"));

        // Act
        var result = await _editorService.LaunchEditorAsync(path, editorType);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ED001");
        result.ErrorMessage.Should().Contain("Failed to launch");
    }
}
