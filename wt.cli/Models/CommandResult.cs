namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// コマンド実行結果を表すジェネリッククラス（Resultパターン）
/// </summary>
public class CommandResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Solution { get; init; }
    public List<string> Warnings { get; init; } = new();

    private CommandResult() { }

    /// <summary>
    /// 成功結果を作成
    /// </summary>
    public static CommandResult<T> Success(T data, List<string>? warnings = null)
    {
        return new CommandResult<T>
        {
            IsSuccess = true,
            Data = data,
            Warnings = warnings ?? new List<string>()
        };
    }

    /// <summary>
    /// 失敗結果を作成
    /// </summary>
    public static CommandResult<T> Failure(string errorCode, string errorMessage, string? solution = null)
    {
        return new CommandResult<T>
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            Solution = solution
        };
    }

    /// <summary>
    /// 警告付き成功結果を作成
    /// </summary>
    public static CommandResult<T> SuccessWithWarnings(T data, List<string> warnings)
    {
        return new CommandResult<T>
        {
            IsSuccess = true,
            Data = data,
            Warnings = warnings
        };
    }
}
