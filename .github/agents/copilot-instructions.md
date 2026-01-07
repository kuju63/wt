# wt Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-01-03

## Active Technologies
- C# / .NET 10.0 + System.CommandLine 2.0.1, System.IO.Abstractions 22.1.0 (004-complete-sbom-generation)
- N/A (CLI tool, no persistent storage) (004-complete-sbom-generation)

- C# .NET 10 (既存プロジェクトwt.cliに統合) (002-list-worktree-branches)
- N/A (ファイルシステムとGitの内部情報のみ使用) (002-list-worktree-branches)

- C# .NET 10 (既存プロジェクトに統合) + System.CommandLine (CLI フレームワーク), System.IO.Abstractions (パス操作の抽象化), Newtonsoft.Json または System.Text.Json (JSON出力) (001-create-worktree)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# .NET 10 (既存プロジェクトに統合)

## Code Style

C# .NET 10 (既存プロジェクトに統合): Follow standard conventions

## Recent Changes
- 004-complete-sbom-generation: Added C# / .NET 10.0 + System.CommandLine 2.0.1, System.IO.Abstractions 22.1.0

- 002-list-worktree-branches: Added C# .NET 10 (既存プロジェクトwt.cliに統合)

- 001-create-worktree: Added C# .NET 10 (既存プロジェクトに統合) + System.CommandLine (CLI フレームワーク), System.IO.Abstractions (パス操作の抽象化), Newtonsoft.Json または System.Text.Json (JSON出力)

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
