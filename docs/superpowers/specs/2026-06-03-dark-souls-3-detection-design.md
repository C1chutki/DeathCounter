# Dark Souls 3 detection support — design

**Date:** 2026-06-03
**Branch:** Beta-version
**Status:** Approved design, pending implementation plan

## Problem

The app already switches *themes*, *data folders*, and *settings files* per game (Elden Ring,
Dark Souls 1/2/3), but the **detection pipeline is game-agnostic and effectively Elden-Ring-only**:

- `DeathDetectionService` only ever receives `AppSettings`; it is never told which game is active.
- Boss-list resolution is hardcoded to `PL_BossList.txt` / `ENG_BossList.txt`
  (`DeathDetectionService.ResolveBossListFileName`). The `*_DS3_BossList.txt` (and DS1/DS2) assets
  exist but are never loaded — so DS3 boss **names** never match.
- Death/victory image templates are keyed only by language (PL/ENG), loaded from `Assets/`. There
  are no DS3 templates, and ER templates can be matched against DS3 frames.
- The boss-bar analyzer is a color+position heuristic tuned for Elden Ring. DS3's bar sits lower
  (~88% screen height) and is a thin single line that the `cluster.Bottom - cluster.Top >= 4`
  filter likely rejects.
- `ScreenCaptureService` only recognizes the `eldenring` process for the auto capture target;
  DS3 falls back to the primary screen.
- The overlay (`Topmost="True"`) is invisible over DS3 in fullscreen because it never re-asserts
  top-most when the game takes the top z-order slot.

Death **counting** for DS3 already works today: it is driven by OCR phrase matching against the
dual-language list `["YOU DIED", "NIE ŻYJESZ"]`, independent of the `GameLanguage` setting. The
referenced diagnostics frame (`...frame-3329-count-ocr-YOU-DIED`) confirms `reason="ocr:YOU DIED"`,
`score=1`. So this work is about **per-boss tracking, overlay visibility, and game-correct assets**,
not about fixing global death counting.

## Goals

1. Full per-boss tracking for DS3 (boss name + per-boss death/timer counts), like Elden Ring.
2. Overlay visible on top of DS3 running in fullscreen.
3. Game-correct boss lists and image templates, wired generically for **all four games**
   (fixes the same latent DS1/DS2 boss-name bug).
4. DS3 game-window recognized by the auto capture target.

Non-goals: true exclusive-fullscreen overlay (DS3 is not exclusive — GDI capture works);
changing the OCR death-counting path (already works).

## Decisions (confirmed with user)

- **Boss scope:** full per-boss tracking for DS3.
- **Wiring scope:** generic for all games, not DS3-only.
- **DS3 display mode:** DS3 offers only "Fullscreen" or "Window" (no borderless). Its "Fullscreen"
  runs under Windows Fullscreen Optimizations (flip-model) — proven by GDI captures succeeding —
  so a top-most overlay can composite over it.
- **DS3 image templates:** load only the templates matching the selected `GameLanguage`
  (PL templates when PL is selected). OCR already covers both languages for the actual count;
  the image template is a same-language fallback.
- **Topmost cadence:** re-assert on foreground-window change (`SetWinEventHook` /
  `EVENT_SYSTEM_FOREGROUND`) plus a low-frequency (~1s) safety timer.

## Architecture

### 1. Game identity threaded into detection

Add `string GameId` to `AppSettings` (default `"EldenRing"`). `AppSettingsStore.LoadAsync(path,
desktop, profile)` and `Normalize(settings, desktop, profile)` stamp `settings.GameId = profile.Id`
(the store already receives the profile and is the single normalization point). The id then flows
through the existing `DeathDetectionService.Start(settings)` / `RestartAsync(settings)` calls — no
new constructor or method signatures.

`GameId` is `[JsonIgnore]` (purely derived, never read from disk): the store always sets it from the
active profile, so a stale or hand-edited JSON value can never select the wrong game's assets.

### 2. Per-game boss list

`ResolveBossListFileName(language, gameId)` returns the game-specific file:

| GameId      | PL                  | ENG                  |
|-------------|---------------------|----------------------|
| EldenRing   | PL_BossList.txt     | ENG_BossList.txt     |
| DarkSouls1  | PL_DS1_BossList.txt | ENG_DS1_BossList.txt |
| DarkSouls2  | PL_DS2_BossList.txt | ENG_DS2_BossList.txt |
| DarkSouls3  | PL_DS3_BossList.txt | ENG_DS3_BossList.txt |

The matcher cache key (`_bossNameMatcherLanguage`) becomes game+language so switching games reloads
the matcher.

### 3. Per-game boss-bar detection

- `BossHealthBarCaptureRegionCalculator.Calculate(width, height, gameId)` — DS3 returns a vertical
  band tuned to the lower, thinner bar; other games keep the current 0.64–0.96 band.
- `BossHealthBarAnalyzer` accepts a small per-game parameter set: minimum cluster height (lowered
  for DS3's thin single-line bar) and the name-region vertical offset (DS3's boss name sits directly
  above the bar's left end, closer than Elden Ring's). Calibrated against
  `Assets/Dark souls 3/PL_BossBar.jpg` and captured DS3 diagnostics frames.

### 4. Per-game death/victory image templates

`TemplateDeathTextImageSignalDetector` and `TemplateBossVictoryTextImageSignalDetector` resolve
templates by **game + language**:

- DS3 death templates: `Assets/Dark souls 3/PL_YouDied.jpg`, `PL_YouDied_v2.jpg` (PL); ER death
  templates remain for Elden Ring.
- DS3 victory templates: `Assets/Dark souls 3/PL_Victory.jpg`, `PL_Victory_v2.jpg`.
- Per the language decision, DS3 loads only the selected-language template set.
- The `Assets/Dark souls 3/` folder is added to the csproj as copied `Content` so it reaches output.

### 5. DS3 game-window capture target

`ScreenCaptureService` generalizes `IsEldenRingProcess` into a game-aware process matcher driven by
the active `GameId`:

| GameId      | Process names (case-insensitive)          |
|-------------|-------------------------------------------|
| EldenRing   | eldenring, start_protected_game           |
| DarkSouls3  | darksoulsiii                              |
| DarkSouls2  | darksoulsii, darksouls2                    |
| DarkSouls1  | darksouls, darksoulsremastered            |

The auto target (`"EldenRingWindow"`) keeps its name for settings compatibility but selects the
screen of whichever active game's window is found.

### 6. Overlay over fullscreen

Add a top-most enforcer to the overlay:

- New interop helper re-asserts z-order via `SetWindowPos(hwnd, HWND_TOPMOST, 0,0,0,0,
  SWP_NOMOVE|SWP_NOSIZE|SWP_NOACTIVATE)`.
- Triggered by a `SetWinEventHook` on `EVENT_SYSTEM_FOREGROUND` (when any window — e.g. the game —
  becomes foreground) plus a ~1s `DispatcherTimer` safety net.
- Add `WS_EX_NOACTIVATE` to the existing click-through styles (`WS_EX_TRANSPARENT|TOOLWINDOW|LAYERED`)
  so the overlay never steals focus from the game.
- Hook is registered in `OnSourceInitialized` and unhooked on `Closed`.

This is the standard Discord/RTSS-style technique and benefits all four games.

## Testing

- **Unit tests** (`EldenDeathCounter.Tests`, following `AppGameThemeTests` / `AppProjectAssetTests`
  patterns):
  - `ResolveBossListFileName` returns the correct file per game+language.
  - DS3 boss-list and DS3 template assets exist and are wired (asset-presence test).
  - Boss-bar capture region and analyzer tuning detect the DS3 bar from `PL_BossBar.jpg` and from a
    captured DS3 diagnostics frame; reject non-bar frames.
  - `AppSettingsStore` stamps `GameId` from the profile and ignores any on-disk value.
  - Game-aware process matcher maps GameId → process names.
- **Manual verification** against running DS3 for the Win32 pieces: overlay stays visible/on-top in
  fullscreen, capture target selects the DS3 window, per-boss counting increments on a real death.

## Risks

- DS3 bar tuning is empirical; calibrate against real frames and keep Elden Ring detection unchanged
  (regression-guard with existing ER tests).
- Win32 overlay/foreground-hook and process matching are not unit-testable; rely on manual DS3 runs.
- Boss-list Polish files contain mojibake (`StraĹĽnicy...`) — encoding should be validated when
  loading (read as UTF-8); harmless if a line fails to match, but worth a note in the plan.
