using Kuju63.WorkTree.CommandLine.Utils;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Utils;

public class ProcessRunnerTests
{
    [Fact]
    public async Task RunAsync_WithValidCommand_ShouldReturnSuccessResult()
    {
        // Arrange
        var runner = new ProcessRunner();

        // Act
        var result = await runner.RunAsync("git", "--version");

        // Assert
        result.ShouldNotBeNull();
        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("git version");
        result.StandardError.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_WithInvalidCommand_ShouldReturnErrorResult()
    {
        // Arrange
        var runner = new ProcessRunner();

        // Act
        var result = await runner.RunAsync("git", "invalid-command");

        // Assert
        result.ShouldNotBeNull();
        result.ExitCode.ShouldNotBe(0);
    }

    [Fact]
    public async Task RunAsync_WithWorkingDirectory_ShouldExecuteInDirectory()
    {
        // Arrange
        var runner = new ProcessRunner();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = await runner.RunAsync("pwd", "", tempDir, CancellationToken.None);

            // Assert
            result.ShouldNotBeNull();
            result.ExitCode.ShouldBe(0);
            // macOSでは /private プレフィックスが付くことがあるため、両方を許容
            var output = result.StandardOutput.Trim();
            (output == tempDir || output == $"/private{tempDir}").ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task RunAsync_WithTimeout_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var runner = new ProcessRunner();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await runner.RunAsync("sleep", "10", null, cancellationToken: cts.Token);
        });
    }
}
