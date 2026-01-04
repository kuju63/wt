using Kuju63.WorkTree.CommandLine.Formatters;
using Kuju63.WorkTree.CommandLine.Models;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Formatters;

public class TableFormatterTests
{
    [Fact]
    public void Format_EmptyList_ReturnsEmptyString()
    {
        // Arrange
        var formatter = new TableFormatter();

        // Act
        var result = formatter.Format(Enumerable.Empty<WorktreeInfo>());

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Format_SingleWorktree_ReturnsFormattedTable()
    {
        // Arrange
        var formatter = new TableFormatter();
        var worktrees = new[]
        {
            new WorktreeInfo(
                "/path/to/worktree",
                "main",
                false,
                "abc1234567890abcdef1234567890abcdef1234",
                DateTime.UtcNow,
                true)
        };

        // Act
        var result = formatter.Format(worktrees);

        // Assert
        result.ShouldContain("┌");
        result.ShouldContain("│");
        result.ShouldContain("└");
        result.ShouldContain("Path");
        result.ShouldContain("Branch");
        result.ShouldContain("Status");
        result.ShouldContain("/path/to/worktree");
        result.ShouldContain("main");
        result.ShouldContain("active");
    }

    [Fact]
    public void Format_DetachedHead_DisplaysCorrectly()
    {
        // Arrange
        var formatter = new TableFormatter();
        var worktrees = new[]
        {
            new WorktreeInfo(
                "/path/to/worktree",
                "abc1234567890abcdef",
                true,
                "abc1234567890abcdef1234567890abcdef1234",
                DateTime.UtcNow,
                true)
        };

        // Act
        var result = formatter.Format(worktrees);

        // Assert
        result.ShouldContain("abc1234 (detached)");
    }

    [Fact]
    public void Format_MissingWorktree_DisplaysMissingStatus()
    {
        // Arrange
        var formatter = new TableFormatter();
        var worktrees = new[]
        {
            new WorktreeInfo(
                "/path/to/missing",
                "feature",
                false,
                "abc1234567890abcdef1234567890abcdef1234",
                DateTime.UtcNow,
                false)
        };

        // Act
        var result = formatter.Format(worktrees);

        // Assert
        result.ShouldContain("missing");
    }

    [Fact]
    public void Format_CustomColumns_FormatsWithCustomColumns()
    {
        // Arrange
        var columns = new[]
        {
            new TableColumn("Worktree", w => w.Path),
            new TableColumn("Checked Out", w => w.Branch)
        };
        var formatter = new TableFormatter(columns);
        var worktrees = new[]
        {
            new WorktreeInfo(
                "/path/to/worktree",
                "main",
                false,
                "abc1234567890abcdef1234567890abcdef1234",
                DateTime.UtcNow,
                true)
        };

        // Act
        var result = formatter.Format(worktrees);

        // Assert
        result.ShouldContain("Worktree");
        result.ShouldContain("Checked Out");
        result.ShouldNotContain("Status");
        result.ShouldContain("/path/to/worktree");
        result.ShouldContain("main");
    }

    [Fact]
    public void Format_MultipleWorktrees_CalculatesCorrectColumnWidths()
    {
        // Arrange
        var formatter = new TableFormatter();
        var worktrees = new[]
        {
            new WorktreeInfo("/short", "main", false, "abc123", DateTime.UtcNow, true),
            new WorktreeInfo("/very/long/path/to/worktree", "feature-with-long-name", false, "def456", DateTime.UtcNow, true)
        };

        // Act
        var result = formatter.Format(worktrees);

        // Assert
        result.ShouldContain("/short");
        result.ShouldContain("/very/long/path/to/worktree");
        result.ShouldContain("feature-with-long-name");
        // Verify table structure is intact
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(6); // Top border, header, separator, 2 data rows, bottom border
    }

    [Fact]
    public void Constructor_WithNullColumns_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TableFormatter(null!));
    }

    [Fact]
    public void Constructor_WithEmptyColumns_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new TableFormatter(Array.Empty<TableColumn>()));
    }

    [Fact]
    public void Format_CachesDisplayValues_AvoidsDuplicateCalls()
    {
        // Arrange
        int displayBranchCallCount = 0;
        int displayStatusCallCount = 0;

        var columns = new[]
        {
            new TableColumn("Path", w => w.Path),
            new TableColumn("Branch", w =>
            {
                displayBranchCallCount++;
                return w.GetDisplayBranch();
            }),
            new TableColumn("Status", w =>
            {
                displayStatusCallCount++;
                return w.GetDisplayStatus();
            })
        };

        var formatter = new TableFormatter(columns);
        var worktrees = new[]
        {
            new WorktreeInfo("/path1", "main", false, "abc123", DateTime.UtcNow, true),
            new WorktreeInfo("/path2", "feature", false, "def456", DateTime.UtcNow, true)
        };

        // Act
        formatter.Format(worktrees);

        // Assert - Each method should be called exactly once per worktree
        displayBranchCallCount.ShouldBe(2);
        displayStatusCallCount.ShouldBe(2);
    }
}
