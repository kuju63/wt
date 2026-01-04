# Conventional Commits 規約

このプロジェクトでは、コミットメッセージに **Conventional Commits** 規約を採用しています。

## 公式仕様

- 公式サイト: <https://www.conventionalcommits.org/>
- バージョン: 1.0.0

## 基本フォーマット

```text
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### type (必須)

コミットの種類を示します。以下のtypeを使用してください:

- **feat**: 新機能の追加 (MINOR version bump)
- **fix**: バグ修正 (PATCH version bump)
- **docs**: ドキュメントのみの変更
- **style**: コードの意味に影響を与えない変更 (空白、フォーマット、セミコロン等)
- **refactor**: バグ修正や機能追加を伴わないコード変更
- **perf**: パフォーマンス改善
- **test**: テストの追加や修正
- **chore**: ビルドプロセス、補助ツール、ライブラリの変更 (例: package.json更新)
- **ci**: CI設定ファイルやスクリプトの変更 (例: GitHub Actions)
- **build**: ビルドシステムや外部依存関係に影響する変更

### scope (オプション)

変更の影響範囲を示します。このプロジェクトでは**原則として使用しません**が、必要な場合のみ以下のように記述できます:

```text
feat(cli): add --version flag
fix(worktree): handle invalid branch names
```

### description (必須)

変更内容を簡潔に説明します:

- **英語で記述**すること
- **命令形・現在形**を使用 (例: "add" not "added" or "adds")
- **小文字で開始** (最初の文字は小文字)
- **末尾にピリオドを付けない**

### body (オプション)

変更の理由、以前の動作との違い、影響範囲などを説明します:

- description から1行空けて記述
- 複数行可能
- Markdown形式可能

### footer (オプション)

Breaking changes、Issue参照を記述します:

- **BREAKING CHANGE**: 破壊的変更を示す (MAJOR version bump)
- **Closes #123**: Issueのクローズを示す

## 例

### 新機能 (feat)

```text
feat: add multi-platform binary distribution

Implement GitHub Actions workflow to build binaries for
Windows x64, Linux x64/ARM, and Mac ARM64.

Closes #42
```

### バグ修正 (fix)

```text
fix: prevent crash when branch name contains special characters

Handle edge case where branch names with slashes cause
path resolution errors.
```

### 破壊的変更 (BREAKING CHANGE)

```text
feat: change CLI argument format to kebab-case

BREAKING CHANGE: All CLI arguments now use kebab-case instead of camelCase.
Users must update scripts: `--outputFormat` → `--output-format`

Migration guide: docs/migration/v2.0.md
```

### ドキュメント (docs)

```text
docs: update installation guide for macOS ARM64
```

### CI/CD (ci)

```text
ci: add Codacy coverage upload to test workflow
```

### テスト (test)

```text
test: add integration tests for SBOM generation
```

## セマンティックバージョニングとの関係

Conventional Commitsは、自動バージョニングに使用されます:

| Commit Type | Version Bump | 例 |
|-------------|--------------|-----|
| `fix:` | PATCH (0.0.X) | 0.1.0 → 0.1.1 |
| `feat:` | MINOR (0.X.0) | 0.1.0 → 0.2.0 |
| `BREAKING CHANGE:` | MAJOR (X.0.0) | 0.1.0 → 1.0.0 |
| その他 (docs, style, etc.) | バージョン変更なし | - |

## 自動バージョニングの仕組み

1. mainブランチへのマージ時、最新Gitタグを取得
2. 前回リリース以降のコミットメッセージを解析
3. `feat:`, `fix:`, `BREAKING CHANGE:` の有無でバージョンを決定
4. 新しいバージョンタグを作成し、GitHub Releaseを公開

## コミットメッセージのチェック

将来的に、以下のツールを導入予定です:

- **commitlint**: コミットメッセージの形式チェック
- **husky**: Git hooksによるローカルバリデーション

現在は、手動でConventional Commits規約に従ってください。

## 参考資料

- [Conventional Commits 日本語翻訳](https://www.conventionalcommits.org/ja/)
- [Semantic Versioning 2.0.0](https://semver.org/)
- [GitHub - semantic-release](https://github.com/semantic-release/semantic-release)

---

**最終更新**: 2026-01-05  
**担当者**: Release Pipeline Team
