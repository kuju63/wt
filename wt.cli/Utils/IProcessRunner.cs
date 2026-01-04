namespace Kuju63.WorkTree.CommandLine.Utils;

public interface IProcessRunner
{
    /// <summary>
    /// Run a process asynchronously. This overload does not accept a working directory or cancellation token.
    /// </summary>
    /// <param name="fileName">The executable or command name to run.</param>
    /// <param name="arguments">The command line arguments.</param>
    /// <returns>A <see cref="ProcessResult"/> containing the process exit code and output.</returns>
    Task<ProcessResult> RunAsync(string fileName, string arguments);

    /// <summary>
    /// Run a process asynchronously with a specified working directory. This overload does not accept a cancellation token.
    /// </summary>
    /// <param name="fileName">The executable or command name to run.</param>
    /// <param name="arguments">The command line arguments.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <returns>A <see cref="ProcessResult"/> containing the process exit code and output.</returns>
    Task<ProcessResult> RunAsync(string fileName, string arguments, string? workingDirectory);

    /// <summary>
    /// Run a process asynchronously with a specified working directory and cancellation token.
    /// </summary>
    /// <param name="fileName">The executable or command name to run.</param>
    /// <param name="arguments">The command line arguments.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="ProcessResult"/> containing the process exit code and output.</returns>
    Task<ProcessResult> RunAsync(string fileName, string arguments, string? workingDirectory, CancellationToken cancellationToken);
}
