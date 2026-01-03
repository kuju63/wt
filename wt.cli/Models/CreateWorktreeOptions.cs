using Kuju63.WorkTree.CommandLine.Utils;

namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Specifies the type of code editor.
/// </summary>
public enum EditorType
{
    /// <summary>
    /// Visual Studio Code editor.
    /// </summary>
    VSCode,

    /// <summary>
    /// Vim text editor.
    /// </summary>
    Vim,

    /// <summary>
    /// Emacs text editor.
    /// </summary>
    Emacs,

    /// <summary>
    /// Nano text editor.
    /// </summary>
    Nano,

    /// <summary>
    /// IntelliJ IDEA integrated development environment.
    /// </summary>
    IntelliJIDEA
}

/// <summary>
/// Specifies the output format for command results.
/// </summary>
public enum OutputFormat
{
    /// <summary>
    /// Human-readable text format.
    /// </summary>
    Human,

    /// <summary>
    /// JSON format for machine parsing.
    /// </summary>
    Json
}

/// <summary>
/// Represents the options for creating a new worktree.
/// </summary>
public class CreateWorktreeOptions
{
    public required string BranchName { get; init; }
    public string? BaseBranch { get; init; }
    public string? WorktreePath { get; init; }
    public EditorType? EditorType { get; init; }
    public bool CheckoutExisting { get; init; }
    public OutputFormat OutputFormat { get; init; } = OutputFormat.Human;
    public bool Verbose { get; init; }

    /// <summary>
    /// Validates the worktree creation options.
    /// </summary>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the options are valid.</returns>
    public ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(BranchName))
        {
            return new ValidationResult(false, "Branch name is required");
        }

        var branchValidation = Validators.ValidateBranchName(BranchName);
        if (!branchValidation.IsValid)
        {
            return branchValidation;
        }

        return new ValidationResult(true);
    }
}
