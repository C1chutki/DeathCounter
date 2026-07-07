using System.Text.Json;
using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.Core.Detection;
using EldenDeathCounter.Core.Logging;

namespace EldenDeathCounter.Core.Storage;

public sealed class AppSettingsStore
{
    private readonly ILogService _log;

    public AppSettingsStore(ILogService log)
    {
        _log = log;
    }

    public async Task<AppSettings> LoadAsync(string settingsFilePath, string desktopPath)
    {
        return await LoadAsync(settingsFilePath, desktopPath, AppGameProfile.EldenRing);
    }

    public async Task<AppSettings> LoadAsync(string settingsFilePath, string desktopPath, AppGameProfile profile)
    {
        try
        {
            EnsureParentDirectory(settingsFilePath);
            if (!File.Exists(settingsFilePath))
            {
                var defaults = AppSettings.CreateDefault(desktopPath, profile);
                await SaveAsync(settingsFilePath, defaults);
                _log.Info($"Created settings file at {settingsFilePath}.");
                return defaults;
            }

            await using var stream = File.OpenRead(settingsFilePath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonFileOptions.Value);
            return Normalize(settings ?? AppSettings.CreateDefault(desktopPath, profile), desktopPath, profile);
        }
        catch (JsonException exception)
        {
            _log.Error($"Corrupt settings data found at {settingsFilePath}. Creating backup and clean file.", exception);
            BackupCorruptFile(settingsFilePath, "appsettings");
            var defaults = AppSettings.CreateDefault(desktopPath, profile);
            await SaveAsync(settingsFilePath, defaults);
            return defaults;
        }
        catch (Exception exception)
        {
            _log.Error($"Failed to load settings from {settingsFilePath}.", exception);
            throw;
        }
    }

    public async Task SaveAsync(string settingsFilePath, AppSettings settings)
    {
        try
        {
            await JsonFileWriter.WriteAtomicAsync(
                settingsFilePath,
                stream => JsonSerializer.SerializeAsync(stream, settings, JsonFileOptions.Value));
        }
        catch (Exception exception)
        {
            _log.Error($"Failed to save settings to {settingsFilePath}.", exception);
            throw;
        }
    }

    private static AppSettings Normalize(AppSettings settings, string desktopPath, AppGameProfile profile)
    {
        var defaults = AppSettings.CreateDefault(desktopPath, profile);

        if (string.IsNullOrWhiteSpace(settings.DataFolderPath))
        {
            settings.DataFolderPath = defaults.DataFolderPath;
        }

        settings.GameId = profile.Id;
        settings.CharacterProfileName = AppCharacterProfile.NormalizeName(settings.CharacterProfileName ?? string.Empty);
        settings.DetectionPhrases = AppSettings.CreateDefaultDetectionPhrases();
        settings.BossVictoryPhrases = AppSettings.CreateDefaultBossVictoryPhrases();

        if (settings.BossNameCorrections is null || settings.BossNameCorrections.Count == 0)
        {
            settings.BossNameCorrections = defaults.BossNameCorrections;
        }

        settings.GameLanguage = NormalizeGameLanguage(settings.GameLanguage, defaults.GameLanguage);
        settings.BossHealthBarStyle = BossHealthBarStyles.Normalize(settings.BossHealthBarStyle);
        settings.DetectionMode = DetectionModePresets.Get(settings.DetectionMode).Mode;

        if (settings.DetectionIntervalMs == 500 && settings.DetectionCooldownSeconds == 5)
        {
            settings.DetectionIntervalMs = defaults.DetectionIntervalMs;
            settings.DetectionCooldownSeconds = defaults.DetectionCooldownSeconds;
        }
        else
        {
            settings.DetectionIntervalMs = DetectionTimingOptions.NormalizeBaseIntervalMs(settings.DetectionIntervalMs);
        }

        if (settings.DetectionCooldownSeconds <= 0)
        {
            settings.DetectionCooldownSeconds = defaults.DetectionCooldownSeconds;
        }

        settings.DetectionSensitivity = Math.Clamp(settings.DetectionSensitivity, 0.1, 1.0);
        if (!Enum.IsDefined(settings.DiagnosticsMode))
        {
            settings.DiagnosticsMode = defaults.DiagnosticsMode;
        }

        if (settings.DiagnosticsSessionMinutes <= 0)
        {
            settings.DiagnosticsSessionMinutes = defaults.DiagnosticsSessionMinutes;
        }

        if (settings.DiagnosticsMaxEventLogMb <= 0)
        {
            settings.DiagnosticsMaxEventLogMb = defaults.DiagnosticsMaxEventLogMb;
        }

        if (settings.DiagnosticsRetentionDays <= 0)
        {
            settings.DiagnosticsRetentionDays = defaults.DiagnosticsRetentionDays;
        }

        settings.CaptureTarget = string.IsNullOrWhiteSpace(settings.CaptureTarget)
            ? defaults.CaptureTarget
            : settings.CaptureTarget;
        settings.ManualAddHotkey = string.IsNullOrWhiteSpace(settings.ManualAddHotkey)
            ? defaults.ManualAddHotkey
            : settings.ManualAddHotkey;
        settings.ManualSubtractHotkey = string.IsNullOrWhiteSpace(settings.ManualSubtractHotkey)
            ? defaults.ManualSubtractHotkey
            : settings.ManualSubtractHotkey;
        if (settings.ManualAddHotkey.Equals("F8", StringComparison.OrdinalIgnoreCase) &&
            settings.ManualSubtractHotkey.Equals("F9", StringComparison.OrdinalIgnoreCase))
        {
            settings.ManualAddHotkey = defaults.ManualAddHotkey;
            settings.ManualSubtractHotkey = defaults.ManualSubtractHotkey;
        }

        settings.BossDefeatedHotkey = string.IsNullOrWhiteSpace(settings.BossDefeatedHotkey)
            ? defaults.BossDefeatedHotkey
            : settings.BossDefeatedHotkey;
        settings.OverlayToggleHotkey = string.IsNullOrWhiteSpace(settings.OverlayToggleHotkey)
            ? defaults.OverlayToggleHotkey
            : settings.OverlayToggleHotkey;
        settings.DetectionToggleHotkey = string.IsNullOrWhiteSpace(settings.DetectionToggleHotkey)
            ? defaults.DetectionToggleHotkey
            : settings.DetectionToggleHotkey;
        settings.BossSkipHotkey = string.IsNullOrWhiteSpace(settings.BossSkipHotkey)
            ? defaults.BossSkipHotkey
            : settings.BossSkipHotkey;
        settings.OverlayFontScale = settings.OverlayFontScale <= 0
            ? defaults.OverlayFontScale
            : Math.Clamp(settings.OverlayFontScale, 0.6, 1.6);
        settings.OverlayBackgroundOpacity = Math.Clamp(settings.OverlayBackgroundOpacity, 0.0, 1.0);

        return settings;
    }

    private static string NormalizeGameLanguage(string? language, string fallback)
    {
        return language?.Trim().ToUpperInvariant() switch
        {
            "PL" => "PL",
            "ENG" => "ENG",
            "EN" => "ENG",
            _ => fallback
        };
    }

    private static void EnsureParentDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static void BackupCorruptFile(string filePath, string filePrefix)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(filePath) ?? ".";
        var backupPath = Path.Combine(directory, $"{filePrefix}.corrupt-{DateTime.Now:yyyyMMdd-HHmmssfff}.json");
        File.Move(filePath, backupPath, overwrite: false);
    }
}
