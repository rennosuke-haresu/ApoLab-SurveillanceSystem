# ApoLab SurveillanceSystem

VRChat ワールド向け監視カメラシステムです。複数のカメラ映像をモニターに切り替え表示できます。

**BOOTH**: https://apocrypha-lab.booth.pm/

---

## 機能

- 最大8台のカメラをモニターに切り替え表示
- モニターごとに切り替えるカメラを割り当て可能（例：受付は入口カメラだけ）
- [◀][▶] ボタンでカメラを順送り・逆送り
- [OFF] ボタンでモニターを黒画面に切り替え
- RenderTexture による軽量なリアルタイム映像表示
- 複数モニターで同じカメラを表示可能
- UdonSharp 製のため改変・拡張が容易

> 表示状態はプレイヤーごとにローカルです。切り替え操作は操作した本人の画面にのみ反映され、他プレイヤーには同期されません。

---

## 動作環境

| 項目 | バージョン |
|---|---|
| Unity | 2022.3.22f1 LTS |
| VRChat World SDK | 3.8.2 以降（3.10.5 で動作確認）|
| UdonSharp | SDK 同梱 |
| TextMeshPro | Unity Registry 版 |

---

## セットアップ

1. **VRChat Creator Companion (VCC)** でプロジェクトに World SDK を追加
2. **TextMeshPro** を Package Manager からインストール（Import TMP Essentials を実行）
3. `ApoLabSurveillanceSystem_vX.X.X.unitypackage` をダブルクリックしてインポート
4. `Assets/ApoLab/SurveillanceSystem/Scenes/SurveillanceSystemSample.unity` を開いて動作確認

詳細は [Assets/ApoLab/SurveillanceSystem/Documentation/セットアップガイド.md](Assets/ApoLab/SurveillanceSystem/Documentation/セットアップガイド.md) を参照してください。

---

## 構成

```
Assets/ApoLab/SurveillanceSystem/
├── Documentation/      # セットアップガイド
├── Materials/          # マテリアル・RenderTexture・ボタン画像
├── Prefabs/            # Camera / Monitor / Manager プレハブ
├── Scenes/             # SurveillanceSystemSample.unity（デモシーン）
└── Scripts/
    ├── SurveillanceManager.cs   # カメラ・モニターの統括管理
    ├── CameraController.cs      # 個別カメラの制御
    ├── MonitorDisplay.cs        # RenderTexture のモニター表示
    └── CameraSelector.cs        # カメラ切り替え UI
```

---

## 使い方

1. **SurveillanceManager** プレハブをシーンに配置
2. **SurveillanceCamera_Template** を使用するカメラ分複製・配置
3. **SurveillanceMonitor_Template** をモニター分複製・配置
4. SurveillanceManager の Inspector でカメラとモニターを割り当て
5. （任意）モニターごとに見せるカメラを絞る場合は、各モニターの **MonitorDisplay → Camera Controllers** にカメラを登録

---

## リリース履歴

| バージョン | 日付 | 内容 |
|---|---|---|
| v1.2.0 | 2026-09-27 | モニターごとに切り替えるカメラを割り当て可能に、RenderTexture 未設定のカメラが画面に映り込む問題を修正、モニターの非アクティブ化・開始時非アクティブに対応、**CameraController の `_SetCameraActive` / `_ToggleCameraActive` / `_ActivateCamera` / `_DeactivateCamera` と MonitorDisplay の `surveillanceManager` フィールドを削除**（カメラの ON/OFF はモニター経由で行ってください） |
| v1.1.0 | 2026-07-27 | RenderTexture の深度バッファ修正、複数モニターで同じカメラを表示できるよう修正、無効なカメラの読み飛ばし、カメラ既定値の最適化 |
| v1.0.2 | 2026-05-16 | モニターシェーダー変更・OFF 時に黒画面を表示するよう修正 |
| v1.0.1 | 2026-05-14 | カメラ設定修正・モニターマテリアル修正 |
| v1.0.0 | 2026-05-14 | 初回リリース |

---

## ライセンス

本プロジェクトは **VN3ライセンス** のもとに配布されます。  
ライセンス全文（日本語・English・한국어・中文）へのリンクは [LICENSE.md](LICENSE.md) を参照してください。
