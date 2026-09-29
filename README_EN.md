# ApoLab SurveillanceSystem

English | [日本語](README.md)

A surveillance camera system for VRChat worlds. Switch between multiple camera feeds on monitors.

**BOOTH**: https://apocrypha-lab.booth.pm/

---

## Features

- Switch between up to 8 cameras on a monitor
- Assign which cameras each monitor switches between (e.g. the reception monitor shows only the entrance camera)
- Step forward and back through cameras with the [◀][▶] buttons
- Turn a monitor to a black screen with the [OFF] button
- Switch with 3D buttons (MonitorButton) as well (e.g. buttons on your own models)
- 3D buttons show their label in the player's language (EN / JA / KO / ZH)
- Lightweight real-time feeds using RenderTextures
- Several monitors can show the same camera
- Written in UdonSharp, easy to modify and extend

> What each monitor shows is local to each player. Switching only changes the screen of the player who pressed the button; it is not synced to other players.

---

## Requirements

| Item | Version |
|---|---|
| Unity | 2022.3.22f1 LTS |
| VRChat World SDK | 3.8.2 or later (tested with 3.10.5) |
| UdonSharp | Included with the SDK |
| TextMeshPro | Unity Registry version |

---

## Setup

1. Add the World SDK to your project with **VRChat Creator Companion (VCC)**
2. Install **TextMeshPro** from the Package Manager (run Import TMP Essentials)
3. Double-click `ApoLabSurveillanceSystem_vX.X.X.unitypackage` to import it
4. Open `Assets/ApoLab/SurveillanceSystem/Scenes/SurveillanceSystemSample.unity` to try it

For details, see [Assets/ApoLab/SurveillanceSystem/Documentation/SetupGuide_EN.md](Assets/ApoLab/SurveillanceSystem/Documentation/SetupGuide_EN.md).

---

## Structure

```
Assets/ApoLab/SurveillanceSystem/
├── Documentation/      # Setup guides
├── Materials/          # Materials, RenderTextures, button images
├── Prefabs/            # Camera / Monitor / Manager prefabs
├── Scenes/             # SurveillanceSystemSample.unity (demo scene)
└── Scripts/
    ├── SurveillanceManager.cs   # Manages all cameras and monitors
    ├── CameraController.cs      # Controls one camera
    ├── MonitorDisplay.cs        # Shows RenderTextures on a monitor
    ├── CameraSelector.cs        # Camera switching UI
    └── MonitorButton.cs         # Switching with 3D buttons
```

---

## Usage

1. Place the **SurveillanceManager** prefab in the scene
2. Duplicate and place **SurveillanceCamera_Template** for each camera
3. Duplicate and place **SurveillanceMonitor_Template** for each monitor
4. Assign the cameras and monitors in the SurveillanceManager Inspector
5. (Optional) To limit the cameras a monitor shows, register them in that monitor's **MonitorDisplay → Camera Controllers**

---

## Release history

| Version | Date | Changes |
|---|---|---|
| v1.3.0 | 2026-09-28 | Added 3D buttons (MonitorButton); added an optional status light setting to MonitorDisplay |
| v1.2.0 | 2026-09-27 | Cameras can be assigned per monitor; fixed cameras without a RenderTexture rendering to the screen; monitors can be deactivated or start inactive; **removed CameraController's `_SetCameraActive` / `_ToggleCameraActive` / `_ActivateCamera` / `_DeactivateCamera` and MonitorDisplay's `surveillanceManager` field** (turn cameras on and off through monitors) |
| v1.1.0 | 2026-07-27 | Fixed the RenderTexture depth buffer; fixed showing the same camera on several monitors; skip invalid cameras; tuned camera defaults |
| v1.0.2 | 2026-05-16 | Changed the monitor shader; show a black screen when off |
| v1.0.1 | 2026-05-14 | Fixed camera settings and the monitor material |
| v1.0.0 | 2026-05-14 | First release |

---

## License

This project is distributed under the **VN3 License**.  
See [LICENSE.md](LICENSE.md) for links to the full license text (Japanese, English, Korean, Chinese).
