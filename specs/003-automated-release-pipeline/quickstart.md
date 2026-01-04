# Quick Start: 自動化されたバイナリリリースパイプライン

**Feature**: `003-automated-release-pipeline`  
**Date**: 2026-01-04  
**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

このガイドは、自動化されたバイナリリリースパイプラインの使用方法を説明します。

---

## 📦 リリースバイナリのダウンロード

### ステップ1: GitHub Releasesページにアクセス

1. リポジトリのGitHub Releasesページに移動: <https://github.com/kuju63/wt/releases>
2. 最新リリースを選択 (例: `v1.0.0`)

### ステップ2: プラットフォームに適したバイナリをダウンロード

利用可能なプラットフォーム:

- **Windows x64**: `wt-v<version>-windows-x64.exe`
- **Linux x64**: `wt-v<version>-linux-x64`
- **Linux ARM**: `wt-v<version>-linux-arm` (オプション)
- **macOS ARM64**: `wt-v<version>-macos-arm64`

### ステップ3: バイナリに実行権限を付与 (Linux/macOS)

```bash
chmod +x wt-v<version>-linux-x64
# または
chmod +x wt-v<version>-macos-arm64
```

### ステップ4: バイナリを実行

```bash
# Linux/macOS
./wt-v<version>-linux-x64 --version

# Windows (PowerShell)
.\wt-v<version>-windows-x64.exe --version
```

---

## 🔒 バイナリの整合性検証

### SHA256ハッシュ値の検証

#### ステップ1: チェックサムファイルをダウンロード

GitHub Releasesページから以下のファイルをダウンロード:

- `SHA256SUMS`
- `SHA256SUMS.asc` (GPG署名)

#### ステップ2: GPG署名を検証

```bash
# GPG公開鍵をインポート (初回のみ)
curl -fsSL https://raw.githubusercontent.com/kuju63/wt/main/docs/GPG_PUBLIC_KEY.asc | gpg --import

# 署名を検証
gpg --verify SHA256SUMS.asc SHA256SUMS
```

期待される出力:

```text
gpg: Signature made ...
gpg: Good signature from "Release Pipeline Bot <release@kuju63.example.com>"
```

#### ステップ3: ハッシュ値を検証

```bash
# Linux/macOS
sha256sum -c SHA256SUMS --ignore-missing

# macOS (sha256sum がない場合)
shasum -a 256 -c SHA256SUMS --ignore-missing

# Windows (PowerShell)
$hash = (Get-FileHash -Algorithm SHA256 wt-v<version>-windows-x64.exe).Hash.ToLower()
$expected = (Get-Content SHA256SUMS | Select-String "wt-v<version>-windows-x64.exe").Line.Split()[0]
if ($hash -eq $expected) { Write-Host "OK: Hash verified" -ForegroundColor Green } else { Write-Host "ERROR: Hash mismatch" -ForegroundColor Red }
```

期待される出力:

```text
wt-v1.0.0-linux-x64: OK
```

---

## 📄 SBOM (Software Bill of Materials) の確認

### ステップ1: SBOMファイルをダウンロード

GitHub Releasesページから以下のファイルをダウンロード:

- `wt-v<version>-sbom.json` (CycloneDX形式)
- `wt-v<version>-sbom.json.asc` (GPG署名)

### ステップ2: SBOM署名を検証

```bash
gpg --verify wt-v<version>-sbom.json.asc wt-v<version>-sbom.json
```

### ステップ3: SBOMの内容を確認

```bash
# 依存関係の一覧を表示
jq '.components[].name' wt-v<version>-sbom.json

# 依存関係の数を表示
jq '.components | length' wt-v<version>-sbom.json

# 特定の依存関係を検索
jq '.components[] | select(.name | contains("System.CommandLine"))' wt-v<version>-sbom.json
```

### SBOM形式

- **フォーマット**: CycloneDX 1.4 (JSON)
- **仕様**: <https://cyclonedx.org/specification/overview/>

---

## 🚀 リリースの自動作成 (開発者向け)

### 前提条件

1. **GitHub Secrets の設定** (リポジトリ管理者のみ):
   - `GH_RELEASE_TOKEN`: GitHub Personal Access Token (repo権限)
   - `CODACY_PROJECT_TOKEN`: Codacyプロジェクトトークン
   - `GPG_PRIVATE_KEY`: GPG秘密鍵 (ASCII-armored形式)
   - `GPG_PASSPHRASE`: GPG鍵のパスフレーズ

   詳細: [.github/SECRETS.md](../../.github/SECRETS.md)

2. **Conventional Commits規約の遵守**:
   - すべてのコミットメッセージは Conventional Commits 規約に従うこと
   - 詳細: [.github/CONVENTIONAL_COMMITS.md](../../.github/CONVENTIONAL_COMMITS.md)

### リリース作成フロー

#### ステップ1: 機能ブランチを作成

```bash
git checkout -b feature/new-amazing-feature
```

#### ステップ2: Conventional Commitsでコミット

```bash
# 新機能 (MINOR version bump)
git commit -m "feat: add support for multiple worktrees"

# バグ修正 (PATCH version bump)
git commit -m "fix: handle special characters in branch names"

# 破壊的変更 (MAJOR version bump)
git commit -m "feat: change CLI argument format

BREAKING CHANGE: All CLI arguments now use kebab-case.
Migration guide: docs/migration/v2.0.md"
```

#### ステップ3: プルリクエストを作成

```bash
git push origin feature/new-amazing-feature
```

GitHub上でPRを作成し、レビューを受ける。

#### ステップ4: mainにマージ

PRが承認されたら、mainブランチにマージします。

#### ステップ5: 自動リリース作成を確認

1. GitHub Actions workflow (`release.yml`) が自動実行されます
2. 次のバージョン番号が自動計算されます:
   - `feat:` → MINOR bump (0.1.0 → 0.2.0)
   - `fix:` → PATCH bump (0.1.0 → 0.1.1)
   - `BREAKING CHANGE:` → MAJOR bump (0.1.0 → 1.0.0)
3. バイナリが全プラットフォーム向けにビルドされます
4. SHA256SUMS、SBOM、署名ファイルが生成されます
5. リリースノートが自動生成されます
6. GitHub Releaseが公開されます

**予想時間**: mainマージから30分以内

### トラブルシューティング

#### 問題: リリースが作成されない

**原因**: Conventional Commits規約に準拠していない、またはバージョン変更が不要なコミット (docs, style等) のみ

**解決策**:
1. コミットメッセージを確認: `git log`
2. `feat:`, `fix:`, `BREAKING CHANGE:` が含まれているか確認
3. 必要に応じて、`git commit --amend` で修正

#### 問題: ビルドが失敗する

**原因**: テストが失敗している、またはビルドエラー

**解決策**:
1. GitHub Actions のログを確認
2. ローカルでテストを実行: `dotnet test wt.sln`
3. ローカルでビルドを実行: `dotnet build wt.sln --configuration Release`

#### 問題: 署名ファイルが生成されない

**原因**: `GPG_PRIVATE_KEY` または `GPG_PASSPHRASE` が設定されていない

**解決策**:
1. GitHub リポジトリ設定 > Secrets and variables > Actions を確認
2. 必要なシークレットが設定されているか確認
3. 詳細: [.github/SECRETS.md](../../.github/SECRETS.md)

---

## 📊 カバレッジレポートの確認

### Codacyでカバレッジを確認

1. Codacyプロジェクトダッシュボードにアクセス: <https://app.codacy.com/gh/kuju63/wt>
2. "Coverage" タブを選択
3. 最新のコミットのカバレッジを確認

### カバレッジ目標

- **プロジェクト全体**: 80%以上
- **品質ゲート**: カバレッジが80%未満の場合は警告 (マージはブロックしない)

---

## 🛡️ ブランチ保護ルール (リポジトリ管理者向け)

mainブランチを保護するため、以下のルールを設定してください:

1. GitHub リポジトリ設定 > Branches > Branch protection rules
2. "Add rule" をクリック
3. Branch name pattern: `main`
4. 以下を有効化:
   - ✅ **Require a pull request before merging**
   - ✅ **Require status checks to pass before merging**
     - Required checks:
       - `test` (Test and Coverage workflow)
   - ✅ **Require branches to be up to date before merging**
   - ✅ **Do not allow bypassing the above settings**

---

## 📚 参考資料

- [Conventional Commits 規約](../../.github/CONVENTIONAL_COMMITS.md)
- [GitHub Secrets 設定ガイド](../../.github/SECRETS.md)
- [技術調査レポート](research.md)
- [実装計画](plan.md)
- [仕様書](spec.md)

---

**最終更新**: 2026-01-04  
**担当者**: Release Pipeline Team
