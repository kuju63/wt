using FluentAssertions;
using Kuju63.WorkTree.CommandLine.Models;
using Xunit;

namespace Kuju63.WorkTree.Tests.Models;

public class CreateWorktreeOptionsTests
{
    [Fact]
    public void CreateWorktreeOptions_WithMinimalProperties_ShouldCreate()
    {
        // Act
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x"
        };

        // Assert
        options.BranchName.Should().Be("feature-x");
        options.BaseBranch.Should().BeNull();
        options.WorktreePath.Should().BeNull();
        options.EditorType.Should().BeNull();
        options.CheckoutExisting.Should().BeFalse();
        options.OutputFormat.Should().Be(OutputFormat.Human);
        options.Verbose.Should().BeFalse();
    }

    [Fact]
    public void CreateWorktreeOptions_WithAllProperties_ShouldCreate()
    {
        // Act
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "develop",
            WorktreePath = "/custom/path",
            EditorType = EditorType.VSCode,
            CheckoutExisting = true,
            OutputFormat = OutputFormat.Json,
            Verbose = true
        };

        // Assert
        options.BranchName.Should().Be("feature-x");
        options.BaseBranch.Should().Be("develop");
        options.WorktreePath.Should().Be("/custom/path");
        options.EditorType.Should().Be(EditorType.VSCode);
        options.CheckoutExisting.Should().BeTrue();
        options.OutputFormat.Should().Be(OutputFormat.Json);
        options.Verbose.Should().BeTrue();
    }

    [Fact]
    public void CreateWorktreeOptions_DefaultOutputFormat_ShouldBeHuman()
    {
        // Act
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x"
        };

        // Assert
        options.OutputFormat.Should().Be(OutputFormat.Human);
    }

    [Theory]
    [InlineData(EditorType.VSCode)]
    [InlineData(EditorType.Vim)]
    [InlineData(EditorType.Emacs)]
    [InlineData(EditorType.Nano)]
    [InlineData(EditorType.IntelliJIDEA)]
    public void CreateWorktreeOptions_WithDifferentEditors_ShouldAccept(EditorType editorType)
    {
        // Act
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            EditorType = editorType
        };

        // Assert
        options.EditorType.Should().Be(editorType);
    }

    [Theory]
    [InlineData(OutputFormat.Human)]
    [InlineData(OutputFormat.Json)]
    public void CreateWorktreeOptions_WithDifferentOutputFormats_ShouldAccept(OutputFormat format)
    {
        // Act
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            OutputFormat = format
        };

        // Assert
        options.OutputFormat.Should().Be(format);
    }

    [Fact]
    public void CreateWorktreeOptions_Validate_WithEmptyBranchName_ShouldReturnError()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = ""
        };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Branch name");
    }

    [Fact]
    public void CreateWorktreeOptions_Validate_WithValidBranchName_ShouldReturnSuccess()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x"
        };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
