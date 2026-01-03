using FluentAssertions;
using wt.cli.Utils;
using Xunit;

namespace wt.tests.Utils;

public class ValidatorsTests
{
    [Theory]
    [InlineData("feature-x", true)]
    [InlineData("bugfix/issue-123", true)]
    [InlineData("user_story_1", true)]
    [InlineData("release-v1.0.0", true)]
    [InlineData("FEATURE-NAME", true)]
    [InlineData("feature123", true)]
    public void ValidateBranchName_WithValidNames_ShouldReturnTrue(string branchName, bool expected)
    {
        // Act
        var result = Validators.ValidateBranchName(branchName);

        // Assert
        result.IsValid.Should().Be(expected);
        if (expected)
        {
            result.ErrorMessage.Should().BeNull();
        }
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("-feature", false)]
    [InlineData(".feature", false)]
    [InlineData("feature..name", false)]
    [InlineData("feature@{name}", false)]
    [InlineData("feature name", false)]
    [InlineData("feature\\name", false)]
    [InlineData("feature\tname", false)]
    public void ValidateBranchName_WithInvalidNames_ShouldReturnFalse(string branchName, bool expected)
    {
        // Act
        var result = Validators.ValidateBranchName(branchName);

        // Assert
        result.IsValid.Should().Be(expected);
        result.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public void ValidateBranchName_WithNull_ShouldReturnFalse()
    {
        // Act
        var result = Validators.ValidateBranchName(null!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("null or empty");
    }

    [Theory]
    [InlineData("feature-x", "feature-x")]
    [InlineData("  feature-x  ", "feature-x")]
    [InlineData("FEATURE-X", "FEATURE-X")]
    public void SanitizeBranchName_WithVariousInputs_ShouldTrim(string input, string expected)
    {
        // Act
        var result = Validators.SanitizeBranchName(input);

        // Assert
        result.Should().Be(expected);
    }
}
