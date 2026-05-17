# ApoLab SurveillanceSystem

VRChat ワールド向け監視カメラシステムです。複数のカメラ映像をモニターに切り替え表示できます。

**BOOTH**: https://apocrypha-lab.booth.pm/

---

## 機能

- 最大8台のカメラをモニターに切り替え表示
- [◀][▶] ボタンでカメラを順送り・逆送り
- [OFF] ボタンでモニターを黒画面に切り替え
- RenderTexture による軽量なリアルタイム映像表示
- UdonSharp 製のため改変・拡張が容易

---

## 動作環境

| 項目 | バージョン |
|---|---|
| Unity | 2022.3.22f1 LTS |
| VRChat World SDK | 3.8.2 以降 |
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

---

## リリース履歴

| バージョン | 日付 | 内容 |
|---|---|---|
| v1.0.2 | 2026-05-16 | モニターシェーダー変更・OFF 時に黒画面を表示するよう修正 |
| v1.0.1 | 2026-05-14 | カメラ設定修正・モニターマテリアル修正 |
| v1.0.0 | 2026-05-14 | 初回リリース |

---

## ライセンス

© 2026 ApoLab (Apocrypha-Lab). All rights reserved.  
本パッケージは購入者による VRChat ワールドへの利用・改変を許可します。再配布・転売は禁止です。
