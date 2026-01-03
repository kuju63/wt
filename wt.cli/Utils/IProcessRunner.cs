namespace wt.cli.Utils;

public interface IProcessRunner
{
    /// <summary>
    /// Run a process asynchronously
    /// </summary>
    Task<ProcessResult> RunAsync(string fileName, string arguments, string? workingDirectory = null, CancellationToken cancellationToken = default);
}
