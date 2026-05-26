# Game Profile Storage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Store Elden Ring and Dark Souls 3 data/logs in separate subfolders under one desktop root.

**Architecture:** Add a small core profile model for deterministic paths. Keep existing services alive while switching their backing data/log files, so detection and UI commands continue using the current game profile.

**Tech Stack:** .NET 9, WPF, xUnit.

---

### Task 1: Profile Paths

**Files:**
- Create: `src/EldenDeathCounter.Core/Configuration/AppGameProfile.cs`
- Modify: `src/EldenDeathCounter.Core/Configuration/AppSettings.cs`
- Test: `tests/EldenDeathCounter.Tests/Core/AppGameProfileTests.cs`

- [ ] Write failing tests for `DeathCounter\EldenRing` and `DeathCounter\DarkSouls3` profile paths.
- [ ] Run `dotnet test tests\EldenDeathCounter.Tests\EldenDeathCounter.Tests.csproj --filter AppGameProfileTests` and confirm it fails because `AppGameProfile` is missing.
- [ ] Implement `AppGameProfile` and profile-aware `AppSettings.CreateDefault`.
- [ ] Run the same filtered test and confirm it passes.

### Task 2: Switchable Data And Logs

**Files:**
- Create: `src/EldenDeathCounter.Core/Logging/SwitchableLogService.cs`
- Modify: `src/EldenDeathCounter.Core/Storage/DeathCounterService.cs`
- Test: `tests/EldenDeathCounter.Tests/Core/FileLogServiceTests.cs`
- Test: `tests/EldenDeathCounter.Tests/Core/DeathCounterServiceBossTests.cs`

- [ ] Write failing tests for switching log file paths and switching death data files.
- [ ] Run focused tests and confirm they fail because switch APIs are missing.
- [ ] Implement `SwitchableLogService.SwitchTo` and `DeathCounterService.SwitchDataFileAsync`.
- [ ] Run focused tests and confirm they pass.

### Task 3: UI Profile Switching

**Files:**
- Modify: `src/EldenDeathCounter/App.xaml.cs`
- Modify: `src/EldenDeathCounter/MainWindow.xaml.cs`
- Modify: `src/EldenDeathCounter/ViewModels/MainWindowViewModel.cs`

- [ ] Start the app with `AppGameProfile.EldenRing` paths.
- [ ] Add `MainWindowViewModel.SwitchGameProfileAsync(AppGameProfile profile)`.
- [ ] Update `ER` and `DS3` click handlers to call the switch method and apply the profile theme.
- [ ] Refresh counter, settings fields, overlay, hotkeys, and detection state after switching.

### Task 4: Verification

**Files:**
- All changed files.

- [ ] Run `dotnet test EldenDeathCounter.sln`.
- [ ] Run `dotnet build EldenDeathCounter.sln`.
- [ ] Inspect changed files for hard-coded old single-folder behavior.
