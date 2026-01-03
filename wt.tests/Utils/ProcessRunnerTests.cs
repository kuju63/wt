using FluentAssertions;
using wt.cli.Utils;
using Xunit;

namespace wt.tests.Utils;

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
        result.Should().NotBeNull();
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("git version");
        result.StandardError.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_WithInvalidCommand_ShouldReturnErrorResult()
    {
        // Arrange
        var runner = new ProcessRunner();

        // Act
        var result = await runner.RunAsync("git", "invalid-command");

        // Assert
        result.Should().NotBeNull();
        result.ExitCode.Should().NotBe(0);
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
            var result = await runner.RunAsync("pwd", "", tempDir);

            // Assert
            result.Should().NotBeNull();
            result.ExitCode.Should().Be(0);
            // macOSでは /private プレフィックスが付くことがあるため、両方を許容
            var output = result.StandardOutput.Trim();
            (output == tempDir || output == $"/private{tempDir}").Should().BeTrue();
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
            await runner.RunAsync("sleep", "10", cancellationToken: cts.Token);
        });
    }
}
