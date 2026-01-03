using Kuju63.WorkTree.CommandLine.Utils;

namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Represents information about a Git branch.
/// </summary>
/// <param name="Name">The name of the branch.</param>
/// <param name="BaseBranch">The base branch from which this branch was created, if any.</param>
/// <param name="Exists">Indicates whether the branch exists in the repository.</param>
/// <param name="IsRemote">Indicates whether the branch is a remote tracking branch.</param>
public record BranchInfo(
    string Name,
    string? BaseBranch,
    bool Exists,
    bool IsRemote
)
{
    /// <summary>
    /// Validates whether the specified branch name is valid according to Git naming conventions.
    /// </summary>
    /// <param name="name">The branch name to validate.</param>
    /// <returns><see langword="true"/> if the branch name is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidName(string name)
    {
        var result = Validators.ValidateBranchName(name);
        return result.IsValid;
    }
}
