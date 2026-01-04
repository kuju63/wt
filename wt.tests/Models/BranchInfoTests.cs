using Kuju63.WorkTree.CommandLine.Models;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Models;

public class BranchInfoTests
{
    [Fact]
    public void BranchInfo_WithValidProperties_ShouldCreate()
    {
        // Arrange & Act
        var info = new BranchInfo("feature-x", "main", false, false);

        // Assert
        info.Name.ShouldBe("feature-x");
        info.BaseBranch.ShouldBe("main");
        info.Exists.ShouldBeFalse();
        info.IsRemote.ShouldBeFalse();
    }

    [Fact]
    public void BranchInfo_WithNullBaseBranch_ShouldAllowCreation()
    {
        // Act
        var info = new BranchInfo("feature-x", null, false, false);

        // Assert
        info.Name.ShouldBe("feature-x");
        info.BaseBranch.ShouldBeNull();
    }

    [Fact]
    public void BranchInfo_WithExistingBranch_ShouldSetExistsTrue()
    {
        // Act
        var info = new BranchInfo("existing-branch", "main", true, false);

        // Assert
        info.Exists.ShouldBeTrue();
    }

    [Fact]
    public void BranchInfo_WithRemoteBranch_ShouldSetIsRemoteTrue()
    {
        // Act
        var info = new BranchInfo("origin/feature-x", "main", true, true);

        // Assert
        info.IsRemote.ShouldBeTrue();
    }

    [Fact]
    public void BranchInfo_WithRecordEquality_ShouldCompareCorrectly()
    {
        // Arrange
        var info1 = new BranchInfo("feature-x", "main", false, false);
        var info2 = new BranchInfo("feature-x", "main", false, false);
        var info3 = new BranchInfo("feature-y", "main", false, false);

        // Assert
        info1.ShouldBe(info2);
        info1.ShouldNotBe(info3);
    }

    [Theory]
    [InlineData("feature-x", true)]
    [InlineData("bugfix/issue-123", true)]
    [InlineData("", false)]
    [InlineData("-invalid", false)]
    public void BranchInfo_Validate_ShouldCheckName(string name, bool expectedValid)
    {
        // Act
        var isValid = BranchInfo.IsValidName(name);

        // Assert
        isValid.ShouldBe(expectedValid);
    }
}
