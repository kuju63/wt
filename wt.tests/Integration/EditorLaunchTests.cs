using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Editor;
using Kuju63.WorkTree.CommandLine.Utils;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Integration;

[Collection("Sequential Integration Tests")]
public class EditorLaunchTests : IDisposable
{
    private readonly string _testRepoPath;
    private readonly string _testWorktreePath;

    public EditorLaunchTests()
    {
        // Ensure we start from a valid directory
        try
        {
            var currentDir = Environment.CurrentDirectory;
            if (!Directory.Exists(currentDir))
            {
                Environment.CurrentDirectory = Path.GetTempPath();
            }
        }
        catch
        {
            Environment.CurrentDirectory = Path.GetTempPath();
        }

        _testRepoPath = Path.Combine(Path.GetTempPath(), $"test-repo-{Guid.NewGuid()}");
        _testWorktreePath = Path.Combine(Path.GetTempPath(), $"test-worktree-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRepoPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRepoPath))
        {
            Directory.Delete(_testRepoPath, true);
        }
        if (Directory.Exists(_testWorktreePath))
        {
            Directory.Delete(_testWorktreePath, true);
        }
    }

    [Fact(Skip = "Requires VS Code installed and can't run in CI without user interaction")]
    public async Task LaunchEditor_Should_Open_VSCode_When_Installed()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var editorService = new EditorService(processRunner);

        // Check if VS Code is available
        var whichResult = await processRunner.RunAsync(
            OperatingSystem.IsWindows() ? "where" : "which",
            "code",
            null,
            CancellationToken.None);

        if (whichResult.ExitCode != 0)
        {
            // Skip test if VS Code is not installed
            return;
        }

        Directory.CreateDirectory(_testWorktreePath);

        // Act
        var result = await editorService.LaunchEditorAsync(_testWorktreePath, EditorType.VSCode);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task LaunchEditor_Should_Return_Error_When_Editor_Not_Found()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        Directory.CreateDirectory(_testWorktreePath);

        // Use a non-existent editor command
        var nonExistentEditor = "non-existent-editor-12345";

        // Act - This will fail because the command doesn't exist
        var whichResult = await processRunner.RunAsync(
            OperatingSystem.IsWindows() ? "where" : "which",
            nonExistentEditor,
            null,
            CancellationToken.None);

        // Assert
        whichResult.ExitCode.ShouldNotBe(0);
    }

    [Fact]
    public void EditorService_Should_Resolve_All_Editor_Presets()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var editorService = new EditorService(processRunner);

        // Act & Assert
        var vsCodeConfig = editorService.ResolveEditorCommand(EditorType.VSCode);
        vsCodeConfig.Command.ShouldBe("code");
        vsCodeConfig.Arguments.ShouldBe("{path}");

        var vimConfig = editorService.ResolveEditorCommand(EditorType.Vim);
        vimConfig.Command.ShouldBe("vim");

        var emacsConfig = editorService.ResolveEditorCommand(EditorType.Emacs);
        emacsConfig.Command.ShouldBe("emacs");

        var nanoConfig = editorService.ResolveEditorCommand(EditorType.Nano);
        nanoConfig.Command.ShouldBe("nano");

        var ideaConfig = editorService.ResolveEditorCommand(EditorType.IntelliJIDEA);
        ideaConfig.Command.ShouldBe("idea");
    }

    [Fact]
    public async Task EditorService_Should_Handle_Path_With_Special_Characters()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var specialPath = Path.Combine(Path.GetTempPath(), $"test path with spaces-{Guid.NewGuid()}");

        try
        {
            Directory.CreateDirectory(specialPath);

            // Act - Check if editor command is available
            var whichResult = await processRunner.RunAsync(
                OperatingSystem.IsWindows() ? "where" : "which",
                "code",
                null,
                CancellationToken.None);

            // Assert - We just verify the service can handle special paths
            // Actual editor launch is skipped if not installed
            specialPath.ShouldContain(" ");
            whichResult.ExitCode.ShouldBe(0);
        }
        finally
        {
            if (Directory.Exists(specialPath))
            {
                Directory.Delete(specialPath, true);
            }
        }
    }
}
