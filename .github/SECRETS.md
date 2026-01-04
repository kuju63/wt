# GitHub Repository Secrets Configuration

このドキュメントは、自動化されたバイナリリリースパイプラインに必要なGitHubリポジトリシークレットを定義します。

## 必須シークレット

### 1. GH_RELEASE_TOKEN

- **用途**: GitHub Releasesへのバイナリアップロード、タグ作成、リリースノート公開
- **権限**: `repo` (フルリポジトリアクセス)、`write:packages` (オプション)
- **取得方法**:
  1. GitHub Settings > Developer settings > Personal access tokens > Tokens (classic)
  2. "Generate new token (classic)" をクリック
  3. スコープで `repo` を選択
  4. トークンを生成し、安全な場所にコピー
- **設定方法**:
  1. リポジトリ Settings > Secrets and variables > Actions
  2. "New repository secret" をクリック
  3. Name: `GH_RELEASE_TOKEN`
  4. Value: 生成したトークン
  5. "Add secret" をクリック

### 2. CODACY_PROJECT_TOKEN

- **用途**: Codacyへのテストカバレッジレポートアップロード
- **権限**: プロジェクト固有のトークン (読み取り/書き込み)
- **取得方法**:
  1. Codacy プロジェクトダッシュボード > Settings > Integrations
  2. "Project API token" セクションを展開
  3. トークンをコピー (表示されていない場合は "Generate" をクリック)
- **設定方法**:
  1. リポジトリ Settings > Secrets and variables > Actions
  2. "New repository secret" をクリック
  3. Name: `CODACY_PROJECT_TOKEN`
  4. Value: Codacyから取得したトークン
  5. "Add secret" をクリック

## オプションシークレット

### 3. GPG_PRIVATE_KEY (デジタル署名用)

- **用途**: SBOM、SHA256SUMSファイルのGPG署名生成
- **権限**: GPG秘密鍵 (ASCII-armored形式)
- **取得方法**:
  1. ローカルでGPG鍵ペアを生成: `gpg --full-generate-key`
  2. 秘密鍵をエクスポート: `gpg --armor --export-secret-keys YOUR_KEY_ID`
  3. 出力をコピー (BEGIN/END含む)
- **設定方法**:
  1. リポジトリ Settings > Secrets and variables > Actions
  2. "New repository secret" をクリック
  3. Name: `GPG_PRIVATE_KEY`
  4. Value: エクスポートした秘密鍵
  5. "Add secret" をクリック

### 4. GPG_PASSPHRASE (GPG鍵のパスフレーズ)

- **用途**: GPG署名生成時のパスフレーズ入力
- **権限**: GPG鍵のパスフレーズ文字列
- **取得方法**: GPG鍵生成時に設定したパスフレーズを使用
- **設定方法**:
  1. リポジトリ Settings > Secrets and variables > Actions
  2. "New repository secret" をクリック
  3. Name: `GPG_PASSPHRASE`
  4. Value: パスフレーズ
  5. "Add secret" をクリック

## 検証方法

シークレットが正しく設定されているか確認するには、GitHub Actions workflowを実行し、以下をチェックします:

1. **GH_RELEASE_TOKEN**: リリース作成ステップでエラーが発生しないこと
2. **CODACY_PROJECT_TOKEN**: Codacyアップロードステップが成功し、Codacyダッシュボードにカバレッジが表示されること
3. **GPG_PRIVATE_KEY / GPG_PASSPHRASE**: 署名ステップが成功し、.sigファイルが生成されること

## セキュリティベストプラクティス

- **トークンのローテーション**: 6ヶ月ごとにトークンを再生成し、古いトークンを削除する
- **最小権限の原則**: 必要最小限の権限のみを付与する
- **シークレットの共有禁止**: シークレットをログ、コミット、Issueに含めない
- **監査ログの確認**: GitHub Settings > Audit log で不審なアクセスを定期的にチェックする

## トラブルシューティング

### 問題: リリース作成が "Resource not accessible by integration" エラーで失敗する

**原因**: `GH_RELEASE_TOKEN` の権限が不足している、またはトークンが無効  
**解決策**: トークンが `repo` スコープを持っていることを確認し、必要に応じて再生成する

### 問題: Codacyアップロードが "Unauthorized" エラーで失敗する

**原因**: `CODACY_PROJECT_TOKEN` が無効、または間違ったプロジェクトのトークンを使用している  
**解決策**: Codacyダッシュボードから正しいプロジェクトトークンを取得し、再設定する

### 問題: GPG署名生成が "gpg: signing failed: Inappropriate ioctl for device" エラーで失敗する

**原因**: パスフレーズが正しく渡されていない、またはGPG設定の問題  
**解決策**: `GPG_PASSPHRASE` が正しく設定されているか確認し、署名スクリプトで `--batch --yes --passphrase` オプションを使用していることを確認する

---

**最終更新**: 2026-01-04  
**担当者**: Release Pipeline Team
