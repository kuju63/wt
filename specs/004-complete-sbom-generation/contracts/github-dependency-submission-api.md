# GitHub Dependency Submission API Contract

このファイルは、GitHub Dependency Submission API v1の契約を定義します。

## エンドポイント

```curl
POST /repos/{owner}/{repo}/dependency-graph/snapshots
```

### パス パラメータ

| パラメータ | 型     | 必須 | 説明                                           |
| ---------- | ------ | ---- | ---------------------------------------------- |
| `owner`    | string | ✅   | リポジトリのオーナー（ユーザー名または組織名） |
| `repo`     | string | ✅   | リポジトリ名                                   |

### 認証

```text
Authorization: Bearer {GITHUB_TOKEN}
```

必要な権限:

- `contents: write` - 依存関係スナップショットの書き込み
- `id-token: write` - OIDCトークンの生成（GitHub Actions内で自動）

---

## リクエスト

### ヘッダー

```text
Content-Type: application/json
Accept: application/vnd.github+json
X-GitHub-Api-Version: 2022-11-28
```

### ボディ（JSON）

```json
{
  "version": 0,
  "sha": "string",
  "ref": "string",
  "job": {
    "correlator": "string",
    "id": "string",
    "html_url": "string"
  },
  "detector": {
    "name": "string",
    "version": "string",
    "url": "string"
  },
  "scanned": "string",
  "metadata": {},
  "manifests": {
    "{manifest_name}": {
      "name": "string",
      "file": {
        "source_location": "string"
      },
      "metadata": {},
      "resolved": {
        "{package_url}": {
          "package_url": "string",
          "relationship": "string",
          "scope": "string",
          "dependencies": ["string"]
        }
      }
    }
  }
}
```

---

## フィールド定義

### トップレベル

| フィールド  | 型      | 必須 | 説明                                  |
| ----------- | ------- | ---- | ------------------------------------- |
| `version`   | integer | ✅   | APIバージョン（常に `0`）             |
| `sha`       | string  | ✅   | スナップショットに関連するコミットSHA |
| `ref`       | string  | ✅   | Git参照（例: `refs/heads/main`）      |
| `job`       | object  | ✅   | ジョブ情報                            |
| `detector`  | object  | ✅   | 検出ツール情報                        |
| `scanned`   | string  | ✅   | スキャン日時（ISO 8601形式）          |
| `metadata`  | object  | ❌   | 追加メタデータ（任意）                |
| `manifests` | object  | ✅   | マニフェストと依存関係のマップ        |

### job オブジェクト

| フィールド   | 型     | 必須 | 説明                                  |
| ------------ | ------ | ---- | ------------------------------------- |
| `correlator` | string | ✅   | ワークフローとジョブの一意識別子      |
| `id`         | string | ✅   | ジョブID（GitHub Actions run IDなど） |
| `html_url`   | string | ❌   | ジョブのURL                           |

**correlatorの形式**: `{workflow_name}-{job_name}`

例: `release-publish`

### detector オブジェクト

| フィールド | 型     | 必須 | 説明             |
| ---------- | ------ | ---- | ---------------- |
| `name`     | string | ✅   | 検出ツール名     |
| `version`  | string | ✅   | ツールバージョン |
| `url`      | string | ✅   | ツールのURL      |

### manifests オブジェクト

キーはマニフェストファイル名（例: `wt.cli.csproj`）、値はManifestオブジェクト。

#### Manifest オブジェクト

| フィールド             | 型     | 必須 | 説明                     |
| ---------------------- | ------ | ---- | ------------------------ |
| `name`                 | string | ✅   | マニフェスト名           |
| `file.source_location` | string | ❌   | ソースファイルの相対パス |
| `metadata`             | object | ❌   | 追加メタデータ           |
| `resolved`             | object | ✅   | 解決済み依存関係のマップ |

#### resolved オブジェクト

キーはPackage URL（purl）、値はDependencyオブジェクト。

#### Dependency オブジェクト

| フィールド     | 型     | 必須 | 説明                                     |
| -------------- | ------ | ---- | ---------------------------------------- |
| `package_url`  | string | ✅   | Package URL（purl）形式                  |
| `relationship` | string | ❌   | `direct` または `indirect`               |
| `scope`        | string | ❌   | `runtime` または `development`           |
| `dependencies` | array  | ❌   | このパッケージが依存する他のpurlのリスト |

---

## レスポンス

### 成功（201 Created）

```json
{
  "id": 12345678,
  "created_at": "2026-01-06T12:00:00Z",
  "message": "Dependency snapshot created successfully"
}
```

#### レスポンスフィールド

| フィールド   | 型      | 説明                     |
| ------------ | ------- | ------------------------ |
| `id`         | integer | スナップショットID       |
| `created_at` | string  | 作成日時（ISO 8601形式） |
| `message`    | string  | 成功メッセージ           |

### エラー

#### 400 Bad Request

```json
{
  "message": "Invalid request",
  "errors": [
    {
      "field": "manifests",
      "code": "missing_field"
    }
  ]
}
```

#### 401 Unauthorized

```json
{
  "message": "Bad credentials"
}
```

#### 403 Forbidden

```json
{
  "message": "Resource not accessible by integration"
}
```

#### 404 Not Found

```json
{
  "message": "Not Found"
}
```

#### 422 Unprocessable Entity

```json
{
  "message": "Validation Failed",
  "errors": [
    {
      "resource": "DependencySnapshot",
      "field": "package_url",
      "code": "invalid"
    }
  ]
}
```

---

## 使用例

### リクエスト例（wt CLI Tool）

```bash
curl -X POST \
  -H "Authorization: Bearer ${GITHUB_TOKEN}" \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  -H "Content-Type: application/json" \
  https://api.github.com/repos/kuju63/wt/dependency-graph/snapshots \
  -d @- <<EOF
{
  "version": 0,
  "sha": "abc123def456",
  "ref": "refs/heads/main",
  "job": {
    "correlator": "release-publish",
    "id": "87654321"
  },
  "detector": {
    "name": "Microsoft SBOM Tool",
    "version": "2.2.0",
    "url": "https://github.com/microsoft/sbom-tool"
  },
  "scanned": "2026-01-06T12:00:00Z",
  "manifests": {
    "wt.cli.csproj": {
      "name": "wt.cli",
      "file": {
        "source_location": "wt.cli/wt.cli.csproj"
      },
      "resolved": {
        "pkg:nuget/System.CommandLine@2.0.1": {
          "package_url": "pkg:nuget/System.CommandLine@2.0.1",
          "relationship": "direct",
          "scope": "runtime",
          "dependencies": [
            "pkg:nuget/System.Memory@4.5.5"
          ]
        },
        "pkg:nuget/System.Memory@4.5.5": {
          "package_url": "pkg:nuget/System.Memory@4.5.5",
          "relationship": "indirect",
          "scope": "runtime"
        },
        "pkg:nuget/System.IO.Abstractions@22.1.0": {
          "package_url": "pkg:nuget/System.IO.Abstractions@22.1.0",
          "relationship": "direct",
          "scope": "runtime"
        }
      }
    }
  }
}
EOF
```

### レスポンス例

```json
{
  "id": 12345678,
  "created_at": "2026-01-06T12:00:01Z",
  "message": "Dependency snapshot created successfully"
}
```

---

## GitHub Actions統合

### actions/dependency-submission@v3の使用

GitHub公式のアクションを使用する場合（推奨）:

```yaml
- name: Submit dependencies to GitHub
  uses: actions/dependency-submission@v3
  with:
    # SBOMファイルのパス
    sbom-file: _manifest/sbom.spdx.json
    # フォーマット（spdxまたはcyclonedx）
    snapshot-format: spdx
    # トークン（省略時はGITHUB_TOKENを自動使用）
    token: ${{ secrets.GITHUB_TOKEN }}
```

このアクションは内部で以下を実行:

1. SPDX/CycloneDXファイルをパース
2. Dependency Snapshot形式に変換
3. API呼び出し
4. エラーハンドリング

### 手動API呼び出し

より細かい制御が必要な場合:

```yaml
- name: Convert SBOM to Dependency Snapshot
  run: |
    # SPDXからDependency Snapshot形式への変換スクリプト
    python scripts/spdx-to-snapshot.py \
      --spdx _manifest/sbom.spdx.json \
      --output snapshot.json \
      --sha ${{ github.sha }} \
      --ref ${{ github.ref }}

- name: Submit to GitHub API
  run: |
    curl -X POST \
      -H "Authorization: Bearer ${{ secrets.GITHUB_TOKEN }}" \
      -H "Accept: application/vnd.github+json" \
      -H "X-GitHub-Api-Version: 2022-11-28" \
      -H "Content-Type: application/json" \
      https://api.github.com/repos/${{ github.repository }}/dependency-graph/snapshots \
      -d @snapshot.json
```

---

## エラーハンドリング戦略

### リトライ戦略

```yaml
- name: Submit with retry
  uses: nick-fields/retry@v2
  with:
    timeout_minutes: 5
    max_attempts: 3
    retry_wait_seconds: 10
    command: |
      gh api \
        --method POST \
        -H "Accept: application/vnd.github+json" \
        -H "X-GitHub-Api-Version: 2022-11-28" \
        /repos/${{ github.repository }}/dependency-graph/snapshots \
        --input snapshot.json
```

### 失敗時の動作（FR-013準拠）

```yaml
- name: Submit dependencies
  uses: actions/dependency-submission@v3
  with:
    sbom-file: _manifest/sbom.spdx.json
    snapshot-format: spdx
  # 失敗時はパイプライン全体を失敗させる
  continue-on-error: false
```

---

## バリデーション

### リクエストバリデーション

送信前に以下を確認:

1. **Package URL形式**: `pkg:nuget/PackageName@Version`
2. **ISO 8601日時**: `YYYY-MM-DDTHH:MM:SSZ`
3. **Git参照**: `refs/heads/` または `refs/tags/` で始まる
4. **コミットSHA**: 40文字の16進数

### 検証スクリプト例

```bash
#!/bin/bash
set -euo pipefail

SNAPSHOT_FILE="$1"

# JSON形式チェック
jq empty "$SNAPSHOT_FILE" || {
  echo "Invalid JSON format"
  exit 1
}

# 必須フィールドチェック
required_fields=("version" "sha" "ref" "job" "detector" "scanned" "manifests")
for field in "${required_fields[@]}"; do
  jq -e ".$field" "$SNAPSHOT_FILE" > /dev/null || {
    echo "Missing required field: $field"
    exit 1
  }
done

# Package URL形式チェック
jq -r '.manifests[].resolved | keys[]' "$SNAPSHOT_FILE" | while read -r purl; do
  if [[ ! "$purl" =~ ^pkg:[a-z]+/.+@.+ ]]; then
    echo "Invalid package URL: $purl"
    exit 1
  fi
done

echo "Validation passed"
```

---

## レート制限

### GitHub Actions内

- **制限**: 通常制限なし（大規模なスナップショットを除く）
- **監視**: X-RateLimit-* ヘッダーをチェック

### GitHub Actions外

- **認証済みリクエスト**: 5,000リクエスト/時間
- **未認証**: 60リクエスト/時間

---

## セキュリティ考慮事項

1. **トークン権限**: 最小権限（`contents: write`のみ）
2. **秘密情報の除外**: 依存関係情報のみ送信（ソースコードや秘密情報は含まない）
3. **HTTPS必須**: すべてのAPI通信はHTTPS
4. **トークン有効期限**: GitHub Actionsのトークンはジョブごとに自動更新

---

## 参考リンク

- [GitHub Dependency Submission API公式ドキュメント](https://docs.github.com/en/rest/dependency-graph/dependency-submission)
- [actions/dependency-submission](https://github.com/actions/dependency-submission)
- [Package URL仕様](https://github.com/package-url/purl-spec)
- [SPDX仕様](https://spdx.github.io/spdx-spec/)
