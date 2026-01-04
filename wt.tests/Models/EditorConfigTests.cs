using Kuju63.WorkTree.CommandLine.Models;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Models;

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
        config.EditorType.ShouldBe(EditorType.VSCode);
        config.Command.ShouldBe("code");
        config.Arguments.ShouldBe("{path}");
        config.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public void EditorConfig_Should_Support_All_EditorTypes()
    {
        // Arrange & Act & Assert
        var vsCode = new EditorConfig { EditorType = EditorType.VSCode };
        vsCode.EditorType.ShouldBe(EditorType.VSCode);

        var vim = new EditorConfig { EditorType = EditorType.Vim };
        vim.EditorType.ShouldBe(EditorType.Vim);

        var emacs = new EditorConfig { EditorType = EditorType.Emacs };
        emacs.EditorType.ShouldBe(EditorType.Emacs);

        var nano = new EditorConfig { EditorType = EditorType.Nano };
        nano.EditorType.ShouldBe(EditorType.Nano);

        var idea = new EditorConfig { EditorType = EditorType.IntelliJIDEA };
        idea.EditorType.ShouldBe(EditorType.IntelliJIDEA);
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
        config.IsAvailable.ShouldBeFalse();
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
        config.Command.ShouldBe(command);
        config.Arguments.ShouldBe(arguments);
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
        config.Arguments.ShouldContain("{path}");
    }
}
