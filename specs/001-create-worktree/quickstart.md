# Quickstart Guide: Worktree Create Feature

**Feature**: 001-create-worktree  
**Date**: 2026-01-03  
**Audience**: Developers implementing the feature

## Prerequisites

1. .NET 10 SDK インストール済み
2. Git 2.5+ インストール済み
3. 基本的なC#とGitの知識

## Development Setup

### 1. Clone & Build

```bash
# リポジトリのクローン
git clone https://github.com/your-org/wt.git
cd wt

# ビルド
dotnet build

# テスト実行
dotnet test
```

### 2. Project Structure

```text
wt/
├── wt.cli/                       # メインCLIプロジェクト
│   ├── Commands/                 # コマンド実装
│   │   └── WorktreeCreateCommand.cs
│   ├── Services/                 # ビジネスロジック
│   │   ├── GitService.cs
│   │   ├── WorktreeService.cs
│   │   └── EditorService.cs
│   ├── Models/                   # データモデル
│   │   ├── WorktreeInfo.cs
│   │   ├── BranchInfo.cs
│   │   ├── EditorConfig.cs
│   │   ├── CommandResult.cs
│   │   └── CreateWorktreeOptions.cs
│   └── Utils/                    # ユーティリティ
│       ├── PathHelper.cs
│       ├── ProcessRunner.cs
│       └── Validators.cs
└── wt.tests/                     # テストプロジェクト
    ├── Commands/
    ├── Services/
    ├── Models/
    └── Integration/
```

## Implementation Workflow

### Phase 1: Core Models (Day 1)

**Task**: データモデルとエラータイプの実装

```csharp
// Models/WorktreeInfo.cs
public record WorktreeInfo(
    string Path,
    string BranchName,
    string BaseBranch,
    DateTime CreatedAt
);

// Models/CommandResult.cs
public record CommandResult<T>(
    bool IsSuccess,
    T? Data = default,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    string? Solution = null
);
```

**Test First**:

```csharp
[Fact]
public void WorktreeInfo_ValidPath_ShouldCreate()
{
    var info = new WorktreeInfo(
        "/path/to/worktree",
        "feature-x",
        "main",
        DateTime.Now
    );
    
    info.Path.Should().Be("/path/to/worktree");
}
```

### Phase 2: Git Service (Day 2)

**Task**: Git操作のラッパー実装

```csharp
// Services/GitService.cs
public interface IGitService
{
    Task<CommandResult<bool>> IsGitRepositoryAsync();
    Task<CommandResult<string>> GetCurrentBranchAsync();
    Task<CommandResult<bool>> BranchExistsAsync(string branchName);
    Task<CommandResult<BranchInfo>> CreateBranchAsync(string name, string baseBranch);
}

public class GitService : IGitService
{
    private readonly IProcessRunner _processRunner;
    
    public async Task<CommandResult<bool>> IsGitRepositoryAsync()
    {
        var result = await _processRunner.RunAsync(
            "git", 
            "rev-parse --git-dir"
        );
        
        return result.ExitCode == 0
            ? CommandResult<bool>.Success(true)
            : CommandResult<bool>.Failure("GIT001", "Not a Git repository");
    }
}
```

**Test First**:

```csharp
[Fact]
public async Task IsGitRepository_WhenInGitRepo_ReturnsTrue()
{
    // Arrange
    _mockProcessRunner
        .Setup(x => x.RunAsync("git", "rev-parse --git-dir"))
        .ReturnsAsync(new ProcessResult(0, "", ""));
    
    // Act
    var result = await _gitService.IsGitRepositoryAsync();
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Data.Should().BeTrue();
}
```

### Phase 3: Worktree Service (Day 3)

**Task**: Worktree作成ロジック

```csharp
// Services/WorktreeService.cs
public interface IWorktreeService
{
    Task<CommandResult<WorktreeInfo>> CreateWorktreeAsync(
        CreateWorktreeOptions options
    );
}

public class WorktreeService : IWorktreeService
{
    private readonly IGitService _gitService;
    private readonly IPathHelper _pathHelper;
    
    public async Task<CommandResult<WorktreeInfo>> CreateWorktreeAsync(
        CreateWorktreeOptions options)
    {
        // 1. バリデーション
        var validation = ValidateOptions(options);
        if (!validation.IsSuccess) return validation;
        
        // 2. ブランチ作成
        var branch = await _gitService.CreateBranchAsync(
            options.BranchName, 
            options.BaseBranch
        );
        if (!branch.IsSuccess) return CommandResult<WorktreeInfo>.Failure(
            branch.ErrorCode, 
            branch.ErrorMessage
        );
        
        // 3. Worktree追加
        var worktreePath = _pathHelper.ResolveWorktreePath(
            options.Path ?? $"../worktrees/{options.BranchName}"
        );
        
        var result = await _gitService.AddWorktreeAsync(
            worktreePath, 
            options.BranchName
        );
        
        if (!result.IsSuccess) return CommandResult<WorktreeInfo>.Failure(
            result.ErrorCode, 
            result.ErrorMessage
        );
        
        // 4. 成功レスポンス
        return CommandResult<WorktreeInfo>.Success(
            new WorktreeInfo(
                worktreePath,
                options.BranchName,
                options.BaseBranch,
                DateTime.UtcNow
            )
        );
    }
}
```

### Phase 4: Editor Service (Day 4)

**Task**: エディター起動機能

```csharp
// Services/EditorService.cs
public interface IEditorService
{
    Task<CommandResult<bool>> LaunchEditorAsync(
        EditorType editor, 
        string path
    );
    CommandResult<string> ResolveEditorCommand(EditorType editor);
}

public class EditorService : IEditorService
{
    private static readonly Dictionary<EditorType, string[]> EditorCommands = new()
    {
        [EditorType.VSCode] = new[] { "code", "code-insiders" },
        [EditorType.Vim] = new[] { "vim" },
        [EditorType.Emacs] = new[] { "emacs" },
        [EditorType.Nano] = new[] { "nano" },
        [EditorType.Idea] = new[] { "idea" }
    };
    
    public async Task<CommandResult<bool>> LaunchEditorAsync(
        EditorType editor, 
        string path)
    {
        var command = ResolveEditorCommand(editor);
        if (!command.IsSuccess) return CommandResult<bool>.Failure(
            command.ErrorCode, 
            command.ErrorMessage
        );
        
        var result = await _processRunner.RunAsync(
            command.Data!, 
            path
        );
        
        return result.ExitCode == 0
            ? CommandResult<bool>.Success(true)
            : CommandResult<bool>.Failure("ED001", "Failed to launch editor");
    }
}
```

### Phase 5: CLI Command (Day 5)

**Task**: System.CommandLineでCLI統合

```csharp
// Commands/WorktreeCreateCommand.cs
public class WorktreeCreateCommand : Command
{
    public WorktreeCreateCommand() : base(
        "create", 
        "Create a new worktree with branch")
    {
        var branchNameArg = new Argument<string>(
            "branch-name",
            "Name of the branch to create"
        );
        
        var baseBranchOpt = new Option<string?>(
            aliases: new[] { "--base", "-b" },
            description: "Base branch (default: current branch)"
        );
        
        var pathOpt = new Option<string?>(
            aliases: new[] { "--path", "-p" },
            description: "Worktree path (default: ../worktrees/<branch-name>)"
        );
        
        var editorOpt = new Option<EditorType?>(
            aliases: new[] { "--editor", "-e" },
            description: "Editor to launch after creation"
        );
        
        AddArgument(branchNameArg);
        AddOption(baseBranchOpt);
        AddOption(pathOpt);
        AddOption(editorOpt);
        
        this.SetHandler(ExecuteAsync, 
            branchNameArg, 
            baseBranchOpt, 
            pathOpt, 
            editorOpt
        );
    }
    
    private async Task<int> ExecuteAsync(
        string branchName,
        string? baseBranch,
        string? path,
        EditorType? editor)
    {
        var options = new CreateWorktreeOptions
        {
            BranchName = branchName,
            BaseBranch = baseBranch ?? await _gitService.GetCurrentBranchAsync(),
            Path = path,
            Editor = editor
        };
        
        var result = await _worktreeService.CreateWorktreeAsync(options);
        
        if (!result.IsSuccess)
        {
            Console.WriteLine($"✗ Error: {result.ErrorMessage}");
            Console.WriteLine($"→ Solution: {result.Solution}");
            return 1;
        }
        
        Console.WriteLine($"✓ Created branch '{result.Data.BranchName}' from '{result.Data.BaseBranch}'");
        Console.WriteLine($"✓ Added worktree at: {result.Data.Path}");
        Console.WriteLine($"→ Next: cd {result.Data.Path}");
        
        return 0;
    }
}
```

## Testing Strategy

### Unit Tests (80% coverage target)

```csharp
// wt.tests/Services/WorktreeServiceTests.cs
public class WorktreeServiceTests
{
    [Fact]
    public async Task CreateWorktree_ValidOptions_CreatesSuccessfully()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-test",
            BaseBranch = "main",
            Path = null
        };
        
        // Act
        var result = await _service.CreateWorktreeAsync(options);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.BranchName.Should().Be("feature-test");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("feature..test")]
    [InlineData("feature@{test")]
    public async Task CreateWorktree_InvalidBranchName_ReturnsError(
        string invalidName)
    {
        // Arrange
        var options = new CreateWorktreeOptions 
        { 
            BranchName = invalidName 
        };
        
        // Act
        var result = await _service.CreateWorktreeAsync(options);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("BR001");
    }
}
```

### Integration Tests

```csharp
// wt.tests/Integration/WorktreeCreationTests.cs
public class WorktreeCreationTests : IDisposable
{
    private readonly string _testRepoPath;
    
    public WorktreeCreationTests()
    {
        // テスト用Gitリポジトリを作成
        _testRepoPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRepoPath);
        
        ProcessRunner.Run("git", "init", _testRepoPath);
        ProcessRunner.Run("git", "commit --allow-empty -m 'Initial'", _testRepoPath);
    }
    
    [Fact]
    public async Task EndToEnd_CreateWorktree_Success()
    {
        // Arrange
        var service = new WorktreeService(/* ... */);
        var options = new CreateWorktreeOptions
        {
            BranchName = "test-branch",
            BaseBranch = "main",
            Path = Path.Combine(_testRepoPath, "worktrees", "test-branch")
        };
        
        // Act
        var result = await service.CreateWorktreeAsync(options);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(result.Data.Path).Should().BeTrue();
    }
    
    public void Dispose()
    {
        Directory.Delete(_testRepoPath, true);
    }
}
```

### E2E Tests

```bash
# wt.tests/E2E/test-worktree-creation.sh
#!/bin/bash

# Setup
TEMP_REPO=$(mktemp -d)
cd "$TEMP_REPO"
git init
git commit --allow-empty -m "Initial commit"

# Test 1: Basic creation
../wt create feature-test
if [ $? -ne 0 ]; then
    echo "FAIL: Basic creation"
    exit 1
fi

# Test 2: With base branch
../wt create hotfix --base main
if [ $? -ne 0 ]; then
    echo "FAIL: With base branch"
    exit 1
fi

# Test 3: Invalid branch name
../wt create "invalid..name"
if [ $? -eq 0 ]; then
    echo "FAIL: Should reject invalid name"
    exit 1
fi

echo "PASS: All E2E tests"
```

## Development Tips

### 1. Test-Driven Development

```bash
# Red: テスト作成（失敗することを確認）
dotnet test

# Green: 最小限の実装
# code...

# Refactor: コード品質向上
# code...

# 確認
dotnet test
```

### 2. Debugging

```bash
# デバッグビルド
dotnet build --configuration Debug

# デバッガーアタッチ
dotnet run --project wt.cli -- worktree create test-branch
```

### 3. Local Testing

```bash
# テスト用リポジトリ作成
mkdir /tmp/test-repo
cd /tmp/test-repo
git init
git commit --allow-empty -m "Initial"

# wtコマンドをテスト
/path/to/wt/bin/Debug/net10.0/wt create feature-test

# 結果確認
git worktree list
```

## Common Issues & Solutions

### Issue 1: Git Not Found

**Problem**: `git` コマンドが見つからない

**Solution**:

```bash
# PATH確認
echo $PATH

# Gitインストール確認
which git

# 環境変数設定
export PATH="/usr/local/bin:$PATH"
```

### Issue 2: Permission Denied

**Problem**: Worktree作成時にパーミッションエラー

**Solution**:

```bash
# ディレクトリの権限確認
ls -la ../worktrees

# 権限変更
chmod 755 ../worktrees
```

### Issue 3: Branch Already Exists

**Problem**: 同名のブランチが既に存在

**Solution**:

```bash
# --checkout-existingオプションを使用
wt create feature-x --checkout-existing

# または別の名前を使用
wt create feature-x-v2
```

## Performance Benchmarks

```bash
# ベンチマーク実行
cd wt.tests
dotnet run --configuration Release --project Benchmarks

# 期待値:
# - バリデーション: < 100ms
# - ブランチ作成: < 1s
# - Worktree追加: < 2s
# - Total: < 5s
```

## Next Steps

1. **Phase 0完了後**: data-model.mdとresearch.mdをレビュー
2. **Phase 1開始**: CoreモデルとGitServiceから実装開始
3. **各Phase完了時**: テストが全てグリーンであることを確認
4. **Phase 5完了後**: E2Eテストで全シナリオを検証

## Resources

- [System.CommandLine Docs](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [Git Worktree Docs](https://git-scm.com/docs/git-worktree)
- [.NET Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/)
- [Specification Document](./spec.md)
- [Data Model](./data-model.md)
- [CLI Interface Contract](./contracts/cli-interface.md)

## Contact

質問やフィードバックは Issue または Pull Request でお願いします。
