namespace Kuju63.WorkTree.CommandLine.Utils;

public interface IProcessRunner
{
    /// <summary>
    /// Run a process asynchronously
    /// </summary>
    Task<ProcessResult> RunAsync(string fileName, string arguments, string? workingDirectory = null, CancellationToken cancellationToken = default);
}
