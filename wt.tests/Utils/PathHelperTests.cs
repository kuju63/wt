using System.IO.Abstractions.TestingHelpers;
using Kuju63.WorkTree.CommandLine.Utils;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Utils;

public class PathHelperTests
{
    [Theory]
    [InlineData("../worktrees/feature-x", "/Users/dev/projects/repo", "/Users/dev/projects/worktrees/feature-x")]
    [InlineData("/absolute/path", "/Users/dev/projects/repo", "/absolute/path")]
    [InlineData("relative/path", "/Users/dev/projects/repo", "/Users/dev/projects/repo/relative/path")]
    public void ResolvePath_WithVariousPaths_ShouldResolveCorrectly(string inputPath, string basePath, string expectedPath)
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var helper = new PathHelper(fileSystem);

        // Act
        var result = helper.ResolvePath(inputPath, basePath);

        // Assert
        result.ShouldBe(expectedPath);
    }

    [Fact]
    public void NormalizePath_WithMixedSeparators_ShouldNormalize()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var helper = new PathHelper(fileSystem);
        var path = "/path\\to/mixed\\separators";

        // Act
        var result = helper.NormalizePath(path);

        // Assert
        result.ShouldNotContain("\\");
        result.ShouldBe("/path/to/mixed/separators");
    }

    [Fact]
    public void ValidatePath_WithValidPath_ShouldReturnTrue()
    {
        // Arrange
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/valid", new MockDirectoryData() }
        });
        var helper = new PathHelper(fileSystem);

        // Act
        var result = helper.ValidatePath("/valid/newpath");

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidatePath_WithInvalidCharacters_ShouldReturnFalse()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var helper = new PathHelper(fileSystem);
        var invalidPath = "/path/with\0null";

        // Act
        var result = helper.ValidatePath(invalidPath);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
        result.ErrorMessage.ShouldContain("invalid characters");
    }

    [Fact]
    public void EnsureParentDirectoryExists_WhenParentDoesNotExist_ShouldCreateIt()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var helper = new PathHelper(fileSystem);
        var path = "/new/directory/file.txt";

        // Act
        helper.EnsureParentDirectoryExists(path);

        // Assert
        fileSystem.Directory.Exists("/new/directory").ShouldBeTrue();
    }

    [Fact]
    public void ValidatePath_WithNonExistentParentDirectory_ShouldReturnTrue()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var helper = new PathHelper(fileSystem);
        var path = "/nonexistent/parent/worktree";

        // Act
        var result = helper.ValidatePath(path);

        // Assert - Parent directory check is removed, will be created by EnsureParentDirectoryExists
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("/absolute/custom/path", "/absolute/custom/path")]
    [InlineData("../custom/relative/path", "/Users/dev/projects/custom/relative/path")]
    [InlineData("./local/path", "/Users/dev/projects/repo/local/path")]
    public void ResolvePath_WithCustomPaths_ShouldResolveCorrectly(string customPath, string expectedPath)
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var helper = new PathHelper(fileSystem);
        var basePath = "/Users/dev/projects/repo";

        // Act
        var result = helper.ResolvePath(customPath, basePath);

        // Assert
        result.ShouldBe(expectedPath);
    }

    [Fact]
    public void ValidatePath_WithExistingDirectory_ShouldReturnFalse()
    {
        // Arrange
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/existing/worktree", new MockDirectoryData() }
        });
        var helper = new PathHelper(fileSystem);

        // Act
        var result = helper.ValidatePath("/existing/worktree");

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
        result.ErrorMessage.ShouldContain("already exists");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidatePath_WithEmptyOrNullPath_ShouldReturnFalse(string? invalidPath)
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var helper = new PathHelper(fileSystem);

        // Act
        var result = helper.ValidatePath(invalidPath!);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
        result.ErrorMessage.ShouldContain("empty");
    }

    [Fact]
    public void ValidatePath_WithPathTooLong_ShouldReturnFalse()
    {
        // Arrange
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/path", new MockDirectoryData() }
        });
        var helper = new PathHelper(fileSystem);
        // Create a path longer than max allowed (4096 for Unix, 260 for Windows)
        var maxLength = OperatingSystem.IsWindows() ? 260 : 4096;
        var longPath = "/path/" + new string('a', maxLength + 10);

        // Act
        var result = helper.ValidatePath(longPath);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
        result.ErrorMessage.ShouldContain("too long");
    }
}
