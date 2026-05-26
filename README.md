<<<<<<< HEAD
# EldenDeathCounter

Windows desktop app for tracking Elden Ring deaths locally with a live always-on-top overlay.

The overlay text is:

```text
Śmierci: 0
```

## Build

This machine currently has .NET SDK 9.0.305 installed, so the project targets `net9.0-windows10.0.19041.0`. The code is structured so it can be moved to .NET 10 LTS by installing the .NET 10 SDK and changing the app target framework to `net10.0-windows10.0.19041.0`.

```powershell
dotnet restore EldenDeathCounter.sln
dotnet build EldenDeathCounter.sln
```

Run tests:

```powershell
dotnet test EldenDeathCounter.sln
```

## Run

```powershell
dotnet run --project src/EldenDeathCounter/EldenDeathCounter.csproj
```

Start the app before launching Elden Ring. Use Elden Ring in borderless fullscreen or windowed mode. Exclusive fullscreen can prevent normal desktop overlays from appearing correctly.

## What It Does

- Shows a small WPF control window.
- Shows a transparent, always-on-top, click-through WPF overlay.
- Saves all death data locally.
- Captures the configured screen periodically and checks the center region for death text.
- Detects the configured phrases:
  - `YOU DIED`
  - `NIE ŻYJESZ`
- Uses a cooldown to avoid counting one death screen multiple times.
- Provides manual fallback buttons and global hotkeys:
  - `F7` marks the active boss as defeated.
  - `F8` adds one death.
  - `F9` subtracts one death.
- Lets you type a custom active boss name and shows that boss counter under the global overlay counter.
- Can auto-read the current boss name from the bottom boss health bar while detection is running.
- If two boss health bars are visible, the active boss name is combined as `Boss 1 + Boss 2`.

## Detection

Detection is screen-based only. The app does not read or modify Elden Ring memory, inject DLLs, hook the game process, bypass anti-cheat, or interact with Easy Anti-Cheat.

The app uses two local screen-based detection paths:

1. Windows local OCR through `Windows.Media.Ocr`. OCR text is normalized and matched with configurable fuzzy sensitivity against `detectionPhrases`.
2. A local shape/template detector built from the `PL_Death_Screen*` and `ENG_Death_Screen*` reference images. It compares the centered death-message text shape using image contrast and edges, not a count of red pixels. This is the first detector used, with Windows OCR as a fallback when the template does not match.

Both OCR and template matches must be observed on consecutive frames before a death is counted. Once a signal is pending, a strong template candidate slightly below the threshold can confirm it.

Windows OCR was selected because it is built into Windows, runs locally, avoids cloud accounts, and does not require a heavy external OCR runtime. English and Polish OCR engines are initialized when available; if they are not installed, the app falls back to the user's Windows OCR profile languages and logs the issue.

## Local Files

By default, files are stored here:

```text
%USERPROFILE%\Desktop\EldenDeathCounter\
```

Files:

- `deaths.json` stores `currentDeathCount` and `deathEvents`.
- `deaths.json` also stores `activeBoss` and `bossHistory` when you use boss tracking.
- `appsettings.json` stores overlay, detection, phrase, folder, and hotkey settings.
- `log.txt` stores app, detection, hotkey, OCR, and storage logs.
- `detection-screenshots\*.png` stores the captured frame whenever an automatic detection actually increments the counter, so false positives can be reviewed.

If `deaths.json` or `appsettings.json` is corrupt, the app creates a timestamped backup and starts with a clean file.

## Settings

Edit settings in the control window or directly in:

```text
%USERPROFILE%\Desktop\EldenDeathCounter\appsettings.json
```

Important settings:

- `overlayEnabled`
- `overlayX`
- `overlayY`
- `detectionEnabledOnStartup`
- `detectionIntervalMs`
- `detectionCooldownSeconds`
- `detectionSensitivity`
- `dataFolderPath`
- `detectionPhrases`
- `manualAddHotkey`
- `manualSubtractHotkey`
- `bossDefeatedHotkey`
- `autoDetectBossNames`

`dataFolderPath` changes are safest after restarting the app.

`detectionIntervalMs` controls how often the screen is sampled. `detectionCooldownSeconds` controls the minimum time between separate death events. `captureTarget` controls which monitor is sampled. The default `EldenRingWindow` target follows the visible Elden Ring window and falls back to the primary screen when the game window is not available. The app also keeps an internal latch: once a death screen is detected, it will not count again until the death signal disappears for several consecutive frames.

## Troubleshooting

- Overlay missing: use borderless fullscreen or windowed mode, not exclusive fullscreen.
- Missed death: press `F8` to add one manually.
- Accidental detection: press `F9` to subtract one manually.
- Boss defeated: press `F7` or use the `Boss Defeated` button. The active boss is moved into `bossHistory` and cleared from the overlay.
- Boss name not detected: keep `Auto-read boss name` enabled, make sure detection is running, and use the manual boss name field as a fallback if OCR reads the UI incorrectly.
- Double count on one death screen: keep `detectionCooldownSeconds` near the default `25`. The app also ignores repeated detections while the same death screen remains visible.
- OCR not working: check `log.txt` for OCR language messages and install Windows OCR language support for English and/or Polish.
- Hotkeys not working: another app may already own the key. Change `manualAddHotkey` or `manualSubtractHotkey` in `appsettings.json` or the control window.
- Detection too strict or too loose: adjust `detectionSensitivity`. Higher is stricter; lower is more permissive.

## Limitations

- Borderless fullscreen/windowed mode is required or strongly recommended.
- Exclusive fullscreen may hide the overlay.
- Screen capture targets the configured monitor center region, or the monitor containing the Elden Ring window when `captureTarget` is `EldenRingWindow`.
- OCR quality depends on resolution, scaling, language packs, and the death-screen presentation.
- The template fallback intentionally searches only the central death-message region and does not treat red scenery, lava, scarlet rot, or red HUD elements as a death signal by themselves.
- Boss auto-detection looks for long red boss HP bars in the bottom part of the configured monitor, then OCRs the name area above each bar.
- Final reliability should be tested in-game with both English and Polish game language settings.
=======
# DeathCounter
DeathCounter with boss history for From Software games
>>>>>>> 843a39b021b37a1110d51cf88217f533d04492c2
