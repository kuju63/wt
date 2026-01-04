using Kuju63.WorkTree.CommandLine.Models;
using Shouldly;

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
        info.Path.ShouldBe(path);
        info.Branch.ShouldBe(branch);
        info.BaseBranch.ShouldBe(baseBranch);
        info.CreatedAt.ShouldBe(createdAt);
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
        info1.ShouldBe(info2);
        info1.ShouldNotBe(info3);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void WorktreeInfo_WithEmptyPath_ShouldAllowCreation(string? path)
    {
        // Act
        var info = new WorktreeInfo(path!, "branch", "main", DateTime.UtcNow);

        // Assert
        info.Path.ShouldBe(path);
    }

    [Fact]
    public void WorktreeInfo_ToString_ShouldReturnReadableFormat()
    {
        // Arrange
        var info = new WorktreeInfo("/path/to/worktree", "feature-x", "main", DateTime.UtcNow);

        // Act
        var result = info.ToString();

        // Assert
        result.ShouldContain("/path/to/worktree");
        result.ShouldContain("feature-x");
        result.ShouldContain("main");
    }
}
