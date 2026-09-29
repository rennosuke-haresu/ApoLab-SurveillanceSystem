# ApoLab Surveillance System — Setup Guide

English | [日本語](SetupGuide_JA.md)

**Version**: 1.2.0  
**VRChat World SDK**: 3.8.2 or later (tested with 3.10.5)  
**Unity**: 2022.3.22f1 LTS or later

---

## Requirements

| Package | Notes |
|---|---|
| VRChat World SDK 3.8.2 or later | Required (tested with 3.10.5) |
| TextMeshPro | Included with Unity (add it from Window > Package Manager) |
| [tmp-fallback-fonts-jp](https://github.com/Narazaka/tmp-fallback-fonts-jp) | Required to show Japanese (and other CJK) camera names in the editor preview; otherwise they appear as `□□□` |

---

## What this asset does

Adds a **surveillance camera system** to your VRChat world.

- Show live camera feeds from around the world on monitors in a control room
- Switch feeds with the [◀][▶][OFF] buttons
- Place several monitors, each showing a different feed
- Assign which cameras each monitor switches between (e.g. the reception monitor shows only the entrance camera)
- Several monitors can show the same camera
- Cameras nobody is watching turn off automatically, keeping performance light
- For worlds that also want visible camera and monitor models, the Camera & Monitor Models edition (cameras: wall and ceiling / monitors: wall and desk) is sold separately (same BOOTH product page)

> ℹ️ **What each monitor shows is local to each player**  
> Switching a monitor only changes it on the screen of the player who pressed the button; it is not synced to other players. Everyone can watch the camera they like.

---

## Included files

```
ApoLab/SurveillanceSystem/
├── Prefabs/
│   ├── SurveillanceCamera_Template.prefab  ← Surveillance camera (duplicate to use)
│   ├── SurveillanceMonitor_Template.prefab ← Monitor + control buttons
│   └── SurveillanceManager.prefab          ← Manages the whole system
├── Materials/
│   ├── MonitorMaterial.mat                 ← Monitor material
│   └── Textures/
│       ├── RT_Camera_Template.renderTexture ← RT template (duplicate to use)
│       └── btn-*.png                        ← Button images
├── Scripts/                                 ← Scripts (no need to edit)
└── Documentation/                           ← This guide
```

---

## Setup

The overall flow is:

```
① Prepare RenderTextures (one per camera)
② Place cameras
③ Place monitors
④ Place the SurveillanceManager and connect everything
⑤ Test
```

---

### ① Prepare RenderTextures

**What is a RenderTexture (RT)?**  
It works like a videotape that records the camera's view. You need one per camera.

**Steps**

1. Open the `ApoLab/SurveillanceSystem/Materials/Textures/` folder
2. Right-click `RT_Camera_Template` → **Duplicate**
3. Rename it to `RT_Camera_01`
4. Repeat for each camera (e.g. for 3 cameras: `RT_Camera_01` to `03`)

**Check the RT settings (Inspector)**

| Setting | Recommended value |
|---|---|
| Size | **640 × 360** (16:9) |
| Depth Buffer | **At least 24 bits depth (with stencil)** |
| Anti-aliasing | **None** |

> ⚠️ With Depth Buffer set to "No depth buffer", objects in the feed are not drawn in the right depth order (things behind appear in front). Always use 24 bits  
> ⚠️ Avoid 1280×720 or higher; it is heavy  
> For Quest or many cameras, **320 × 180** is recommended

---

### ② Place cameras

1. Drag `SurveillanceCamera_Template.prefab` into the Hierarchy
2. Rename it to `SurveillanceCamera_01`
3. Move and rotate it to the spot you want to watch (ceiling, wall, etc.)
4. Set up the **CameraController** component (lower part of the Inspector)

   | Setting | Value |
   |---|---|
   | Camera Id | `0` (first camera) |
   | Camera Name | Name of the place (e.g. `Entrance`) |
   | Target Camera | Drag in the **Camera** on the same GameObject |
   | Render Texture | Drag in `RT_Camera_01` |

5. Duplicate with Ctrl+D and place as many as you need
   - Camera Id must be **sequential**: `0, 1, 2, 3...`
   - Give each camera its own RT (`RT_Camera_02`, etc.)

**About the Camera component defaults**

The prefab comes with settings that keep the load low. You usually do not need to change them; here is what they mean.

| Setting | Default | Reason |
|---|---|---|
| Target Eye | **None (Main Display)** | Rendering to a RenderTexture does not need VR stereo processing |
| Clipping Planes - Far | **100** | The expected range of a surveillance camera. Increase it only if you want to show distant outdoor scenery |
| Culling Mask | UiMenu / MirrorReflection **excluded** | Keeps VRChat menus and mirror reflections out of the feed |
| Allow HDR | **Off** | The output RenderTexture is not HDR, so enabling it has no effect and only adds load |

---

### ③ Place monitors

1. Drag `SurveillanceMonitor_Template.prefab` into the Hierarchy
2. Place it on a wall in the control room, etc.

> 💡 If you leave the monitor's Camera Controllers empty, the SurveillanceManager fills in all cameras automatically in the next step

#### Switch with 3D buttons (optional)

Instead of the on-screen buttons, players can also press 3D buttons on a model to switch.

1. Add a Collider to the object you want to use as a button
2. Add `MonitorButton`, set **Target Monitor** to the monitor it controls, and **Action** to Previous / Next / Off
3. The text shown when a player approaches the button is what you write in the UdonBehaviour's **Interaction Text** (e.g. Next camera). Turn on **Auto Localize** to switch it to the player's language automatically (English, Japanese, Korean, Chinese; other languages get English). It is off by default

> 💡 When pressed, the button sinks by Press Offset. The direction is in the button's local space
>
> 💡 Put a Renderer such as an LED and two materials into MonitorDisplay's **Status Light (Optional)** to change its look between showing video and off

---

### ④ Place the SurveillanceManager and connect everything

The SurveillanceManager manages all cameras and monitors together. Place **only one** in the scene.

1. Drag `SurveillanceManager.prefab` into the Hierarchy
2. Set up the **SurveillanceManager** component in the Inspector

**All Cameras** (register every camera)

- Set Size to the number of cameras (e.g. 3)
- Element 0 → `SurveillanceCamera_01`, Element 1 → `SurveillanceCamera_02`...
- ⚠️ **Keep the Element order the same as the Camera Id numbers**

```
Correct:
  Element 0 → camera with Camera Id 0  ✅
  Element 1 → camera with Camera Id 1  ✅

Wrong:
  Element 0 → camera with Camera Id 2  ❌
```

**All Monitors** (register monitors)

- Set Size to 1
- Element 0 → `SurveillanceMonitor_01` in the scene

**All Selectors** (register the same objects as All Monitors)

- Set Size to 1
- Element 0 → the same `SurveillanceMonitor_01`

> 💡 All Monitors and All Selectors take **the same objects**

#### Limit the cameras each monitor shows (optional)

If you want, say, the reception monitor to show only the entrance camera, you can choose the cameras on the monitor side.

1. Select the monitor's **MonitorDisplay** component
2. Register only the cameras that monitor should switch between in **Camera Controllers**

- Monitor with **empty** Camera Controllers → switches between all cameras in All Cameras (the usual case)
- Monitor with Camera Controllers **set** → ◀ ▶ switch only between those cameras
- Register those cameras in All Cameras as well

> ⚠️ When calling `_DisplayCamera(number)` from a script, the number is the position within that monitor's Camera Controllers

---

### ⑤ Test

Enter Play Mode with Unity's ▶ button. You are good if the Console shows:

```
[SurveillanceManager] Initialization completed successfully
```

Press each button and check that the feed switches.

Then test in VRChat with VRChat SDK → **Build & Test**.

---

## Troubleshooting

### Monitor shows nothing

| Where to check | What to check |
|---|---|
| The camera's CameraController | Target Camera and Render Texture are set |
| RenderTexture | Each camera has a **different** RT (they cannot be shared) |
| SurveillanceManager | The cameras are registered in All Cameras |
| The monitor's MonitorDisplay | The Quad's MeshRenderer is dragged into Display Renderer |

---

### Buttons don't respond

| Where to check | What to check |
|---|---|
| SurveillanceManager | All Monitors / All Selectors are set correctly |
| CameraSelector | Target Monitor is set |
| The button's OnClick | It is set correctly (see below) |

**Setting a button's OnClick in VRChat**

This differs from Unity's usual button setup. Follow these steps.

1. Select the button → Inspector → Button → click `+` under **On Click ()**
2. Drag the monitor object into the Object field
3. In the Function dropdown, choose `UdonBehaviour > SendCustomEvent (string)`
4. Type the method name in the text field that appears

| Button | Method name to enter |
|---|---|
| Previous button | `OnPreviousButtonClick` |
| Next button | `OnNextButtonClick` |
| OFF button | `OnOffButtonClick` |

---

## Performance

Only the cameras being shown are active, so even many cameras keep the load minimal.

| RenderTexture resolution | Recommended for |
|---|---|
| 640 × 360 | PC (standard, 16:9) |
| 320 × 180 | Quest, or many cameras |

---

## Update history

| Version | Date | Changes |
|---|---|---|
| 1.3.0 | 2026-09-28 | Added 3D buttons (MonitorButton); added an optional status light setting to MonitorDisplay |
| 1.2.0 | 2026-09-27 | Cameras can be assigned per monitor; fixed cameras without a RenderTexture rendering to the screen; monitors can be deactivated or start inactive; **removed CameraController's `_SetCameraActive` / `_ToggleCameraActive` / `_ActivateCamera` / `_DeactivateCamera` and MonitorDisplay's `surveillanceManager` field** (turn cameras on and off through monitors) |
| 1.1.0 | 2026-07-27 | Fixed the RenderTexture depth buffer; fixed showing the same camera on several monitors; skip invalid cameras; tuned camera defaults |
| 1.0.2 | 2026-05-16 | Changed the monitor shader; show a black screen when off |
| 1.0.1 | 2026-05-14 | Fixed camera settings and the monitor material |
| 1.0.0 | 2026-05-14 | First release |

---

## License

This asset is distributed under the **VN3 License**.
https://drive.google.com/drive/folders/1x8BSXXR-kyvnCfBWSBtScSquU2CB01SC

---

*ApoLab — https://apocrypha-lab.booth.pm/*
