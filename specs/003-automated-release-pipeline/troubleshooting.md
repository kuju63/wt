# Troubleshooting Guide: Automated Binary Release Pipeline

**Feature**: `003-automated-release-pipeline`  
**Date**: 2026-01-05  
**Related**: [spec.md](spec.md), [quickstart.md](quickstart.md), [testing-guide.md](testing-guide.md)

このガイドは、自動化されたバイナリリリースパイプラインで発生する一般的な問題の解決方法を説明します。

---

## 📋 目次

1. [ビルド失敗](#ビルド失敗)
2. [リリース失敗](#リリース失敗)
3. [バージョン計算の問題](#バージョン計算の問題)
4. [テスト失敗](#テスト失敗)
5. [カバレッジ報告の問題](#カバレッジ報告の問題)
6. [SBOM生成の問題](#sbom生成の問題)
7. [デジタル署名の問題](#デジタル署名の問題)
8. [性能問題とタイムアウト](#性能問題とタイムアウト)
9. [GitHub Actions の問題](#github-actions-の問題)

---

## ビルド失敗

### 問題: プラットフォーム固有のビルドが失敗する

**症状**:

```text
Error: Build failed for linux-arm
##[error]The process '/usr/bin/bash' failed with exit code 1
```

**原因**:

- 必須プラットフォーム (Windows x64, Linux x64, macOS ARM64) のビルド失敗 → リリース全体が失敗
- オプションプラットフォーム (Linux ARM) のビルド失敗 → 警告のみ、リリース続行

**解決方法**:

1. **ビルドログを確認**:

   ```bash
   gh run view <run-id> --log | grep -A 50 "Build.*failed"
   ```

2. **ローカルでビルドを再現**:

   ```bash
   # 問題のプラットフォームでビルドを試行
   .github/scripts/build-linux-arm.sh "v0.1.0-test" "linux-arm"
   ```

3. **一般的な原因と対処**:

   | 原因                          | 対処方法                                              |
   | ----------------------------- | ----------------------------------------------------- |
   | .NET SDKバージョン不一致      | `.github/workflows/build.yml`の`dotnet-version`を確認 |
   | 依存関係の解決失敗            | `dotnet restore`を実行して依存関係を確認              |
   | RID (Runtime Identifier) 誤り | `wt.cli.csproj`の`<RuntimeIdentifier>`を確認          |
   | ディスク容量不足              | GitHub Actions runnerのディスク使用量を確認           |

4. **オプションプラットフォームの失敗を無視**:

   Linux ARMはオプションなので、失敗しても問題ありません:

   ```yaml
   # .github/workflows/build.yml
   matrix:
     include:
       - platform: linux
         arch: arm
         mandatory: false  # 失敗許容
   ```

---

### 問題: すべてのプラットフォームでビルドが失敗する

**症状**:

```text
Error: dotnet restore failed
NuGet package restore failed
```

**原因**:

- NuGetパッケージの依存関係解決失敗
- ネットワーク問題
- パッケージソースの設定誤り

**解決方法**:

1. **NuGet設定を確認**:

   ```bash
   cat NuGet.Config  # パッケージソースが正しいか確認
   ```

2. **依存関係をローカルで確認**:

   ```bash
   dotnet restore wt.sln --verbosity detailed
   ```

3. **キャッシュをクリア**:

   ```bash
   dotnet nuget locals all --clear
   dotnet restore wt.sln
   ```

4. **GitHub Actionsワークフローを再実行**:

   ```bash
   gh run rerun <run-id>
   ```

---

## リリース失敗

### 問題: GitHub Releaseの作成が失敗する

**症状**:

```text
Error: Failed to create release
HttpError: Resource not accessible by integration
```

**原因**:

- GitHub token権限不足
- リリース名の重複
- ネットワークタイムアウト

**解決方法**:

1. **GitHub token権限を確認**:

   ```yaml
   # .github/workflows/release.yml
   permissions:
     contents: write  # リリース作成に必要
     packages: write
   ```

2. **既存リリースを確認**:

   ```bash
   gh release list | grep "v1.0.0"  # 同じバージョンが存在するか確認
   ```

3. **既存リリースがある場合は削除**:

   ```bash
   gh release delete v1.0.0 --yes
   git push origin :refs/tags/v1.0.0  # タグも削除
   ```

4. **ワークフローを再実行**:

   ```bash
   gh run rerun <run-id>
   ```

---

### 問題: アセットのアップロードが失敗する

**症状**:

```text
Error: Failed to upload release asset
Error: ENOENT: no such file or directory
```

**原因**:

- ビルドアーティファクトが存在しない
- ファイルパスの誤り
- ファイル名の誤り

**解決方法**:

1. **アーティファクトの存在を確認**:

   ```bash
   # GitHub Actionsログで確認
   gh run view <run-id> --log | grep -A 10 "Upload build artifact"
   ```

2. **ローカルでファイル生成を確認**:

   ```bash
   ls -lh release-assets/
   # 期待されるファイル:
   # - wt-v<version>-windows-x64.exe
   # - wt-v<version>-linux-x64
   # - wt-v<version>-macos-arm64
   # - SHA256SUMS
   # - wt-v<version>-sbom.json
   ```

3. **ファイル名パターンを確認**:

   ```yaml
   # .github/workflows/release.yml
   files: |
     release-assets/wt-*-windows-*.exe
     release-assets/wt-*-linux-*
     release-assets/wt-*-macos-*
     release-assets/wt-*-sbom.json
     release-assets/wt-*-sbom.json.asc
     release-assets/SHA256SUMS
     release-assets/SHA256SUMS.asc
   ```

---

## バージョン計算の問題

### 問題: バージョンがインクリメントされない

**症状**:

- mainにマージしてもリリースが作成されない
- バージョン番号が変わらない

**原因**:

- Conventional Commits形式に従っていないコミット
- `docs:`, `chore:`などのバージョン変更不要なコミットのみ

**解決方法**:

1. **コミットメッセージを確認**:

   ```bash
   git log origin/main --oneline --since="1 day ago"
   ```

2. **Conventional Commits形式を確認**:

   有効なコミットタイプ:

   - `feat:` → MINORバージョン増加
   - `fix:` → PATCHバージョン増加
   - `BREAKING CHANGE:` → MAJORバージョン増加

   無効なコミット (バージョン変更なし):

   - `docs:`, `style:`, `refactor:`, `test:`, `chore:`

3. **バージョンを手動で強制する** (緊急時のみ):

   ```bash
   # GitHub Actions workflow_dispatchで手動実行
   gh workflow run release.yml -f force-version=v1.2.3
   ```

---

### 問題: 間違ったバージョンが計算される

**症状**:

- 期待: PATCH増加 (v1.0.0 → v1.0.1)
- 実際: MINOR増加 (v1.0.0 → v1.1.0)

**原因**:

- コミットメッセージに誤ったタイプが含まれている
- スクワッシュマージ時にコミットメッセージが結合されている

**解決方法**:

1. **マージされたコミットを確認**:

   ```bash
   git log origin/main --oneline -1  # 最新のマージコミット
   git show HEAD  # コミットメッセージ全体を確認
   ```

2. **スクワッシュマージのコミットメッセージを修正**:

   PRマージ時に、適切なConventional Commitsフォーマットを使用:

   ```text
   fix: resolve critical bug

   - Fix issue #123
   - Update error handling
   ```

3. **間違ったリリースを削除して再作成**:

   ```bash
   # 間違ったリリースとタグを削除
   gh release delete v1.1.0 --yes
   git push origin :refs/tags/v1.1.0

   # mainブランチの最新コミットを修正 (必要に応じて)
   git revert HEAD --no-edit
   git push origin main
   ```

---

## テスト失敗

### 問題: テストが失敗してPRがマージできない

**症状**:

```text
❌ Test and Coverage / Run Tests — Failed
Tests failed with exit code 1
```

**原因**:

- 単体テストの失敗
- テストコードのバグ
- テスト環境の問題

**解決方法**:

1. **ローカルでテストを実行**:

   ```bash
   dotnet test wt.sln --verbosity detailed
   ```

2. **失敗したテストを特定**:

   ```bash
   dotnet test wt.sln --logger "trx;LogFileName=test-results.trx"
   cat TestResults/test-results.trx | grep -A 5 "Outcome=\"Failed\""
   ```

3. **テストログを確認**:

   ```bash
   gh run view <run-id> --log | grep -A 20 "Test.*Failed"
   ```

4. **テストを修正してプッシュ**:

   ```bash
   # テストを修正
   git add wt.tests/
   git commit -m "test: fix failing unit test"
   git push origin <branch-name>

   # GitHub Actionsが自動的に再実行される
   gh pr checks --watch
   ```

---

### 問題: テストがタイムアウトする

**症状**:

```text
Error: The operation was canceled.
##[error]The action 'Run Tests' has timed out after 10 minutes.
```

**原因**:

- テストが無限ループに陥っている
- テストの実行時間が長すぎる
- ネットワーク待機が発生している

**解決方法**:

1. **タイムアウトしたテストを特定**:

   ```bash
   # ローカルで個別にテストを実行
   dotnet test wt.tests/LongRunningTest.cs --verbosity detailed
   ```

2. **タイムアウト時間を延長** (一時的):

   ```yaml
   # .github/workflows/test.yml
   jobs:
     test:
       timeout-minutes: 20  # デフォルト: 10分
   ```

3. **テストを最適化**:

   - 不要な`Task.Delay()`を削除
   - モックを使用して外部依存を排除
   - テストをより小さな単位に分割

---

## カバレッジ報告の問題

### 問題: Codacyへのカバレッジアップロードが失敗する

**症状**:

```text
Warning: Failed to upload coverage to Codacy
Error: Project token is invalid
```

**原因**:

- Codacyプロジェクトトークンの設定誤り
- Codacy APIの一時的な障害
- カバレッジファイルの形式誤り

**解決方法**:

1. **Codacyトークンを確認**:

   ```bash
   # GitHub Secrets設定を確認
   gh secret list | grep CODACY
   ```

2. **トークンを再設定**:

   ```bash
   # Codacyダッシュボードから新しいトークンを取得
   # Settings → Coverage → Project API Token

   # GitHub Secretsに設定
   gh secret set CODACY_PROJECT_TOKEN
   ```

3. **カバレッジファイルを確認**:

   ```bash
   # ローカルでカバレッジを生成
   dotnet test wt.sln --collect:"XPlat Code Coverage" --results-directory ./coverage

   # カバレッジファイルを確認
   find ./coverage -name "coverage.cobertura.xml"
   cat ./coverage/**/coverage.cobertura.xml | head -20
   ```

4. **Codacy APIステータスを確認**:

   - [Codacy Status Page](https://status.codacy.com/) でサービスステータスを確認
   - 一時的な障害の場合は、`continue-on-error: true`により影響なし

5. **ワークフローを再実行**:

   ```bash
   gh run rerun <run-id>
   ```

---

### 問題: カバレッジが0%と報告される

**症状**:

- Codacyダッシュボードでカバレッジが0%
- テストは成功しているが、カバレッジが計測されていない

**原因**:

- カバレッジ収集の設定誤り
- テストプロジェクトとソースプロジェクトの参照誤り
- カバレッジフィルターの設定誤り

**解決方法**:

1. **カバレッジ収集の設定を確認**:

   ```bash
   # テスト実行時にカバレッジを明示的に指定
   dotnet test wt.sln \
     --collect:"XPlat Code Coverage" \
     --results-directory ./coverage
   ```

2. **カバレッジファイルが生成されているか確認**:

   ```bash
   find ./coverage -name "coverage.cobertura.xml"
   ```

3. **カバレッジフィルターを確認** (runsettings):

   ```xml
   <!-- coverletArgs.runsettings -->
   <RunSettings>
     <DataCollectionRunSettings>
       <DataCollectors>
         <DataCollector friendlyName="XPlat Code Coverage">
           <Configuration>
             <Exclude>[*.Tests]*,[*]*.Program</Exclude>
             <Include>[wt.cli]*</Include>
           </Configuration>
         </DataCollector>
       </DataCollectors>
     </DataCollectionRunSettings>
   </RunSettings>
   ```

---

## SBOM生成の問題

### 問題: SBOM生成が失敗する

**症状**:

```text
Error: Failed to generate SBOM
Syft error: unable to analyze packages
```

**原因**:

- プロジェクトディレクトリのパス誤り
- 依存関係の解決失敗
- Syft/Anchorツールのバージョン誤り

**解決方法**:

1. **SBOM生成スクリプトを手動実行**:

   ```bash
   # Syftを使用してローカルでSBOM生成
   syft packages dir:./wt.cli -o cyclonedx-json > sbom-test.json
   ```

2. **プロジェクトディレクトリを確認**:

   ```yaml
   # .github/workflows/release.yml
   - name: Generate SBOM
     uses: anchore/sbom-action@61119d458adab75f756bc0b9e4bde25725f86a7a
     with:
       path: ./wt.cli  # 正しいプロジェクトパスか確認
       format: cyclonedx-json
   ```

3. **依存関係を事前に復元**:

   ```yaml
   # ワークフローでrestoreを追加
   - name: Restore dependencies
     run: dotnet restore wt.sln

   - name: Generate SBOM
     uses: anchore/sbom-action@...
   ```

4. **Anchorアクションのバージョンを確認**:

   ```yaml
   # 最新の安定版を使用
   uses: anchore/sbom-action@61119d458adab75f756bc0b9e4bde25725f86a7a  # v0.17.2
   ```

---

### 問題: SBOMに依存関係が含まれていない

**症状**:

- SBOM JSONファイルは生成されるが、`.components`が空

**原因**:

- プロジェクトファイル (`.csproj`) に依存関係が含まれていない
- ビルド前にSBOM生成している

**解決方法**:

1. **プロジェクトファイルを確認**:

   ```bash
   cat wt.cli/wt.cli.csproj | grep PackageReference
   ```

2. **SBOM生成前にビルドを実行**:

   ```yaml
   - name: Build solution
     run: dotnet build wt.sln --configuration Release

   - name: Generate SBOM
     uses: anchore/sbom-action@...
   ```

3. **SBOMの内容を確認**:

   ```bash
   jq '.components | length' sbom.json  # 依存関係の数
   jq '.components[].name' sbom.json  # 依存関係の名前一覧
   ```

---

## デジタル署名の問題

### 問題: GPG署名の生成が失敗する

**症状**:

```text
Error: gpg: signing failed: No secret key
Error: gpg: signing failed: Inappropriate ioctl for device
```

**原因**:

- GPG秘密鍵が正しく設定されていない
- GPGパスフレーズの誤り
- GPGエージェントの設定問題

**解決方法**:

1. **GitHub Secretsを確認**:

   ```bash
   gh secret list | grep GPG
   # 期待されるシークレット:
   # - GPG_PRIVATE_KEY
   # - GPG_PASSPHRASE
   ```

2. **GPG秘密鍵のフォーマットを確認**:

   ```bash
   # ASCII-armored形式であることを確認
   cat gpg-private-key.asc | head -5
   # 期待される出力:
   # -----BEGIN PGP PRIVATE KEY BLOCK-----
   ```

3. **GPGキーを再インポート**:

   ```bash
   # ローカルでテスト
   echo "$GPG_PRIVATE_KEY" | gpg --batch --import

   # キーが正しくインポートされたか確認
   gpg --list-secret-keys
   ```

4. **署名スクリプトを手動実行**:

   ```bash
   # テスト用ファイルを署名
   export GPG_PRIVATE_KEY="<your-key>"
   export GPG_PASSPHRASE="<your-passphrase>"
   .github/scripts/sign-artifacts.sh
   ```

5. **GPGエージェント設定を確認**:

   ```yaml
   # .github/workflows/release.yml
   - name: Sign artifacts
     run: |
       gpg --batch --yes --passphrase "$GPG_PASSPHRASE" \
         --pinentry-mode loopback \  # CI環境で必要
         --armor --detach-sign file.txt
   ```

---

### 問題: 署名検証が失敗する

**症状**:

```text
gpg: BAD signature from "Release Bot <release@example.com>"
```

**原因**:

- 署名されたファイルが改変されている
- 間違った公開鍵を使用している
- 署名ファイルが破損している

**解決方法**:

1. **公開鍵を確認**:

   ```bash
   # 公開鍵をインポート
   curl -fsSL https://raw.githubusercontent.com/kuju63/wt/main/docs/GPG_PUBLIC_KEY.asc | gpg --import

   # インポートされた鍵を確認
   gpg --list-keys
   ```

2. **ファイルの整合性を確認**:

   ```bash
   # ハッシュ値を確認
   sha256sum wt-v1.0.0-sbom.json
   sha256sum -c SHA256SUMS --ignore-missing
   ```

3. **署名ファイルを再ダウンロード**:

   ```bash
   # 破損していないか確認
   gh release download v1.0.0 --pattern "*.asc"
   ```

4. **署名を再検証**:

   ```bash
   gpg --verify wt-v1.0.0-sbom.json.asc wt-v1.0.0-sbom.json
   ```

---

## 性能問題とタイムアウト

### 問題: リリースワークフローが30分以内に完了しない

**症状**:

```text
Error: The operation was canceled.
##[error]Workflow canceled by GitHub Actions (timeout-minutes: 25)
```

**原因**:

- ビルド時間が長すぎる
- 外部サービス (Codacy, GitHub API) の遅延
- ネットワークタイムアウト

**解決方法**:

1. **ワークフローのボトルネックを特定**:

   ```bash
   # 各ステップの実行時間を確認
   gh run view <run-id> --log | grep "##\[group\]" | grep -oE "[0-9]+m[0-9]+s"
   ```

2. **ビルドを最適化**:

   - 並列ビルドを活用:

     ```yaml
     strategy:
       max-parallel: 4  # 全プラットフォームを並列ビルド
     ```

   - キャッシュを有効化:

     ```yaml
     - name: Cache NuGet packages
       uses: actions/cache@v4
       with:
         path: ~/.nuget/packages
         key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
     ```

3. **外部サービスのタイムアウトを設定**:

   ```yaml
   - name: Upload coverage to Codacy
     timeout-minutes: 5  # Codacyアップロードに5分以上かかる場合はスキップ
     continue-on-error: true
   ```

4. **タイムアウト時間を延長** (一時的):

   ```yaml
   create-release:
     timeout-minutes: 30  # 25分 → 30分に延長 (SLA上限)
   ```

---

### 問題: ビルドが遅い

**症状**:

- 単一プラットフォームのビルドに10分以上かかる

**解決方法**:

1. **不要なビルド手順を削除**:

   ```yaml
   # 例: Releaseビルドでデバッグシンボルを無効化
   - name: Build
     run: dotnet publish -c Release /p:DebugType=None
   ```

2. **依存関係のキャッシュ**:

   ```yaml
   - name: Cache dependencies
     uses: actions/cache@v4
     with:
       path: ~/.nuget/packages
       key: nuget-${{ hashFiles('**/*.csproj') }}
   ```

3. **ビルド構成を最適化**:

   ```xml
   <!-- wt.cli.csproj -->
   <PropertyGroup>
     <PublishTrimmed>true</PublishTrimmed>  <!-- トリミングで高速化 -->
     <PublishReadyToRun>true</PublishReadyToRun>  <!-- AOTコンパイル -->
   </PropertyGroup>
   ```

---

## GitHub Actions の問題

### 問題: ワークフローが実行されない

**症状**:

- mainにマージしてもリリースワークフローが実行されない

**原因**:

- ワークフローのトリガー設定誤り
- ブランチ名の不一致
- ワークフローファイルの構文エラー

**解決方法**:

1. **トリガー設定を確認**:

   ```yaml
   # .github/workflows/release.yml
   on:
     push:
       branches:
         - main  # mainブランチへのプッシュでトリガー
   ```

2. **ワークフローファイルの構文を検証**:

   ```bash
   # YAMLの構文チェック
   yamllint .github/workflows/release.yml
   ```

3. **GitHub Actionsログを確認**:

   ```bash
   gh run list --workflow=release.yml --limit 5
   ```

4. **ワークフローを手動実行**:

   ```bash
   gh workflow run release.yml
   ```

---

### 問題: アーティファクトのダウンロードが失敗する

**症状**:

```text
Error: Unable to find artifact binary-linux-x64
```

**原因**:

- ビルドジョブが失敗してアーティファクトが生成されなかった
- アーティファクト名の不一致

**解決方法**:

1. **ビルドジョブの状態を確認**:

   ```bash
   gh run view <run-id> --log | grep -A 10 "Build.*linux-x64"
   ```

2. **アーティファクト名を確認**:

   ```yaml
   # build.yml
   - name: Upload build artifact
     uses: actions/upload-artifact@v4
     with:
       name: binary-${{ matrix.platform }}-${{ matrix.arch }}

   # release.yml
   - name: Download build artifacts
     uses: actions/download-artifact@v4
     with:
       path: artifacts/
   ```

3. **アーティファクトを手動ダウンロード**:

   ```bash
   gh run download <run-id> --name binary-linux-x64
   ```

---

## その他の問題

### 問題: ドキュメントが古い

**症状**:

- ドキュメントの手順が実際の動作と一致しない

**解決方法**:

- ドキュメントを更新してPRを作成:

  ```bash
  git checkout -b docs/update-troubleshooting
  # ドキュメントを修正
  git add docs/ specs/
  git commit -m "docs: update troubleshooting guide"
  git push origin docs/update-troubleshooting
  gh pr create --title "docs: update troubleshooting guide"
  ```

---

### 問題: 不明なエラー

**症状**:

- エラーメッセージが不明確
- ログに有用な情報がない

**解決方法**:

1. **詳細ログを有効化**:

   ```yaml
   # .github/workflows/*.yml
   - name: Run command
     run: |
       set -x  # Bashデバッグモード
       <your-command> --verbosity detailed
   ```

2. **GitHub Actionsデバッグモードを有効化**:

   ```bash
   # リポジトリシークレットに設定
   gh secret set ACTIONS_STEP_DEBUG --body "true"
   gh secret set ACTIONS_RUNNER_DEBUG --body "true"
   ```

3. **Issue を作成**:

   ```bash
   gh issue create \
     --title "Release pipeline failure: <error-message>" \
     --body "Steps to reproduce: ..."
   ```

---

## 🔗 関連ドキュメント

- [quickstart.md](quickstart.md): クイックスタートガイド
- [testing-guide.md](testing-guide.md): テストガイド
- [spec.md](spec.md): 仕様書
- [ADR 0002](../../docs/adr/0002-sbom-format-and-signature-choice.md): SBOM形式とデジタル署名
- [ADR 0003](../../docs/adr/0003-semantic-versioning-conventional-commits.md): セマンティックバージョニング
- [ADR 0004](../../docs/adr/0004-release-workflow-timeout-sla.md): タイムアウトとSLA
- [ADR 0005](../../docs/adr/0005-quality-gates-testing-requirements.md): 品質ゲート

---

## サポート

問題が解決しない場合:

1. **GitHub Issueを作成**: [kuju63/wt/issues](https://github.com/kuju63/wt/issues)
2. **ワークフローログを添付**: `gh run view <run-id> --log > workflow.log`
3. **再現手順を記載**: 問題を再現する最小限の手順
