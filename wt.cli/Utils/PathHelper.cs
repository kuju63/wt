using System.IO.Abstractions;

namespace Kuju63.WorkTree.CommandLine.Utils;

/// <summary>
/// パス検証結果
/// </summary>
public record PathValidationResult(
    bool IsValid,
    string? ErrorMessage = null
);

/// <summary>
/// パス操作ヘルパークラス
/// </summary>
public class PathHelper : IPathHelper
{
    private readonly IFileSystem _fileSystem;

    public PathHelper(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// パスを解決する（相対パスを絶対パスに変換）
    /// </summary>
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
    /// パスを正規化する（セパレーターを統一）
    /// </summary>
    public string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// パスを検証する
    /// </summary>
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
    /// 親ディレクトリが存在することを確認し、存在しない場合は作成する
    /// </summary>
    public void EnsureParentDirectoryExists(string filePath)
    {
        var directory = _fileSystem.Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !_fileSystem.Directory.Exists(directory))
        {
            _fileSystem.Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// ディレクトリに書き込み権限があるかチェック
    /// </summary>
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
