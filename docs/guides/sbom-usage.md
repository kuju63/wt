# SBOM生成 クイックスタートガイド

このガイドでは、wtプロジェクトのSBOM（Software Bill of Materials）生成機能の使用方法を説明します。

---

## 📋 目次

1. [概要](#概要)
2. [ユーザー向けガイド](#ユーザー向けガイド)
3. [開発者向けガイド](#開発者向けガイド)
4. [トラブルシューティング](#トラブルシューティング)

---

## 概要

### SBOMとは？

SBOM（Software Bill of Materials）は、ソフトウェアの「部品表」です。アプリケーションが使用しているすべての依存関係（ライブラリ、パッケージ）を一覧化したドキュメントで、以下の目的で使用されます：

- **サプライチェーンの透明性**: ソフトウェアに含まれる全コンポーネントを明示
- **脆弱性管理**: 依存関係の脆弱性を追跡・修正
- **ライセンスコンプライアンス**: 使用ライブラリのライセンスを確認
- **監査証跡**: セキュリティ監査やコンプライアンスチェックに利用

### wtプロジェクトのSBOM

wtプロジェクトは、各リリースに対して以下を提供します：

✅ **SPDX 2.3形式のSBOM** - ISO/IEC 5962:2021準拠  
✅ **GitHub依存関係グラフ統合** - Dependabot/Renovateアラート自動有効化  
✅ **リリースアセット添付** - 誰でもダウンロード可能

---

## ユーザー向けガイド

### SBOMの取得方法

#### 方法1: GitHubリリースからダウンロード（推奨）

1. [wtのリリースページ](https://github.com/kuju63/wt/releases)にアクセス
2. 使用しているバージョンを選択（例: v1.0.0）
3. **Assets** セクションから `wt-v1.0.0-sbom.spdx.json` をダウンロード

```bash
# 最新リリースからダウンロード（Linux/macOS）
VERSION=$(curl -s https://api.github.com/repos/kuju63/wt/releases/latest | grep '"tag_name":' | sed -E 's/.*"([^"]+)".*/\1/')
curl -L https://github.com/kuju63/wt/releases/download/${VERSION}/wt-${VERSION}-sbom.spdx.json \
  -o wt-sbom.spdx.json

# または特定バージョンを指定してダウンロード
curl -L https://github.com/kuju63/wt/releases/download/v1.0.0/wt-v1.0.0-sbom.spdx.json \
  -o wt-sbom.spdx.json
```

#### 方法2: GitHub依存関係グラフから確認

1. [wtリポジトリ](https://github.com/kuju63/wt)にアクセス
2. **Insights** タブをクリック
3. **Dependency graph** を選択
4. すべての依存関係とバージョンを確認

### SBOMの検証

SBOMが正しいフォーマットか検証する方法：

```bash
# SPDX検証ツールをインストール
npm install -g @spdx/spdx-validator

# SBOMを検証
spdx-validator wt-sbom.spdx.json
```

成功時の出力：

```text
✓ SPDX document is valid
```

### SBOMの内容を確認

#### 依存関係の一覧表示

```bash
# jqを使用してパッケージ名とバージョンを抽出
jq -r '.packages[] | "\(.name)@\(.versionInfo)"' wt-sbom.spdx.json
```

出力例：

```text
System.CommandLine@2.0.1
System.IO.Abstractions@22.1.0
System.Memory@4.5.5
...
```

#### ライセンス情報の確認

```bash
# パッケージ名とライセンスを表示
jq -r '.packages[] | "\(.name): \(.licenseDeclared)"' wt-sbom.spdx.json
```

出力例：

```text
System.CommandLine: MIT
System.IO.Abstractions: MIT
System.Memory: MIT
...
```

### 脆弱性チェック

#### 方法1: GitHub Dependabot（自動）

wtプロジェクトは自動的にDependabotが有効化されており、脆弱性が発見されると：

1. リポジトリの **Security** タブにアラート表示
2. メンテナに自動通知
3. 可能であれば自動修正PRが作成される

#### 方法2: ローカルでのスキャン

```bash
# OSV Scannerを使用（推奨）
osv-scanner --sbom=wt-sbom.spdx.json

# Grypeを使用
grype sbom:wt-sbom.spdx.json
```

### エンタープライズ向け: SBOM統合

#### 1. Dependency-Trackへのインポート

```bash
# Dependency-Track APIを使用
curl -X POST "https://your-dependency-track.com/api/v1/bom" \
  -H "X-Api-Key: YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d @wt-sbom.spdx.json
```

#### 2. コンプライアンスチェック

```bash
# SPDX Toolsを使用してライセンスコンプライアンスを確認
spdx-tools verify wt-sbom.spdx.json
```

---

## 開発者向けガイド

### ローカルでのSBOM生成

開発中にローカルでSBOMを生成する方法：

#### 1. Microsoft SBOM Toolのインストール

```bash
dotnet tool install --global Microsoft.Sbom.DotNetTool
```

#### 2. 依存関係のリストア

```bash
cd /path/to/wt
dotnet restore --locked-mode
```

#### 3. SBOM生成

```bash
sbom-tool generate \
  -b ./sbom-output \
  -bc . \
  -pn kuju63/wt \
  -pv 1.0.0-dev \
  -nsb https://github.com/kuju63/wt/sbom/dev
```

#### パラメータ説明

- `-b`: 出力ディレクトリ
- `-bc`: ビルドコンポーネントのルートパス
- `-pn`: パッケージ名
- `-pv`: バージョン
- `-nsb`: 名前空間URI（一意性保証）

生成されたSBOMは `./sbom-output/_manifest/spdx_2.2/manifest.spdx.json` に出力されます。

### CI/CDでの動作確認

#### PR時の自動テスト（推奨）

Pull Requestを作成すると、SBOM生成テストが自動実行されます：

```bash
# ブランチを作成してPRを開く
git checkout -b feature/add-dependency
git push origin feature/add-dependency
gh pr create --title "Add new dependency"

# テスト結果を確認
gh pr checks
```

PR時テストで確認される項目：

- ✅ 依存関係のリストアが成功するか
- ✅ SBOM生成が成功するか
- ✅ SPDX 2.3フォーマットが正しいか
- ✅ 必須パッケージ（System.CommandLine等）が含まれているか
- ✅ パフォーマンス目標（15分以内）を満たしているか

#### ワークフローの手動実行

```bash
# リリースワークフローを手動トリガー
gh workflow run release.yml \
  --ref feature/your-branch

# SBOM テストワークフローを手動実行
gh workflow run sbom-test.yml \
  --ref feature/your-branch

# 実行状況の確認
gh run list --workflow=release.yml --limit 1
gh run list --workflow=sbom-test.yml --limit 1
```

#### ローカルでのワークフロー検証

```bash
# act を使用してGitHub Actionsをローカル実行
act release --secret GITHUB_TOKEN=your_token
```

### SBOM生成のカスタマイズ

#### プラットフォーム固有の依存関係を含める

```bash
# 特定のランタイムIDで生成
dotnet restore --locked-mode -r win-x64
sbom-tool generate -b ./sbom-output -bc . -pn kuju63/wt -pv 1.0.0 -nsb https://github.com/kuju63/wt/sbom/1.0.0
```

### 新しい依存関係の追加時

新しいNuGetパッケージを追加する際の手順：

1. **依存関係を追加**

   ```bash
   dotnet add wt.cli/wt.cli.csproj package NewPackage
   ```

2. **ロックファイルを更新**

   ```bash
   dotnet restore --locked-mode
   ```

3. **SBOM生成をテスト**

   ```bash
   sbom-tool generate -b ./test-sbom -bc . -pn kuju63/wt -pv test -nsb https://github.com/kuju63/wt/sbom/test
   jq '.packages[] | select(.name=="NewPackage")' ./test-sbom/_manifest/spdx_2.2/manifest.spdx.json
   ```

4. **リリース時に自動反映**: リリースワークフローが自動的に新しい依存関係を含むSBOMを生成

---

## トラブルシューティング

### よくある問題と解決方法

#### 問題1: SBOM生成が失敗する

**症状**:

```text
Error: No packages found in the build directory
```

**原因**: 依存関係が復元されていない

**解決方法**:

```bash
# 依存関係をリストア
dotnet restore --locked-mode

# SBOM生成を再実行
sbom-tool generate -b ./sbom-output -bc . -pn kuju63/wt -pv 1.0.0 -nsb https://github.com/kuju63/wt/sbom/1.0.0
```

#### 問題2: GitHub API呼び出しが403 Forbiddenで失敗

**症状**:

```text
Error: Resource not accessible by integration
```

**原因**: GITHUB_TOKENの権限不足

**解決方法**:

```yaml
# .github/workflows/release.ymlで権限を設定
permissions:
  contents: write
  id-token: write
```

#### 問題3: SBOMに一部の依存関係が含まれていない

**症状**: NuGetパッケージの一部がSBOMに表示されない

**原因**: プラットフォーム固有の依存関係が復元されていない

**解決方法**:

```bash
# すべてのターゲットプラットフォームで復元
dotnet restore --locked-mode -r win-x64
dotnet restore --locked-mode -r linux-x64
dotnet restore --locked-mode -r linux-arm
dotnet restore --locked-mode -r osx-arm64

# SBOM生成
sbom-tool generate -b ./sbom-output -bc . -pn kuju63/wt -pv 1.0.0 -nsb https://github.com/kuju63/wt/sbom/1.0.0
```

#### 問題4: ワークフローがタイムアウトする

**症状**: GitHub Actionsが15分でタイムアウト

**原因**: 大規模な依存関係ツリー

**解決方法**:

```yaml
# キャッシュを有効化
- uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/packages.lock.json') }}

# タイムアウトを延長（最大60分）
timeout-minutes: 30
```

### デバッグ方法

#### 詳細ログの有効化

```bash
# SBOM Tool詳細ログ
sbom-tool generate \
  -b ./sbom-output \
  -bc . \
  -pn kuju63/wt \
  -pv 1.0.0 \
  -nsb https://github.com/kuju63/wt/sbom/1.0.0 \
  -v Diagnostic  # ログレベル: Diagnostic, Information, Warning, Error
```

```yaml
# GitHub Actions詳細ログ
- name: Generate SBOM
  run: sbom-tool generate ...
  env:
    ACTIONS_STEP_DEBUG: true
```

#### SBOM内容の詳細確認

```bash
# すべてのパッケージ情報を表示
jq '.packages[]' wt-sbom.spdx.json

# 特定パッケージの依存関係を表示
jq '.relationships[] | select(.spdxElementId | contains("System.CommandLine"))' wt-sbom.spdx.json

# ドキュメントメタデータを表示
jq '{version: .spdxVersion, name: .name, created: .creationInfo.created}' wt-sbom.spdx.json
```

---

## 追加リソース

### ドキュメント

- [SPDX仕様](https://spdx.github.io/spdx-spec/)
- [Microsoft SBOM Tool](https://github.com/microsoft/sbom-tool)
- [GitHub Dependency Submission API](https://docs.github.com/en/rest/dependency-graph/dependency-submission)
- [Package URL仕様](https://github.com/package-url/purl-spec)

### ツール

- [SPDX Validator](https://www.npmjs.com/package/@spdx/spdx-validator) - SPDXフォーマット検証
- [OSV Scanner](https://github.com/google/osv-scanner) - 脆弱性スキャン
- [Grype](https://github.com/anchore/grype) - 脆弱性検出
- [Dependency-Track](https://dependencytrack.org/) - SBOMプラットフォーム

### コミュニティ

- [Issue報告](https://github.com/kuju63/wt/issues)
- [Discussion](https://github.com/kuju63/wt/discussions)

---

## サポート

質問や問題がある場合は、以下の方法でお問い合わせください：

- **GitHub Issues**: <https://github.com/kuju63/wt/issues>
- **GitHub Discussions**: <https://github.com/kuju63/wt/discussions>

---

最終更新: 2026-01-10
