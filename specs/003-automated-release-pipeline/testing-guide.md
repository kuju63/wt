# Testing Guide: Automated Binary Release Pipeline

**Feature**: `003-automated-release-pipeline`  
**Date**: 2026-01-05  
**Related**: [spec.md](spec.md), [quickstart.md](quickstart.md)

このガイドは、自動化されたバイナリリリースパイプラインのテスト方法を説明します。

---

## 📋 テスト概要

### テスト対象

1. **ビルドプロセス**: 全プラットフォームのバイナリ生成
2. **バージョン計算**: Conventional Commitsベースの自動バージョニング
3. **リリースノート生成**: コミット履歴からの自動生成
4. **ハッシュ生成**: SHA256チェックサム計算
5. **SBOM生成**: CycloneDX形式のSBOM作成
6. **デジタル署名**: GPG署名の生成と検証
7. **GitHub Release作成**: Release APIへのアップロード
8. **テスト自動化**: 全ブランチでのテスト実行
9. **カバレッジ報告**: Codacy連携

### テスト戦略

- **Phase 1 Tests**: ビルドとリリースの基本機能 (T017, T021-T022, T026, T032, T032e-f)
- **Phase 2 Tests**: バージョン計算とテスト自動化 (T040, T044, T048-T049, T055-T056)
- **Phase 3 Tests**: エンドツーエンド統合テスト (T086-T096)

---

## 🧪 Phase 1: ビルドとリリースの基本テスト

### T017: ローカルビルドテスト

**目的**: 全プラットフォームのビルドスクリプトが正常に動作することを確認

**手順**:

```bash
# 1. Windows x64ビルド
cd /Users/kuriharajun/project/wt
.github/scripts/build-windows.sh "v0.1.0-test" "win-x64"

# 期待される出力: release-assets/wt-v0.1.0-test-windows-x64.exe

# 2. Linux x64ビルド
.github/scripts/build-linux-x64.sh "v0.1.0-test" "linux-x64"

# 期待される出力: release-assets/wt-v0.1.0-test-linux-x64

# 3. Linux ARMビルド (オプション)
.github/scripts/build-linux-arm.sh "v0.1.0-test" "linux-arm"

# 期待される出力: release-assets/wt-v0.1.0-test-linux-arm (失敗しても問題なし)

# 4. macOS ARM64ビルド
.github/scripts/build-macos-arm64.sh "v0.1.0-test" "osx-arm64"

# 期待される出力: release-assets/wt-v0.1.0-test-macos-arm64
```

**検証**:

```bash
# バイナリが生成されたことを確認
ls -lh release-assets/

# 各バイナリが実行可能であることを確認 (Linux/macOS)
chmod +x release-assets/wt-*-linux-*
chmod +x release-assets/wt-*-macos-*

# バージョン情報を表示 (実装されている場合)
./release-assets/wt-v0.1.0-test-linux-x64 --version
```

**成功基準**:

- ✅ Windows, Linux x64, macOS ARM64バイナリが生成される (MANDATORY)
- ✅ 各バイナリが実行可能で、エラーなく起動する
- ⚠️ Linux ARMビルドは失敗してもよい (OPTIONAL)

---

### T021-T022: リリースワークフローテストとドキュメント

**目的**: mainブランチへのマージでリリースが自動作成されることを確認

**手順**:

```bash
# 1. テスト用ブランチを作成
git checkout -b test/release-workflow-001

# 2. 軽微な変更をコミット
echo "# Test Release" >> README.md
git add README.md
git commit -m "test: verify release workflow automation"

# 3. リモートにプッシュ
git push origin test/release-workflow-001

# 4. GitHubでプルリクエストを作成
gh pr create --title "test: release workflow" --body "Testing automated release creation"

# 5. PRをmainにマージ
gh pr merge --squash --delete-branch

# 6. GitHub Actionsワークフローを監視
gh run watch
```

**検証**:

```bash
# リリースが作成されたことを確認
gh release list

# 最新リリースの詳細を確認
gh release view --web
```

**期待される結果**:

- ✅ mainマージから30分以内にリリースが作成される
- ✅ リリースに全プラットフォームのバイナリが含まれる
- ✅ SHA256SUMS、SBOM、署名ファイルが含まれる
- ✅ リリースノートが自動生成される

**ドキュメント更新** (T022):

[quickstart.md](quickstart.md)に以下を追加:

- リリース作成フローの説明
- 手動ダウンロード手順
- トラブルシューティング

---

### T026: ハッシュ生成テスト

**目的**: SHA256チェックサムが正しく生成されることを確認

**手順**:

```bash
# 1. テスト用バイナリを作成
mkdir -p test-assets
echo "test binary content" > test-assets/wt-v0.1.0-test-linux-x64

# 2. チェックサムを生成
.github/scripts/generate-checksums.sh test-assets/

# 3. SHA256SUMSファイルを確認
cat test-assets/SHA256SUMS
```

**検証**:

```bash
# ハッシュ値を手動計算して比較
sha256sum test-assets/wt-v0.1.0-test-linux-x64

# SHA256SUMSファイルを使って検証
cd test-assets
sha256sum -c SHA256SUMS --ignore-missing
```

**期待される結果**:

- ✅ `SHA256SUMS`ファイルが生成される
- ✅ 各バイナリのハッシュ値が正しい
- ✅ `sha256sum -c`コマンドで検証成功

---

### T032: SBOMテスト

**目的**: CycloneDX SBOMが正しく生成されることを確認

**手順**:

```bash
# 1. GitHub Actionsワークフローを実行してSBOMを生成
# (ローカルでSBOM生成をテストする場合)

# Syftをインストール (Anchore SBOM Action の代替)
brew install syft  # macOS
# または
curl -sSfL https://raw.githubusercontent.com/anchore/syft/main/install.sh | sh -s -- -b /usr/local/bin

# 2. SBOMを生成
syft packages dir:./wt.cli -o cyclonedx-json > test-sbom.json

# 3. SBOMの内容を確認
cat test-sbom.json | jq .
```

**検証**:

```bash
# 依存関係の数を確認
jq '.components | length' test-sbom.json

# 主要な依存関係を確認
jq '.components[].name' test-sbom.json | grep -E 'System.CommandLine|Microsoft'

# SBOM形式が正しいことを確認
jq '.bomFormat' test-sbom.json  # 期待値: "CycloneDX"
jq '.specVersion' test-sbom.json  # 期待値: "1.4" または "1.5"
```

**期待される結果**:

- ✅ SBOM JSONファイルが生成される
- ✅ `.bomFormat`が`"CycloneDX"`である
- ✅ `.components`に依存関係が含まれる (.NET SDKなど)

---

### T032e-f: デジタル署名テストとドキュメント

**目的**: GPG署名が正しく生成・検証できることを確認

**前提条件**:

```bash
# GPGキーペアを生成 (テスト用)
gpg --batch --gen-key <<EOF
%no-protection
Key-Type: RSA
Key-Length: 4096
Name-Real: Test Release Bot
Name-Email: test-release@example.com
Expire-Date: 0
%commit
EOF

# 秘密鍵をエクスポート
gpg --armor --export-secret-keys test-release@example.com > test-gpg-private.asc

# 公開鍵をエクスポート
gpg --armor --export test-release@example.com > test-gpg-public.asc
```

**手順** (T032e):

```bash
# 1. テストファイルを作成
echo "test content" > test-file.txt

# 2. 署名を生成
gpg --armor --detach-sign test-file.txt

# 3. 署名ファイルを確認
ls -lh test-file.txt.asc
cat test-file.txt.asc
```

**検証**:

```bash
# 署名を検証
gpg --verify test-file.txt.asc test-file.txt

# 期待される出力:
# gpg: Signature made ...
# gpg: Good signature from "Test Release Bot <test-release@example.com>"
```

**期待される結果**:

- ✅ `.asc`署名ファイルが生成される
- ✅ GPG署名が有効である
- ✅ 署名検証が成功する

**ドキュメント更新** (T032f):

[quickstart.md](quickstart.md)に以下を追加:

```markdown
### 署名検証プロセス

1. 公開鍵をインポート:
   \`\`\`bash
   curl -fsSL https://raw.githubusercontent.com/kuju63/wt/main/docs/GPG_PUBLIC_KEY.asc | gpg --import
   \`\`\`

2. SBOM署名を検証:
   \`\`\`bash
   gpg --verify wt-v1.0.0-sbom.json.asc wt-v1.0.0-sbom.json
   \`\`\`

3. チェックサム署名を検証:
   \`\`\`bash
   gpg --verify SHA256SUMS.asc SHA256SUMS
   \`\`\`
```

---

## 🧪 Phase 2: バージョン計算とテスト自動化

### T040: バージョン計算テスト

**目的**: Conventional Commitsに基づくバージョンインクリメントが正しく動作することを確認

**手順**:

```bash
# 1. 最新タグを確認
git describe --tags --abbrev=0

# 2. feat: コミット (MINOR bump)
git checkout -b test/version-minor
echo "# New Feature" >> docs/test.md
git add docs/test.md
git commit -m "feat: add new amazing feature"
git push origin test/version-minor

# 3. PRを作成してマージ
gh pr create --title "feat: new feature" --body "Test MINOR version bump"
gh pr merge --squash --delete-branch

# 4. リリースを確認
gh release list

# 5. fix: コミット (PATCH bump)
git checkout -b test/version-patch
echo "# Bug Fix" >> docs/test.md
git add docs/test.md
git commit -m "fix: resolve critical bug"
git push origin test/version-patch

# 6. PRを作成してマージ
gh pr create --title "fix: bug fix" --body "Test PATCH version bump"
gh pr merge --squash --delete-branch

# 7. リリースを確認
gh release list

# 8. BREAKING CHANGE: コミット (MAJOR bump)
git checkout -b test/version-major
echo "# Breaking Change" >> docs/test.md
git add docs/test.md
git commit -m "feat: change API format

BREAKING CHANGE: All CLI arguments now use kebab-case.
Migration guide: docs/migration/v2.0.md"
git push origin test/version-major

# 9. PRを作成してマージ
gh pr create --title "feat: breaking change" --body "Test MAJOR version bump"
gh pr merge --squash --delete-branch

# 10. リリースを確認
gh release list
```

**期待される結果**:

- ✅ `feat:` → MINORバージョン増加 (例: v0.5.0 → v0.6.0)
- ✅ `fix:` → PATCHバージョン増加 (例: v0.6.0 → v0.6.1)
- ✅ `BREAKING CHANGE:` → MAJORバージョン増加 (例: v0.6.1 → v1.0.0)
- ✅ `docs:`, `chore:` → バージョン変更なし

---

### T044: リリースノートテスト

**目的**: リリースノートが正しく生成されることを確認

**手順**:

```bash
# 1. 複数のコミットタイプを含むPRを作成
git checkout -b test/release-notes
git commit --allow-empty -m "feat: add feature A"
git commit --allow-empty -m "fix: resolve bug B"
git commit --allow-empty -m "docs: update README"
git push origin test/release-notes

# 2. PRをマージ
gh pr create --title "test: release notes" --body "Test release notes generation"
gh pr merge --squash --delete-branch

# 3. リリースノートを確認
gh release view --web
```

**期待されるリリースノート形式**:

```markdown
## Features
- add feature A

## Bug Fixes
- resolve bug B

## Documentation
- update README
```

**検証**:

- ✅ `feat:`コミットが「Features」セクションに表示される
- ✅ `fix:`コミットが「Bug Fixes」セクションに表示される
- ✅ `BREAKING CHANGE:`が強調表示される
- ✅ `docs:`, `chore:`コミットは除外される (またはOther Changesセクション)

---

### T048-T049: リリース自動化のエンドツーエンドテスト

**目的**: mainマージからリリース作成まで の全フローが正常に動作することを確認

**手順**:

```bash
# 1. テスト用ブランチを作成
git checkout -b test/e2e-release
echo "# E2E Test" >> docs/test-e2e.md
git add docs/test-e2e.md
git commit -m "feat: end-to-end release test"
git push origin test/e2e-release

# 2. PRを作成
gh pr create --title "feat: E2E test" --body "Testing full release pipeline"

# 3. GitHub Actionsを監視
gh pr checks --watch

# 4. テストが成功したらマージ
gh pr merge --squash --delete-branch

# 5. リリースワークフローを監視
gh run watch

# 6. リリースが作成されるまで待機 (最大30分)
timeout 1800 bash -c 'until gh release list | grep -q "v"; do sleep 30; done'

# 7. リリースを確認
gh release view --web
```

**検証**:

```bash
# リリースアセットを確認
gh release view --json assets -q '.assets[].name'

# 期待されるファイル:
# - wt-v<version>-windows-x64.exe
# - wt-v<version>-linux-x64
# - wt-v<version>-linux-arm (オプション)
# - wt-v<version>-macos-arm64
# - wt-v<version>-sbom.json
# - wt-v<version>-sbom.json.asc
# - SHA256SUMS
# - SHA256SUMS.asc
```

**性能検証** (SC-003):

```bash
# mainマージ時刻を取得
MERGE_TIME=$(git log -1 --format=%ct origin/main)

# リリース作成時刻を取得
RELEASE_TIME=$(gh release view --json publishedAt -q '.publishedAt' | date -j -f "%Y-%m-%dT%H:%M:%SZ" "+%s")

# 経過時間を計算 (秒)
DURATION=$((RELEASE_TIME - MERGE_TIME))

echo "Release duration: ${DURATION} seconds ($((DURATION / 60)) minutes)"

# 30分 (1800秒) 以内であることを確認
if [ $DURATION -le 1800 ]; then
  echo "✅ PASS: Release published within 30 minutes"
else
  echo "❌ FAIL: Release took longer than 30 minutes"
fi
```

**期待される結果**:

- ✅ mainマージから30分以内にリリースが作成される
- ✅ 全必須プラットフォームのバイナリが含まれる
- ✅ SHA256SUMS、SBOM、署名ファイルが含まれる
- ✅ リリースノートが正しく生成される

---

### T055-T056: ブランチプロテクションとテスト自動化

**目的**: テスト失敗時にmainへのマージがブロックされることを確認

**手順**:

```bash
# 1. テスト失敗を含むブランチを作成
git checkout -b test/failing-tests

# 2. 意図的にテストを失敗させる
# (例: wt.tests/SampleTest.cs に失敗するテストを追加)
cat <<EOF >> wt.tests/FailingTest.cs
using Xunit;

namespace wt.tests
{
    public class FailingTest
    {
        [Fact]
        public void ThisTestShouldFail()
        {
            Assert.True(false, "Intentional test failure");
        }
    }
}
EOF

git add wt.tests/FailingTest.cs
git commit -m "test: add intentionally failing test"
git push origin test/failing-tests

# 3. PRを作成
gh pr create --title "test: failing tests" --body "Testing branch protection with failing tests"

# 4. GitHub Actionsを監視
gh pr checks --watch

# 5. テストが失敗することを確認
gh pr checks

# 6. マージを試みる (失敗するはず)
gh pr merge --squash --delete-branch
# 期待されるエラー: "Required status check 'Test and Coverage / Run Tests' is failing"
```

**検証**:

- ✅ テストが失敗したPRはマージできない
- ✅ PRに「❌ Test and Coverage / Run Tests」が表示される
- ✅ GitHub UIで「Merge」ボタンが無効になる

**修正して再テスト**:

```bash
# 7. 失敗するテストを削除
git rm wt.tests/FailingTest.cs
git commit -m "test: remove failing test"
git push origin test/failing-tests

# 8. テストが成功することを確認
gh pr checks --watch

# 9. マージが許可されることを確認
gh pr merge --squash --delete-branch
```

**期待される結果**:

- ✅ テスト失敗でマージブロック
- ✅ テスト成功でマージ許可

---

## 🧪 Phase 3: エンドツーエンド統合テスト

### T086-T087: 完全なリリースフローテスト

**目的**: 機能ブランチ作成からリリース公開までの全フローを検証

**手順**:

```bash
# 1. 機能ブランチを作成
git checkout -b feature/complete-e2e-test

# 2. 新機能を実装 (簡易版)
echo "// New feature implementation" >> wt.cli/Program.cs
git add wt.cli/Program.cs
git commit -m "feat: implement complete E2E test feature"

# 3. テストを追加
echo "// Test for new feature" >> wt.tests/E2ETest.cs
git add wt.tests/E2ETest.cs
git commit -m "test: add E2E test for new feature"

# 4. リモートにプッシュ
git push origin feature/complete-e2e-test

# 5. PRを作成
gh pr create --title "feat: complete E2E test" --body "Testing full pipeline from feature to release"

# 6. テストワークフローを監視
gh pr checks --watch

# 7. テストが成功したらマージ
gh pr merge --squash --delete-branch

# 8. リリースワークフローを監視
gh run watch

# 9. リリースを確認
gh release view
```

**検証ポイント** (T086):

- ✅ テストワークフローが自動実行される
- ✅ カバレッジがCodacyにアップロードされる
- ✅ テスト成功後にマージが許可される

**検証ポイント** (T087):

- ✅ mainマージ後にリリースワークフローが自動実行される
- ✅ 正しいバージョン番号が計算される
- ✅ リリースノートに`feat:`コミットが含まれる
- ✅ 全プラットフォームのバイナリが生成される
- ✅ ハッシュ、SBOM、署名ファイルが含まれる

---

### T088-T090: リリースアセット検証

**目的**: リリースに含まれる全アセットが正しいことを確認

**手順**:

```bash
# 最新リリースのアセットをダウンロード
LATEST_RELEASE=$(gh release list --limit 1 | awk '{print $1}')
gh release download "$LATEST_RELEASE"

# ファイル一覧を確認
ls -lh

# 期待されるファイル:
# - wt-v<version>-windows-x64.exe
# - wt-v<version>-linux-x64
# - wt-v<version>-linux-arm (オプション)
# - wt-v<version>-macos-arm64
# - wt-v<version>-sbom.json
# - wt-v<version>-sbom.json.asc
# - SHA256SUMS
# - SHA256SUMS.asc
```

**T088: 全プラットフォームバイナリ確認**:

```bash
# Windows x64
file wt-v*-windows-x64.exe  # PE32+ executable

# Linux x64
file wt-v*-linux-x64  # ELF 64-bit LSB executable, x86-64

# Linux ARM (オプション)
file wt-v*-linux-arm  # ELF 32-bit LSB executable, ARM

# macOS ARM64
file wt-v*-macos-arm64  # Mach-O 64-bit arm64 executable
```

**T089: SHA256SUMS検証**:

```bash
# チェックサムファイルを確認
cat SHA256SUMS

# GPG署名を検証
gpg --verify SHA256SUMS.asc SHA256SUMS

# ハッシュ値を検証
sha256sum -c SHA256SUMS --ignore-missing
```

**T090: SBOM検証**:

```bash
# SBOMファイルを確認
cat wt-v*-sbom.json | jq .

# GPG署名を検証
gpg --verify wt-v*-sbom.json.asc wt-v*-sbom.json

# SBOM形式を確認
jq '.bomFormat' wt-v*-sbom.json  # "CycloneDX"
jq '.specVersion' wt-v*-sbom.json  # "1.4" または "1.5"

# 依存関係を確認
jq '.components[].name' wt-v*-sbom.json
```

**期待される結果**:

- ✅ 全バイナリが正しい形式である
- ✅ SHA256SUMSのハッシュ値が一致する
- ✅ GPG署名が有効である
- ✅ SBOMにCycloneDX形式で依存関係が含まれる

---

### T091: 手動ダウンロードテスト

**目的**: ユーザー視点でバイナリのダウンロードと実行が正常に行えることを確認

**手順** (各プラットフォーム):

**Windows**:

```powershell
# PowerShellでダウンロード
$LATEST = (gh release list --limit 1 | Select-String -Pattern "v\d+\.\d+\.\d+").Matches.Value
gh release download $LATEST --pattern "wt-*-windows-x64.exe"

# 実行
.\wt-*-windows-x64.exe --version
```

**Linux x64**:

```bash
# ダウンロード
LATEST=$(gh release list --limit 1 | awk '{print $1}')
gh release download "$LATEST" --pattern "wt-*-linux-x64"

# 実行権限を付与
chmod +x wt-*-linux-x64

# 実行
./wt-*-linux-x64 --version
```

**macOS ARM64**:

```bash
# ダウンロード
LATEST=$(gh release list --limit 1 | awk '{print $1}')
gh release download "$LATEST" --pattern "wt-*-macos-arm64"

# 実行権限を付与
chmod +x wt-*-macos-arm64

# 実行
./wt-*-macos-arm64 --version
```

**期待される結果**:

- ✅ 各プラットフォームでバイナリがダウンロードできる
- ✅ バイナリが実行可能である
- ✅ `--version`フラグでバージョン情報が表示される

---

### T092: リリースノート検証

**目的**: リリースノートが正確でフォーマットが適切であることを確認

**手順**:

```bash
# リリースノートを確認
gh release view --json body -q .body
```

**検証ポイント**:

- ✅ `## Features`セクションに`feat:`コミットが含まれる
- ✅ `## Bug Fixes`セクションに`fix:`コミットが含まれる
- ✅ `## Breaking Changes`セクションに`BREAKING CHANGE:`コミットが含まれる
- ✅ Markdown形式が正しい
- ✅ コミットメッセージが読みやすく整形されている

---

### T093: 性能テスト

**目的**: リリース作成がSLA (30分) 以内に完了することを確認

**手順**:

```bash
# スクリプトを作成: test-performance.sh
cat > test-performance.sh <<'EOF'
#!/bin/bash
set -e

# mainブランチの最新コミット時刻を取得
COMMIT_TIME=$(git log -1 --format=%ct origin/main)

# 最新リリースの作成時刻を取得
RELEASE_TIME=$(gh release list --limit 1 --json publishedAt -q '.[0].publishedAt')
RELEASE_TIMESTAMP=$(date -j -f "%Y-%m-%dT%H:%M:%SZ" "$RELEASE_TIME" "+%s" 2>/dev/null || date -d "$RELEASE_TIME" "+%s")

# 経過時間を計算
DURATION=$((RELEASE_TIMESTAMP - COMMIT_TIME))
MINUTES=$((DURATION / 60))

echo "📊 Performance Test Results:"
echo "  Commit time: $(date -r $COMMIT_TIME)"
echo "  Release time: $(date -r $RELEASE_TIMESTAMP)"
echo "  Duration: ${MINUTES} minutes (${DURATION} seconds)"
echo ""

# SLA検証 (30分 = 1800秒)
if [ $DURATION -le 1800 ]; then
  echo "✅ PASS: Release published within 30 minutes (SLA: SC-003)"
  exit 0
else
  echo "❌ FAIL: Release took longer than 30 minutes (SLA violation)"
  exit 1
fi
EOF

chmod +x test-performance.sh
./test-performance.sh
```

**期待される結果**:

- ✅ リリース作成が30分以内に完了する (SC-003)
- ✅ 平均リリース時間が20分以下である (80%ルール)

---

### T094: バイナリダウンロード性能テスト

**目的**: バイナリのダウンロードが2分以内に完了することを確認 (SC-004)

**手順**:

```bash
# ダウンロード性能テスト
time gh release download $(gh release list --limit 1 | awk '{print $1}') --pattern "wt-*-linux-x64"

# 期待される結果: real < 2m0.000s
```

**期待される結果**:

- ✅ バイナリダウンロードが2分以内に完了する (典型的な接続環境)

---

### T095: セキュリティテスト

**目的**: ワークフローやスクリプトでシークレットが漏洩していないことを確認

**手順**:

```bash
# ワークフローログを確認 (シークレットがマスクされていることを確認)
gh run view --log | grep -i "secret\|token\|password\|key"

# 期待される結果: すべてのシークレットが `***` でマスクされている
```

**検証ポイント**:

- ✅ `GPG_PRIVATE_KEY`が漏洩していない
- ✅ `GPG_PASSPHRASE`が漏洩していない
- ✅ `CODACY_PROJECT_TOKEN`が漏洩していない
- ✅ `GITHUB_TOKEN`が漏洩していない

**セキュリティチェックリスト**:

```bash
# リポジトリ内でシークレットの痕跡を検索
git grep -i "BEGIN.*PRIVATE KEY"  # 期待: ヒットなし (テスト用キー除く)
git grep -i "ghp_"  # GitHub Personal Access Token形式
git grep -i "sk_live"  # Stripe等のAPIキー形式
```

---

### T096: アクセシビリティテスト

**目的**: ドキュメントが新規開発者にとって明確であることを確認

**手順**:

1. **新規開発者視点でドキュメントをレビュー**:
   - [quickstart.md](quickstart.md): リリースのダウンロードと検証手順が明確か
   - [research.md](research.md): 技術的決定の背景が理解できるか
   - [ADR文書](../../docs/adr/): 決定理由が明確に記録されているか

2. **ドキュメントのチェックリスト**:
   - ✅ 専門用語に説明がある
   - ✅ コマンド例が実行可能である
   - ✅ 前提条件が明記されている
   - ✅ トラブルシューティングセクションがある
   - ✅ 日本語ドキュメントが提供されている

**期待される結果**:

- ✅ 新規開発者がドキュメントを読んで理解できる
- ✅ 手順が明確で実行可能である
- ✅ トラブルシューティング情報が充実している

---

## 📊 テスト結果の記録

### テストレポート形式

各テスト完了後、以下の形式で結果を記録:

```markdown
## Test Report: T<番号>

**Date**: 2026-01-05  
**Tester**: [名前]  
**Status**: ✅ PASS / ❌ FAIL / ⚠️ PARTIAL

**Details**:
- [テスト手順と結果]

**Issues**:
- [発見された問題]

**Follow-up**:
- [必要なフォローアップアクション]
```

### テスト完了基準

- ✅ 全Phase 1テストが成功
- ✅ 全Phase 2テストが成功
- ✅ 全Phase 3テストが成功
- ✅ セキュリティテストが成功
- ✅ 性能テストがSLAを満たす
- ✅ アクセシビリティテストが成功

---

## 🔗 関連ドキュメント

- [spec.md](spec.md): 仕様書
- [quickstart.md](quickstart.md): クイックスタートガイド
- [troubleshooting.md](troubleshooting.md): トラブルシューティングガイド
- [ADR 0002](../../docs/adr/0002-sbom-format-and-signature-choice.md): SBOM形式とデジタル署名の選択
- [ADR 0003](../../docs/adr/0003-semantic-versioning-conventional-commits.md): セマンティックバージョニング戦略
- [ADR 0004](../../docs/adr/0004-release-workflow-timeout-sla.md): リリースワークフローのタイムアウトとSLA
- [ADR 0005](../../docs/adr/0005-quality-gates-testing-requirements.md): 品質ゲートとテスト要件
