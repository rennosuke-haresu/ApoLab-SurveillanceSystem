# CLAUDE.md — ApoLab SurveillanceSystem

## プロジェクト概要
- **用途**: VRChat ワールド向け監視カメラシステム（BOOTH 配布用独立プロジェクト）
- **Unity**: 2022.3.22f1 LTS
- **VRChat World SDK**: 3.8.2 以降
- **言語**: UdonSharp (C#ライクシンタックス)
- **BOOTH**: https://apocrypha-lab.booth.pm/

## プロジェクト構造
```
Assets/ApoLab/SurveillanceSystem/
├── Documentation/   # エンドユーザー向けセットアップガイド
├── Materials/       # マテリアル・RenderTexture・ボタン画像
├── Prefabs/         # SurveillanceCamera/Monitor/Manager テンプレート
├── Scenes/          # SurveillanceSystemSample.unity（デモシーン）
└── Scripts/         # SurveillanceManager / CameraController / MonitorDisplay / CameraSelector
```

## 開発ワークフロー
1. このプロジェクトで開発・テスト
2. `Assets/ApoLab/SurveillanceSystem` を unitypackage でエクスポート
3. Nexus-World（`d:\Unity Project\Nexus-World`）にインポートして反映
4. BOOTH の商品ページを新バージョンとして更新

## 命名規則
- Namespace: `ApoLab.SurveillanceSystem`
- スクリプト: PascalCase
- ブランチ: `feature/*`, `fix/*`, `release/v*`

## 実装の注意点
- `sharedMaterial` は Udon で動作しない → `material`（インスタンス）を使う
- `offTexture` は public フィールドで保持
- RenderTexture: サイズ **640×360**（16:9）/ Depth Buffer は **24bit**（YAML: `m_DepthStencilFormat: 92`）。深度なしだと映像内の前後関係が壊れる
- カメラ: `Allow HDR: Off`（出力先 RT が非 HDR のため効果なし）、`Target Eye: None`、`Far Clip: 100`、Culling Mask から UiMenu / MirrorReflection を除外
- 状態を変更する public メソッドは `_` 接頭辞を付ける（ネットワークイベント経由の呼び出しを防ぐ）。ただしボタン OnClick から呼ばれる `OnPreviousButtonClick` / `OnNextButtonClick` / `OnOffButtonClick` は互換性のため接頭辞なしを維持
- カメラの ON/OFF は `CameraController` の表示カウント（`_AddViewer` / `_RemoveViewer`）で制御する。複数モニターが同じカメラを見ている状態を壊さないため、`Camera.enabled` を直接操作しない
- `MonitorMaterial.mat` シェーダー: `Unlit/Texture`

## Git 管理対象
- **含める**: `Assets/`, `ProjectSettings/`, `Packages/manifest.json`
- **除外**: `Library/`, `Temp/`, `Logs/`, `obj/`, `*.tmp`, `ClientSimStorage/`, `.uloop/`

## 使用ツール
- uLoopMCP: MCP server for Unity
