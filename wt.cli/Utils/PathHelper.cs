using System.IO.Abstractions;

namespace Kuju63.WorkTree.CommandLine.Utils;

/// <summary>
/// Represents the result of a path validation operation.
/// </summary>
public record PathValidationResult(
    bool IsValid,
    string? ErrorMessage = null
);

/// <summary>
/// Provides helper methods for path operations and validation.
/// </summary>
public class PathHelper : IPathHelper
{
    private readonly IFileSystem _fileSystem;

    public PathHelper(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Resolves a path by converting relative paths to absolute paths.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <param name="basePath">The base path to use for relative path resolution.</param>
    /// <returns>The fully resolved absolute path.</returns>
    public string ResolvePath(string path, string basePath)
    {
        if (_fileSystem.Path.IsPathRooted(path))
        {
            return _fileSystem.Path.GetFullPath(path);
        }

        var combined = _fileSystem.Path.Combine(basePath, path);
        return _fileSystem.Path.GetFullPath(combined);
    }

    /// <summary>
    /// Normalizes a path by unifying directory separators.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path with forward slashes as directory separators.</returns>
    public string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// Validates a path for correctness and availability.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>A <see cref="PathValidationResult"/> indicating whether the path is valid.</returns>
    public PathValidationResult ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new PathValidationResult(false, "Path is null or empty");
        }

        try
        {
            // 無効な文字をチェック
            var invalidChars = _fileSystem.Path.GetInvalidPathChars();
            if (path.Any(c => invalidChars.Contains(c)))
            {
                return new PathValidationResult(false, "Path contains invalid characters");
            }

            // パス長チェック（Windows: 260文字、Unix系: 4096文字）
            var maxPathLength = OperatingSystem.IsWindows() ? 260 : 4096;
            if (path.Length > maxPathLength)
            {
                return new PathValidationResult(false, $"Path is too long (max {maxPathLength} characters)");
            }

            // 既に存在するディレクトリはエラー
            if (_fileSystem.Directory.Exists(path))
            {
                return new PathValidationResult(false, $"Path already exists: {path}");
            }

            // Note: Parent directory existence is not checked here
            // It will be created by EnsureParentDirectoryExists if needed

            return new PathValidationResult(true);
        }
        catch (Exception ex)
        {
            return new PathValidationResult(false, $"Path validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensures the parent directory of the specified file path exists, creating it if necessary.
    /// </summary>
    /// <param name="filePath">The file path whose parent directory should be ensured.</param>
    public void EnsureParentDirectoryExists(string filePath)
    {
        var directory = _fileSystem.Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !_fileSystem.Directory.Exists(directory))
        {
            _fileSystem.Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Determines whether the current user has write permission to the specified directory.
    /// </summary>
    /// <param name="directory">The directory to check for write permission.</param>
    /// <returns><see langword="true"/> if the directory has write permission; otherwise, <see langword="false"/>.</returns>
    public bool HasWritePermission(string directory)
    {
        try
        {
            var testFile = _fileSystem.Path.Combine(directory, $".wt_test_{Guid.NewGuid()}.tmp");
            _fileSystem.File.WriteAllText(testFile, "test");
            _fileSystem.File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
