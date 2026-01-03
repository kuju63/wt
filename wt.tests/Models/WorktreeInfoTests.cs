using FluentAssertions;
using Kuju63.WorkTree.CommandLine.Models;
using Xunit;

namespace Kuju63.WorkTree.Tests.Models;

public class WorktreeInfoTests
{
    [Fact]
    public void WorktreeInfo_WithValidProperties_ShouldCreate()
    {
        // Arrange
        var path = "/Users/dev/worktrees/feature-x";
        var branch = "feature-x";
        var baseBranch = "main";
        var createdAt = DateTime.UtcNow;

        // Act
        var info = new WorktreeInfo(path, branch, baseBranch, createdAt);

        // Assert
        info.Path.Should().Be(path);
        info.Branch.Should().Be(branch);
        info.BaseBranch.Should().Be(baseBranch);
        info.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void WorktreeInfo_WithRecordEquality_ShouldCompareCorrectly()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var info1 = new WorktreeInfo("/path/to/worktree", "feature-x", "main", createdAt);
        var info2 = new WorktreeInfo("/path/to/worktree", "feature-x", "main", createdAt);
        var info3 = new WorktreeInfo("/different/path", "feature-x", "main", createdAt);

        // Assert
        info1.Should().Be(info2);
        info1.Should().NotBe(info3);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void WorktreeInfo_WithEmptyPath_ShouldAllowCreation(string? path)
    {
        // Act
        var info = new WorktreeInfo(path!, "branch", "main", DateTime.UtcNow);

        // Assert
        info.Path.Should().Be(path);
    }

    [Fact]
    public void WorktreeInfo_ToString_ShouldReturnReadableFormat()
    {
        // Arrange
        var info = new WorktreeInfo("/path/to/worktree", "feature-x", "main", DateTime.UtcNow);

        // Act
        var result = info.ToString();

        // Assert
        result.Should().Contain("/path/to/worktree");
        result.Should().Contain("feature-x");
        result.Should().Contain("main");
    }
}
