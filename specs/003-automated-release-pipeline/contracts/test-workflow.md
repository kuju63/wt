# Contract: Test Workflow

**Workflow**: `.github/workflows/test.yml`  
**Feature**: 003-automated-release-pipeline  
**Date**: 2026-01-05

## 概要

全ブランチでテストを実行し、コードカバレッジをCodacyに報告するワークフロー。

---

## トリガー

### Push to branches

```yaml
on:
  push:
    branches:
      - main
      - 'feature/**'
      - 'fix/**'
```

**動作**: main, feature, fix ブランチへのプッシュで自動実行

### Pull request to main

```yaml
on:
  pull_request:
    branches:
      - main
```

**動作**: mainブランチへのPRで自動実行

---

## ジョブ定義

### Job: `test`

**Purpose**: テストを実行し、カバレッジを報告する

**Runner**: `ubuntu-latest`

**ステップ**:

1. **Checkout repository**
   - Action: `actions/checkout@v4`
   - Purpose: テスト実行のためリポジトリコードを取得

2. **Setup .NET**
   - Action: `actions/setup-dotnet@v4`
   - Version: `10.0.x`
   - Purpose: .NET SDKをインストール

3. **Restore dependencies**
   - Command: `dotnet restore wt.sln`
   - Purpose: NuGetパッケージを復元

4. **Build solution**
   - Command: `dotnet build wt.sln --configuration Release --no-restore`
   - Purpose: Releaseモードでビルド

5. **Run tests with coverage**
   - Command:

     ```bash
     dotnet test wt.sln \
       --configuration Release \
       --no-build \
       --verbosity normal \
       --collect:"XPlat Code Coverage" \
       --results-directory ./coverage \
       --logger "trx;LogFileName=test-results.trx"
     ```

   - Purpose: テストを実行し、カバレッジを収集

6. **Find coverage file**
   - Command: `find ./coverage -name "coverage.cobertura.xml"`
   - Output: `coverage-file` (カバレッジファイルのパス)
   - Purpose: カバレッジファイルの場所を特定

7. **Generate coverage report**
   - Tool: `reportgenerator`
   - Input: `coverage.cobertura.xml`
   - Output: `./coverage/report/` (HTML + TextSummary)
   - Purpose: 人間が読めるカバレッジレポートを生成

8. **Upload coverage to Codacy**
   - Action: `codacy/codacy-coverage-reporter-action@a38818475bb21847788496e9f0fddaa4e84955ba`
   - Input: `coverage.cobertura.xml`
   - Token: `${{ secrets.CODACY_PROJECT_TOKEN }}`
   - Continue on error: `true` (Codacy失敗でもテストは続行)
   - Purpose: カバレッジをCodacyに報告

9. **Upload test results**
   - Action: `actions/upload-artifact@v4`
   - Condition: `always()`
   - Files:
     - `coverage/`
     - `**/TestResults/**/*.trx`
   - Retention: 7日
   - Purpose: テスト結果とカバレッジをアーティファクトとして保存

10. **Publish test results**
    - Action: `dorny/test-reporter@bdab7eb6dfb6be17ac3d72352f67e559a72c8db1` (v2)
    - Condition: `always()`
    - Reporter: `dotnet-trx`
    - Fail on error: `true` (テスト失敗でジョブ失敗)
    - Purpose: PRにテスト結果を表示

---

## テスト要件

### テストタイプ

| Type                | Required      | Tool     | Location                |
| ------------------- | ------------- | -------- | ----------------------- |
| Unit Tests          | ✅ Yes        | xUnit    | `wt.tests/`             |
| Integration Tests   | ⚠️ Recommended| xUnit    | `wt.tests/Integration/` |
| Coverage Collection | ✅ Yes        | coverlet | (自動)                  |

### カバレッジ目標

| Metric          | Target | Enforcement              |
| --------------- | ------ | ------------------------ |
| Line Coverage   | 80%    | ⚠️ Warning (not blocking)|
| Branch Coverage | 70%    | ⚠️ Warning (not blocking)|
| Method Coverage | 75%    | ⚠️ Warning (not blocking)|

**注意**: カバレッジ目標は aspirational goal であり、マージをブロックしません (ADR 0005参照)。

---

## 品質ゲート (Quality Gates)

### ブロッキング (Merge blocked)

1. **テスト失敗** → ❌ マージブロック
   - 条件: `dotnet test` が exit code 1 を返す
   - 理由: 回帰防止

### 非ブロッキング (Warning only)

1. **カバレッジ低下** → ⚠️ 警告のみ
   - 条件: カバレッジが前回より低下
   - 理由: 開発速度とのバランス

2. **Codacyアップロード失敗** → ⚠️ 警告のみ
   - 条件: Codacy APIエラー
   - 理由: 外部サービスの一時的障害を許容

---

## カバレッジレポート形式

### Cobertura XML

```xml
<?xml version="1.0"?>
<coverage line-rate="0.85" branch-rate="0.72">
  <packages>
    <package name="wt.cli" line-rate="0.85">
      <classes>
        <class name="Program" line-rate="1.0">
          <lines>
            <line number="10" hits="5" branch="false"/>
            <line number="11" hits="5" branch="true" condition-coverage="75% (3/4)"/>
          </lines>
        </class>
      </classes>
    </package>
  </packages>
</coverage>
```

### GitHub Step Summary

```markdown
### 📊 Coverage Summary

Summary
  Generated on: 2026-01-05 12:00:00
  Line coverage: 85.2%
  Branch coverage: 72.1%
  Method coverage: 78.5%
  
  Assemblies: 2
  Classes: 25
  Files: 30
  Covered lines: 1234
  Uncovered lines: 215
  Coverable lines: 1449
```

---

## Codacy連携

### プロジェクト設定

- **Organization**: `kuju63`
- **Repository**: `wt`
- **Token**: `${{ secrets.CODACY_PROJECT_TOKEN }}`

### アップロードフォーマット

- **Format**: Cobertura XML
- **Language**: C#
- **Coverage Tool**: coverlet (via `dotnet test --collect`)

### Codacyコメント例 (PR)

```markdown
## Codacy Coverage Report

Coverage: 85.2% (+2.5%)

Changes by file:
- wt.cli/Program.cs: 95% (+5%)
- wt.cli/Services/GitService.cs: 80% (-3%)

Overall: ✅ Coverage increased
```

---

## エラーハンドリング

### テスト失敗

```text
Error: Test failed with exit code 1
  
  Failed tests:
    - wt.tests.GitServiceTests.CreateWorktree_ShouldReturnSuccess
    - wt.tests.EditorServiceTests.OpenEditor_ShouldLaunchVSCode
```

**動作**: ジョブ失敗、PRマージブロック

### カバレッジファイル未検出

```text
Warning: Coverage file not found
```

**動作**: カバレッジアップロードスキップ、テストは続行

### Codacyアップロード失敗

```text
Warning: Failed to upload coverage to Codacy
Error: Project token is invalid
```

**動作**: 警告ログ、テストは続行 (continue-on-error: true)

---

## 性能要件

| Metric                       | Target | Acceptable |
| ---------------------------- | ------ | ---------- |
| Test execution time          | < 5分  | < 10分     |
| Coverage collection overhead | < 20%  | < 30%      |
| Total workflow time          | < 8分  | < 15分     |

---

## 権限

```yaml
permissions:
  contents: read        # リポジトリコード読み取り
  checks: write         # テスト結果の書き込み
  pull-requests: write  # PRへのコメント
```

---

## ブランチプロテクション設定

### Required Status Checks

```yaml
# GitHub Settings → Branches → main → Branch protection rules
protection:
  required_status_checks:
    strict: true
    checks:
      - "Test and Coverage / Run Tests"  # ✅ Required
```

**動作**:

- ✅ テスト成功 → PRマージ可能
- ❌ テスト失敗 → PRマージブロック
- ⚠️ Codacyアップロード失敗 → PRマージ可能 (非ブロッキング)

---

## テストデータ管理

### テストフィクスチャ

```csharp
// wt.tests/Fixtures/GitFixture.cs
public class GitFixture : IDisposable
{
    public string TempRepoPath { get; }
    
    public GitFixture()
    {
        TempRepoPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(TempRepoPath);
        // Initialize git repo
    }
    
    public void Dispose()
    {
        if (Directory.Exists(TempRepoPath))
        {
            Directory.Delete(TempRepoPath, recursive: true);
        }
    }
}
```

### モック

```csharp
// wt.tests/Mocks/MockProcessRunner.cs
public class MockProcessRunner : IProcessRunner
{
    public ProcessResult Run(string command, string args)
    {
        return new ProcessResult
        {
            ExitCode = 0,
            Output = "mocked output",
            Error = string.Empty
        };
    }
}
```

---

## ローカルテスト実行

### すべてのテストを実行

```bash
dotnet test wt.sln --configuration Release --verbosity normal
```

### カバレッジ付きで実行

```bash
dotnet test wt.sln \
  --configuration Release \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage
```

### カバレッジレポートを生成

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool

reportgenerator \
  -reports:"./coverage/**/coverage.cobertura.xml" \
  -targetdir:"./coverage/report" \
  -reporttypes:"Html;TextSummary"

# HTMLレポートを開く
open ./coverage/report/index.html
```

### 特定のテストを実行

```bash
dotnet test wt.tests/GitServiceTests.cs --filter "CreateWorktree"
```

---

## トラブルシューティング

### 問題: テストが失敗する

**解決**:

1. ローカルでテストを実行:

   ```bash
   dotnet test wt.sln --verbosity detailed
   ```

2. 失敗したテストのログを確認:

   ```bash
   cat TestResults/test-results.trx
   ```

3. テストを修正してプッシュ

### 問題: カバレッジが0%

**解決**:

1. カバレッジファイルを確認:

   ```bash
   find ./coverage -name "coverage.cobertura.xml"
   cat ./coverage/**/coverage.cobertura.xml
   ```

2. カバレッジ収集の設定を確認:

   ```bash
   dotnet test --collect:"XPlat Code Coverage"
   ```

### 問題: Codacyアップロード失敗

**解決**:

1. Codacyトークンを確認:

   ```bash
   gh secret list | grep CODACY
   ```

2. トークンを再設定:

   ```bash
   gh secret set CODACY_PROJECT_TOKEN
   ```

---

## リファレンス

- ワークフローファイル: [.github/workflows/test.yml](../../../.github/workflows/test.yml)
- テストプロジェクト: [wt.tests/](../../../wt.tests/)
- ADR 0005: [品質ゲート](../../../docs/adr/0005-quality-gates-testing-requirements.md)
- タスク: [tasks.md](../tasks.md#user-story-4-verify-all-changes-are-tested-with-coverage)
