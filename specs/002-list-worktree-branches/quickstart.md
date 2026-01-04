# Quickstart: Worktree List 機能の実装

**Phase**: 1 - Design & Contracts  
**Date**: 2026-01-04  
**Feature**: [spec.md](spec.md) | [plan.md](plan.md)

## Purpose

このクイックスタートガイドは、worktree一覧表示機能を実装する開発者向けの実践的な手順を提供します。TDDアプローチに従い、テストファーストで実装を進めます。

## Prerequisites

- .NET 10 SDK がインストールされていること
- Git 2.5以上がインストールされていること
- 既存プロジェクト（wt.cli、wt.tests）がビルド可能であること
- IDE（Visual Studio Code、Visual Studio、Rider等）がセットアップされていること

## Development Workflow

このプロジェクトは **TDD（テスト駆動開発）** を採用しています。以下のサイクルに従ってください：

```text
Red → Green → Refactor

1. Red: テストを書き、失敗を確認
2. Green: テストをパスする最小限の実装
3. Refactor: コード品質を向上（テストは常にパス）
```

## Phase 1: モデルクラスの実装

### Step 1.1: WorktreeInfoモデルのテスト作成（Red）

`wt.tests/Models/WorktreeInfoTests.cs`を作成:

```csharp
using Shouldly;
using Kuju63.WorkTree.CommandLine.Models;
using Xunit;

namespace Kuju63.WorkTree.Tests.Models;

public class WorktreeInfoTests
{
    [Fact]
    public void GetDisplayBranch_NormalBranch_ReturnsBranchName()
    {
        // Arrange
        var worktree = new WorktreeInfo
        {
            Path = "/path/to/worktree",
            Branch = "main",
            IsDetached = false,
            CommitHash = "abc1234567890abcdef1234567890abcdef1234",
            CreatedAt = DateTime.Now,
            Exists = true
        };

        // Act
        var result = worktree.GetDisplayBranch();

        // Assert
        result.ShouldBe("main");
    }

    [Fact]
    public void GetDisplayBranch_DetachedHead_ReturnsShortHashWithLabel()
    {
        // Arrange
        var worktree = new WorktreeInfo
        {
            Path = "/path/to/worktree",
            Branch = "abc1234567890abcdef1234567890abcdef1234",
            IsDetached = true,
            CommitHash = "abc1234567890abcdef1234567890abcdef1234",
            CreatedAt = DateTime.Now,
            Exists = true
        };

        // Act
        var result = worktree.GetDisplayBranch();

        // Assert
        result.ShouldBe("abc1234 (detached)");
    }

    [Fact]
    public void GetDisplayStatus_ExistingWorktree_ReturnsActive()
    {
        // Arrange
        var worktree = new WorktreeInfo
        {
            Path = "/path/to/worktree",
            Branch = "main",
            IsDetached = false,
            CommitHash = "abc1234567890abcdef1234567890abcdef1234",
            CreatedAt = DateTime.Now,
            Exists = true
        };

        // Act
        var result = worktree.GetDisplayStatus();

        // Assert
        result.ShouldBe("active");
    }

    [Fact]
    public void GetDisplayStatus_MissingWorktree_ReturnsMissing()
    {
        // Arrange
        var worktree = new WorktreeInfo
        {
            Path = "/path/to/worktree",
            Branch = "main",
            IsDetached = false,
            CommitHash = "abc1234567890abcdef1234567890abcdef1234",
            CreatedAt = DateTime.Now,
            Exists = false
        };

        // Act
        var result = worktree.GetDisplayStatus();

        // Assert
        result.ShouldBe("missing");
    }
}
```

**テスト実行**: `dotnet test` → **失敗を確認**（RedフェーズOK）

### Step 1.2: WorktreeInfoモデルの実装（Green）

`wt.cli/Models/WorktreeInfo.cs`を作成:

```csharp
namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Git worktreeの情報を表現します。
/// </summary>
public class WorktreeInfo
{
    public required string Path { get; init; }
    public required string Branch { get; init; }
    public bool IsDetached { get; init; }
    public required string CommitHash { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool Exists { get; init; }

    public string GetDisplayBranch()
    {
        if (IsDetached)
        {
            var shortHash = CommitHash.Length >= 7 
                ? CommitHash.Substring(0, 7) 
                : CommitHash;
            return $"{shortHash} (detached)";
        }
        return Branch;
    }

    public string GetDisplayStatus()
    {
        return Exists ? "active" : "missing";
    }
}
```

**テスト実行**: `dotnet test` → **成功を確認**（GreenフェーズOK）

### Step 1.3: リファクタリング（Refactor）

- コードスタイルの確認
- XMLドキュメントコメントの追加
- 必要に応じてヘルパーメソッドの抽出

**テスト実行**: `dotnet test` → **成功を維持**

## Phase 2: GitServiceの拡張

### Step 2.1: GitServiceのテスト追加（Red）

`wt.tests/Services/Git/GitServiceTests.cs`に追加:

```csharp
[Fact]
public async Task ListWorktreesAsync_MultipleWorktrees_ReturnsAllWorktrees()
{
    // Arrange
    var mockProcessRunner = new Mock<IProcessRunner>();
    mockProcessRunner
        .Setup(x => x.RunAsync("git", "worktree list --porcelain", It.IsAny<string>()))
        .ReturnsAsync(new ProcessResult
        {
            ExitCode = 0,
            Output = @"worktree /path/to/main
HEAD abc1234567890abcdef1234567890abcdef1234
branch refs/heads/main

worktree /path/to/feature
HEAD def5678901234567890abcdef1234567890abcde
branch refs/heads/feature-branch
",
            Error = string.Empty
        });

    var service = new GitService(mockProcessRunner.Object);

    // Act
    var result = await service.ListWorktreesAsync();

    // Assert
    result.Count().ShouldBe(2);
    result.First().Path.ShouldBe("/path/to/main");
    result.First().Branch.ShouldBe("main");
}
```

**テスト実行**: `dotnet test` → **失敗を確認**

### Step 2.2: GitServiceの実装（Green）

`wt.cli/Services/Git/IGitService.cs`にメソッド追加:

```csharp
Task<IEnumerable<WorktreeInfo>> ListWorktreesAsync();
```

`wt.cli/Services/Git/GitService.cs`に実装:

```csharp
public async Task<IEnumerable<WorktreeInfo>> ListWorktreesAsync()
{
    var result = await _processRunner.RunAsync(
        "git", 
        "worktree list --porcelain", 
        _repositoryPath);

    if (result.ExitCode != 0)
    {
        throw new GitException($"Failed to list worktrees: {result.Error}");
    }

    return ParseWorktreePorcelain(result.Output);
}

private IEnumerable<WorktreeInfo> ParseWorktreePorcelain(string output)
{
    var worktrees = new List<WorktreeInfo>();
    var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    
    string? currentPath = null;
    string? currentHead = null;
    string? currentBranch = null;
    bool isDetached = false;

    foreach (var line in lines)
    {
        if (line.StartsWith("worktree "))
        {
            // 前のworktreeを保存
            if (currentPath != null && currentHead != null)
            {
                worktrees.Add(CreateWorktreeInfo(
                    currentPath, currentHead, currentBranch, isDetached));
            }

            // 新しいworktree開始
            currentPath = line.Substring(9).Trim();
            currentHead = null;
            currentBranch = null;
            isDetached = false;
        }
        else if (line.StartsWith("HEAD "))
        {
            currentHead = line.Substring(5).Trim();
        }
        else if (line.StartsWith("branch "))
        {
            var branchRef = line.Substring(7).Trim();
            currentBranch = branchRef.Replace("refs/heads/", "");
        }
        else if (line == "detached")
        {
            isDetached = true;
        }
    }

    // 最後のworktreeを保存
    if (currentPath != null && currentHead != null)
    {
        worktrees.Add(CreateWorktreeInfo(
            currentPath, currentHead, currentBranch, isDetached));
    }

    return worktrees;
}

private WorktreeInfo CreateWorktreeInfo(
    string path, string commitHash, string? branch, bool isDetached)
{
    var createdAt = GetWorktreeCreationTime(path);
    var exists = Directory.Exists(path);

    return new WorktreeInfo
    {
        Path = path,
        Branch = branch ?? commitHash.Substring(0, Math.Min(7, commitHash.Length)),
        IsDetached = isDetached,
        CommitHash = commitHash,
        CreatedAt = createdAt,
        Exists = exists
    };
}

private DateTime GetWorktreeCreationTime(string worktreePath)
{
    try
    {
        // .git/worktrees/<name>/gitdir のタイムスタンプを取得
        var repoPath = FindGitDirectory();
        var worktreeName = Path.GetFileName(worktreePath);
        var gitdirPath = Path.Combine(repoPath, "worktrees", worktreeName, "gitdir");
        
        if (File.Exists(gitdirPath))
        {
            return File.GetCreationTime(gitdirPath);
        }
    }
    catch
    {
        // エラー時はデフォルト値
    }

    return DateTime.MinValue;
}
```

**テスト実行**: `dotnet test` → **成功を確認**

## Phase 3: TableFormatterの実装

### Step 3.1: TableFormatterのテスト作成（Red）

`wt.tests/Formatters/TableFormatterTests.cs`を作成:

```csharp
[Fact]
public void Format_MultipleWorktrees_ReturnsFormattedTable()
{
    // Arrange
    var worktrees = new List<WorktreeInfo>
    {
        new WorktreeInfo
        {
            Path = "/path/to/main",
            Branch = "main",
            IsDetached = false,
            CommitHash = "abc1234567890abcdef1234567890abcdef1234",
            CreatedAt = DateTime.Now,
            Exists = true
        },
        new WorktreeInfo
        {
            Path = "/path/to/feature",
            Branch = "feature",
            IsDetached = false,
            CommitHash = "def5678901234567890abcdef1234567890abcde",
            CreatedAt = DateTime.Now.AddMinutes(-10),
            Exists = true
        }
    };

    var formatter = new TableFormatter();

    // Act
    var result = formatter.Format(worktrees);

    // Assert
    result.ShouldContain("Path");
    result.ShouldContain("Branch");
    result.ShouldContain("Status");
    result.ShouldContain("/path/to/main");
    result.ShouldContain("/path/to/feature");
    result.ShouldContain("main");
    result.ShouldContain("feature");
}
```

**テスト実行**: `dotnet test` → **失敗を確認**

### Step 3.2: TableFormatterの実装（Green）

`wt.cli/Formatters/TableFormatter.cs`を作成:

```csharp
namespace Kuju63.WorkTree.CommandLine.Formatters;

public class TableFormatter
{
    public string Format(IEnumerable<WorktreeInfo> worktrees)
    {
        var list = worktrees.ToList();
        if (!list.Any())
        {
            return "No worktrees found in this repository.";
        }

        // 列データを準備
        var paths = list.Select(w => w.Path).ToList();
        var branches = list.Select(w => w.GetDisplayBranch()).ToList();
        var statuses = list.Select(w => w.GetDisplayStatus()).ToList();

        // 列幅を計算
        var pathWidth = CalculateColumnWidth("Path", paths);
        var branchWidth = CalculateColumnWidth("Branch", branches);
        var statusWidth = CalculateColumnWidth("Status", statuses);

        var widths = new[] { pathWidth, branchWidth, statusWidth };

        // テーブルを構築
        var sb = new StringBuilder();
        sb.AppendLine(GenerateSeparator(widths, "┌", "┬", "┐"));
        sb.AppendLine(GenerateRow(new[] { "Path", "Branch", "Status" }, widths));
        sb.AppendLine(GenerateSeparator(widths, "├", "┼", "┤"));

        for (int i = 0; i < list.Count; i++)
        {
            sb.AppendLine(GenerateRow(
                new[] { paths[i], branches[i], statuses[i] }, widths));
        }

        sb.AppendLine(GenerateSeparator(widths, "└", "┴", "┘"));

        return sb.ToString().TrimEnd();
    }

    private int CalculateColumnWidth(string header, List<string> values)
    {
        var maxContentWidth = values.Any() ? values.Max(v => v.Length) : 0;
        return Math.Max(header.Length, maxContentWidth);
    }

    private string GenerateSeparator(int[] widths, string left, string mid, string right)
    {
        var parts = widths.Select(w => new string('─', w + 2));
        return left + string.Join(mid, parts) + right;
    }

    private string GenerateRow(string[] values, int[] widths)
    {
        var parts = values.Select((v, i) => $" {v.PadRight(widths[i])} ");
        return "│" + string.Join("│", parts) + "│";
    }
}
```

**テスト実行**: `dotnet test` → **成功を確認**

## Phase 4: ListCommandの実装

### Step 4.1: ListCommandのテスト作成（Red）

`wt.tests/Commands/ListCommandTests.cs`を作成:

```csharp
[Fact]
public async Task Handle_MultipleWorktrees_DisplaysTable()
{
    // Arrange
    var mockService = new Mock<IWorktreeService>();
    mockService
        .Setup(x => x.ListWorktreesAsync())
        .ReturnsAsync(new List<WorktreeInfo>
        {
            new WorktreeInfo
            {
                Path = "/path/to/main",
                Branch = "main",
                IsDetached = false,
                CommitHash = "abc1234567890abcdef1234567890abcdef1234",
                CreatedAt = DateTime.Now,
                Exists = true
            }
        });

    var command = new ListCommand(mockService.Object);
    var console = new TestConsole();

    // Act
    var exitCode = await command.InvokeAsync(console);

    // Assert
    exitCode.ShouldBe(0);
    console.Out.ToString().ShouldContain("Path");
    console.Out.ToString().ShouldContain("main");
}
```

### Step 4.2: ListCommandの実装（Green）

`wt.cli/Commands/ListCommand.cs`を作成:

```csharp
using Kuju63.WorkTree.CommandLine.Formatters;

namespace Kuju63.WorkTree.CommandLine.Commands;

public class ListCommand : Command
{
    private readonly IWorktreeService _worktreeService;

    public ListCommand(IWorktreeService worktreeService) 
        : base("list", "List all worktrees with their branches")
    {
        _worktreeService = worktreeService;
        this.SetHandler(HandleAsync);
    }

    private async Task<int> HandleAsync()
    {
        try
        {
            var worktrees = await _worktreeService.ListWorktreesAsync();
            var sortedWorktrees = worktrees
                .OrderByDescending(w => w.CreatedAt)
                .ToList();

            // 警告メッセージ（存在しないworktree）
            foreach (var worktree in sortedWorktrees.Where(w => !w.Exists))
            {
                Console.Error.WriteLine(
                    $"Warning: Worktree at '{worktree.Path}' does not exist on disk");
            }

            // アクティブなworktreeのみを表示
            var activeWorktrees = sortedWorktrees.Where(w => w.Exists).ToList();
            var formatter = new TableFormatter();
            var output = formatter.Format(activeWorktrees);
            
            Console.WriteLine(output);
            return 0;
        }
        catch (GitException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 10;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            return 99;
        }
    }
}
```

**テスト実行**: `dotnet test` → **成功を確認**

## Phase 5: Program.csへの統合

`wt.cli/Program.cs`にコマンドを登録:

```csharp
var rootCommand = new RootCommand("Git worktree management tool");

// サービスのセットアップ
var gitService = new GitService(new ProcessRunner());
var worktreeService = new WorktreeService(gitService);

// コマンドの追加
rootCommand.AddCommand(new CreateCommand(worktreeService));
rootCommand.AddCommand(new ListCommand(worktreeService));  // 新規追加

return await rootCommand.InvokeAsync(args);
```

## Phase 6: 統合テスト

実際のGitリポジトリでテスト:

```bash
# Worktreeを作成
git worktree add ../test-wt test-branch

# コマンドを実行
dotnet run --project wt.cli -- list

# 期待される出力:
# ┌─────────────────────┬──────────────┬────────┐
# │ Path                │ Branch       │ Status │
# ├─────────────────────┼──────────────┼────────┤
# │ /path/to/test-wt    │ test-branch  │ active │
# │ /path/to/main       │ main         │ active │
# └─────────────────────┴──────────────┴────────┘

# クリーンアップ
git worktree remove ../test-wt
```

## Best Practices

### TDDサイクルの厳守

- 常にテストを先に書く（Red）
- 最小限の実装でテストをパスさせる（Green）
- リファクタリングでコード品質を向上（Refactor）
- 各サイクルでコミット

### テストの粒度

- ユニットテスト: 個別のメソッド・クラスのテスト
- 統合テスト: サービス間の連携テスト
- E2Eテスト: 実際のGitリポジトリでのテスト

### コミット戦略

```bash
# Red フェーズ
git add .
git commit -m "test: add tests for WorktreeInfo model"

# Green フェーズ
git add .
git commit -m "feat: implement WorktreeInfo model"

# Refactor フェーズ
git add .
git commit -m "refactor: improve WorktreeInfo code quality"
```

## Troubleshooting

### テストが失敗する

1. `dotnet test --logger "console;verbosity=detailed"`で詳細を確認
2. テストのArrange部分を確認（モックの設定等）
3. 実装が仕様に合っているか確認

### Gitコマンドが失敗する

1. Git 2.5以上がインストールされているか確認
2. カレントディレクトリがGitリポジトリか確認
3. `git worktree list --porcelain`を手動実行して出力を確認

## Next Steps

1. `/speckit.tasks`コマンドでタスク分解
2. 各タスクをTDDサイクルで実装
3. コードレビュー
4. ドキュメント更新

## Summary

このクイックスタートに従うことで:

- TDDアプローチで品質の高いコードを実装
- 各フェーズでテストをパスさせながら進行
- 仕様書と設計ドキュメントに準拠した実装

開発を開始する準備が整いました！
