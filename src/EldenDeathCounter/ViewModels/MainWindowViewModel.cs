using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.Core.Detection;
using EldenDeathCounter.Core.Hotkeys;
using EldenDeathCounter.Core.Logging;
using EldenDeathCounter.Core.Storage;
using EldenDeathCounter.Detection;
using EldenDeathCounter.Hotkeys;
using EldenDeathCounter.Localization;
using EldenDeathCounter.UI;

namespace EldenDeathCounter.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string _settingsPath;
    private readonly AppSettingsStore _settingsStore;
    private readonly DeathCounterService _counterService;
    private readonly DeathDetectionService _detectionService;
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly OverlayWindow _overlayWindow;
    private readonly SwitchableLogService _log;
    private readonly IDetectionEventLogService _detectionEventLog;
    private readonly string _desktopPath;
    private readonly Dispatcher _dispatcher;
    private readonly SemaphoreSlim _detectionStateLock = new(1, 1);
    private AppGameProfile _activeGameProfile = AppGameProfile.EldenRing;
    private Window? _window;
    private string _detectionStatus = LocalizationService.Instance.GetString("Vm_DetectionStopped");
    private string _hotkeyStatus = "Hotkeys not registered yet.";
    private string _bossSearchText = string.Empty;
    private BossHistorySortMode _selectedBossSortMode = BossHistorySortMode.Default;
    private BossHistorySortDirection _selectedBossSortDirection = BossHistorySortDirection.Descending;
    private BossHistoryEntry? _editingBossHistoryEntry;
    private bool _isBossHistoryEditorOpen;
    private string _bossEditNameText = string.Empty;
    private string _bossEditAttemptsText = string.Empty;
    private string _bossEditDurationText = string.Empty;
    private string _bossEditRecordedAtText = string.Empty;
    private string _bossEditCompletedByText = string.Empty;
    private bool _isDetectionRunning;
    private bool _isShuttingDown;
    private DateTimeOffset? _lastDetectedDeath;
    private string _selectedAppLanguageValue = "en";
    private string _selectedDetectionModeValue = DetectionModePresets.Balanced;
    private string _overlayFontScaleInput = string.Empty;
    private string _overlayBackgroundOpacityInput = string.Empty;
    private readonly DateTimeOffset _sessionStartedAt = DateTimeOffset.Now;
    private CounterStatsSummary _statsSummary = CounterStatsService.CreateSummary(new DeathCounterState(), DateTimeOffset.Now, DateTimeOffset.Now);
    private string _exportStatusText = string.Empty;

    public MainWindowViewModel(
        AppSettings settings,
        string settingsPath,
        AppSettingsStore settingsStore,
        DeathCounterService counterService,
        DeathDetectionService detectionService,
        GlobalHotkeyService hotkeyService,
        OverlayWindow overlayWindow,
        SwitchableLogService log,
        IDetectionEventLogService detectionEventLog,
        string desktopPath)
    {
        Settings = settings;
        _settingsPath = settingsPath;
        _settingsStore = settingsStore;
        _counterService = counterService;
        _detectionService = detectionService;
        _hotkeyService = hotkeyService;
        _overlayWindow = overlayWindow;
        _log = log;
        _detectionEventLog = detectionEventLog;
        _desktopPath = desktopPath;
        _dispatcher = Dispatcher.CurrentDispatcher;

        DetectionIntervalMsText = string.Empty;
        DetectionCooldownSecondsText = string.Empty;
        DetectionSensitivityText = string.Empty;
        foreach (var option in CreateCaptureTargetOptions(_activeGameProfile))
        {
            CaptureTargetOptions.Add(option);
        }

        foreach (var option in CreateGameLanguageOptions())
        {
            GameLanguageOptions.Add(option);
        }

        foreach (var option in CreateAppLanguageOptions())
        {
            AppLanguageOptions.Add(option);
        }

        foreach (var option in CreateDetectionModeOptions())
        {
            DetectionModeOptions.Add(option);
        }

        foreach (var option in CreateBossHealthBarStyleOptions())
        {
            BossHealthBarStyleOptions.Add(option);
        }

        foreach (var option in CreateBossSortModeOptions())
        {
            BossSortModeOptions.Add(option);
        }

        foreach (var option in CreateBossSortDirectionOptions())
        {
            BossSortDirectionOptions.Add(option);
        }

        SelectedCaptureTargetValue = string.Empty;
        SelectedGameLanguageValue = string.Empty;
        SelectedBossHealthBarStyleValue = string.Empty;
        SelectedDetectionModeValue = string.Empty;
        OverlayXText = string.Empty;
        OverlayYText = string.Empty;
        ManualAddHotkeyText = string.Empty;
        ManualSubtractHotkeyText = string.Empty;
        ManualBossDefeatedHotkeyText = string.Empty;
        OverlayToggleHotkeyText = string.Empty;
        DetectionToggleHotkeyText = string.Empty;
        BossSkipHotkeyText = string.Empty;
        CharacterProfileNameText = string.Empty;
        DataFolderPathText = string.Empty;
        ManualCounterText = string.Empty;
        BossNameText = string.Empty;
        RefreshSettingsTextFields();
        RefreshCounterTextFields();
        RefreshBosses();
        RefreshStats();

        StartDetectionCommand = AsyncCommand(StartDetectionAsync);
        StopDetectionCommand = AsyncCommand(StopDetectionAsync);
        ToggleDetectionCommand = AsyncCommand(ToggleDetectionAsync);
        ResetCounterCommand = AsyncCommand(ResetCounterAsync);
        AddDeathCommand = AsyncCommand(() => AddDeathAsync("manual-button", "Added from control window."));
        SubtractDeathCommand = AsyncCommand(() => SubtractDeathAsync("manual-button", "Subtracted from control window."));
        SetCounterCommand = AsyncCommand(SetCounterAsync);
        ToggleOverlayCommand = AsyncCommand(ToggleOverlayAsync);
        OpenDataFileCommand = new RelayCommand(_ => OpenPath(Path.Combine(Settings.DataFolderPath, "deaths.json")));
        OpenDataFolderCommand = new RelayCommand(_ => OpenPath(Settings.DataFolderPath));
        SaveSettingsCommand = AsyncCommand(SaveSettingsAsync);
        ResetDetectionSettingsCommand = AsyncCommand(ResetDetectionSettingsAsync);
        ResetProfileSettingsCommand = AsyncCommand(ResetProfileSettingsAsync);
        ApplyCharacterProfileCommand = AsyncCommand(ApplyCharacterProfileAsync);
        SetActiveBossCommand = AsyncCommand(SetActiveBossAsync);
        ClearActiveBossCommand = AsyncCommand(() => ClearActiveBossAsync("manual-button"));
        BossDefeatedCommand = AsyncCommand(() => MarkBossDefeatedAsync("manual-button"));
        SkipBossCommand = AsyncCommand(() => SkipBossAsync("manual-button"));
        ClearDetectionLogCommand = new RelayCommand(_ => DetectionLogEntries.Clear());
        StartDiagnosticsCommand = new RelayCommand(_ => StartDiagnosticsSession());
        ExportProfileCommand = AsyncCommand(ExportProfileAsync);
        OpenAddBossHistoryEditorCommand = new RelayCommand(_ => OpenAddBossHistoryEditor());
        OpenBossHistoryEditorCommand = new RelayCommand(OpenBossHistoryEditor);
        SaveBossHistoryEditorCommand = AsyncCommand(SaveBossHistoryEditorAsync);
        DeleteBossHistoryEditorCommand = AsyncCommand(DeleteBossHistoryEditorAsync);
        CancelBossHistoryEditorCommand = new RelayCommand(_ => CloseBossHistoryEditor());

        if (_log is ILogEntrySource logEntrySource)
        {
            logEntrySource.EntryWritten += (_, entry) => AddDetectionLogEntry(entry);
        }

        _log.Info("Detection log initialized. Awaiting events.");

        _counterService.StateChanged += (_, _) =>
        {
            _dispatcher.Invoke(() =>
            {
                RefreshCounter();
                RefreshStats();
                ConfigureDetectionDiagnostics();
            });
        };
        _detectionService.StatusChanged += (_, args) =>
        {
            _dispatcher.Invoke(() =>
            {
                DetectionStatus = args.Status;
                _lastDetectedDeath = args.LastDetectedDeath;
                OnPropertyChanged(nameof(IsDetectionRunning));
                OnPropertyChanged(nameof(LastDetectedDeathText));
                OnPropertyChanged(nameof(StatusSummary));
            });
        };
        _hotkeyService.HotkeyPressed += (_, args) => HandleHotkey(args.Name);
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AppSettings Settings { get; private set; }

    public string CounterText => DeathCounterText.FormatGlobalCount(_counterService.State.CurrentDeathCount, Settings.AppLanguage);

    public string ActiveBossText => _counterService.State.ActiveBoss is null
        ? "Active boss: none"
        : $"Active boss: {_counterService.State.ActiveBoss.Name} ({_counterService.State.ActiveBoss.DeathCount})";

    public string ActiveBossDeathCountText => _counterService.State.ActiveBoss is null
        ? "0"
        : _counterService.State.ActiveBoss.DeathCount.ToString(CultureInfo.InvariantCulture);

    public string BossesActiveName => _counterService.State.ActiveBoss?.Name ?? L("Bosses_NoActiveEncounter");

    public string BossesActiveAttemptsText => FormatAttempts(_counterService.State.ActiveBoss?.DeathCount ?? 0);

    public string BossesActiveStartedText => _counterService.State.ActiveBoss is null
        ? L("Bosses_StartPrompt")
        : string.Format(L("Bosses_StartedFormat"), _counterService.State.ActiveBoss.StartedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

    public string DefeatedBossesEmptyText => DefeatedBosses.Count == 0
        ? string.IsNullOrWhiteSpace(BossSearchText) || _counterService.State.BossHistory.Count == 0
            ? L("Bosses_EmptyNone")
            : L("Bosses_EmptyNoMatch")
        : string.Empty;

    public string StatusSummary => $"{CounterText} | Overlay: {(Settings.OverlayEnabled ? "on" : "off")} | Detection: {DetectionStatus}";

    public string StatusDeathCountText => _counterService.State.CurrentDeathCount.ToString(CultureInfo.InvariantCulture);

    public string StatusOverlayStateText => Settings.OverlayEnabled ? L("Status_OverlayActive") : L("Status_OverlayOff");

    public string StatusDetectionStateText => IsDetectionRunning ? L("Status_DetectionRunning") : L("Status_DetectionStopped");

    public string StatsTotalDeathsText => _statsSummary.TotalDeaths.ToString(CultureInfo.InvariantCulture);

    public string StatsDeathsTodayText => _statsSummary.DeathsToday.ToString(CultureInfo.InvariantCulture);

    public string StatsSessionDeathsText => _statsSummary.SessionDeaths.ToString(CultureInfo.InvariantCulture);

    public string StatsDeathsPerHourText => _statsSummary.DeathsPerHour.ToString("0.00", CultureInfo.InvariantCulture);

    public string StatsActiveBossText => string.IsNullOrWhiteSpace(_statsSummary.ActiveBossName)
        ? L("Common_None")
        : $"{_statsSummary.ActiveBossName} ({_statsSummary.ActiveBossDeaths})";

    public string StatsBestBossText => string.IsNullOrWhiteSpace(_statsSummary.BestBossName)
        ? L("Common_None")
        : FormatBossStat(_statsSummary.BestBossName, _statsSummary.BestBossDeaths, _statsSummary.BestBossDuration);

    public string StatsHardestBossText => string.IsNullOrWhiteSpace(_statsSummary.HardestBossName)
        ? L("Common_None")
        : FormatBossStat(_statsSummary.HardestBossName, _statsSummary.HardestBossDeaths, _statsSummary.HardestBossDuration);

    public string StatsLongestBossText => string.IsNullOrWhiteSpace(_statsSummary.LongestBossName)
        ? L("Common_None")
        : FormatBossStat(_statsSummary.LongestBossName, _statsSummary.LongestBossDeaths, _statsSummary.LongestBossDuration);

    public string ExportStatusText
    {
        get => _exportStatusText;
        private set => SetField(ref _exportStatusText, value);
    }

    public string DetectionStatus
    {
        get => _detectionStatus;
        private set => SetField(ref _detectionStatus, value);
    }

    public bool IsDetectionRunning
    {
        get => _isDetectionRunning;
        private set
        {
            if (SetField(ref _isDetectionRunning, value))
            {
                _overlayWindow.UpdateDetectionState(value);
                OnPropertyChanged(nameof(StatusDetectionStateText));
                OnPropertyChanged(nameof(StatusSummary));
            }
        }
    }

    public string LastDetectedDeathText => _lastDetectedDeath?.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.CurrentCulture) ?? L("Common_None");

    public string HotkeyStatus
    {
        get => _hotkeyStatus;
        private set => SetField(ref _hotkeyStatus, value);
    }

    public bool OverlayEnabled
    {
        get => Settings.OverlayEnabled;
        set
        {
            if (Settings.OverlayEnabled != value)
            {
                Settings.OverlayEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusOverlayStateText));
                OnPropertyChanged(nameof(StatusSummary));
            }
        }
    }

    public bool DetectionEnabledOnStartup
    {
        get => Settings.DetectionEnabledOnStartup;
        set
        {
            if (Settings.DetectionEnabledOnStartup != value)
            {
                Settings.DetectionEnabledOnStartup = value;
                OnPropertyChanged();
            }
        }
    }

    public bool AutoDetectBossNames
    {
        get => Settings.AutoDetectBossNames;
        set
        {
            if (Settings.AutoDetectBossNames != value)
            {
                Settings.AutoDetectBossNames = value;
                OnPropertyChanged();
            }
        }
    }

    public bool DetectDeaths
    {
        get => Settings.DetectDeaths;
        set
        {
            if (Settings.DetectDeaths != value)
            {
                Settings.DetectDeaths = value;
                OnPropertyChanged();
            }
        }
    }

    public bool DetectBossVictories
    {
        get => Settings.DetectBossVictories;
        set
        {
            if (Settings.DetectBossVictories != value)
            {
                Settings.DetectBossVictories = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ShowBossTimer
    {
        get => Settings.ShowBossTimer;
        set
        {
            if (Settings.ShowBossTimer != value)
            {
                Settings.ShowBossTimer = value;
                _overlayWindow.ApplyBossTimerVisibility(value);
                OnPropertyChanged();
            }
        }
    }

    public bool ShowDetectionStatus
    {
        get => Settings.ShowDetectionStatus;
        set
        {
            if (Settings.ShowDetectionStatus != value)
            {
                Settings.ShowDetectionStatus = value;
                _overlayWindow.ApplyDetectionStatusVisibility(value);
                OnPropertyChanged();
            }
        }
    }

    public const double OverlayFontScaleMin = 0.6;

    public const double OverlayFontScaleMax = 1.6;

    public const double OverlayBackgroundOpacityMin = 0.0;

    public const double OverlayBackgroundOpacityMax = 1.0;

    public string OverlayFontScaleInput
    {
        get => _overlayFontScaleInput;
        set => SetField(ref _overlayFontScaleInput, value);
    }

    public string OverlayBackgroundOpacityInput
    {
        get => _overlayBackgroundOpacityInput;
        set => SetField(ref _overlayBackgroundOpacityInput, value);
    }

    public string DetectionIntervalMsText { get; set; }

    public string DetectionCooldownSecondsText { get; set; }

    public string DetectionSensitivityText { get; set; }

    public string SelectedCaptureTargetValue { get; set; }

    public string SelectedGameLanguageValue { get; set; }

    public string SelectedBossHealthBarStyleValue { get; set; }

    public string SelectedDetectionModeValue
    {
        get => _selectedDetectionModeValue;
        set
        {
            var mode = DetectionModePresets.Get(value).Mode;
            if (SetField(ref _selectedDetectionModeValue, mode))
            {
                ApplyDetectionModePreset(DetectionModePresets.Get(mode));
            }
        }
    }

    public string SelectedAppLanguageValue
    {
        get => _selectedAppLanguageValue;
        set
        {
            var normalized = LocalizationService.NormalizeLanguage(value);
            if (SetField(ref _selectedAppLanguageValue, normalized))
            {
                _ = ApplyAppLanguageAsync(normalized);
            }
        }
    }

    public string OverlayXText { get; set; }

    public string OverlayYText { get; set; }

    public string ManualAddHotkeyText { get; set; }

    public string ManualSubtractHotkeyText { get; set; }

    public string ManualBossDefeatedHotkeyText { get; set; }

    public string OverlayToggleHotkeyText { get; set; }

    public string DetectionToggleHotkeyText { get; set; }

    public string BossSkipHotkeyText { get; set; }

    public string CharacterProfileNameText { get; set; }

    public string DataFolderPathText { get; set; }

    public string ManualCounterText { get; set; }

    public string BossNameText { get; set; }

    public bool IsBossHistoryEditorOpen
    {
        get => _isBossHistoryEditorOpen;
        private set => SetField(ref _isBossHistoryEditorOpen, value);
    }

    public string BossHistoryEditorTitle => _editingBossHistoryEntry is null
        ? L("Editor_AddTitle")
        : L("Editor_EditTitle");

    public bool CanDeleteBossHistoryEntry => _editingBossHistoryEntry is not null;

    public string BossEditNameText
    {
        get => _bossEditNameText;
        set => SetField(ref _bossEditNameText, value);
    }

    public string BossEditAttemptsText
    {
        get => _bossEditAttemptsText;
        set => SetField(ref _bossEditAttemptsText, value);
    }

    public string BossEditDurationText
    {
        get => _bossEditDurationText;
        set => SetField(ref _bossEditDurationText, value);
    }

    public string BossEditRecordedAtText
    {
        get => _bossEditRecordedAtText;
        set => SetField(ref _bossEditRecordedAtText, value);
    }

    public string BossEditCompletedByText
    {
        get => _bossEditCompletedByText;
        set => SetField(ref _bossEditCompletedByText, value);
    }

    public string BossSearchText
    {
        get => _bossSearchText;
        set
        {
            if (SetField(ref _bossSearchText, value))
            {
                RefreshBosses();
            }
        }
    }

    public BossHistorySortMode SelectedBossSortMode
    {
        get => _selectedBossSortMode;
        set
        {
            if (SetField(ref _selectedBossSortMode, value))
            {
                RefreshBosses();
            }
        }
    }

    public BossHistorySortDirection SelectedBossSortDirection
    {
        get => _selectedBossSortDirection;
        set
        {
            if (SetField(ref _selectedBossSortDirection, value))
            {
                RefreshBosses();
            }
        }
    }

    public string FooterText => "Use borderless fullscreen or windowed mode for Elden Ring. Exclusive fullscreen may hide the overlay. F6 toggles detection, F9 adds a death, F8 subtracts one, F7 marks the active boss defeated, and Ctrl+Shift+P skips the active boss by default.";

    public ICommand StartDetectionCommand { get; }

    public ICommand StopDetectionCommand { get; }

    public ICommand ToggleDetectionCommand { get; }

    public ICommand ResetCounterCommand { get; }

    public ICommand AddDeathCommand { get; }

    public ICommand SubtractDeathCommand { get; }

    public ICommand SetCounterCommand { get; }

    public ICommand ToggleOverlayCommand { get; }

    public ICommand OpenDataFileCommand { get; }

    public ICommand OpenDataFolderCommand { get; }

    public ICommand SaveSettingsCommand { get; }

    public ICommand ResetDetectionSettingsCommand { get; }

    public ICommand ResetProfileSettingsCommand { get; }

    public ICommand ApplyCharacterProfileCommand { get; }

    public ICommand SetActiveBossCommand { get; }

    public ICommand ClearActiveBossCommand { get; }

    public ICommand BossDefeatedCommand { get; }

    public ICommand SkipBossCommand { get; }

    public ICommand ClearDetectionLogCommand { get; }

    public ICommand StartDiagnosticsCommand { get; }

    public ICommand ExportProfileCommand { get; }

    public ICommand OpenAddBossHistoryEditorCommand { get; }

    public ICommand OpenBossHistoryEditorCommand { get; }

    public ICommand SaveBossHistoryEditorCommand { get; }

    public ICommand DeleteBossHistoryEditorCommand { get; }

    public ICommand CancelBossHistoryEditorCommand { get; }

    public ObservableCollection<LogEntry> DetectionLogEntries { get; } = [];

    public ObservableCollection<BossDisplayItem> DefeatedBosses { get; } = [];

    public ObservableCollection<StatsRecentEventDisplayItem> StatsRecentEvents { get; } = [];

    public ObservableCollection<CaptureTargetOption> CaptureTargetOptions { get; } = [];

    public ObservableCollection<GameLanguageOption> GameLanguageOptions { get; } = [];

    public ObservableCollection<AppLanguageOption> AppLanguageOptions { get; } = [];

    public ObservableCollection<DetectionModeOption> DetectionModeOptions { get; } = [];

    public ObservableCollection<BossHealthBarStyleOption> BossHealthBarStyleOptions { get; } = [];

    public ObservableCollection<BossSortModeOption> BossSortModeOptions { get; } = [];

    public ObservableCollection<BossSortDirectionOption> BossSortDirectionOptions { get; } = [];

    public void AttachWindow(Window window)
    {
        _window = window;
        RegisterHotkeys();
    }

    public void ApplyGameTheme(AppGameTheme theme)
    {
        _overlayWindow.ApplyTheme(theme);
    }

    public async Task<bool> SwitchGameProfileAsync(AppGameProfile profile)
    {
        var wasDetectionRunning = IsDetectionRunning;
        if (!await ApplySettingsFromTextAsync(restartDetection: false, updateStatus: false))
        {
            return false;
        }

        if (wasDetectionRunning)
        {
            await StopDetectionAsync();
        }

        _activeGameProfile = profile;
        _settingsPath = profile.GetSettingsFilePath(_desktopPath);
        _log.SwitchTo(profile.GetLogFilePath(_desktopPath));
        Settings = await _settingsStore.LoadAsync(_settingsPath, _desktopPath, profile);
        Settings.DataFolderPath = Environment.ExpandEnvironmentVariables(Settings.DataFolderPath);
        Directory.CreateDirectory(Settings.DataFolderPath);
        _log.SwitchTo(Path.Combine(Settings.DataFolderPath, "log.txt"));
        await _counterService.SwitchDataFileAsync(Path.Combine(Settings.DataFolderPath, "deaths.json"));
        ConfigureDetectionDiagnostics();

        RebuildLocalizedOptions();
        RefreshSettingsTextFields();
        RefreshCounter();
        RefreshStats();
        _overlayWindow.ApplyPosition(Settings.OverlayX, Settings.OverlayY);
        ApplyOverlayState();
        RegisterHotkeys();

        if (wasDetectionRunning)
        {
            await _counterService.ResumeActiveBossTimerAsync();
            await _detectionService.RestartAsync(Settings);
            IsDetectionRunning = true;
            ConfigureDetectionDiagnostics();
        }

        DetectionStatus = string.Format(L("Vm_SwitchedToFormat"), profile.Theme.Title);
        _log.Info($"Game profile switched to {profile.Id}.");
        OnPropertyChanged(nameof(StatusSummary));
        return true;
    }

    public Task StartDetectionAsync() => RunDetectionStateChangeAsync(StartDetectionCoreAsync);

    public Task StopDetectionAsync() => RunDetectionStateChangeAsync(StopDetectionCoreAsync);

    private async Task StartDetectionCoreAsync()
    {
        if (_isShuttingDown)
        {
            return;
        }

        if (!await ApplySettingsFromTextAsync())
        {
            return;
        }

        await _counterService.ResumeActiveBossTimerAsync();
        _detectionService.Start(Settings);
        IsDetectionRunning = true;
        ConfigureDetectionDiagnostics();
    }

    private async Task StopDetectionCoreAsync()
    {
        IsDetectionRunning = false;
        await _counterService.PauseActiveBossTimerAsync();
        await _detectionService.StopAsync();
        ConfigureDetectionDiagnostics();
    }

    public async Task ShutdownAsync()
    {
        if (_isShuttingDown)
        {
            return;
        }

        _isShuttingDown = true;
        try
        {
            await StopDetectionAsync();
        }
        catch (Exception exception)
        {
            _log.Error("Error while stopping detection and saving state during shutdown.", exception);
        }

        try
        {
            _hotkeyService.Dispose();
            _overlayWindow.Close();
        }
        catch (Exception exception)
        {
            _log.Error("Error while releasing UI resources during shutdown.", exception);
        }

        _log.Info("Application shutdown completed.");
    }

    private void StartDiagnosticsSession()
    {
        var until = _detectionService.StartDiagnosticsSession(Settings, TimeSpan.FromMinutes(Settings.DiagnosticsSessionMinutes));
        DetectionStatus = string.Format(L("Vm_DiagnosticsActiveFormat"), until.LocalDateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
    }

    private async Task ExportProfileAsync()
    {
        try
        {
            if (!await ApplySettingsFromTextAsync(restartDetection: false, updateStatus: false))
            {
                return;
            }

            var dataFilePath = Path.Combine(Settings.DataFolderPath, "deaths.json");
            var result = await CounterExportService.ExportAsync(_counterService.State, Settings.DataFolderPath, dataFilePath, _settingsPath);
            ExportStatusText = string.Format(L("Stats_ExportedFormat"), Path.GetDirectoryName(result.DeathEventsCsvPath));
            DetectionStatus = ExportStatusText;
            _log.Info($"Profile exported to '{Path.GetDirectoryName(result.DeathEventsCsvPath)}'.");
        }
        catch (Exception exception)
        {
            ExportStatusText = L("Stats_ExportFailed");
            DetectionStatus = ExportStatusText;
            _log.Error("Profile export failed.", exception);
        }
    }

    private void ConfigureDetectionDiagnostics()
    {
        _detectionEventLog.Configure(
            Settings,
            new DetectionDiagnosticsState(
                _counterService.State.CurrentDeathCount,
                _counterService.State.ActiveBoss?.Name,
                IsDetectionRunning));
    }

    private Task ToggleDetectionAsync()
    {
        return RunDetectionStateChangeAsync(IsDetectionRunning ? StopDetectionCoreAsync : StartDetectionCoreAsync);
    }

    private async Task RunDetectionStateChangeAsync(Func<Task> operation)
    {
        await _detectionStateLock.WaitAsync();
        try
        {
            await operation();
        }
        finally
        {
            _detectionStateLock.Release();
        }
    }

    private async Task ResetCounterAsync()
    {
        await _counterService.ResetAsync("manual-button", "Reset from control window.");
    }

    private async Task AddDeathAsync(string detectionMethod, string note)
    {
        await _counterService.AddDeathAsync(detectionMethod, note);
    }

    private async Task SubtractDeathAsync(string detectionMethod, string note)
    {
        await _counterService.SubtractDeathAsync(detectionMethod, note);
    }

    private async Task SetCounterAsync()
    {
        if (!int.TryParse(ManualCounterText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
        {
            DetectionStatus = L("Vm_ManualCounterInvalid");
            return;
        }

        await _counterService.SetCountAsync(count, "manual-button", "Set from control window.");
    }

    private async Task SetActiveBossAsync()
    {
        if (string.IsNullOrWhiteSpace(BossNameText))
        {
            DetectionStatus = L("Vm_BossNameEmpty");
            return;
        }

        if (_counterService.State.ActiveBoss is null)
        {
            await _counterService.SetActiveBossAsync(BossNameText, timerRunning: IsDetectionRunning);
            return;
        }

        await _counterService.RenameActiveBossAsync(BossNameText);
    }

    private async Task ClearActiveBossAsync(string detectionMethod)
    {
        await _counterService.ClearActiveBossAsync(detectionMethod);
    }

    private async Task SkipBossAsync(string detectionMethod)
    {
        await _counterService.SkipActiveBossAsync(detectionMethod);
    }

    private async Task MarkBossDefeatedAsync(string detectionMethod)
    {
        await _counterService.MarkActiveBossDefeatedAsync(detectionMethod);
    }

    private void OpenAddBossHistoryEditor()
    {
        _editingBossHistoryEntry = null;
        BossEditNameText = string.Empty;
        BossEditAttemptsText = "0";
        BossEditDurationText = "00:00:00";
        BossEditRecordedAtText = DateTimeOffset.Now.LocalDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        BossEditCompletedByText = "manual-entry";
        OnPropertyChanged(nameof(BossHistoryEditorTitle));
        OnPropertyChanged(nameof(CanDeleteBossHistoryEntry));
        IsBossHistoryEditorOpen = true;
    }

    private void OpenBossHistoryEditor(object? parameter)
    {
        if (parameter is not BossDisplayItem item)
        {
            return;
        }

        _editingBossHistoryEntry = item.Entry;
        BossEditNameText = item.Entry.Name;
        BossEditAttemptsText = item.Entry.DeathCount.ToString(CultureInfo.InvariantCulture);
        BossEditDurationText = FormatDuration(GetBossKillDuration(item.Entry));
        BossEditRecordedAtText = item.Entry.DefeatedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        BossEditCompletedByText = item.Entry.CompletedBy;
        OnPropertyChanged(nameof(BossHistoryEditorTitle));
        OnPropertyChanged(nameof(CanDeleteBossHistoryEntry));
        IsBossHistoryEditorOpen = true;
    }

    private async Task SaveBossHistoryEditorAsync()
    {
        if (string.IsNullOrWhiteSpace(BossEditNameText))
        {
            DetectionStatus = L("Vm_BossNameEmpty");
            return;
        }

        if (!int.TryParse(BossEditAttemptsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var deathCount) || deathCount < 0)
        {
            DetectionStatus = L("Vm_BossAttemptsInvalid");
            return;
        }

        if (!TimeSpan.TryParseExact(BossEditDurationText, "c", CultureInfo.InvariantCulture, out var duration) &&
            !TimeSpan.TryParse(BossEditDurationText, CultureInfo.InvariantCulture, out duration))
        {
            DetectionStatus = L("Vm_FightDurationFormat");
            return;
        }

        if (duration < TimeSpan.Zero)
        {
            DetectionStatus = L("Vm_FightDurationNegative");
            return;
        }

        if (!DateTime.TryParseExact(
                BossEditRecordedAtText,
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var defeatedLocalTime))
        {
            DetectionStatus = L("Vm_RecordedAtFormat");
            return;
        }

        var defeatedAt = new DateTimeOffset(defeatedLocalTime, TimeZoneInfo.Local.GetUtcOffset(defeatedLocalTime));
        var startedAt = defeatedAt - duration;
        if (_editingBossHistoryEntry is null)
        {
            await _counterService.AddBossHistoryEntryAsync(
                BossEditNameText,
                deathCount,
                startedAt,
                defeatedAt,
                BossEditCompletedByText);
        }
        else
        {
            await _counterService.UpdateBossHistoryEntryAsync(
                _editingBossHistoryEntry,
                BossEditNameText,
                deathCount,
                startedAt,
                defeatedAt,
                BossEditCompletedByText);
        }

        CloseBossHistoryEditor();
    }

    private async Task DeleteBossHistoryEditorAsync()
    {
        if (_editingBossHistoryEntry is null)
        {
            return;
        }

        await _counterService.DeleteBossHistoryEntryAsync(_editingBossHistoryEntry);
        CloseBossHistoryEditor();
    }

    private void CloseBossHistoryEditor()
    {
        _editingBossHistoryEntry = null;
        OnPropertyChanged(nameof(BossHistoryEditorTitle));
        OnPropertyChanged(nameof(CanDeleteBossHistoryEntry));
        IsBossHistoryEditorOpen = false;
    }

    private async Task ToggleOverlayAsync()
    {
        OverlayEnabled = !OverlayEnabled;
        ApplyOverlayState();

        await _settingsStore.SaveAsync(_settingsPath, Settings);
    }

    private async Task SaveSettingsAsync()
    {
        await ApplySettingsFromTextAsync();
    }

    private async Task ResetDetectionSettingsAsync()
    {
        ApplyDetectionModePreset(DetectionModePresets.Get(DetectionModePresets.Balanced));
        SelectedDetectionModeValue = DetectionModePresets.Balanced;
        SelectedBossHealthBarStyleValue = BossHealthBarStyles.Vanilla;
        OnPropertyChanged(nameof(SelectedBossHealthBarStyleValue));
        await ApplySettingsFromTextAsync();
    }

    private async Task ResetProfileSettingsAsync()
    {
        var currentDataFolderPath = Settings.DataFolderPath;
        var currentCharacterProfileName = Settings.CharacterProfileName;
        var currentAppLanguage = Settings.AppLanguage;
        var defaults = AppSettings.CreateDefault(_desktopPath, _activeGameProfile);
        defaults.DataFolderPath = currentDataFolderPath;
        defaults.CharacterProfileName = currentCharacterProfileName;
        defaults.AppLanguage = currentAppLanguage;
        Settings = defaults;
        RefreshSettingsTextFields();
        _overlayWindow.ApplyScale(Settings.OverlayFontScale);
        _overlayWindow.ApplyBackgroundOpacity(Settings.OverlayBackgroundOpacity);
        _overlayWindow.ApplyBossTimerVisibility(Settings.ShowBossTimer);
        _overlayWindow.ApplyDetectionStatusVisibility(Settings.ShowDetectionStatus);
        await ApplySettingsFromTextAsync();
    }

    private async Task ApplyCharacterProfileAsync()
    {
        var characterName = AppCharacterProfile.NormalizeName(CharacterProfileNameText);
        CharacterProfileNameText = characterName;
        DataFolderPathText = AppCharacterProfile.GetDataFolderPath(_desktopPath, _activeGameProfile, characterName);
        OnPropertyChanged(nameof(CharacterProfileNameText));
        OnPropertyChanged(nameof(DataFolderPathText));

        if (!await ApplySettingsFromTextAsync())
        {
            return;
        }

        DetectionStatus = string.IsNullOrWhiteSpace(characterName)
            ? L("Vm_UsingDefaultFolder")
            : string.Format(L("Vm_UsingCharacterProfileFormat"), characterName);
        _log.Info($"Character profile switched to '{(string.IsNullOrWhiteSpace(characterName) ? "default" : characterName)}'.");
        OnPropertyChanged(nameof(StatusSummary));
    }

    private async Task<bool> ApplySettingsFromTextAsync(bool restartDetection = true, bool updateStatus = true)
    {
        var previousDataFolderPath = Settings.DataFolderPath;
        if (!TryReadSettings(out var error))
        {
            DetectionStatus = error;
            return false;
        }

        Directory.CreateDirectory(Settings.DataFolderPath);
        if (!string.Equals(
            Path.GetFullPath(previousDataFolderPath),
            Path.GetFullPath(Settings.DataFolderPath),
            StringComparison.OrdinalIgnoreCase))
        {
            _log.SwitchTo(Path.Combine(Settings.DataFolderPath, "log.txt"));
            await _counterService.SwitchDataFileAsync(Path.Combine(Settings.DataFolderPath, "deaths.json"));
            RefreshCounter();
            RefreshCounterTextFields();
            RefreshBosses();
            RefreshStats();
        }

        await _settingsStore.SaveAsync(_settingsPath, Settings);
        ConfigureDetectionDiagnostics();
        _overlayWindow.ApplyPosition(Settings.OverlayX, Settings.OverlayY);
        _overlayWindow.ApplyScale(Settings.OverlayFontScale);
        _overlayWindow.ApplyBackgroundOpacity(Settings.OverlayBackgroundOpacity);
        _overlayWindow.ApplyBossTimerVisibility(Settings.ShowBossTimer);
        _overlayWindow.ApplyDetectionStatusVisibility(Settings.ShowDetectionStatus);
        _overlayWindow.UpdateCount(_counterService.State.CurrentDeathCount, _counterService.State.ActiveBoss, Settings.AppLanguage);
        ApplyOverlayState();
        RegisterHotkeys();
        if (restartDetection && IsDetectionRunning)
        {
            await _detectionService.RestartAsync(Settings);
            IsDetectionRunning = true;
        }

        if (updateStatus)
        {
            DetectionStatus = L("Vm_SettingsSaved");
        }

        _log.Info("Settings saved.");
        OnPropertyChanged(nameof(StatusSummary));
        return true;
    }

    private void AddDetectionLogEntry(LogEntry entry)
    {
        if (!ShouldShowInDetectionLog(entry))
        {
            return;
        }

        _dispatcher.Invoke(() =>
        {
            DetectionLogEntries.Add(entry);
            while (DetectionLogEntries.Count > 100)
            {
                DetectionLogEntries.RemoveAt(0);
            }
        });
    }

    private static bool ShouldShowInDetectionLog(LogEntry entry)
    {
        if (entry.Level.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return entry.Message.StartsWith("Detection ", StringComparison.OrdinalIgnoreCase) &&
               !entry.Message.StartsWith("Detection frame #", StringComparison.OrdinalIgnoreCase) ||
               entry.Message.StartsWith("Death ", StringComparison.OrdinalIgnoreCase) ||
               entry.Message.StartsWith("Detected death", StringComparison.OrdinalIgnoreCase) ||
               entry.Message.StartsWith("Detected boss victory", StringComparison.OrdinalIgnoreCase) ||
               entry.Message.StartsWith("Boss victory", StringComparison.OrdinalIgnoreCase) ||
               entry.Message.StartsWith("Ignored repeated boss victory", StringComparison.OrdinalIgnoreCase) ||
               entry.Message.StartsWith("Weak image boss-victory", StringComparison.OrdinalIgnoreCase) ||
               entry.Message.StartsWith("Ignored repeated death", StringComparison.OrdinalIgnoreCase) ||
               entry.Message.StartsWith("Weak image death-text", StringComparison.OrdinalIgnoreCase) ||
               entry.Message.StartsWith("Settings saved", StringComparison.OrdinalIgnoreCase) ||
               entry.Message.StartsWith("Windows OCR", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryReadSettings(out string error)
    {
        error = string.Empty;

        if (!int.TryParse(DetectionIntervalMsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var interval) ||
            interval < DetectionTimingOptions.MinimumBaseIntervalMs)
        {
            error = L("Vm_DetectionIntervalInvalid");
            return false;
        }

        if (!int.TryParse(DetectionCooldownSecondsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cooldown) || cooldown < 1)
        {
            error = L("Vm_CooldownInvalid");
            return false;
        }

        if (!double.TryParse(DetectionSensitivityText, NumberStyles.Float, CultureInfo.InvariantCulture, out var sensitivity))
        {
            error = L("Vm_SensitivityInvalid");
            return false;
        }

        if (!double.TryParse(OverlayXText, NumberStyles.Float, CultureInfo.InvariantCulture, out var overlayX) ||
            !double.TryParse(OverlayYText, NumberStyles.Float, CultureInfo.InvariantCulture, out var overlayY))
        {
            error = L("Vm_OverlayXYInvalid");
            return false;
        }

        if (!double.TryParse(OverlayFontScaleInput, NumberStyles.Float, CultureInfo.InvariantCulture, out var overlayScale))
        {
            error = L("Vm_OverlayScaleInvalid");
            return false;
        }

        if (!double.TryParse(OverlayBackgroundOpacityInput, NumberStyles.Float, CultureInfo.InvariantCulture, out var overlayOpacity))
        {
            error = L("Vm_OverlayOpacityInvalid");
            return false;
        }

        var addHotkey = HotkeyDefinition.Parse(ManualAddHotkeyText);
        if (!addHotkey.IsValid)
        {
            error = string.Format(L("Vm_AddHotkeyInvalidFormat"), addHotkey.Error);
            return false;
        }

        var subtractHotkey = HotkeyDefinition.Parse(ManualSubtractHotkeyText);
        if (!subtractHotkey.IsValid)
        {
            error = string.Format(L("Vm_SubtractHotkeyInvalidFormat"), subtractHotkey.Error);
            return false;
        }

        var bossDefeatedHotkey = HotkeyDefinition.Parse(ManualBossDefeatedHotkeyText);
        if (!bossDefeatedHotkey.IsValid)
        {
            error = string.Format(L("Vm_BossDefeatedHotkeyInvalidFormat"), bossDefeatedHotkey.Error);
            return false;
        }

        var overlayToggleHotkey = HotkeyDefinition.Parse(OverlayToggleHotkeyText);
        if (!overlayToggleHotkey.IsValid)
        {
            error = string.Format(L("Vm_OverlayToggleHotkeyInvalidFormat"), overlayToggleHotkey.Error);
            return false;
        }

        var detectionToggleHotkey = HotkeyDefinition.Parse(DetectionToggleHotkeyText);
        if (!detectionToggleHotkey.IsValid)
        {
            error = string.Format(L("Vm_DetectionToggleHotkeyInvalidFormat"), detectionToggleHotkey.Error);
            return false;
        }

        var bossSkipHotkey = HotkeyDefinition.Parse(BossSkipHotkeyText);
        if (!bossSkipHotkey.IsValid)
        {
            error = string.Format(L("Vm_BossSkipHotkeyInvalidFormat"), bossSkipHotkey.Error);
            return false;
        }

        var dataFolderPath = Environment.ExpandEnvironmentVariables(DataFolderPathText.Trim());
        if (string.IsNullOrWhiteSpace(dataFolderPath))
        {
            error = L("Vm_DataFolderEmpty");
            return false;
        }

        Settings.DetectionIntervalMs = interval;
        Settings.DetectionCooldownSeconds = cooldown;
        Settings.DetectionSensitivity = Math.Clamp(sensitivity, 0.1, 1.0);
        Settings.DetectionMode = DetectionModePresets.Get(SelectedDetectionModeValue).Mode;
        Settings.CaptureTarget = string.IsNullOrWhiteSpace(SelectedCaptureTargetValue)
            ? "PrimaryScreen"
            : SelectedCaptureTargetValue.Trim();
        Settings.GameLanguage = NormalizeGameLanguage(SelectedGameLanguageValue, Settings.GameLanguage);
        Settings.BossHealthBarStyle = BossHealthBarStyles.Normalize(SelectedBossHealthBarStyleValue);
        Settings.OverlayX = overlayX;
        Settings.OverlayY = overlayY;
        Settings.OverlayFontScale = Math.Clamp(overlayScale, OverlayFontScaleMin, OverlayFontScaleMax);
        Settings.OverlayBackgroundOpacity = Math.Clamp(overlayOpacity, OverlayBackgroundOpacityMin, OverlayBackgroundOpacityMax);
        _overlayFontScaleInput = Settings.OverlayFontScale.ToString("0.0", CultureInfo.InvariantCulture);
        _overlayBackgroundOpacityInput = Settings.OverlayBackgroundOpacity.ToString("0.0", CultureInfo.InvariantCulture);
        OnPropertyChanged(nameof(OverlayFontScaleInput));
        OnPropertyChanged(nameof(OverlayBackgroundOpacityInput));
        Settings.DetectionPhrases = AppSettings.CreateDefaultDetectionPhrases();
        Settings.BossVictoryPhrases = AppSettings.CreateDefaultBossVictoryPhrases();
        Settings.ManualAddHotkey = ManualAddHotkeyText.Trim();
        Settings.ManualSubtractHotkey = ManualSubtractHotkeyText.Trim();
        Settings.BossDefeatedHotkey = ManualBossDefeatedHotkeyText.Trim();
        Settings.OverlayToggleHotkey = OverlayToggleHotkeyText.Trim();
        Settings.DetectionToggleHotkey = DetectionToggleHotkeyText.Trim();
        Settings.BossSkipHotkey = BossSkipHotkeyText.Trim();
        Settings.CharacterProfileName = AppCharacterProfile.NormalizeName(CharacterProfileNameText);
        Settings.DataFolderPath = dataFolderPath;
        return true;
    }

    private void ApplyOverlayState()
    {
        if (Settings.OverlayEnabled)
        {
            _overlayWindow.Show();
        }
        else
        {
            _overlayWindow.Hide();
        }
    }

    private void RegisterHotkeys()
    {
        if (_window is null)
        {
            return;
        }

        _hotkeyService.Clear();
        var add = _hotkeyService.Register(_window, Settings.ManualAddHotkey, "manual-add");
        var subtract = _hotkeyService.Register(_window, Settings.ManualSubtractHotkey, "manual-subtract");
        var bossDefeated = _hotkeyService.Register(_window, Settings.BossDefeatedHotkey, "boss-defeated");
        var overlayToggle = _hotkeyService.Register(_window, Settings.OverlayToggleHotkey, "overlay-toggle");
        var detectionToggle = _hotkeyService.Register(_window, Settings.DetectionToggleHotkey, "detection-toggle");
        var bossSkip = _hotkeyService.Register(_window, Settings.BossSkipHotkey, "boss-skip");
        HotkeyStatus = $"Hotkeys: {add}; {subtract}; {bossDefeated}; {overlayToggle}; {detectionToggle}; {bossSkip}";
    }

    private void HandleHotkey(string name)
    {
        _ = _dispatcher.InvokeAsync(() => ExecuteHotkeyAsync(name));
    }

    private async void ExecuteHotkeyAsync(string name)
    {
        try
        {
            if (name == "manual-add")
            {
                await AddDeathAsync("manual-hotkey", "Added with global hotkey.");
            }
            else if (name == "manual-subtract")
            {
                await SubtractDeathAsync("manual-hotkey", "Subtracted with global hotkey.");
            }
            else if (name == "boss-defeated")
            {
                await MarkBossDefeatedAsync("manual-hotkey");
            }
            else if (name == "overlay-toggle")
            {
                await ToggleOverlayAsync();
            }
            else if (name == "detection-toggle")
            {
                await ToggleDetectionAsync();
            }
            else if (name == "boss-skip")
            {
                await SkipBossAsync("manual-hotkey");
            }
        }
        catch (Exception exception)
        {
            HandleCommandException(exception);
        }
    }

    private RelayCommand AsyncCommand(Func<Task> execute) => new(execute, HandleCommandException);

    private void HandleCommandException(Exception exception)
    {
        _log.Error("Command execution failed.", exception);
        DetectionStatus = "Operation failed. Check the detection log.";
    }

    private void RefreshCounter()
    {
        _dispatcher.Invoke(() =>
        {
            _overlayWindow.UpdateCount(_counterService.State.CurrentDeathCount, _counterService.State.ActiveBoss, Settings.AppLanguage);
            RefreshCounterTextFields();
            OnPropertyChanged(nameof(CounterText));
            OnPropertyChanged(nameof(StatusDeathCountText));
            OnPropertyChanged(nameof(ActiveBossText));
            OnPropertyChanged(nameof(ActiveBossDeathCountText));
            OnPropertyChanged(nameof(ManualCounterText));
            OnPropertyChanged(nameof(BossNameText));
            OnPropertyChanged(nameof(StatusSummary));
            RefreshBosses();
        });
    }

    private void RefreshSettingsTextFields()
    {
        DetectionIntervalMsText = Settings.DetectionIntervalMs.ToString(CultureInfo.InvariantCulture);
        DetectionCooldownSecondsText = Settings.DetectionCooldownSeconds.ToString(CultureInfo.InvariantCulture);
        DetectionSensitivityText = Settings.DetectionSensitivity.ToString("0.00", CultureInfo.InvariantCulture);
        _selectedDetectionModeValue = DetectionModePresets.Get(Settings.DetectionMode).Mode;
        SelectedCaptureTargetValue = CaptureTargetOptions.Any(option => option.Value.Equals(Settings.CaptureTarget, StringComparison.OrdinalIgnoreCase))
            ? Settings.CaptureTarget
            : "PrimaryScreen";
        SelectedGameLanguageValue = NormalizeGameLanguage(Settings.GameLanguage, "PL");
        SelectedBossHealthBarStyleValue = BossHealthBarStyles.Normalize(Settings.BossHealthBarStyle);
        // Set the backing field directly so re-reading settings (e.g. profile switch) does not
        // re-trigger a language swap and settings save through the property setter.
        _selectedAppLanguageValue = LocalizationService.NormalizeLanguage(Settings.AppLanguage);
        OverlayXText = Settings.OverlayX.ToString(CultureInfo.InvariantCulture);
        OverlayYText = Settings.OverlayY.ToString(CultureInfo.InvariantCulture);
        _overlayFontScaleInput = Settings.OverlayFontScale.ToString("0.0", CultureInfo.InvariantCulture);
        _overlayBackgroundOpacityInput = Settings.OverlayBackgroundOpacity.ToString("0.0", CultureInfo.InvariantCulture);
        ManualAddHotkeyText = Settings.ManualAddHotkey;
        ManualSubtractHotkeyText = Settings.ManualSubtractHotkey;
        ManualBossDefeatedHotkeyText = Settings.BossDefeatedHotkey;
        OverlayToggleHotkeyText = Settings.OverlayToggleHotkey;
        DetectionToggleHotkeyText = Settings.DetectionToggleHotkey;
        BossSkipHotkeyText = Settings.BossSkipHotkey;
        CharacterProfileNameText = Settings.CharacterProfileName;
        DataFolderPathText = Settings.DataFolderPath;

        OnPropertyChanged(nameof(DetectionIntervalMsText));
        OnPropertyChanged(nameof(DetectionCooldownSecondsText));
        OnPropertyChanged(nameof(DetectionSensitivityText));
        OnPropertyChanged(nameof(SelectedDetectionModeValue));
        OnPropertyChanged(nameof(SelectedCaptureTargetValue));
        OnPropertyChanged(nameof(SelectedGameLanguageValue));
        OnPropertyChanged(nameof(SelectedBossHealthBarStyleValue));
        OnPropertyChanged(nameof(SelectedAppLanguageValue));
        OnPropertyChanged(nameof(OverlayXText));
        OnPropertyChanged(nameof(OverlayYText));
        OnPropertyChanged(nameof(ManualAddHotkeyText));
        OnPropertyChanged(nameof(ManualSubtractHotkeyText));
        OnPropertyChanged(nameof(ManualBossDefeatedHotkeyText));
        OnPropertyChanged(nameof(OverlayToggleHotkeyText));
        OnPropertyChanged(nameof(DetectionToggleHotkeyText));
        OnPropertyChanged(nameof(BossSkipHotkeyText));
        OnPropertyChanged(nameof(CharacterProfileNameText));
        OnPropertyChanged(nameof(DataFolderPathText));
        OnPropertyChanged(nameof(OverlayEnabled));
        OnPropertyChanged(nameof(DetectionEnabledOnStartup));
        OnPropertyChanged(nameof(AutoDetectBossNames));
        OnPropertyChanged(nameof(DetectDeaths));
        OnPropertyChanged(nameof(DetectBossVictories));
        OnPropertyChanged(nameof(ShowBossTimer));
        OnPropertyChanged(nameof(ShowDetectionStatus));
        OnPropertyChanged(nameof(OverlayFontScaleInput));
        OnPropertyChanged(nameof(OverlayBackgroundOpacityInput));
        OnPropertyChanged(nameof(StatusOverlayStateText));
    }

    private void RefreshCounterTextFields()
    {
        ManualCounterText = _counterService.State.CurrentDeathCount.ToString(CultureInfo.InvariantCulture);
        BossNameText = _counterService.State.ActiveBoss?.Name ?? string.Empty;
        OnPropertyChanged(nameof(ManualCounterText));
        OnPropertyChanged(nameof(BossNameText));
    }

    private void RefreshBosses()
    {
        DefeatedBosses.Clear();
        foreach (var numberedBoss in BossHistoryDisplayOrder.CreateNumberedEntries(
                     _counterService.State.BossHistory,
                     BossSearchText,
                     SelectedBossSortMode,
                     SelectedBossSortDirection))
        {
            var boss = numberedBoss.Entry;
            DefeatedBosses.Add(new BossDisplayItem(
                $"#{numberedBoss.KillNumber}",
                boss.Name,
                FormatDefeatedAttempts(boss.DeathCount),
                FormatDuration(GetBossKillDuration(boss)),
                string.Format(L("Bosses_RecordedFormat"), boss.DefeatedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),
                boss.CompletedBy,
                boss));
        }

        OnPropertyChanged(nameof(BossesActiveName));
        OnPropertyChanged(nameof(BossesActiveAttemptsText));
        OnPropertyChanged(nameof(BossesActiveStartedText));
        OnPropertyChanged(nameof(DefeatedBossesEmptyText));
    }

    private void RefreshStats()
    {
        _statsSummary = CounterStatsService.CreateSummary(_counterService.State, _sessionStartedAt, DateTimeOffset.Now);
        StatsRecentEvents.Clear();
        foreach (var item in _statsSummary.RecentEvents)
        {
            StatsRecentEvents.Add(new StatsRecentEventDisplayItem(
                item.Timestamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                item.DetectionMethod,
                item.Note,
                item.CountAfter.ToString(CultureInfo.InvariantCulture)));
        }

        OnPropertyChanged(nameof(StatsTotalDeathsText));
        OnPropertyChanged(nameof(StatsDeathsTodayText));
        OnPropertyChanged(nameof(StatsSessionDeathsText));
        OnPropertyChanged(nameof(StatsDeathsPerHourText));
        OnPropertyChanged(nameof(StatsActiveBossText));
        OnPropertyChanged(nameof(StatsBestBossText));
        OnPropertyChanged(nameof(StatsHardestBossText));
        OnPropertyChanged(nameof(StatsLongestBossText));
    }

    private static string FormatAttempts(int deathCount)
    {
        return string.Format(L(deathCount == 1 ? "Attempts_Singular" : "Attempts_Plural"), deathCount);
    }

    private static string FormatDefeatedAttempts(int deathCount)
    {
        return deathCount == 0 ? L("Bosses_FirstTry") : string.Format(L("Bosses_DeathsFormat"), deathCount);
    }

    private static string FormatBossStat(string name, int deathCount, TimeSpan duration)
    {
        return string.Format(L("Stats_BossStatFormat"), name, FormatDefeatedAttempts(deathCount), FormatDuration(duration));
    }

    private static TimeSpan GetBossKillDuration(BossHistoryEntry boss)
    {
        return boss.KillDuration > TimeSpan.Zero
            ? boss.KillDuration
            : boss.DefeatedAt - boss.StartedAt;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var safeDuration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
        return $"{(int)safeDuration.TotalHours:00}:{safeDuration.Minutes:00}:{safeDuration.Seconds:00}";
    }

    private static IReadOnlyList<CaptureTargetOption> CreateCaptureTargetOptions(AppGameProfile profile)
    {
        var options = new List<CaptureTargetOption>
        {
            new(GameWindowTargetResolver.GetCaptureTarget(profile), L("Capture_EldenRingWindow")),
            new("PrimaryScreen", L("Capture_PrimaryScreen"))
        };

        var screens = System.Windows.Forms.Screen.AllScreens;
        for (var index = 0; index < screens.Length; index++)
        {
            var screen = screens[index];
            var bounds = screen.Bounds;
            var primary = screen.Primary ? L("Capture_PrimarySuffix") : string.Empty;
            options.Add(new CaptureTargetOption(
                $"Screen:{index}",
                string.Format(L("Capture_ScreenFormat"), index + 1, bounds.Width, bounds.Height, bounds.Left, bounds.Top, primary)));
        }

        return options;
    }

    private static IReadOnlyList<GameLanguageOption> CreateGameLanguageOptions()
    {
        return
        [
            new("PL", "Polski"),
            new("ENG", "English")
        ];
    }

    private static IReadOnlyList<AppLanguageOption> CreateAppLanguageOptions()
    {
        return LocalizationService.Instance.AvailableLanguages
            .Select(language => new AppLanguageOption(language.Code, language.DisplayName))
            .ToList();
    }

    private static IReadOnlyList<DetectionModeOption> CreateDetectionModeOptions()
    {
        return DetectionModePresets.All
            .Select(preset => new DetectionModeOption(preset.Mode, L($"DetectionMode_{preset.Mode}")))
            .ToList();
    }

    private static IReadOnlyList<BossHealthBarStyleOption> CreateBossHealthBarStyleOptions()
    {
        return
        [
            new(BossHealthBarStyles.Vanilla, L("BossHealthBarStyle_Vanilla")),
            new(BossHealthBarStyles.Reforged, L("BossHealthBarStyle_Reforged")),
            new(BossHealthBarStyles.Convergence, L("BossHealthBarStyle_Convergence"))
        ];
    }

    private void ApplyDetectionModePreset(DetectionModePreset preset)
    {
        DetectionIntervalMsText = preset.IntervalMs.ToString(CultureInfo.InvariantCulture);
        DetectionCooldownSecondsText = preset.CooldownSeconds.ToString(CultureInfo.InvariantCulture);
        DetectionSensitivityText = preset.Sensitivity.ToString("0.00", CultureInfo.InvariantCulture);
        OnPropertyChanged(nameof(DetectionIntervalMsText));
        OnPropertyChanged(nameof(DetectionCooldownSecondsText));
        OnPropertyChanged(nameof(DetectionSensitivityText));
    }

    private async Task ApplyAppLanguageAsync(string language)
    {
        Settings.AppLanguage = language;
        LocalizationService.Instance.SetLanguage(language);
        await _settingsStore.SaveAsync(_settingsPath, Settings);
        _log.Info($"App language switched to '{language}'.");
    }

    private static IReadOnlyList<BossSortModeOption> CreateBossSortModeOptions()
    {
        return
        [
            new(BossHistorySortMode.Default, L("Sort_Default")),
            new(BossHistorySortMode.Time, L("Sort_Time")),
            new(BossHistorySortMode.Deaths, L("Sort_Deaths"))
        ];
    }

    private static IReadOnlyList<BossSortDirectionOption> CreateBossSortDirectionOptions()
    {
        return
        [
            new(BossHistorySortDirection.Descending, L("Sort_Descending")),
            new(BossHistorySortDirection.Ascending, L("Sort_Ascending"))
        ];
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

    private void OpenPath(string path)
    {
        try
        {
            if (path.EndsWith("deaths.json", StringComparison.OrdinalIgnoreCase) && !File.Exists(path))
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, JsonSerializer.Serialize(_counterService.State, JsonFileOptions.Value));
            }

            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            _log.Error($"Failed to open path {path}.", exception);
            DetectionStatus = string.Format(L("Vm_FailedToOpenFormat"), path);
        }
    }

    private static string L(string key) => LocalizationService.Instance.GetString(key);

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        _dispatcher.Invoke(() =>
        {
            RebuildLocalizedOptions();
            // Re-pushes the overlay counter and raises CounterText so the Deaths/Śmierci label
            // follows the new UI language; also refreshes the boss cards.
            RefreshCounter();
            RefreshStats();
            OnPropertyChanged(nameof(BossesActiveName));
            OnPropertyChanged(nameof(BossesActiveAttemptsText));
            OnPropertyChanged(nameof(BossesActiveStartedText));
            OnPropertyChanged(nameof(DefeatedBossesEmptyText));
            OnPropertyChanged(nameof(StatusOverlayStateText));
            OnPropertyChanged(nameof(StatusDetectionStateText));
            OnPropertyChanged(nameof(LastDetectedDeathText));
            OnPropertyChanged(nameof(BossHistoryEditorTitle));
            OnPropertyChanged(nameof(StatusSummary));
            OnPropertyChanged(nameof(StatsActiveBossText));
            OnPropertyChanged(nameof(StatsBestBossText));
            OnPropertyChanged(nameof(StatsHardestBossText));
            OnPropertyChanged(nameof(StatsLongestBossText));
        });
    }

    private void RebuildLocalizedOptions()
    {
        var captureTarget = SelectedCaptureTargetValue;
        CaptureTargetOptions.Clear();
        foreach (var option in CreateCaptureTargetOptions(_activeGameProfile))
        {
            CaptureTargetOptions.Add(option);
        }

        SelectedCaptureTargetValue = captureTarget;
        OnPropertyChanged(nameof(SelectedCaptureTargetValue));

        var detectionMode = SelectedDetectionModeValue;
        DetectionModeOptions.Clear();
        foreach (var option in CreateDetectionModeOptions())
        {
            DetectionModeOptions.Add(option);
        }

        _selectedDetectionModeValue = detectionMode;
        OnPropertyChanged(nameof(SelectedDetectionModeValue));

        var sortMode = SelectedBossSortMode;
        BossSortModeOptions.Clear();
        foreach (var option in CreateBossSortModeOptions())
        {
            BossSortModeOptions.Add(option);
        }

        var sortDirection = SelectedBossSortDirection;
        BossSortDirectionOptions.Clear();
        foreach (var option in CreateBossSortDirectionOptions())
        {
            BossSortDirectionOptions.Add(option);
        }

        // Re-assert selections so the combo boxes rebind to the freshly localized display names.
        _selectedBossSortMode = sortMode;
        _selectedBossSortDirection = sortDirection;
        OnPropertyChanged(nameof(SelectedBossSortMode));
        OnPropertyChanged(nameof(SelectedBossSortDirection));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public sealed record BossDisplayItem(
    string NumberText,
    string Name,
    string AttemptsText,
    string FightDurationText,
    string RecordedText,
    string CompletedBy,
    BossHistoryEntry Entry);

public sealed record CaptureTargetOption(string Value, string DisplayName);

public sealed record GameLanguageOption(string Value, string DisplayName);

public sealed record AppLanguageOption(string Value, string DisplayName);

public sealed record DetectionModeOption(string Value, string DisplayName);

public sealed record BossHealthBarStyleOption(string Value, string DisplayName);

public sealed record BossSortModeOption(BossHistorySortMode Value, string DisplayName);

public sealed record BossSortDirectionOption(BossHistorySortDirection Value, string DisplayName);

public sealed record StatsRecentEventDisplayItem(
    string TimestampText,
    string DetectionMethod,
    string Note,
    string CountAfterText);
