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
# コマンドラインでダウンロード（Linux/macOS）
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
# jqを使用したオフライン検証（推奨）
SBOM_FILE="wt-sbom.spdx.json"

# 必須フィールドの検証
jq -e '.spdxVersion, .dataLicense, .name, .documentNamespace, .creationInfo.created, .packages' "$SBOM_FILE" > /dev/null && echo "✅ SBOM validation passed" || echo "❌ SBOM validation failed"

# 詳細な検証結果を表示
echo "SPDX Version: $(jq -r '.spdxVersion' "$SBOM_FILE")"
echo "Data License: $(jq -r '.dataLicense' "$SBOM_FILE")"
echo "Document Name: $(jq -r '.name' "$SBOM_FILE")"
echo "Package Count: $(jq '.packages | length' "$SBOM_FILE")"
```

成功時の出力：

```text
✅ SBOM validation passed
SPDX Version: SPDX-2.2
Data License: CC0-1.0
Document Name: kuju63/wt v1.0.0
Package Count: 7
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

**重要: PR時の制限事項**

- ❌ GitHub Dependency Graphへのアップロードは**行いません**
- ❌ Dependency Submission APIは**呼び出しません**
- ✅ SBOM生成とフォーマット検証のみ実行（Dry-runモード）

理由:

- PR時の依存関係はまだ確定していない（レビュー中）
- mainブランチの依存関係グラフを汚染しない
- テスト目的のみ（リリース前の品質確認）

これにより、**リリース前に不具合を発見**しつつ、本番環境を汚染しません。

#### PR時 vs リリース時の比較

| 項目                     | PR時（sbom-test.yml） | リリース時（release.yml） |
| ------------------------ | --------------------- | ------------------------- |
| **依存関係リストア**     | ✅ 実行               | ✅ 実行                   |
| **SBOM生成**             | ✅ 実行               | ✅ 実行                   |
| **SPDX検証**             | ✅ 実行               | ✅ 実行                   |
| **Dependency Graph送信** | ❌ **送信しない**     | ✅ **送信する**           |
| **リリースアセット添付** | ❌ なし               | ✅ 添付する               |
| **Dependabotアラート**   | ❌ 更新なし           | ✅ 自動有効化             |
| **目的**                 | テスト・品質確認      | 本番デプロイ              |
| **実行タイミング**       | PR作成・更新時        | mainへのマージ時          |

**ポイント**: PR時は「SBOM生成が成功するか」のみをテストし、実際のGitHub統合（Dependency Graph、Dependabot）はリリース時のみ実行されます。

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

#### 追加メタデータの付与

```bash
# 環境変数で追加情報を設定
export SBOM_BUILD_ID="12345"
export SBOM_BUILD_URL="https://github.com/kuju63/wt/actions/runs/12345"

sbom-tool generate \
  -b ./sbom-output \
  -bc . \
  -pn kuju63/wt \
  -pv 1.0.0 \
  -nsb https://github.com/kuju63/wt/sbom/1.0.0 \
  -m build_id=$SBOM_BUILD_ID \
  -m build_url=$SBOM_BUILD_URL
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

### GitHub Dependency Submission APIのテスト

#### 1. スナップショットファイルの準備

```bash
# SBOMからDependency Snapshot形式に変換（スクリプトが必要）
python scripts/spdx-to-snapshot.py \
  --spdx sbom-output/_manifest/spdx_2.2/manifest.spdx.json \
  --output snapshot.json \
  --sha $(git rev-parse HEAD) \
  --ref refs/heads/$(git branch --show-current)
```

#### 2. API呼び出し

```bash
# GitHub CLIを使用（推奨）
gh api \
  --method POST \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  /repos/kuju63/wt/dependency-graph/snapshots \
  --input snapshot.json

# curlを使用
curl -X POST \
  -H "Authorization: Bearer $GITHUB_TOKEN" \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  https://api.github.com/repos/kuju63/wt/dependency-graph/snapshots \
  -d @snapshot.json
```

#### 3. 依存関係グラフの確認

```bash
# GitHub CLIで依存関係グラフを取得
gh api /repos/kuju63/wt/dependency-graph/sbom

# ブラウザで確認
open "https://github.com/kuju63/wt/network/dependencies"
```

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

#### 問題5: PR時のテストが失敗する

**症状**: SBOM生成テストワークフロー（sbom-test.yml）がPRで失敗

**原因**:

- 新しい依存関係が追加されたが、packages.lock.jsonが更新されていない
- 必須パッケージが誤って削除された
- SBOMフォーマットが無効

**解決方法**:

```bash
# ロックファイルを更新
dotnet restore --locked-mode

# ローカルでSBOM生成をテスト
sbom-tool generate -b ./test-sbom -bc . -pn test -pv test -nsb https://test

# SPDXバリデーション（jqベース）
SBOM_FILE="./test-sbom/_manifest/spdx_2.2/manifest.spdx.json"
jq -e '.spdxVersion, .dataLicense, .name, .documentNamespace, .creationInfo.created, .packages' "$SBOM_FILE" > /dev/null && echo "✅ Validation passed" || echo "❌ Validation failed"

# 変更をコミット
git add wt.cli/packages.lock.json
git commit -m "fix: update packages.lock.json"
git push
```

**確認項目**:

1. packages.lock.jsonが最新か？
2. 必須パッケージ（System.CommandLine、System.IO.Abstractions）が残っているか？
3. ローカルでSBOM生成が成功するか？

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

- [jq](https://stedolan.github.io/jq/) - JSON処理ツール（SBOM検証に使用、推奨）
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

最終更新: 2026-01-06
