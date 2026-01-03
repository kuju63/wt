using Kuju63.WorkTree.CommandLine.Utils;

namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// ブランチ情報を表すレコード
/// </summary>
public record BranchInfo(
    string Name,
    string? BaseBranch,
    bool Exists,
    bool IsRemote
)
{
    /// <summary>
    /// ブランチ名が有効かどうかを検証
    /// </summary>
    public static bool IsValidName(string name)
    {
        var result = Validators.ValidateBranchName(name);
        return result.IsValid;
    }
}
