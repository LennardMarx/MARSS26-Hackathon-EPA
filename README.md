# Hackathon Project 5

## 1. Project Setup

Import the following packages:

- **extOSC for Unity** — [Asset Store](https://assetstore.unity.com/packages/tools/input-management/extosc-open-sound-control-72005)
- **NuGet for Unity** — [GitHub](https://github.com/GlitchEnzo/NuGetForUnity)

Using NuGet, install:

- `MathNet.Numerics`
- `System.ValueTuple`
- `System.IO.Ports`

## 2. Project Structure (BasicScene)

### Folders

- **Materials** — Contains basic shaders for visualization. You can improve and extend these.
- **Prefabs** — Includes the dynamic heart and related models.

## 4. Core Scripts
Before modifying the project, please understand what each script does.

- **Esanimato** — Handles visualization of the dynamic heart.
- **NeedlePivotCalibration** — Guides the user through pivot calibration of the needle and instantiates the needle visualization.
  - If a calibration file exists, it will be loaded automatically
  - Example file: `test.txt`
  - You can modify the script to:
    - start calibration automatically when the scene starts, or
    - trigger it using a simple UI button
- **ToMax** — Computes navigation parameters and streams data to Max/MSP via OSC.

## 5. Hierarchy and GameObjects

> **Important:** Do not change the hierarchy if you want to use the camera frame as the global reference frame. In this case, no landmark-based registration is needed.

- **OSCManager** — Handles OSC communication with Max/MSP.
- **Tracking** — Main tracking manager. Contains ROM files (`.bytes` format). The order of the ROM files is important.
- **NDISpace** — Fixed coordinate system defined by the camera.
  - **targetBody** (related to the phantom)
    - **MainCamera** — The visualization follows the tracked phantom position.
    - **target** — Contains anatomical models. You can replace the skin model and update the anatomy with a scanned phantom. Keep ribs and PZT aligned with the dynamic heart. Adjust positions manually if needed.
    - **PlannedTarget** — Defines navigation and trajectory.
    - **SonicArea** — A sphere used to define the sonification region.
      - Main script: `ToMaxPatch` (the sonification module)
  - **MovingSpace** — Represents tool space.
    - **TrackedNeedle** — Contains the tracking script and `NeedlePivotCalibration` for tool calibration.
    - **ToMax** — Contains the `ToMaxSonixSense` script. Handles navigation parameter computation and streaming to Max/MSP.

## 6. Notes for Team

- Try to understand what each script computes before modifying anything
- Focus on interaction and process, not only visualization
- You are encouraged to:
  - modify sonification
  - experiment with mapping strategies
  - explore different interaction-based designs
