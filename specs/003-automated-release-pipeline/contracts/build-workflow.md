# Contract: Build Workflow

**Workflow**: `.github/workflows/build.yml`  
**Feature**: 003-automated-release-pipeline  
**Date**: 2026-01-05

## 概要

マルチプラットフォームバイナリのビルドを並列実行するワークフロー。

---

## トリガー

**Type**: `workflow_call` (再利用可能ワークフロー)

**Inputs**:

| パラメータ | 型     | 必須 | 説明                     | 例       |
| ---------- | ------ | ---- | ------------------------ | -------- |
| `version`  | string | ✅   | ビルドするバージョンタグ | `v1.2.3` |

**Outputs**:

| パラメータ     | 型     | 説明                     | 例                                      |
| -------------- | ------ | ------------------------ | --------------------------------------- |
| `build-status` | string | ビルドステータスサマリー | `success`, `partial-failure`, `failure` |

---

## ジョブ定義

### Job: `build`

**Purpose**: 各プラットフォームのバイナリをビルドする

**Strategy**:

```yaml
strategy:
  fail-fast: false  # OPTIONALプラットフォームの失敗を許容
  max-parallel: 4   # 全プラットフォームを並列ビルド
  matrix:
    include:
      - os: windows-latest
        platform: windows
        arch: x64
        rid: win-x64
        mandatory: true
        script: build-windows.sh
        artifact-name: wt-${{ inputs.version }}-windows-x64.exe
      
      - os: ubuntu-latest
        platform: linux
        arch: x64
        rid: linux-x64
        mandatory: true
        script: build-linux-x64.sh
        artifact-name: wt-${{ inputs.version }}-linux-x64
      
      - os: ubuntu-latest
        platform: linux
        arch: arm
        rid: linux-arm
        mandatory: false
        script: build-linux-arm.sh
        artifact-name: wt-${{ inputs.version }}-linux-arm
      
      - os: macos-latest
        platform: macos
        arch: arm64
        rid: osx-arm64
        mandatory: true
        script: build-macos-arm64.sh
        artifact-name: wt-${{ inputs.version }}-macos-arm64
```

**ステップ**:

1. **Checkout repository**
   - Action: `actions/checkout@v4`
   - Purpose: リポジトリコードを取得

2. **Setup .NET**
   - Action: `actions/setup-dotnet@v4`
   - Version: `10.0.x` (最新の.NET 10)
   - Purpose: .NET SDKをインストール

3. **Restore dependencies**
   - Command: `dotnet restore wt.sln`
   - Purpose: NuGetパッケージを復元

4. **Build {platform}-{arch}**
   - Script: `.github/scripts/{matrix.script}`
   - Args: `version`, `rid`
   - Continue on error: `!matrix.mandatory` (OPTIONALプラットフォームのみ)
   - Purpose: プラットフォーム固有のバイナリをビルド

5. **Check build result**
   - Condition: `always()`
   - Purpose: MANDATORYプラットフォームの失敗を検出
   - Exit code:
     - `1` if mandatory platform failed
     - `0` if optional platform failed (warning only)

6. **Upload build artifact**
   - Action: `actions/upload-artifact@v4`
   - Condition: `steps.build.outcome == 'success'`
   - Artifact name: `binary-{platform}-{arch}`
   - Path: `release-assets/{artifact-name}`
   - Purpose: ビルド成果物をアップロード

---

## プラットフォームマトリックス

### MANDATORYプラットフォーム (失敗でリリースブロック)

| Platform | Arch  | OS             | RID       | Script               |
| -------- | ----- | -------------- | --------- | -------------------- |
| Windows  | x64   | windows-latest | win-x64   | build-windows.sh     |
| Linux    | x64   | ubuntu-latest  | linux-x64 | build-linux-x64.sh   |
| macOS    | arm64 | macos-latest   | osx-arm64 | build-macos-arm64.sh |

### OPTIONALプラットフォーム (失敗でも警告のみ)

| Platform | Arch | OS            | RID       | Script             |
| -------- | ---- | ------------- | --------- | ------------------ |
| Linux    | arm  | ubuntu-latest | linux-arm | build-linux-arm.sh |

---

## ビルドスクリプト契約

各ビルドスクリプト (`.github/scripts/build-*.sh`) は以下の契約に従う:

**引数**:

1. `$1`: バージョンタグ (例: `v1.2.3`)
2. `$2`: Runtime Identifier (例: `linux-x64`)

**出力**:

- バイナリファイル: `release-assets/wt-{version}-{platform}-{arch}[.exe]`
- Exit code:
  - `0`: ビルド成功
  - `1`: ビルド失敗

**実装例** (`build-linux-x64.sh`):

```bash
#!/usr/bin/env bash
set -euo pipefail

VERSION=$1
RID=$2

echo "Building for Linux x64..."
mkdir -p release-assets

dotnet publish wt.cli/wt.cli.csproj \
  --configuration Release \
  --runtime "$RID" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:Version="${VERSION#v}" \
  --output release-assets/

# Rename binary
mv release-assets/wt release-assets/wt-${VERSION}-linux-x64

echo "✅ Build completed: release-assets/wt-${VERSION}-linux-x64"
```

---

## 成果物 (Artifacts)

ビルド成功後、以下のアーティファクトがアップロードされます:

| Artifact Name        | Platform    | Files    | Size (approx) |
| -------------------- | ----------- | -------- | ------------- |
| `binary-windows-x64` | Windows x64 | `wt.exe` | 15-20 MB      |
| `binary-linux-x64`   | Linux x64   | `wt`     | 15-20 MB      |
| `binary-linux-arm`   | Linux ARM   | `wt`     | 12-18 MB      |
| `binary-macos-arm64` | macOS ARM64 | `wt`     | 15-20 MB      |

**Retention**: 7日間 (GitHub Actionsデフォルト)

---

## エラーハンドリング

### MANDATORYプラットフォームの失敗

```yaml
- name: Check build result
  if: always()
  shell: bash
  run: |
    if [ "${{ steps.build.outcome }}" == "failure" ]; then
      if [ "${{ matrix.mandatory }}" == "true" ]; then
        echo "::error::MANDATORY platform ${{ matrix.platform }}-${{ matrix.arch }} build failed!"
        exit 1
      fi
    fi
```

**動作**:

- MANDATORYプラットフォームが失敗 → ジョブ全体が失敗
- OPTIONALプラットフォームが失敗 → 警告のみ、他のプラットフォームは続行

### OPTIONALプラットフォームの失敗

```yaml
- name: Build ${{ matrix.platform }}-${{ matrix.arch }}
  continue-on-error: ${{ !matrix.mandatory }}
```

**動作**:

- Linux ARMビルドが失敗 → 警告ログ、アーティファクトなし、他は続行

---

## 性能要件

| 指標                                | 目標                  | 許容範囲 |
| ----------------------------------- | --------------------- | -------- |
| 単一プラットフォームビルド時間      | < 10分                | < 15分   |
| 全プラットフォームビルド時間 (並列) | < 15分                | < 20分   |
| タイムアウト                        | 20分/プラットフォーム | -        |

---

## テスト

### ローカルビルドテスト

```bash
# Linux x64ビルド
.github/scripts/build-linux-x64.sh "v0.1.0-test" "linux-x64"

# 期待される結果:
# - release-assets/wt-v0.1.0-test-linux-x64 が生成される
# - Exit code: 0
```

### ワークフローテスト

```bash
# ワークフローを手動実行
gh workflow run build.yml --ref main
gh run watch
```

---

## 依存関係

**必要なツール**:

- .NET SDK 10.0.x
- Bash (ビルドスクリプト実行用)
- Git (リポジトリチェックアウト用)

**GitHub Actions**:

- `actions/checkout@v4`
- `actions/setup-dotnet@v4`
- `actions/upload-artifact@v4`

---

## セキュリティ

**権限**:

```yaml
permissions:
  contents: read  # リポジトリコードの読み取り
```

**シークレット**: なし (ビルドのみ、署名やアップロードは不要)

---

## リファレンス

- ワークフローファイル: [.github/workflows/build.yml](../../../.github/workflows/build.yml)
- ビルドスクリプト: [.github/scripts/](../../../.github/scripts/)
- データモデル: [data-model.md](../data-model.md)
- タスク: [tasks.md](../tasks.md#build-infrastructure-prerequisite-for-us1)
