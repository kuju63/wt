namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Represents the result of a command operation using the Result pattern.
/// </summary>
/// <typeparam name="T">The type of data returned on success.</typeparam>
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
    /// Creates a successful result with the specified data.
    /// </summary>
    /// <param name="data">The data to include in the result.</param>
    /// <param name="warnings">Optional list of warning messages.</param>
    /// <returns>A successful <see cref="CommandResult{T}"/> containing the data.</returns>
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
    /// Creates a failure result with the specified error information.
    /// </summary>
    /// <param name="errorCode">The error code identifying the type of error.</param>
    /// <param name="errorMessage">A descriptive error message.</param>
    /// <param name="solution">An optional solution or suggestion to resolve the error.</param>
    /// <returns>A failed <see cref="CommandResult{T}"/> containing error details.</returns>
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
    /// Creates a successful result with warning messages.
    /// </summary>
    /// <param name="data">The data to include in the result.</param>
    /// <param name="warnings">A list of warning messages.</param>
    /// <returns>A successful <see cref="CommandResult{T}"/> containing the data and warnings.</returns>
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
