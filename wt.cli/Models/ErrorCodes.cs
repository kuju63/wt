namespace wt.cli.Models;

/// <summary>
/// エラーコード定数クラス
/// </summary>
public static class ErrorCodes
{
    // Git関連エラー (GIT001-003)
    public const string GitNotFound = "GIT001";
    public const string NotGitRepository = "GIT002";
    public const string GitCommandFailed = "GIT003";

    // ブランチ関連エラー (BR001-003)
    public const string InvalidBranchName = "BR001";
    public const string BranchAlreadyExists = "BR002";
    public const string BranchAlreadyInUse = "BR003";

    // Worktree関連エラー (WT001-002)
    public const string WorktreeAlreadyExists = "WT001";
    public const string WorktreeCreationFailed = "WT002";

    // ファイルシステム関連エラー (FS001-003)
    public const string InvalidPath = "FS001";
    public const string PathNotWritable = "FS002";
    public const string DiskSpaceLow = "FS003";

    // エディター関連エラー (ED001)
    public const string EditorNotFound = "ED001";

    /// <summary>
    /// エラーコードに対する解決策を取得
    /// </summary>
    public static string GetSolution(string errorCode)
    {
        return errorCode switch
        {
            GitNotFound => "Install Git 2.5 or later and ensure it's in your PATH",
            NotGitRepository => "Navigate to a Git repository directory or initialize one with 'git init'",
            GitCommandFailed => "Check Git configuration and repository state",
            InvalidBranchName => "Use only alphanumeric characters, '-', '_', '/' in branch names",
            BranchAlreadyExists => "Use a different branch name or use --checkout-existing flag",
            BranchAlreadyInUse => "The branch is already checked out in another worktree. Choose a different branch or remove the other worktree",
            WorktreeAlreadyExists => "Remove the existing worktree or choose a different path",
            WorktreeCreationFailed => "Check file permissions and disk space",
            InvalidPath => "Provide a valid file system path",
            PathNotWritable => "Ensure you have write permissions to the target directory",
            DiskSpaceLow => "Free up disk space and try again",
            EditorNotFound => "Install the editor or specify a custom editor command",
            _ => "Unknown error occurred"
        };
    }
}
