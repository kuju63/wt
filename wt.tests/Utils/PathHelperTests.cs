using FluentAssertions;
using System.IO.Abstractions.TestingHelpers;
using wt.cli.Utils;
using Xunit;

namespace wt.tests.Utils;

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
        result.Should().Be(expectedPath);
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
        result.Should().NotContain("\\");
        result.Should().Be("/path/to/mixed/separators");
    }

    [Fact]
    public void ValidatePath_WithValidPath_ShouldReturnTrue()
    {
        // Arrange
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/valid/path", new MockDirectoryData() }
        });
        var helper = new PathHelper(fileSystem);

        // Act
        var result = helper.ValidatePath("/valid/path");

        // Assert
        result.IsValid.Should().BeTrue();
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
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("invalid characters");
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
        fileSystem.Directory.Exists("/new/directory").Should().BeTrue();
    }

    [Fact]
    public void ValidatePath_WithNonExistentParentDirectory_ShouldReturnFalse()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var helper = new PathHelper(fileSystem);
        var path = "/nonexistent/parent/worktree";

        // Act
        var result = helper.ValidatePath(path);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("parent directory");
    }

    [Theory]
    [InlineData("/absolute/custom/path")]
    [InlineData("../custom/relative/path")]
    [InlineData("./local/path")]
    public void ResolvePath_WithCustomPaths_ShouldResolveCorrectly(string customPath)
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var helper = new PathHelper(fileSystem);
        var basePath = "/Users/dev/projects/repo";

        // Act
        var result = helper.ResolvePath(customPath, basePath);

        // Assert
        result.Should().NotBeNullOrEmpty();
        if (Path.IsPathRooted(customPath))
        {
            result.Should().Be(customPath);
        }
        else
        {
            result.Should().StartWith(basePath);
        }
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
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
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
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty");
    }

    [Fact]
    public void ValidatePath_WithPathTooLong_ShouldReturnFalse()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var helper = new PathHelper(fileSystem);
        var longPath = "/path/" + new string('a', 500);

        // Act
        var result = helper.ValidatePath(longPath);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("too long");
    }
}
