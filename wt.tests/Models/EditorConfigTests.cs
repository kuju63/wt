using FluentAssertions;
using wt.cli.Models;
using Xunit;

namespace wt.tests.Models;

public class EditorConfigTests
{
    [Fact]
    public void EditorConfig_Should_Create_With_Valid_Properties()
    {
        // Arrange & Act
        var config = new EditorConfig
        {
            EditorType = EditorType.VSCode,
            Command = "code",
            Arguments = "{path}",
            IsAvailable = true
        };

        // Assert
        config.EditorType.Should().Be(EditorType.VSCode);
        config.Command.Should().Be("code");
        config.Arguments.Should().Be("{path}");
        config.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void EditorConfig_Should_Support_All_EditorTypes()
    {
        // Arrange & Act & Assert
        var vsCode = new EditorConfig { EditorType = EditorType.VSCode };
        vsCode.EditorType.Should().Be(EditorType.VSCode);

        var vim = new EditorConfig { EditorType = EditorType.Vim };
        vim.EditorType.Should().Be(EditorType.Vim);

        var emacs = new EditorConfig { EditorType = EditorType.Emacs };
        emacs.EditorType.Should().Be(EditorType.Emacs);

        var nano = new EditorConfig { EditorType = EditorType.Nano };
        nano.EditorType.Should().Be(EditorType.Nano);

        var idea = new EditorConfig { EditorType = EditorType.IntelliJIDEA };
        idea.EditorType.Should().Be(EditorType.IntelliJIDEA);
    }

    [Fact]
    public void EditorConfig_Should_Handle_Unavailable_Editor()
    {
        // Arrange & Act
        var config = new EditorConfig
        {
            EditorType = EditorType.VSCode,
            Command = "code",
            Arguments = "{path}",
            IsAvailable = false
        };

        // Assert
        config.IsAvailable.Should().BeFalse();
    }

    [Theory]
    [InlineData("code", "{path}")]
    [InlineData("vim", "{path}")]
    [InlineData("emacs", "{path}")]
    [InlineData("nano", "{path}")]
    [InlineData("idea", "{path}")]
    public void EditorConfig_Should_Accept_Valid_Commands(string command, string arguments)
    {
        // Arrange & Act
        var config = new EditorConfig
        {
            Command = command,
            Arguments = arguments
        };

        // Assert
        config.Command.Should().Be(command);
        config.Arguments.Should().Be(arguments);
    }

    [Fact]
    public void EditorConfig_Arguments_Should_Contain_Path_Placeholder()
    {
        // Arrange
        var config = new EditorConfig
        {
            Command = "code",
            Arguments = "{path}"
        };

        // Assert
        config.Arguments.Should().Contain("{path}");
    }
}
