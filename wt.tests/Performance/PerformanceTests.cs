using System.Diagnostics;
using System.IO.Abstractions;
using Kuju63.WorkTree.CommandLine.Utils;
using Moq;
using Shouldly;
using Xunit.Abstractions;

namespace Kuju63.WorkTree.Tests.Performance;

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
            result.IsValid.ShouldBeTrue();
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(10,
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
            (!string.IsNullOrEmpty(normalized)).ShouldBeTrue();
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(10,
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

        // Run multiple trials and use the median elapsed time to reduce flakiness
        const int trials = 5;
        var elapsedList = new List<long>(trials);

        for (int t = 0; t < trials; t++)
        {
            var stopwatch = Stopwatch.StartNew();

            var tasks = branches.Select(async branch =>
            {
                await Task.Yield(); // Simulate async work
                return Validators.ValidateBranchName(branch);
            });

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Ensure validation correctness for each trial
            foreach (var r in results) r.IsValid.ShouldBeTrue();

            elapsedList.Add(stopwatch.ElapsedMilliseconds);
        }

        elapsedList.Sort();
        var median = elapsedList[elapsedList.Count / 2];

        // Assert median is within expected threshold
        median.ShouldBeLessThan(100,
            $"Validated {branchCount} branches concurrently, median elapsed {median}ms over {trials} trials, expected < 100ms");

        _output.WriteLine($"Validated {branchCount} branches concurrently, median elapsed {median}ms over {trials} trials");
    }
}
