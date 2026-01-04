using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Utils;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Integration;

/// <summary>
/// Integration tests for .git directory resolution in worktree scenarios.
/// These tests verify that the GitService correctly handles the case where
/// .git is a file (pointing to a git directory) rather than a directory itself.
/// </summary>
[Collection("Sequential Integration Tests")]
public class GitDirectoryResolutionTests : IDisposable
{
    private readonly string _testRepoPath;
    private readonly string _worktreePath;
    private readonly string _originalDirectory;

    public GitDirectoryResolutionTests()
    {
        SafeSetCurrentDirectory();
        _originalDirectory = Environment.CurrentDirectory;

        // Create main repository
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"wt-git-dir-test-main-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRepoPath);

        // Create worktree path
        _worktreePath = Path.Combine(Path.GetTempPath(), $"wt-git-dir-test-worktree-{Guid.NewGuid()}");

        InitializeGitRepository();
    }

    private void SafeSetCurrentDirectory()
    {
        try
        {
            var currentDir = Environment.CurrentDirectory;
            if (!Directory.Exists(currentDir))
            {
                Environment.CurrentDirectory = Path.GetTempPath();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error setting current directory in test ctor: {ex.Message}");
            Environment.CurrentDirectory = Path.GetTempPath();
        }
    }

    private void InitializeGitRepository()
    {
        Environment.CurrentDirectory = _testRepoPath;
        
        RunGitCommand("init");
        RunGitCommand("config user.email \"test@example.com\"");
        RunGitCommand("config user.name \"Test User\"");
        RunGitCommand("checkout -b main");

        var readmePath = Path.Combine(_testRepoPath, "README.md");
        File.WriteAllText(readmePath, "# Test Repository\n");
        RunGitCommand("add README.md");
        RunGitCommand("commit -m \"Initial commit\"");
    }

    private void RunGitCommand(string arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        process?.WaitForExit();
        
        if (process?.ExitCode != 0)
        {
            var error = process?.StandardError.ReadToEnd();
            throw new InvalidOperationException($"Git command failed: {arguments}\nError: {error}");
        }
    }

    public void Dispose()
    {
        RestoreOriginalDirectory();
        CleanupWorktrees();
        DeleteTestRepository();
        GC.SuppressFinalize(this);
    }

    private void RestoreOriginalDirectory()
    {
        try
        {
            if (Directory.Exists(_originalDirectory))
            {
                Environment.CurrentDirectory = _originalDirectory;
            }
            else
            {
                Environment.CurrentDirectory = Path.GetTempPath();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error restoring directory: {ex.Message}");
            try
            {
                Environment.CurrentDirectory = Path.GetTempPath();
            }
            catch
            {
                // Ignore if we can't set temp path either
            }
        }
    }

    private void CleanupWorktrees()
    {
        try
        {
            if (Directory.Exists(_testRepoPath))
            {
                Environment.CurrentDirectory = _testRepoPath;
                var result = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "worktree list --porcelain",
                    WorkingDirectory = _testRepoPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                result?.WaitForExit();
                var output = result?.StandardOutput.ReadToEnd() ?? "";
                
                foreach (var line in output.Split('\n'))
                {
                    if (line.StartsWith("worktree "))
                    {
                        var path = line.Substring(9).Trim();
                        if (path != _testRepoPath && Directory.Exists(path))
                        {
                            try
                            {
                                RunGitCommand($"worktree remove \"{path}\" --force");
                            }
                            catch
                            {
                                // Ignore cleanup errors
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private void DeleteTestRepository()
    {
        try
        {
            if (Directory.Exists(_testRepoPath))
            {
                Directory.Delete(_testRepoPath, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        try
        {
            if (Directory.Exists(_worktreePath))
            {
                Directory.Delete(_worktreePath, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task ListWorktreesAsync_WhenRunFromWorktree_ResolvesGitDirectoryCorrectly()
    {
        // Arrange - Create a worktree
        Environment.CurrentDirectory = _testRepoPath;
        RunGitCommand($"worktree add \"{_worktreePath}\" -b feature-test");

        // Switch to the worktree directory
        Environment.CurrentDirectory = _worktreePath;

        // Verify .git is a file, not a directory
        var gitPath = Path.Combine(_worktreePath, ".git");
        File.Exists(gitPath).ShouldBeTrue();
        Directory.Exists(gitPath).ShouldBeFalse();

        // Verify .git file contains gitdir reference
        var gitFileContent = File.ReadAllText(gitPath);
        gitFileContent.ShouldStartWith("gitdir:");

        // Act - List worktrees from within the worktree
        var processRunner = new ProcessRunner();
        var gitService = new GitService(processRunner);
        var result = await gitService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBeGreaterThanOrEqualTo(2); // Main repo + our worktree
        
        // Verify both worktrees are listed
        var mainWorktree = result.Data.FirstOrDefault(w => w.Path == _testRepoPath);
        var featureWorktree = result.Data.FirstOrDefault(w => w.Path == _worktreePath);
        
        mainWorktree.ShouldNotBeNull();
        featureWorktree.ShouldNotBeNull();
        featureWorktree!.Branch.ShouldBe("feature-test");
    }

    [Fact]
    public async Task ListWorktreesAsync_WhenRunFromMainRepo_ListsWorktreesWithTimestamps()
    {
        // Arrange - Create a worktree
        Environment.CurrentDirectory = _testRepoPath;
        RunGitCommand($"worktree add \"{_worktreePath}\" -b feature-timestamp");

        // Wait a moment to ensure timestamps are different
        await Task.Delay(100);

        // Act - List worktrees from main repository
        var processRunner = new ProcessRunner();
        var gitService = new GitService(processRunner);
        var result = await gitService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBeGreaterThanOrEqualTo(2);

        // Verify worktree has a creation timestamp
        var featureWorktree = result.Data.FirstOrDefault(w => w.Path == _worktreePath);
        featureWorktree.ShouldNotBeNull();
        
        // The timestamp should be recent (within last minute)
        var timeDiff = DateTime.Now - featureWorktree!.CreatedAt;
        timeDiff.TotalMinutes.ShouldBeLessThan(1);
    }

    [Fact]
    public void GitFile_InWorktree_ContainsValidGitdirReference()
    {
        // Arrange - Create a worktree
        Environment.CurrentDirectory = _testRepoPath;
        RunGitCommand($"worktree add \"{_worktreePath}\" -b feature-gitdir");

        // Act - Read .git file from worktree
        var gitFilePath = Path.Combine(_worktreePath, ".git");
        File.Exists(gitFilePath).ShouldBeTrue();

        var content = File.ReadAllText(gitFilePath);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Assert
        lines.Length.ShouldBeGreaterThan(0);
        lines[0].ShouldStartWith("gitdir:");
        
        var gitDirPath = lines[0].Substring(7).Trim();
        gitDirPath.ShouldNotBeEmpty();
        
        // The path should point to .git/worktrees/<name> directory
        gitDirPath.ShouldContain("worktrees");
        
        // Verify the referenced directory exists
        if (Path.IsPathRooted(gitDirPath))
        {
            Directory.Exists(gitDirPath).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task ListWorktreesAsync_WithMultipleWorktrees_ReturnsAllWorktrees()
    {
        // Arrange - Create multiple worktrees
        Environment.CurrentDirectory = _testRepoPath;
        
        var worktree2Path = Path.Combine(Path.GetTempPath(), $"wt-git-dir-test-worktree2-{Guid.NewGuid()}");
        var worktree3Path = Path.Combine(Path.GetTempPath(), $"wt-git-dir-test-worktree3-{Guid.NewGuid()}");

        try
        {
            RunGitCommand($"worktree add \"{_worktreePath}\" -b feature-1");
            RunGitCommand($"worktree add \"{worktree2Path}\" -b feature-2");
            RunGitCommand($"worktree add \"{worktree3Path}\" -b feature-3");

            // Switch to one of the worktrees
            Environment.CurrentDirectory = _worktreePath;

            // Act - List worktrees from within a worktree
            var processRunner = new ProcessRunner();
            var gitService = new GitService(processRunner);
            var result = await gitService.ListWorktreesAsync();

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Data.ShouldNotBeNull();
            result.Data!.Count.ShouldBe(4); // Main + 3 worktrees

            var worktreePaths = result.Data.Select(w => w.Path).ToList();
            worktreePaths.ShouldContain(_testRepoPath);
            worktreePaths.ShouldContain(_worktreePath);
            worktreePaths.ShouldContain(worktree2Path);
            worktreePaths.ShouldContain(worktree3Path);
        }
        finally
        {
            // Cleanup additional worktrees
            try
            {
                Environment.CurrentDirectory = _testRepoPath;
                if (Directory.Exists(worktree2Path))
                {
                    RunGitCommand($"worktree remove \"{worktree2Path}\" --force");
                    Directory.Delete(worktree2Path, recursive: true);
                }
                if (Directory.Exists(worktree3Path))
                {
                    RunGitCommand($"worktree remove \"{worktree3Path}\" --force");
                    Directory.Delete(worktree3Path, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
