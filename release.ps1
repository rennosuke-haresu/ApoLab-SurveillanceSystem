# ApoLab SurveillanceSystem — Release Script
#
# Usage:
#   .\release.ps1 -Version 1.0.3 -Notes "変更内容" [-PackagePath "path\to\file.unitypackage"]
#
# PackagePath を省略すると Downloads フォルダを自動検索します。

param(
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$Notes,
    [string]$PackagePath
)

$Tag = "v$Version"
$ExpectedFileName = "ApoLabSurveillanceSystem_v$Version.unitypackage"

# ── 1. unitypackage の解決 ─────────────────────────────────────────────────────

if (-not $PackagePath) {
    $PackagePath = Join-Path $PSScriptRoot "Releases\$ExpectedFileName"
}

if (-not (Test-Path $PackagePath)) {
    Write-Error "unitypackage が見つかりません: $PackagePath"
    Write-Host "Unity から以下のパスへエクスポートしてください:"
    Write-Host "  Releases\$ExpectedFileName"
    exit 1
}

Write-Host "unitypackage: $PackagePath" -ForegroundColor Cyan

# ── 2. git commit ──────────────────────────────────────────────────────────────

# .gitignore で除外済みのファイル（Library/ Temp/ Releases/ など）は含まれない
git add -A
if ($LASTEXITCODE -ne 0) { Write-Error "git add 失敗"; exit 1 }

# ステージに差分があるときだけコミットする（--quiet は差分ありで exit 1）
git diff --cached --quiet
if ($LASTEXITCODE -ne 0) {
    git commit -m "release: $Tag"
    if ($LASTEXITCODE -ne 0) { Write-Error "git commit 失敗"; exit 1 }
} else {
    Write-Host "コミットする変更なし、タグのみ作成します。" -ForegroundColor Yellow
}

# ── 3. git tag ────────────────────────────────────────────────────────────────

git tag -a $Tag -m "$Tag - $Notes"
if ($LASTEXITCODE -ne 0) { Write-Error "git tag 失敗（既に存在する可能性）"; exit 1 }

# ── 4. git push ───────────────────────────────────────────────────────────────

git push origin master
git push origin $Tag
if ($LASTEXITCODE -ne 0) { Write-Error "git push 失敗"; exit 1 }

# ── 5. GitHub Release 作成 + unitypackage アップロード ─────────────────────────

gh release create $Tag `
    --title $Tag `
    --notes $Notes `
    $PackagePath

if ($LASTEXITCODE -ne 0) { Write-Error "gh release create 失敗"; exit 1 }

Write-Host ""
Write-Host "リリース完了: https://github.com/rennosuke-haresu/ApoLab-SurveillanceSystem/releases/tag/$Tag" -ForegroundColor Green
