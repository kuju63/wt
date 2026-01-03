using Kuju63.WorkTree.CommandLine.Utils;

namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// エディターの種類
/// </summary>
public enum EditorType
{
    VSCode,
    Vim,
    Emacs,
    Nano,
    IntelliJIDEA
}

/// <summary>
/// 出力形式
/// </summary>
public enum OutputFormat
{
    Human,
    Json
}

/// <summary>
/// Worktree作成オプション
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
    /// オプションを検証
    /// </summary>
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
