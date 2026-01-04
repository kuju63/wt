using System.Text.RegularExpressions;

namespace Kuju63.WorkTree.CommandLine.Utils;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public record ValidationResult(
    bool IsValid,
    string? ErrorMessage = null
);

/// <summary>
/// Provides validation helper methods for various input types.
/// </summary>
public static class Validators
{
    private static readonly Regex BranchNamePattern = new(@"^[a-zA-Z0-9][a-zA-Z0-9/_.-]*$", RegexOptions.Compiled);

    /// <summary>
    /// Validates a branch name according to Git naming conventions.
    /// </summary>
    /// <param name="branchName">The branch name to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the branch name is valid.</returns>
    public static ValidationResult ValidateBranchName(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return new ValidationResult(false, "Branch name is null or empty");
        }

        // 先頭文字のチェック（英数字で開始）
        if (branchName.StartsWith("-") || branchName.StartsWith("."))
        {
            return new ValidationResult(false, "Branch name cannot start with '-' or '.'");
        }

        // 禁止パターンのチェック
        if (branchName.Contains(".."))
        {
            return new ValidationResult(false, "Branch name cannot contain '..'");
        }

        if (branchName.Contains("@{"))
        {
            return new ValidationResult(false, "Branch name cannot contain '@{'");
        }

        // 正規表現パターンのチェック
        if (!BranchNamePattern.IsMatch(branchName))
        {
            return new ValidationResult(false,
                "Branch name must start with alphanumeric and contain only alphanumeric, '-', '_', '/', '.' characters");
        }

        return new ValidationResult(true);
    }

    /// <summary>
    /// Sanitizes a branch name by trimming leading and trailing whitespace.
    /// </summary>
    /// <param name="branchName">The branch name to sanitize.</param>
    /// <returns>The sanitized branch name with leading and trailing whitespace removed.</returns>
    public static string SanitizeBranchName(string branchName)
    {
        return branchName?.Trim() ?? string.Empty;
    }
}
