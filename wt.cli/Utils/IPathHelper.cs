namespace Kuju63.WorkTree.CommandLine.Utils;

public interface IPathHelper
{
    /// <summary>
    /// Resolve a path (convert relative to absolute)
    /// </summary>
    string ResolvePath(string path, string basePath);

    /// <summary>
    /// Normalize a path (unify separators)
    /// </summary>
    string NormalizePath(string path);

    /// <summary>
    /// Validate a path
    /// </summary>
    PathValidationResult ValidatePath(string path);

    /// <summary>
    /// Ensure parent directory exists
    /// </summary>
    void EnsureParentDirectoryExists(string filePath);

    /// <summary>
    /// Check if directory has write permission
    /// </summary>
    bool HasWritePermission(string directory);
}
