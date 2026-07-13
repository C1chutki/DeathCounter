# AGENTS.md

## Project Purpose

EldenDeathCounter is a Windows desktop app for tracking Elden Ring deaths locally. It shows a WPF control window plus an always-on-top click-through overlay, stores data on disk, supports manual hotkeys, and can detect death/boss-victory signals from screen captures.

## Tech Stack

- .NET 9 solution: `EldenDeathCounter.sln`
- WPF app: `src/EldenDeathCounter`
- Testable core library: `src/EldenDeathCounter.Core`
- Tests: xUnit in `tests/EldenDeathCounter.Tests`
- Image analysis: OpenCvSharp in the core project
- Windows-only app target: `net9.0-windows10.0.19041.0`, `win-x64`

## Common Commands

Run from the repository root:

```powershell
dotnet restore EldenDeathCounter.sln
dotnet build EldenDeathCounter.sln
dotnet test EldenDeathCounter.sln
dotnet run --project src/EldenDeathCounter/EldenDeathCounter.csproj
```

Use `dotnet test EldenDeathCounter.sln` as the default verification command after logic changes.

## File Map

- `src/EldenDeathCounter/App.xaml*` - WPF application startup and service wiring.
- `src/EldenDeathCounter/MainWindow.xaml*` - main control window UI.
- `src/EldenDeathCounter/OverlayWindow.xaml*` - transparent always-on-top overlay.
- `src/EldenDeathCounter/ViewModels/MainWindowViewModel.cs` - main UI state, commands, settings updates, detection control, boss history editing.
- `src/EldenDeathCounter/Detection/` - runtime screen capture, Windows OCR, and app-level detection orchestration.
- `src/EldenDeathCounter/Hotkeys/` - global hotkey registration/runtime events.
- `src/EldenDeathCounter/UI/RelayCommand.cs` - simple WPF command helper.
- `src/EldenDeathCounter/Interop/` - Windows interop for overlay/window behavior.
- `src/EldenDeathCounter.Core/Configuration/` - app settings, game profiles, language/theme/counter text configuration.
- `src/EldenDeathCounter.Core/Detection/` - pure or mostly pure detection algorithms, region calculators, phrase matching, stabilizers, boss-name correction, OpenCV analyzers.
- `src/EldenDeathCounter.Core/Storage/` - persisted death counter state, boss history, JSON stores, file options.
- `src/EldenDeathCounter.Core/Logging/` - app log and detection diagnostics log services.
- `src/EldenDeathCounter.Core/Hotkeys/HotkeyDefinition.cs` - hotkey parsing/formatting model.
- `tests/EldenDeathCounter.Tests/Core/` - unit tests for core behavior and selected storage/logging/detection pieces.
- `Assets/` - source images/icons/reference screenshots copied into the app output by the WPF project.
- `docs/superpowers/` - prior specs and implementation plans; useful for intent, not runtime code.
- `TODO.md` - user backlog and loose notes.

## Architecture Notes

- Keep domain logic in `EldenDeathCounter.Core` when it can be tested without WPF or Windows UI services.
- Keep WPF-specific state and commands in `MainWindowViewModel`.
- `DeathCounterService` owns mutations of death count, active boss state, boss history, and persistence through `DeathCounterStore`.
- `DeathDetectionService` coordinates capture, template detection, OCR fallback, boss-victory detection, boss-name detection, diagnostics, cooldowns, and stabilizers.
- Detection should stay screen-based only. Do not add game memory reads, process injection, hooks, anti-cheat bypasses, or game-process modification.
- Default local data folder is under the user's desktop, normally `%USERPROFILE%\Desktop\EldenDeathCounter\`.

## Editing Rules

- Prefer small, focused changes that follow the existing C# style: file-scoped namespaces, nullable enabled, async methods where I/O is involved.
- Wszystkie edytowane pliki tekstowe zapisuj w UTF-8.
- Przed commitem zmian zawierających polskie znaki odczytaj zmienione pliki jako UTF-8 i potwierdź, że znaki diakrytyczne nie zostały uszkodzone.
- Nie ufaj wyłącznie renderowaniu konsoli; artefakty typu `Ã³`, `Ä™`, `Å›` oznaczają korupcję treści.
- Add or update tests in `tests/EldenDeathCounter.Tests/Core/` for changes to core detection, storage, configuration, logging, hotkey parsing, or formatting behavior.
- Avoid putting new business logic directly in XAML code-behind if it can live in the view model or core library.
- Do not edit generated build output in `bin/` or `obj/`.
- Treat `Assets/` reference screenshots as behavior-sensitive inputs for template detection; update tests when changing them or the analyzers that consume them.
- Keep logs and diagnostics useful but bounded; avoid noisy per-frame logging unless controlled by diagnostics settings.
- Preserve local-only behavior and avoid adding cloud services or external runtime dependencies unless explicitly requested.

## Changelog Rules

- After every completed project change, update `CHANGELOG.md`.
- Add changes under the current date in `YYYY-MM-DD` format.
- If the current date already exists, append a short bullet under that date.
- Describe only changes that were actually made and verified from the current chat, files, or commands.
- Keep each bullet short, with a maximum of 3 sentences.
- Do not recreate past history from guesses.

## Git Commits

- Create clear, descriptive Git commits after completing a logical unit of work.
- Use the Git author identity already configured in the repository.
- Do not add `Co-authored-by`, AI attribution, Codex attribution, or similar trailers.
- Do not mention AI or Codex in commit messages unless explicitly requested.

## Versioning Rules

- Every change, large or small, MUST bump the application version in `src/EldenDeathCounter/EldenDeathCounter.csproj` (`<Version>X.Y.Z</Version>`).
- Use semantic versioning: patch (Z) for small fixes/tweaks, minor (Y) for new features, major (X) for breaking changes.
- The overlay shows the version dynamically from the assembly (`v{Major}.{Minor}.{Build}`), so keep the sidebar version label in `src/EldenDeathCounter/MainWindow.xaml` in sync with the csproj version.
- Note the new version in the matching `CHANGELOG.md` bullet.

## Verification Checklist

- For core or behavior changes: run `dotnet test EldenDeathCounter.sln`.
- For project or dependency changes: run `dotnet build EldenDeathCounter.sln`.
- For WPF/UI changes: build the solution and, when possible, run the app with `dotnet run --project src/EldenDeathCounter/EldenDeathCounter.csproj`.
- For detection changes: add targeted tests around calculators/analyzers/stabilizers and note any manual in-game verification that still remains.
- Do not mark a task complete before running the applicable verification.
- In the final report, state the exact command, its result, and the artifact path.
- If verification could not run, state `niezweryfikowane` and explain why; never guess.

## Cost-Saving Guidance For Future AI Sessions

- Start by reading this file, then inspect only the files relevant to the requested feature.
- Use the file map above before broad searches.
- For death counting, boss history, settings, and storage tasks, inspect `DeathCounterService`, `AppSettings`, and related tests first.
- For screen detection tasks, inspect `DeathDetectionService` plus the relevant core analyzer/stabilizer tests first.
- For UI layout or command tasks, inspect `MainWindow.xaml` and `MainWindowViewModel.cs` first.

## Task Routing

Before editing, classify the task as one of:
- Documentation
- UI/XAML
- Overlay
- Detection
- Boss history/storage
- Settings/configuration
- Hotkeys
- Build/test infrastructure

Only inspect the matching file group from File Map first.
Do not search the full repository unless the first targeted inspection proves insufficient.

For UI-only changes:
- Start with MainWindow.xaml, OverlayWindow.xaml, MainWindowViewModel.cs.
- Do not inspect Detection/ unless behavior changes are requested.

For detection-only changes:
- Start with DeathDetectionService and Core/Detection tests.
- Do not inspect MainWindow.xaml unless the task needs UI settings.

For documentation-only changes:
- Read README.md, CHANGELOG.md, AGENTS.md only.
- Do not inspect src/ or tests/.

## Context Budget Rules

- Prefer targeted file reads over broad repository scans.
- Before reading more than 5 source files, explain why.
- Before large refactors, propose a short plan and wait for approval unless the task is explicitly marked "implement directly".
- Keep diffs small and avoid unrelated formatting changes.
