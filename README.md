# EldenDeathCounter

## Overview

EldenDeathCounter is a Windows desktop app for tracking Elden Ring deaths locally. It provides a WPF control window and an always-on-top click-through overlay that can show the total death count, active boss information, boss-specific deaths, and boss timer details.

The default local data folder remains:

```text
%USERPROFILE%\Desktop\EldenDeathCounter\
```

## Features

- Tracks total deaths and stores the counter locally.
- Shows a transparent, always-on-top overlay for use while playing.
- Provides manual controls and global hotkeys for adding, subtracting, and recording boss victories.
- Stores active boss state and completed boss history.
- Can detect Elden Ring death screens from local screen captures.
- Can detect configured death phrases such as `YOU DIED` and `NIE ŻYJESZ`.
- Can attempt to read active boss names from the boss health bar area while detection is running.
- Supports English and Polish counter text, including examples such as `Deaths` and `Śmierci`.
- Writes local logs and detection diagnostics for troubleshooting.

## Requirements

- Windows 10 version 19041 or later.
- .NET 9 SDK.
- Elden Ring running in borderless fullscreen or windowed mode for the most reliable overlay behavior.
- Windows OCR language support for English and/or Polish if OCR detection is used.

The app targets `net9.0-windows10.0.19041.0` and `win-x64`.

## Build

Run these commands from the repository root:

```powershell
dotnet restore EldenDeathCounter.sln
dotnet build EldenDeathCounter.sln
```

Run tests with:

```powershell
dotnet test EldenDeathCounter.sln
```

## Run

```powershell
dotnet run --project src/EldenDeathCounter/EldenDeathCounter.csproj
```

Start EldenDeathCounter before or alongside Elden Ring. Use borderless fullscreen or windowed mode. Exclusive fullscreen can prevent normal desktop overlays from appearing correctly.

## How It Works

EldenDeathCounter keeps all state on the local machine. The WPF control window is used to manage settings, detection, manual counter changes, boss state, and history. The overlay is a separate click-through WPF window designed to stay above the game without receiving mouse input.

Death counting can happen manually through the UI or hotkeys, and automatically through screen-based detection. Boss history is updated when the active boss is marked as defeated.

## Detection

Detection is screen-based only. The app does not read or modify Elden Ring memory, inject DLLs, hook the game process, bypass anti-cheat, or interact with Easy Anti-Cheat.

The app uses local detection paths:

1. A local shape/template detector built from bundled death-screen reference images. It compares the centered death-message text shape using image contrast and edges.
2. Windows local OCR through `Windows.Media.Ocr`. OCR text is normalized and matched against configured detection phrases with fuzzy sensitivity.

Template detection is used first, with OCR as a fallback. Detection requires stable signals across frames before incrementing the counter, and a cooldown/latch prevents one death screen from being counted repeatedly.

Boss name detection looks for long red boss health bars in the lower part of the capture area, then OCRs the name area above each bar. If two boss health bars are visible, the active boss name can be combined as `Boss 1 + Boss 2`.

## Local Files

By default, EldenDeathCounter stores files under:

```text
%USERPROFILE%\Desktop\EldenDeathCounter\
```

Common files include:

- `deaths.json` stores `currentDeathCount`, `deathEvents`, `activeBoss`, and `bossHistory`.
- `appsettings.json` stores overlay, detection, phrase, folder, language, and hotkey settings.
- `log.txt` stores app, detection, hotkey, OCR, and storage log entries.
- `detection-events.jsonl` stores structured detection events when diagnostics are enabled.
- `diagnostics-latest.json` stores the latest diagnostics snapshot when diagnostics are enabled.
- `diagnostics\` stores diagnostics packages when full diagnostics are enabled.
- `detection-screenshots\*.png` stores captured frames for automatic detections so false positives can be reviewed.

If `deaths.json` or `appsettings.json` is corrupt, the app creates a timestamped backup and starts with a clean file.

## Settings

Settings can be edited in the control window or directly in:

```text
%USERPROFILE%\Desktop\EldenDeathCounter\appsettings.json
```

Important settings include:

- `overlayEnabled`
- `overlayX`
- `overlayY`
- `detectionEnabledOnStartup`
- `detectionIntervalMs`
- `detectionCooldownSeconds`
- `detectionSensitivity`
- `captureTarget`
- `dataFolderPath`
- `detectionPhrases`
- `manualAddHotkey`
- `manualSubtractHotkey`
- `bossDefeatedHotkey`
- `autoDetectBossNames`

`detectionIntervalMs` controls how often the screen is sampled. `detectionCooldownSeconds` controls the minimum time between separate death events. `captureTarget` controls which monitor or window target is sampled. `dataFolderPath` changes are safest after restarting the app.

## Hotkeys

Default global hotkeys:

- `F7` marks the active boss as defeated.
- `F8` adds one death.
- `F9` subtracts one death.

Hotkeys can be changed in the control window or in `appsettings.json`. If a hotkey does not work, another application may already be using it.

## Troubleshooting

- Overlay missing: use borderless fullscreen or windowed mode instead of exclusive fullscreen.
- Missed death: press `F8` to add one manually.
- Accidental detection: press `F9` to subtract one manually.
- Boss defeated: press `F7` or use the boss defeated button in the app.
- Boss name not detected: keep boss auto-detection enabled, make sure detection is running, and use the manual boss name field as a fallback.
- Double count on one death screen: keep `detectionCooldownSeconds` near the default value and avoid lowering detection stability too far.
- OCR not working: check `log.txt` for OCR language messages and install Windows OCR language support for English and/or Polish.
- Hotkeys not working: change the hotkey bindings in the control window or `appsettings.json`.
- Detection too strict or too loose: adjust `detectionSensitivity`. Higher values are stricter; lower values are more permissive.

## Limitations

- Borderless fullscreen or windowed mode is required or strongly recommended.
- Exclusive fullscreen may hide the overlay.
- OCR quality depends on resolution, scaling, language packs, and the death-screen presentation.
- Template detection searches the central death-message region and does not treat red scenery, lava, scarlet rot, or red HUD elements as death signals by themselves.
- Boss auto-detection depends on visible boss health bars and readable boss name text.
- Detection reliability should be verified in-game for the selected language and display setup.

## Privacy and Safety

EldenDeathCounter is designed to work locally. It stores data on disk, uses local screen capture, uses local Windows OCR when available, and does not require cloud services for detection.

The app is screen-based only. It does not modify the game, inspect game memory, inject code, hook the game process, or bypass anti-cheat systems.

## Development

Solution and project layout:

- `EldenDeathCounter.sln` is the main solution.
- `src/EldenDeathCounter` contains the WPF desktop app.
- `src/EldenDeathCounter.Core` contains testable core logic.
- `tests/EldenDeathCounter.Tests` contains xUnit tests.
- `Assets` contains reference images, icons, and other app assets copied into the app output.

For behavior or core logic changes, run:

```powershell
dotnet test EldenDeathCounter.sln
```

For project or dependency changes, run:

```powershell
dotnet build EldenDeathCounter.sln
```
