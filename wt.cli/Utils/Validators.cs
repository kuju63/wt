using System.Text.RegularExpressions;

namespace Kuju63.WorkTree.CommandLine.Utils;

/// <summary>
/// 検証結果
/// </summary>
public record ValidationResult(
    bool IsValid,
    string? ErrorMessage = null
);

/// <summary>
/// バリデーションヘルパークラス
/// </summary>
public static class Validators
{
    private static readonly Regex BranchNamePattern = new(@"^[a-zA-Z0-9][a-zA-Z0-9/_.-]*$", RegexOptions.Compiled);

    /// <summary>
    /// ブランチ名を検証する
    /// </summary>
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
    /// ブランチ名をサニタイズする（前後の空白を削除）
    /// </summary>
    public static string SanitizeBranchName(string branchName)
    {
        return branchName?.Trim() ?? string.Empty;
    }
}
