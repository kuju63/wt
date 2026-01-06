# Research: Automated Binary Release Pipeline

**Feature**: `003-automated-release-pipeline`  
**Date**: 2026-01-04  
**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

このドキュメントは、自動化されたバイナリリリースパイプラインの技術調査結果を記録します。

---

## 1. セマンティックバージョニング決定ログ

### 背景

プロジェクトでは、バージョン番号の自動管理が必要です。Conventional Commitsに基づいて、次のバージョン番号を自動計算する仕組みを構築します。

### 採用規約

- **Semantic Versioning 2.0.0** を採用
- フォーマット: `MAJOR.MINOR.PATCH` (例: `1.2.3`)
- プレリリース版: `MAJOR.MINOR.PATCH-alpha.1` (例: `1.0.0-alpha.1`)

### バージョンインクリメントルール

| Commit Type                                     | Version Bump       | 例            |
| ----------------------------------------------- | ------------------ | ------------- |
| `BREAKING CHANGE:` (footer)                     | MAJOR (X.0.0)      | 0.5.2 → 1.0.0 |
| `feat:`                                         | MINOR (0.X.0)      | 0.5.2 → 0.6.0 |
| `fix:`                                          | PATCH (0.0.X)      | 0.5.2 → 0.5.3 |
| その他 (docs, style, refactor, test, chore, ci) | バージョン変更なし | -             |

### 初期バージョン

- プロジェクト初回リリース: `v0.1.0`
- 初回MAJORリリース (安定版): `v1.0.0`

### バージョンタグ形式

- Gitタグ: `v1.2.3` (プレフィックス `v` を付ける)
- GitHub Release名: `v1.2.3`
- バイナリファイル名: `wt-v1.2.3-windows-x64.exe`

### 実装戦略

1. **バージョン計算スクリプト** (`.github/scripts/calculate-version.sh`)
   - 最新タグを取得: `git describe --tags --abbrev=0`
   - 前回リリース以降のコミットを解析: `git log <last-tag>..HEAD --pretty=format:%s`
   - Conventional Commitsをパース: 正規表現で `feat:`, `fix:`, `BREAKING CHANGE:` を検出
   - 次のバージョンを計算: MAJOR/MINOR/PATCH インクリメント
   - 新しいタグを作成: `git tag -a v1.2.3 -m "Release v1.2.3"`

2. **タグプッシュとリリース作成**
   - GitHub Actions workflow (`release.yml`) でタグをプッシュ
   - GitHub Releases APIでリリースを作成
   - 計算されたバージョンをリリース名とタグに使用

### 参考資料

- [Semantic Versioning 2.0.0](https://semver.org/)
- [Conventional Commits 1.0.0](https://www.conventionalcommits.org/)
- [GitHub Releases API](https://docs.github.com/en/rest/releases)

---

## 2. CycloneDX統合オプション

### CycloneDX統合の背景

Software Bill of Materials (SBOM) を生成し、依存関係の透明性を確保する必要があります。CycloneDX形式を採用します。

### 調査対象ツール

#### Option 1: dotnet-sbom (推奨)

- **ツール名**: Microsoft dotnet-sbom
- **リポジトリ**: <https://github.com/microsoft/sbom-tool>
- **対応言語**: .NET, NuGet, npm, Python, Go, Rust
- **出力形式**: CycloneDX JSON/XML, SPDX
- **インストール**: `dotnet tool install --global Microsoft.Sbom.DotNetTool`
- **使用例**:
  
  ```bash
  dotnet tool install --global Microsoft.Sbom.DotNetTool
  sbom-tool generate -b /path/to/project -bc /path/to/project -pn wt -pv 1.0.0 -ps kuju63 -nsb https://github.com/kuju63
  ```

- **メリット**:
  - Microsoft公式ツール
  - .NETプロジェクトに最適化
  - GitHub Actionsとの統合が容易
  - CycloneDX 1.4準拠
- **デメリット**:
  - マルチプラットフォームビルドの場合、各ビルドごとにSBOMを生成する必要がある
  - 集約SBOMの生成には追加ロジックが必要

#### Option 2: CycloneDX CLI

- **ツール名**: CycloneDX CLI
- **リポジトリ**: <https://github.com/CycloneDX/cyclonedx-cli>
- **対応言語**: 汎用 (任意のSBOM操作)
- **出力形式**: CycloneDX JSON/XML
- **インストール**: `npm install -g @cyclonedx/cyclonedx-cli` または GitHub Releasesからバイナリダウンロード
- **使用例**:
  
  ```bash
  cyclonedx-cli convert --input-file sbom1.json --output-file sbom-cyclonedx.xml --output-format xml
  cyclonedx-cli merge --input-files sbom1.json sbom2.json --output-file merged.json
  ```

- **メリット**:
  - 汎用ツール (言語非依存)
  - 複数SBOMのマージ機能あり
  - CycloneDX公式ツール
- **デメリット**:
  - .NET依存関係の自動検出機能なし (別ツールと併用が必要)
  - Node.js依存 (GitHub Actionsでのインストール必要)

#### Option 3: Syft (Anchore)

- **ツール名**: Syft
- **リポジトリ**: <https://github.com/anchore/syft>
- **対応言語**: .NET, Go, Python, Java, JavaScript, Ruby, Rust等
- **出力形式**: CycloneDX JSON/XML, SPDX, Syft JSON
- **インストール**: `curl -sSfL https://raw.githubusercontent.com/anchore/syft/main/install.sh | sh`
- **使用例**:

  ```bash
  syft packages dir:/path/to/project -o cyclonedx-json > sbom.json
  ```

- **メリット**:
  - 自動依存関係検出
  - マルチ言語対応
  - 高速
- **デメリット**:
  - .NETプロジェクトでは dotnet-sbom ほど詳細な情報を提供しない
  - 外部依存 (Anchore製品)

### 決定

**Option 4: anchore/sbom-action (GitHub Action)** を採用 ← 実装時に変更

理由:

- より広く使われている（1.5k+ stars）
- セットアップが簡単（dotnet tool installが不要）
- エラーハンドリングが充実
- CycloneDX 1.4準拠
- 保守コストの削減

### 実装

```yaml
- name: Generate SBOM
  uses: anchore/sbom-action@61119d458adab75f756bc0b9e4bde25725f86a7a # v0.17.2
  with:
    path: ./wt.cli
    format: cyclonedx-json
    output-file: release-assets/wt-${{ needs.calculate-version.outputs.version }}-sbom.json
```

**セキュリティ**: コミットID指定（`61119d45...`）でサプライチェーン攻撃を防止

**メリット**: 50行のカスタムスクリプトが4行のAction設定に簡素化

### 参考資料

- [CycloneDX Specification](https://cyclonedx.org/specification/overview/)
- [Microsoft SBOM Tool Documentation](https://github.com/microsoft/sbom-tool/blob/main/README.md)
- [NTIA Minimum Elements for SBOM](https://www.ntia.gov/report/2021/minimum-elements-software-bill-materials-sbom)

---

## 3. Conventional Commitsパーサーオプション

### 背景

Conventional Commitsメッセージを解析し、バージョンインクリメントを自動決定する必要があります。

### 調査対象ツール

#### Option 1: カスタムBashスクリプト (推奨)

- **実装**: `.github/scripts/calculate-version.sh`
- **依存**: git, grep, sed (GitHub Actions標準環境に含まれる)
- **使用例**:

  ```bash
  #!/usr/bin/env bash
  LAST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0")
  COMMITS=$(git log ${LAST_TAG}..HEAD --pretty=format:%s)
  
  if echo "$COMMITS" | grep -q "BREAKING CHANGE:"; then
    # MAJOR bump
    VERSION="X.0.0"
  elif echo "$COMMITS" | grep -q "^feat"; then
    # MINOR bump
    VERSION="0.X.0"
  elif echo "$COMMITS" | grep -q "^fix"; then
    # PATCH bump
    VERSION="0.0.X"
  else
    echo "No version bump needed"
    exit 1
  fi
  ```

- **メリット**:
  - 追加依存なし (標準Unixツールのみ)
  - GitHub Actionsで直接実行可能
  - カスタマイズが容易
  - デバッグが簡単
- **デメリット**:
  - 複雑なパース (スコープ、複数行body等) は実装が煩雑
  - テストが必要

#### Option 2: semantic-release

- **ツール名**: semantic-release
- **リポジトリ**: <https://github.com/semantic-release/semantic-release>
- **依存**: Node.js, npm
- **使用例**:

  ```bash
  npm install -g semantic-release
  npx semantic-release
  ```

- **メリット**:
  - Conventional Commits完全対応
  - プラグインエコシステム豊富
  - GitHub Releasesへの自動公開機能
  - リリースノート自動生成
- **デメリット**:
  - Node.js依存 (追加のランタイムが必要)
  - 設定が複雑 (.releaserc ファイル必要)
  - ビルドプロセスとの統合が必要

#### Option 3: commitlint + standard-version

- **ツール名**: commitlint + standard-version
- **リポジトリ**:
  - <https://github.com/conventional-changelog/commitlint>
  - <https://github.com/conventional-changelog/standard-version>
- **依存**: Node.js, npm
- **使用例**:

  ```bash
  npm install -g @commitlint/cli @commitlint/config-conventional standard-version
  npx commitlint --from HEAD~1 --to HEAD
  npx standard-version
  ```

- **メリット**:
  - commitlint: コミットメッセージバリデーション
  - standard-version: バージョンインクリメント + CHANGELOG生成
  - Conventional Commits標準対応
- **デメリット**:
  - Node.js依存
  - 2つのツールを組み合わせる必要がある
  - GitHub Actions統合には追加設定が必要

### 決定

**Option 4: paulhatch/semantic-version (GitHub Action)** を採用 ← 実装時に変更

理由:

- コミュニティメンテナンス（7.3k+ stars）による保守性向上
- 信頼性が高く、広く使われている実績
- カスタムスクリプトの保守コストを削減
- エラーハンドリングが充実
- Conventional Commits完全対応

### 実装

```yaml
- name: Calculate next version
  id: version
  uses: paulhatch/semantic-version@a8f8f59fd7f0625188492e945240f12d7ad2dca3 # v5.4.0
  with:
    tag_prefix: "v"
    major_pattern: "(BREAKING CHANGE:|BREAKING:)"
    minor_pattern: "feat:"
    version_format: "v${major}.${minor}.${patch}"
    bump_each_commit: false
    search_commit_body: true
```

**セキュリティ**: コミットID指定（`a8f8f59f...`）でサプライチェーン攻撃を防止

### 参考資料

- [Bash Parameter Expansion](https://www.gnu.org/software/bash/manual/html_node/Shell-Parameter-Expansion.html)
- [Git Log Formatting](https://git-scm.com/docs/git-log#_pretty_formats)
- [Semantic Versioning Parser in Bash](https://github.com/fsaintjacques/semver-tool)

---

## 4. GitHub Actions並行性制限

### 背景

複数プラットフォーム向けビルドを並行実行する際、GitHub Actionsの並行性制限を考慮する必要があります。

### 調査結果

#### GitHub-hostedランナーの制限

**Freeプラン (Public repositories)**:

- 並行ジョブ数: 20ジョブ
- macOSランナー: 5並行ジョブ
- 月間実行時間: 無制限
- ストレージ: 500MB

**Proプラン**:

- 並行ジョブ数: 40ジョブ
- macOSランナー: 5並行ジョブ
- 月間実行時間: 3,000分 (Linux/Windows), 600分 (macOS)

**Enterpriseプラン**:

- 並行ジョブ数: 180ジョブ
- macOSランナー: 50並行ジョブ
- 月間実行時間: 50,000分 (Linux/Windows), 10,000分 (macOS)

#### ビルドマトリックス設計

このプロジェクトでは、以下のプラットフォームを並行ビルドします:

1. Windows x64 (MANDATORY)
2. Linux x64 (MANDATORY)
3. Linux ARM (OPTIONAL)
4. Mac ARM64 (MANDATORY)

**並行ジョブ数**: 4ジョブ (Freeプランの制限内)

**ビルドマトリックス設定** (`.github/workflows/build.yml`):

```yaml
strategy:
  fail-fast: false  # OPTIONAL platformが失敗してもMANDATORYは続行
  matrix:
    include:
      - os: windows-latest
        platform: windows
        arch: x64
        rid: win-x64
        mandatory: true
      - os: ubuntu-latest
        platform: linux
        arch: x64
        rid: linux-x64
        mandatory: true
      - os: ubuntu-latest
        platform: linux
        arch: arm
        rid: linux-arm
        mandatory: false  # OPTIONAL
      - os: macos-latest
        platform: macos
        arch: arm64
        rid: osx-arm64
        mandatory: true
```

### 実行時間予測

各プラットフォームのビルド時間 (概算):

- Windows x64: 5-7分 (.NETビルド + テスト)
- Linux x64: 4-6分
- Linux ARM: 6-8分 (クロスコンパイル)
- Mac ARM64: 5-7分

並行実行により、最も遅いビルド (Linux ARM: 8分) に合わせて完了。

**リリース全体の実行時間目標**: 30分以内 (ビルド + SBOM + ハッシュ + リリース公開)

### 参考資料

- [GitHub Actions: Usage limits](https://docs.github.com/en/actions/learn-github-actions/usage-limits-billing-and-administration)
- [GitHub Actions: Matrix strategies](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions#jobsjob_idstrategymatrix)

---

## 5. Codacy統合ステータス

### 背景

テストカバレッジをCodacyに報告し、PRで品質ゲートを表示する必要があります。

### 現在のCodacy統合状況

- **Codacyプロジェクト**: 既に設定済み
- **カバレッジアップロード**: 未設定 (これから実装)
- **品質ゲート**: 未設定 (これから実装)

### Codacy Coverage Reporter

**ツール名**: Codacy Coverage Reporter  
**リポジトリ**: <https://github.com/codacy/codacy-coverage-reporter>  
**インストール**: GitHub Actions Marketplaceから利用可能

#### 使用例 (.github/workflows/test.yml)

```yaml
- name: Upload coverage to Codacy
  uses: codacy/codacy-coverage-reporter-action@v1
  with:
    project-token: ${{ secrets.CODACY_PROJECT_TOKEN }}
    coverage-reports: coverage/cobertura.xml
```

#### カバレッジフォーマット

.NETプロジェクトでは、**Cobertura XML形式**を使用:

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

出力ファイル: `coverage/**/coverage.cobertura.xml`

### Codacy品質ゲート設定

Codacyダッシュボードで以下を設定:

1. **カバレッジ閾値**: 80% (プロジェクト全体)
2. **PR品質ゲート**:
   - カバレッジが閾値を下回る場合: **警告** (マージはブロックしない)
   - テストが失敗する場合: **エラー** (マージをブロック)
3. **通知設定**:
   - PRコメントでカバレッジ変化を表示
   - Slackへの通知 (オプション)

### ワークフロー統合

1. **test.yml**: 全ブランチでテスト実行、カバレッジ収集、Codacyアップロード
2. **release.yml**: mainマージ時、test.ymlが成功した場合のみリリース作成
3. **ブランチ保護ルール**: `test.yml` workflowの成功をmainマージの必須条件に設定

### 参考資料

- [Codacy Coverage Reporter](https://github.com/codacy/codacy-coverage-reporter)
- [dotnet test coverage options](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)

---

## 6. プラットフォーム固有コンパイラ可用性

### 背景

GitHub-hosted runnersで各プラットフォーム向けビルドが可能か確認する必要があります。

### GitHub-hosted runners環境

#### Ubuntu Latest (linux)

- **OS**: Ubuntu 22.04
- **.NET SDK**: .NET 6.0, 7.0, 8.0 プリインストール
- **クロスコンパイル**: Linux x64ネイティブビルド可能、ARM向けクロスコンパイルも可能
- **利用可能ツール**: gcc, clang, make, cmake, git, curl, wget

**ビルドコマンド**:

```bash
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r linux-arm --self-contained
```

#### Windows Latest (windows)

- **OS**: Windows Server 2022
- **.NET SDK**: .NET 6.0, 7.0, 8.0 プリインストール
- **ビルドツール**: MSBuild, Visual Studio Build Tools
- **利用可能ツール**: git, PowerShell, Chocolatey

**ビルドコマンド**:

```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

#### macOS Latest (macos)

- **OS**: macOS 14 (Sonoma)
- **.NET SDK**: .NET 6.0, 7.0, 8.0 プリインストール
- **アーキテクチャ**: ARM64 (Apple Silicon)
- **利用可能ツール**: Xcode, Homebrew, git

**ビルドコマンド**:

```bash
dotnet publish -c Release -r osx-arm64 --self-contained
```

### .NETランタイム識別子 (RID)

| プラットフォーム | RID         | ランナー         | ビルド可否         |
| ---------------- | ----------- | ---------------- | ------------------ |
| Windows x64      | `win-x64`   | `windows-latest` | ✅ ネイティブ       |
| Linux x64        | `linux-x64` | `ubuntu-latest`  | ✅ ネイティブ       |
| Linux ARM        | `linux-arm` | `ubuntu-latest`  | ✅ クロスコンパイル |
| macOS ARM64      | `osx-arm64` | `macos-latest`   | ✅ ネイティブ       |

### 追加依存関係

特になし。.NET SDKが全てのランナーにプリインストールされているため、追加のセットアップは不要。

### 参考資料

- [GitHub Actions: Runner Images](https://github.com/actions/runner-images)
- [.NET RID Catalog](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)

---

## 7. デジタル署名ツール選定

### 背景

SBOM と SHA256SUMS ファイルにデジタル署名を付与し、改ざん検証を可能にする必要があります (FR-011)。

### 調査対象ツール

#### Option 1: GPG (GNU Privacy Guard)

- **ツール名**: GPG
- **バージョン**: 2.x
- **使用例**:

  ```bash
  gpg --armor --detach-sign --default-key YOUR_KEY_ID sbom.json
  # 生成: sbom.json.asc
  ```

- **メリット**:
  - 業界標準ツール
  - GitHub Actions環境にプリインストール
  - OpenPGP準拠
  - 公開鍵の配布が容易
- **デメリット**:
  - 秘密鍵の管理が必要
  - パスフレーズ入力が必要 (自動化時は環境変数で対応)

#### Option 2: Cosign (Sigstore)

- **ツール名**: Cosign
- **リポジトリ**: <https://github.com/sigstore/cosign>
- **使用例**:

  ```bash
  cosign sign-blob --key cosign.key sbom.json > sbom.json.sig
  cosign verify-blob --key cosign.pub --signature sbom.json.sig sbom.json
  ```

- **メリット**:
  - モダンな署名ツール
  - Keyless署名対応 (OIDC認証)
  - コンテナイメージ署名にも使用可能
  - 透明性ログ (Rekor) 統合
- **デメリット**:
  - GitHub Actions環境に手動インストールが必要
  - 比較的新しいツール (学習コストあり)
  - GPGほど広く普及していない

#### Option 3: GitHub Actions Native Attestation

- **ツール名**: GitHub Actions Attestation (Beta)
- **使用例**:

  ```yaml
  - name: Attest Build Provenance
    uses: actions/attest-build-provenance@v1
    with:
      subject-path: 'wt-v1.0.0-windows-x64.exe'
  ```

- **メリット**:
  - GitHub公式機能
  - 追加ツール不要
  - 自動的に検証可能
  - ビルドプロベナンス情報も含む
- **デメリット**:
  - Beta機能 (安定性未知)
  - GitHub外での検証が困難
  - SBOMファイル自体の署名には適さない (バイナリのみ)

### 決定

Option 1: GPG を採用

理由:

- GitHub Actions環境に標準でインストール済み
- 業界標準で広く認知されている
- SBOM と SHA256SUMS の両方に署名可能
- 公開鍵を簡単に配布・検証できる

### 実装計画

1. **GPG鍵ペア生成** (ローカル作業)

   ```bash
   gpg --full-generate-key
   # 鍵タイプ: RSA and RSA (default)
   # 鍵サイズ: 4096
   # 有効期限: 2年
   # 名前: Release Pipeline Bot
   # メール: release@kuju63.example.com
   ```

2. **秘密鍵のエクスポートとシークレット設定**

   ```bash
   gpg --armor --export-secret-keys YOUR_KEY_ID
   # 出力を SECRETS.md に従って GitHub Secrets に設定
   ```

3. **署名スクリプト作成** (`.github/scripts/sign-artifacts.sh`)

   ```bash
   #!/usr/bin/env bash
   echo "$GPG_PRIVATE_KEY" | gpg --batch --import
   echo "$GPG_PASSPHRASE" | gpg --batch --yes --passphrase-fd 0 --armor --detach-sign sbom.json
   echo "$GPG_PASSPHRASE" | gpg --batch --yes --passphrase-fd 0 --armor --detach-sign SHA256SUMS
   # 生成: sbom.json.asc, SHA256SUMS.asc
   ```

4. **公開鍵の配布**
   - `docs/GPG_PUBLIC_KEY.asc` としてリポジトリに保存
   - GitHub Releaseの説明に公開鍵のフィンガープリントを記載

5. **検証方法のドキュメント化** (`quickstart.md`)

   ```bash
   # 公開鍵のインポート
   gpg --import docs/GPG_PUBLIC_KEY.asc
   
   # 署名の検証
   gpg --verify sbom.json.asc sbom.json
   gpg --verify SHA256SUMS.asc SHA256SUMS
   ```

### 参考資料

- [GPG Manual](https://www.gnupg.org/documentation/manuals/gnupg/)
- [GitHub: Managing GPG keys](https://docs.github.com/en/authentication/managing-commit-signature-verification/generating-a-new-gpg-key)
- [Sigstore Cosign](https://docs.sigstore.dev/cosign/overview/)

---

## まとめ

| 技術決定項目                 | 選択ツール/手法                                  | 理由                                              |
| ---------------------------- | ------------------------------------------------ | ------------------------------------------------- |
| セマンティックバージョニング | **paulhatch/semantic-version** (Action)          | コミュニティメンテ、保守コスト削減、7.3k+ stars   |
| CycloneDX統合                | **anchore/sbom-action** (Action)                 | セットアップ簡単、1.5k+ stars、保守コスト削減     |
| Conventional Commitsパーサー | paulhatch/semantic-version内蔵                   | Action統合済み、追加実装不要                      |
| 並行ビルド                   | GitHub Actionsマトリックス (**max-parallel: 4**) | **全4プラットフォーム並列実行保証、70-75%高速化** |
| カバレッジ報告               | Codacy Coverage Reporter (コミットID指定)        | 既存Codacy統合活用、PR品質ゲート                  |
| デジタル署名                 | GPG 2.x                                          | 業界標準、プリインストール、広く普及              |
| **セキュリティ強化**         | **全サードパーティActionコミットID指定**         | **サプライチェーン攻撃防止、不変参照**            |
| **認証**                     | **GITHUB_TOKEN (自動提供)**                      | **Personal Access Token不要、安全**               |

### 実装時の主要変更点

1. **バージョン計算**: カスタムスクリプト削除 → `paulhatch/semantic-version@a8f8f59f` 採用
2. **SBOM生成**: カスタムスクリプト削除 → `anchore/sbom-action@61119d45` 採用
3. **セキュリティ**: 全サードパーティActions（5つ）をコミットID指定に変更
   - paulhatch/semantic-version@a8f8f59f (v5.4.0)
   - anchore/sbom-action@61119d45 (v0.17.2)
   - codacy/codacy-coverage-reporter-action@a3881847
   - dorny/test-reporter@bdab7eb6 (v2)
   - softprops/action-gh-release@a06a81a0 (v2)
4. **並列化**: build.ymlに`max-parallel: 4`追加で全プラットフォーム同時ビルド保証
5. **認証**: GH_RELEASE_TOKEN削除、GITHUB_TOKEN（自動提供）使用
6. **ドキュメント**: CONVENTIONAL_COMMITS.mdを`.github/`から`docs/ja/`に移動

全ての技術調査と実装が完了しました。

---

**最終更新**: 2026-01-05  
**担当者**: Release Pipeline Team
