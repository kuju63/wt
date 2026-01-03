using FluentAssertions;
using System.Diagnostics;
using System.IO.Abstractions;
using Moq;
using wt.cli.Utils;
using Xunit;
using Xunit.Abstractions;

namespace wt.tests.Performance;

[Collection("Sequential")]
public class PerformanceTests
{
    private readonly ITestOutputHelper _output;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BranchValidation_ShouldBeInstant()
    {
        // Arrange
        var testBranches = new[]
        {
            "feature-x",
            "bugfix/issue-123",
            "user_story_1",
            "release/v1.0.0",
            "hotfix-urgent"
        };

        // Act
        var stopwatch = Stopwatch.StartNew();

        foreach (var branch in testBranches)
        {
            var result = Validators.ValidateBranchName(branch);
            result.IsValid.Should().BeTrue();
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10,
            $"Branch validation took {stopwatch.ElapsedMilliseconds}ms, expected < 10ms");

        _output.WriteLine($"Validated {testBranches.Length} branches in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void PathNormalization_ShouldBeInstant()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var pathHelper = new PathHelper(mockFileSystem.Object);
        var testPaths = new[]
        {
            "../worktrees/test-1",
            "~/projects/test-2",
            "/absolute/path/test-3",
            "relative/path/test-4",
            "../../../deep/relative/test-5"
        };

        // Act
        var stopwatch = Stopwatch.StartNew();

        foreach (var path in testPaths)
        {
            var normalized = pathHelper.NormalizePath(path);
            normalized.Should().NotBeNullOrEmpty();
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10,
            $"Path normalization took {stopwatch.ElapsedMilliseconds}ms, expected < 10ms");

        _output.WriteLine($"Normalized {testPaths.Length} paths in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ConcurrentBranchValidation_ShouldScale()
    {
        // Arrange
        var branchCount = 100;
        var branches = Enumerable.Range(1, branchCount)
            .Select(i => $"feature-{i}")
            .ToArray();

        // Act
        var stopwatch = Stopwatch.StartNew();

        var tasks = branches.Select(async branch =>
        {
            await Task.Yield(); // Simulate async work
            return Validators.ValidateBranchName(branch);
        });

        var results = await Task.WhenAll(tasks);

        stopwatch.Stop();

        // Assert
        results.Should().AllSatisfy(r => r.IsValid.Should().BeTrue());
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
            $"Validated {branchCount} branches concurrently in {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");

        _output.WriteLine($"Validated {branchCount} branches concurrently in {stopwatch.ElapsedMilliseconds}ms");
    }
}
