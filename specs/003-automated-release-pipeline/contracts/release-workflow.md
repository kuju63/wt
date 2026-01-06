# Contract: Release Workflow

**Workflow**: `.github/workflows/release.yml`  
**Feature**: 003-automated-release-pipeline  
**Date**: 2026-01-05

## 概要

mainブランチへのマージをトリガーとして、バージョン計算、ビルド、リリース作成を自動実行するワークフロー。

---

## トリガー

### Push to main

```yaml
on:
  push:
    branches:
      - main
```

**動作**: mainブランチへのプッシュで自動実行

### Manual dispatch (手動実行)

```yaml
on:
  workflow_dispatch:
    inputs:
      force-version:
        description: 'Force specific version (e.g., v1.0.0)'
        required: false
        type: string
```

**動作**: GitHub Actions UIから手動実行可能、バージョンを強制指定可能

---

## ジョブ定義

### Job 1: `calculate-version`

**Purpose**: 次のバージョン番号を計算する

**Outputs**:

| 出力               | 型      | 説明                 | 例              |
| ------------------ | ------- | -------------------- | --------------- |
| `version`          | string  | 新しいバージョンタグ | `v1.2.3`        |
| `previous-version` | string  | 前回のバージョンタグ | `v1.2.2`        |
| `should-release`   | boolean | リリース作成が必要か | `true`, `false` |

**ステップ**:

1. **Checkout repository** (fetch-depth: 0)
   - すべてのGit履歴を取得 (バージョン計算に必要)

2. **Calculate next version**
   - Action: `paulhatch/semantic-version@a8f8f59fd7f0625188492e945240f12d7ad2dca3` (v5.4.0)
   - 条件: `force-version`が指定されていない場合
   - パターン:
     - MAJOR: `(BREAKING CHANGE:|BREAKING:)`
     - MINOR: `feat:`
     - PATCH: (デフォルト)

3. **Use forced version**
   - 条件: `force-version`が指定されている場合
   - 動作: 指定されたバージョンを使用

4. **Check if release is needed**
   - 条件: バージョンが変更されている場合のみ `should-release=true`
   - 動作: バージョン変更なし → リリーススキップ

**バージョン計算ロジック**:

```text
Input: Commits since last tag
Logic:
  if any commit contains "BREAKING CHANGE:" → MAJOR++
  elif any commit starts with "feat:" → MINOR++
  elif any commit starts with "fix:" → PATCH++
  else → No version change
Output: New version tag (e.g., v1.3.0)
```

---

### Job 2: `build`

**Purpose**: 全プラットフォームのバイナリをビルドする

**Dependencies**: `calculate-version`

**Condition**: `needs.calculate-version.outputs.should-release == 'true'`

**Implementation**: `workflow_call` to `.github/workflows/build.yml`

**Inputs**:

- `version`: `${{ needs.calculate-version.outputs.version }}`

**詳細**: [build-workflow.md](build-workflow.md) を参照

---

### Job 3: `create-release`

**Purpose**: GitHub Releaseを作成し、アセットをアップロードする

**Dependencies**: `calculate-version`, `build`

**Timeout**: 25分 (SLA: 30分, 5分バッファ)

**ステップ**:

1. **Checkout repository** (fetch-depth: 0)
   - リリースノート生成のため全履歴を取得

2. **Download build artifacts**
   - Action: `actions/download-artifact@v4`
   - Path: `artifacts/`
   - 動作: ビルドジョブの全アーティファクトをダウンロード

3. **Organize binaries**
   - スクリプト: Bashスクリプト (inline)
   - 動作: バイナリを `release-assets/` ディレクトリに整理
   - ファイル名変更: `wt-{version}-{platform}-{arch}[.exe]`

4. **Generate checksums**
   - スクリプト: `.github/scripts/generate-checksums.sh`
   - 入力: `release-assets/`
   - 出力: `release-assets/SHA256SUMS`

5. **Generate SBOM**
   - Action: `anchore/sbom-action@61119d458adab75f756bc0b9e4bde25725f86a7a` (v0.17.2)
   - 入力: `./wt.cli`
   - 出力: `release-assets/wt-{version}-sbom.json`
   - フォーマット: CycloneDX JSON

6. **Sign artifacts**
   - スクリプト: `.github/scripts/sign-artifacts.sh`
   - 入力: `release-assets/`
   - 出力:
     - `release-assets/wt-{version}-sbom.json.asc`
     - `release-assets/SHA256SUMS.asc`
   - シークレット: `GPG_PRIVATE_KEY`, `GPG_PASSPHRASE`

7. **Generate release notes**
   - スクリプト: `.github/scripts/generate-release-notes.sh`
   - 引数:
     - `$1`: 前回バージョン
     - `$2`: 新バージョン
   - 出力: `release-notes.md`

8. **Create Git tag**
   - コマンド: `git tag -a {version} -m "Release {version}"`
   - プッシュ: `git push origin {version}`

9. **Create GitHub Release**
   - Action: `softprops/action-gh-release@a06a81a03ee405af7f2048a818ed3f03bbf83c7b` (v2)
   - Tag: `${{ needs.calculate-version.outputs.version }}`
   - Body: `release-notes.md`
   - Files:
     - `release-assets/wt-*-windows-*.exe`
     - `release-assets/wt-*-linux-*`
     - `release-assets/wt-*-macos-*`
     - `release-assets/wt-*-sbom.json`
     - `release-assets/wt-*-sbom.json.asc`
     - `release-assets/SHA256SUMS`
     - `release-assets/SHA256SUMS.asc`

10. **Release summary**
    - 動作: GitHub Actions Step Summaryにリリース情報を表示

---

## リリースアセット

### ファイル一覧

| File Pattern                    | Description                 | Size (approx) |
| ------------------------------- | --------------------------- | ------------- |
| `wt-v{version}-windows-x64.exe` | Windows x64 binary          | 15-20 MB      |
| `wt-v{version}-linux-x64`       | Linux x64 binary            | 15-20 MB      |
| `wt-v{version}-linux-arm`       | Linux ARM binary (optional) | 12-18 MB      |
| `wt-v{version}-macos-arm64`     | macOS ARM64 binary          | 15-20 MB      |
| `wt-v{version}-sbom.json`       | CycloneDX SBOM              | 200-500 KB    |
| `wt-v{version}-sbom.json.asc`   | SBOM GPG signature          | 1-5 KB        |
| `SHA256SUMS`                    | Checksums for all binaries  | 1-5 KB        |
| `SHA256SUMS.asc`                | Checksums GPG signature     | 1-5 KB        |

**Total size**: 45-80 MB (within GitHub Release limits)

---

## リリースノート形式

```markdown
## Features
- add support for multiple worktrees (#123)
- implement config file parsing (#124)

## Bug Fixes
- handle special characters in branch names (#125)
- fix path resolution on Windows (#126)

## Breaking Changes
- change CLI argument format to kebab-case

Migration guide: docs/migration/v2.0.md

## Contributors
- @user1
- @user2
```

**生成ロジック**:

1. 前回タグから現在までのコミットを取得
2. Conventional Commitsでパース
3. タイプ別にグループ化 (`feat:`, `fix:`, `BREAKING CHANGE:`)
4. Markdownフォーマットで出力

---

## バージョン計算例

### 例1: MINOR bump (feat:)

```text
Last tag: v1.2.3
Commits since last tag:
  - feat: add new feature
  - docs: update README
  - chore: update dependencies
  
Result: v1.3.0 (MINOR bump)
```

### 例2: PATCH bump (fix:)

```text
Last tag: v1.3.0
Commits since last tag:
  - fix: resolve critical bug
  - test: add unit tests
  
Result: v1.3.1 (PATCH bump)
```

### 例3: MAJOR bump (BREAKING CHANGE:)

```text
Last tag: v1.3.1
Commits since last tag:
  - feat: change API format
    
    BREAKING CHANGE: All CLI arguments now use kebab-case.
  
Result: v2.0.0 (MAJOR bump)
```

### 例4: No version change

```text
Last tag: v2.0.0
Commits since last tag:
  - docs: update documentation
  - chore: update CI configuration
  
Result: No release (version unchanged)
```

---

## エラーハンドリング

### ビルド失敗

- **MANDATORY プラットフォーム失敗**: リリース全体が中止
- **OPTIONAL プラットフォーム失敗**: 警告のみ、リリース続行

### SBOM生成失敗

- **動作**: リリース中止 (セキュリティクリティカル)
- **エラーコード**: `1`

### 署名生成失敗

- **動作**: リリース中止 (セキュリティクリティカル)
- **エラーコード**: `1`

### タイムアウト

- **制限**: 25分
- **動作**: ワークフロー強制終了、リリース未作成
- **ログ**: タイムアウトエラーメッセージ

---

## 性能要件 (SC-003)

| フェーズ       | 目標時間   | 許容時間   | SLA      |
| -------------- | ---------- | ---------- | -------- |
| バージョン計算 | < 1分      | < 2分      | -        |
| ビルド (並列)  | < 15分     | < 20分     | -        |
| リリース作成   | < 10分     | < 15分     | -        |
| **合計**       | **< 20分** | **< 30分** | **30分** |

---

## シークレット

| Name              | Purpose            | Format                 |
| ----------------- | ------------------ | ---------------------- |
| `GITHUB_TOKEN`    | GitHub Release作成 | 自動生成 (actions権限) |
| `GPG_PRIVATE_KEY` | デジタル署名       | ASCII-armored GPG key  |
| `GPG_PASSPHRASE`  | GPG鍵パスフレーズ  | Plain text             |

---

## 権限

```yaml
permissions:
  contents: write   # Git tag作成, Release作成
  packages: write   # (将来のパッケージ公開用)
```

---

## テスト

### シナリオ1: feat: コミット (MINOR bump)

```bash
git checkout -b test/feat
echo "test" >> README.md
git add README.md
git commit -m "feat: add new feature"
git push origin test/feat
gh pr create --title "feat: new feature" --body "Test"
gh pr merge --squash

# 期待される結果:
# - バージョン: v1.2.0 → v1.3.0
# - リリースノート: "Features" セクションに含まれる
# - リリース時間: < 30分
```

### シナリオ2: fix: コミット (PATCH bump)

```bash
git checkout -b test/fix
echo "fix" >> README.md
git add README.md
git commit -m "fix: resolve bug"
git push origin test/fix
gh pr create --title "fix: bug fix" --body "Test"
gh pr merge --squash

# 期待される結果:
# - バージョン: v1.3.0 → v1.3.1
# - リリースノート: "Bug Fixes" セクションに含まれる
```

### シナリオ3: BREAKING CHANGE: (MAJOR bump)

```bash
git checkout -b test/breaking
echo "breaking" >> README.md
git add README.md
git commit -m "feat: breaking change

BREAKING CHANGE: Change API format"
git push origin test/breaking
gh pr create --title "feat: breaking change" --body "Test"
gh pr merge --squash

# 期待される結果:
# - バージョン: v1.3.1 → v2.0.0
# - リリースノート: "Breaking Changes" セクションに強調表示
```

---

## リファレンス

- ワークフローファイル: [.github/workflows/release.yml](../../../.github/workflows/release.yml)
- ビルドワークフロー契約: [build-workflow.md](build-workflow.md)
- データモデル: [data-model.md](../data-model.md)
- タスク: [tasks.md](../tasks.md)
- ADR 0003: [セマンティックバージョニング](../../../docs/adr/0003-semantic-versioning-conventional-commits.md)
- ADR 0004: [タイムアウトとSLA](../../../docs/adr/0004-release-workflow-timeout-sla.md)
