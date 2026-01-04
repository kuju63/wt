using Kuju63.WorkTree.CommandLine.Models;
using Shouldly;

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
        options.BranchName.ShouldBe("feature-x");
        options.BaseBranch.ShouldBeNull();
        options.WorktreePath.ShouldBeNull();
        options.EditorType.ShouldBeNull();
        options.CheckoutExisting.ShouldBeFalse();
        options.OutputFormat.ShouldBe(OutputFormat.Human);
        options.Verbose.ShouldBeFalse();
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
        options.BranchName.ShouldBe("feature-x");
        options.BaseBranch.ShouldBe("develop");
        options.WorktreePath.ShouldBe("/custom/path");
        options.EditorType.ShouldBe(EditorType.VSCode);
        options.CheckoutExisting.ShouldBeTrue();
        options.OutputFormat.ShouldBe(OutputFormat.Json);
        options.Verbose.ShouldBeTrue();
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
        options.OutputFormat.ShouldBe(OutputFormat.Human);
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
        options.EditorType.ShouldBe(editorType);
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
        options.OutputFormat.ShouldBe(format);
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
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
        result.ErrorMessage.ShouldContain("Branch name");
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
        result.IsValid.ShouldBeTrue();
    }
}
