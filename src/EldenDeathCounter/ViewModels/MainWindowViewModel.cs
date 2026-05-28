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
using EldenDeathCounter.Core.Hotkeys;
using EldenDeathCounter.Core.Logging;
using EldenDeathCounter.Core.Storage;
using EldenDeathCounter.Detection;
using EldenDeathCounter.Hotkeys;
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
    private Window? _window;
    private string _detectionStatus = "Detection stopped";
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
        foreach (var option in CreateCaptureTargetOptions())
        {
            CaptureTargetOptions.Add(option);
        }

        foreach (var option in CreateGameLanguageOptions())
        {
            GameLanguageOptions.Add(option);
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
        OverlayXText = string.Empty;
        OverlayYText = string.Empty;
        DetectionPhrasesText = string.Empty;
        BossVictoryPhrasesText = string.Empty;
        ManualAddHotkeyText = string.Empty;
        ManualSubtractHotkeyText = string.Empty;
        ManualBossDefeatedHotkeyText = string.Empty;
        DataFolderPathText = string.Empty;
        ManualCounterText = string.Empty;
        BossNameText = string.Empty;
        RefreshSettingsTextFields();
        RefreshCounterTextFields();
        RefreshBosses();

        StartDetectionCommand = new RelayCommand(_ => _ = StartDetectionAsync());
        StopDetectionCommand = new RelayCommand(_ => _ = StopDetectionAsync());
        ToggleDetectionCommand = new RelayCommand(_ => _ = ToggleDetectionAsync());
        ResetCounterCommand = new RelayCommand(_ => _ = ResetCounterAsync());
        AddDeathCommand = new RelayCommand(_ => _ = AddDeathAsync("manual-button", "Added from control window."));
        SubtractDeathCommand = new RelayCommand(_ => _ = SubtractDeathAsync("manual-button", "Subtracted from control window."));
        SetCounterCommand = new RelayCommand(_ => _ = SetCounterAsync());
        ToggleOverlayCommand = new RelayCommand(_ => _ = ToggleOverlayAsync());
        OpenDataFileCommand = new RelayCommand(_ => OpenPath(Path.Combine(Settings.DataFolderPath, "deaths.json")));
        OpenDataFolderCommand = new RelayCommand(_ => OpenPath(Settings.DataFolderPath));
        SaveSettingsCommand = new RelayCommand(_ => _ = SaveSettingsAsync());
        SetActiveBossCommand = new RelayCommand(_ => _ = SetActiveBossAsync());
        ClearActiveBossCommand = new RelayCommand(_ => _ = ClearActiveBossAsync("manual-button"));
        BossDefeatedCommand = new RelayCommand(_ => _ = MarkBossDefeatedAsync("manual-button"));
        ClearDetectionLogCommand = new RelayCommand(_ => DetectionLogEntries.Clear());
        StartDiagnosticsCommand = new RelayCommand(_ => StartDiagnosticsSession());
        OpenBossHistoryEditorCommand = new RelayCommand(OpenBossHistoryEditor);
        SaveBossHistoryEditorCommand = new RelayCommand(_ => _ = SaveBossHistoryEditorAsync());
        DeleteBossHistoryEditorCommand = new RelayCommand(_ => _ = DeleteBossHistoryEditorAsync());
        CancelBossHistoryEditorCommand = new RelayCommand(_ => CloseBossHistoryEditor());

        if (_log is ILogEntrySource logEntrySource)
        {
            logEntrySource.EntryWritten += (_, entry) => AddDetectionLogEntry(entry);
        }

        _log.Info("Detection log initialized. Awaiting events.");

        _counterService.StateChanged += (_, _) =>
        {
            RefreshCounter();
            ConfigureDetectionDiagnostics();
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
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AppSettings Settings { get; private set; }

    public string CounterText => DeathCounterText.FormatGlobalCount(_counterService.State.CurrentDeathCount, Settings.GameLanguage);

    public string ActiveBossText => _counterService.State.ActiveBoss is null
        ? "Active boss: none"
        : $"Active boss: {_counterService.State.ActiveBoss.Name} ({_counterService.State.ActiveBoss.DeathCount})";

    public string ActiveBossDeathCountText => _counterService.State.ActiveBoss is null
        ? "(0)"
        : $"({_counterService.State.ActiveBoss.DeathCount})";

    public string BossesActiveName => _counterService.State.ActiveBoss?.Name ?? "No active encounter";

    public string BossesActiveAttemptsText => _counterService.State.ActiveBoss is null
        ? "0 attempts"
        : FormatAttempts(_counterService.State.ActiveBoss.DeathCount);

    public string BossesActiveStartedText => _counterService.State.ActiveBoss is null
        ? "Start a boss encounter from the dashboard."
        : $"Started {_counterService.State.ActiveBoss.StartedAt.LocalDateTime:yyyy-MM-dd HH:mm}";

    public string DefeatedBossesEmptyText => DefeatedBosses.Count == 0
        ? string.IsNullOrWhiteSpace(BossSearchText) || _counterService.State.BossHistory.Count == 0
            ? "No conquered foes recorded yet."
            : "No conquered foes match search."
        : string.Empty;

    public string StatusSummary => $"{CounterText} | Overlay: {(Settings.OverlayEnabled ? "on" : "off")} | Detection: {DetectionStatus}";

    public string StatusDeathCountText => _counterService.State.CurrentDeathCount.ToString(CultureInfo.InvariantCulture);

    public string StatusOverlayStateText => Settings.OverlayEnabled ? "ACTIVE" : "OFF";

    public string StatusDetectionStateText => IsDetectionRunning ? "RUNNING" : "STOPPED";

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

    public string LastDetectedDeathText => _lastDetectedDeath?.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.CurrentCulture) ?? "None";

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

    public string DetectionIntervalMsText { get; set; }

    public string DetectionCooldownSecondsText { get; set; }

    public string DetectionSensitivityText { get; set; }

    public string SelectedCaptureTargetValue { get; set; }

    public string SelectedGameLanguageValue { get; set; }

    public string OverlayXText { get; set; }

    public string OverlayYText { get; set; }

    public string DetectionPhrasesText { get; set; }

    public string BossVictoryPhrasesText { get; set; }

    public string ManualAddHotkeyText { get; set; }

    public string ManualSubtractHotkeyText { get; set; }

    public string ManualBossDefeatedHotkeyText { get; set; }

    public string DataFolderPathText { get; set; }

    public string ManualCounterText { get; set; }

    public string BossNameText { get; set; }

    public bool IsBossHistoryEditorOpen
    {
        get => _isBossHistoryEditorOpen;
        private set => SetField(ref _isBossHistoryEditorOpen, value);
    }

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

    public string FooterText => "Use borderless fullscreen or windowed mode for Elden Ring. Exclusive fullscreen may hide the overlay. F8 adds a death, F9 subtracts one, and F7 marks the active boss defeated by default.";

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

    public ICommand SetActiveBossCommand { get; }

    public ICommand ClearActiveBossCommand { get; }

    public ICommand BossDefeatedCommand { get; }

    public ICommand ClearDetectionLogCommand { get; }

    public ICommand StartDiagnosticsCommand { get; }

    public ICommand OpenBossHistoryEditorCommand { get; }

    public ICommand SaveBossHistoryEditorCommand { get; }

    public ICommand DeleteBossHistoryEditorCommand { get; }

    public ICommand CancelBossHistoryEditorCommand { get; }

    public ObservableCollection<LogEntry> DetectionLogEntries { get; } = [];

    public ObservableCollection<BossDisplayItem> DefeatedBosses { get; } = [];

    public ObservableCollection<CaptureTargetOption> CaptureTargetOptions { get; } = [];

    public ObservableCollection<GameLanguageOption> GameLanguageOptions { get; } = [];

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

        _settingsPath = profile.GetSettingsFilePath(_desktopPath);
        _log.SwitchTo(profile.GetLogFilePath(_desktopPath));
        Settings = await _settingsStore.LoadAsync(_settingsPath, _desktopPath, profile);
        Settings.DataFolderPath = Environment.ExpandEnvironmentVariables(Settings.DataFolderPath);
        Directory.CreateDirectory(Settings.DataFolderPath);
        _log.SwitchTo(Path.Combine(Settings.DataFolderPath, "log.txt"));
        await _counterService.SwitchDataFileAsync(Path.Combine(Settings.DataFolderPath, "deaths.json"));
        ConfigureDetectionDiagnostics();

        RefreshSettingsTextFields();
        RefreshCounter();
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

        DetectionStatus = $"Switched to {profile.Theme.Title}";
        _log.Info($"Game profile switched to {profile.Id}.");
        OnPropertyChanged(nameof(StatusSummary));
        return true;
    }

    public async Task StartDetectionAsync()
    {
        if (!await ApplySettingsFromTextAsync())
        {
            return;
        }

        await _counterService.ResumeActiveBossTimerAsync();
        _detectionService.Start(Settings);
        IsDetectionRunning = true;
        ConfigureDetectionDiagnostics();
    }

    public async Task StopDetectionAsync()
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
            IsDetectionRunning = false;
            await _detectionService.StopAsync();
            await _counterService.PauseActiveBossTimerAsync();
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
        DetectionStatus = $"Full diagnostics active until {until.LocalDateTime:HH:mm:ss}";
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

    private async Task ToggleDetectionAsync()
    {
        if (IsDetectionRunning)
        {
            await StopDetectionAsync();
            return;
        }

        await StartDetectionAsync();
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
            DetectionStatus = "Manual counter value must be a whole number.";
            return;
        }

        await _counterService.SetCountAsync(count, "manual-button", "Set from control window.");
    }

    private async Task SetActiveBossAsync()
    {
        if (string.IsNullOrWhiteSpace(BossNameText))
        {
            DetectionStatus = "Boss name cannot be empty.";
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

    private async Task MarkBossDefeatedAsync(string detectionMethod)
    {
        await _counterService.MarkActiveBossDefeatedAsync(detectionMethod);
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
        IsBossHistoryEditorOpen = true;
    }

    private async Task SaveBossHistoryEditorAsync()
    {
        if (_editingBossHistoryEntry is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(BossEditNameText))
        {
            DetectionStatus = "Boss name cannot be empty.";
            return;
        }

        if (!int.TryParse(BossEditAttemptsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var deathCount) || deathCount < 0)
        {
            DetectionStatus = "Boss attempts must be a whole number of 0 or higher.";
            return;
        }

        if (!TimeSpan.TryParseExact(BossEditDurationText, "c", CultureInfo.InvariantCulture, out var duration) &&
            !TimeSpan.TryParse(BossEditDurationText, CultureInfo.InvariantCulture, out duration))
        {
            DetectionStatus = "Fight duration must use hh:mm:ss format.";
            return;
        }

        if (duration < TimeSpan.Zero)
        {
            DetectionStatus = "Fight duration cannot be negative.";
            return;
        }

        if (!DateTime.TryParseExact(
                BossEditRecordedAtText,
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var defeatedLocalTime))
        {
            DetectionStatus = "Recorded at must use yyyy-MM-dd HH:mm format.";
            return;
        }

        var defeatedAt = new DateTimeOffset(defeatedLocalTime, TimeZoneInfo.Local.GetUtcOffset(defeatedLocalTime));
        var startedAt = defeatedAt - duration;
        await _counterService.UpdateBossHistoryEntryAsync(
            _editingBossHistoryEntry,
            BossEditNameText,
            deathCount,
            startedAt,
            defeatedAt,
            BossEditCompletedByText);
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

    private async Task<bool> ApplySettingsFromTextAsync(bool restartDetection = true, bool updateStatus = true)
    {
        if (!TryReadSettings(out var error))
        {
            DetectionStatus = error;
            return false;
        }

        Directory.CreateDirectory(Settings.DataFolderPath);
        await _settingsStore.SaveAsync(_settingsPath, Settings);
        ConfigureDetectionDiagnostics();
        _overlayWindow.ApplyPosition(Settings.OverlayX, Settings.OverlayY);
        _overlayWindow.UpdateCount(_counterService.State.CurrentDeathCount, _counterService.State.ActiveBoss, Settings.GameLanguage);
        ApplyOverlayState();
        RegisterHotkeys();
        if (restartDetection && IsDetectionRunning)
        {
            await _detectionService.RestartAsync(Settings);
            IsDetectionRunning = true;
        }

        if (updateStatus)
        {
            DetectionStatus = "Settings saved";
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

        if (!int.TryParse(DetectionIntervalMsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var interval) || interval < 300)
        {
            error = "Detection interval must be at least 300 ms.";
            return false;
        }

        if (!int.TryParse(DetectionCooldownSecondsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cooldown) || cooldown < 1)
        {
            error = "Cooldown must be at least 1 second.";
            return false;
        }

        if (!double.TryParse(DetectionSensitivityText, NumberStyles.Float, CultureInfo.InvariantCulture, out var sensitivity))
        {
            error = "Detection sensitivity must be a number from 0.1 to 1.0.";
            return false;
        }

        if (!double.TryParse(OverlayXText, NumberStyles.Float, CultureInfo.InvariantCulture, out var overlayX) ||
            !double.TryParse(OverlayYText, NumberStyles.Float, CultureInfo.InvariantCulture, out var overlayY))
        {
            error = "Overlay X and Y must be numbers.";
            return false;
        }

        var phrases = DetectionPhrasesText
            .Split([Environment.NewLine, "\n", "\r", ";"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (phrases.Count == 0)
        {
            error = "At least one detection phrase is required.";
            return false;
        }

        var bossVictoryPhrases = BossVictoryPhrasesText
            .Split([Environment.NewLine, "\n", "\r", ";"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (bossVictoryPhrases.Count == 0)
        {
            error = "At least one boss victory phrase is required.";
            return false;
        }

        var addHotkey = HotkeyDefinition.Parse(ManualAddHotkeyText);
        if (!addHotkey.IsValid)
        {
            error = $"Add hotkey is invalid: {addHotkey.Error}";
            return false;
        }

        var subtractHotkey = HotkeyDefinition.Parse(ManualSubtractHotkeyText);
        if (!subtractHotkey.IsValid)
        {
            error = $"Subtract hotkey is invalid: {subtractHotkey.Error}";
            return false;
        }

        var bossDefeatedHotkey = HotkeyDefinition.Parse(ManualBossDefeatedHotkeyText);
        if (!bossDefeatedHotkey.IsValid)
        {
            error = $"Boss defeated hotkey is invalid: {bossDefeatedHotkey.Error}";
            return false;
        }

        var dataFolderPath = Environment.ExpandEnvironmentVariables(DataFolderPathText.Trim());
        if (string.IsNullOrWhiteSpace(dataFolderPath))
        {
            error = "Data folder path cannot be empty.";
            return false;
        }

        Settings.DetectionIntervalMs = interval;
        Settings.DetectionCooldownSeconds = cooldown;
        Settings.DetectionSensitivity = Math.Clamp(sensitivity, 0.1, 1.0);
        Settings.CaptureTarget = string.IsNullOrWhiteSpace(SelectedCaptureTargetValue)
            ? "PrimaryScreen"
            : SelectedCaptureTargetValue.Trim();
        Settings.GameLanguage = NormalizeGameLanguage(SelectedGameLanguageValue, Settings.GameLanguage);
        Settings.OverlayX = overlayX;
        Settings.OverlayY = overlayY;
        Settings.DetectionPhrases = phrases;
        Settings.BossVictoryPhrases = bossVictoryPhrases;
        Settings.ManualAddHotkey = ManualAddHotkeyText.Trim();
        Settings.ManualSubtractHotkey = ManualSubtractHotkeyText.Trim();
        Settings.BossDefeatedHotkey = ManualBossDefeatedHotkeyText.Trim();
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
        HotkeyStatus = $"Hotkeys: {add}; {subtract}; {bossDefeated}";
    }

    private void HandleHotkey(string name)
    {
        _ = _dispatcher.InvokeAsync(async () =>
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
        });
    }

    private void RefreshCounter()
    {
        _dispatcher.Invoke(() =>
        {
            _overlayWindow.UpdateCount(_counterService.State.CurrentDeathCount, _counterService.State.ActiveBoss, Settings.GameLanguage);
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
        SelectedCaptureTargetValue = CaptureTargetOptions.Any(option => option.Value.Equals(Settings.CaptureTarget, StringComparison.OrdinalIgnoreCase))
            ? Settings.CaptureTarget
            : "PrimaryScreen";
        SelectedGameLanguageValue = NormalizeGameLanguage(Settings.GameLanguage, "PL");
        OverlayXText = Settings.OverlayX.ToString(CultureInfo.InvariantCulture);
        OverlayYText = Settings.OverlayY.ToString(CultureInfo.InvariantCulture);
        DetectionPhrasesText = string.Join(Environment.NewLine, Settings.DetectionPhrases);
        BossVictoryPhrasesText = string.Join(Environment.NewLine, Settings.BossVictoryPhrases);
        ManualAddHotkeyText = Settings.ManualAddHotkey;
        ManualSubtractHotkeyText = Settings.ManualSubtractHotkey;
        ManualBossDefeatedHotkeyText = Settings.BossDefeatedHotkey;
        DataFolderPathText = Settings.DataFolderPath;

        OnPropertyChanged(nameof(DetectionIntervalMsText));
        OnPropertyChanged(nameof(DetectionCooldownSecondsText));
        OnPropertyChanged(nameof(DetectionSensitivityText));
        OnPropertyChanged(nameof(SelectedCaptureTargetValue));
        OnPropertyChanged(nameof(SelectedGameLanguageValue));
        OnPropertyChanged(nameof(OverlayXText));
        OnPropertyChanged(nameof(OverlayYText));
        OnPropertyChanged(nameof(DetectionPhrasesText));
        OnPropertyChanged(nameof(BossVictoryPhrasesText));
        OnPropertyChanged(nameof(ManualAddHotkeyText));
        OnPropertyChanged(nameof(ManualSubtractHotkeyText));
        OnPropertyChanged(nameof(ManualBossDefeatedHotkeyText));
        OnPropertyChanged(nameof(DataFolderPathText));
        OnPropertyChanged(nameof(OverlayEnabled));
        OnPropertyChanged(nameof(DetectionEnabledOnStartup));
        OnPropertyChanged(nameof(AutoDetectBossNames));
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
                FormatAttempts(boss.DeathCount),
                FormatDuration(GetBossKillDuration(boss)),
                $"Recorded {boss.DefeatedAt.LocalDateTime:yyyy-MM-dd HH:mm}",
                boss.CompletedBy,
                boss));
        }

        OnPropertyChanged(nameof(BossesActiveName));
        OnPropertyChanged(nameof(BossesActiveAttemptsText));
        OnPropertyChanged(nameof(BossesActiveStartedText));
        OnPropertyChanged(nameof(DefeatedBossesEmptyText));
    }

    private static string FormatAttempts(int deathCount)
    {
        return deathCount == 1 ? "1 attempt" : $"{deathCount} attempts";
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

    private static IReadOnlyList<CaptureTargetOption> CreateCaptureTargetOptions()
    {
        var options = new List<CaptureTargetOption>
        {
            new("EldenRingWindow", "Elden Ring window"),
            new("PrimaryScreen", "Primary screen")
        };

        var screens = System.Windows.Forms.Screen.AllScreens;
        for (var index = 0; index < screens.Length; index++)
        {
            var screen = screens[index];
            var bounds = screen.Bounds;
            var primary = screen.Primary ? ", primary" : string.Empty;
            options.Add(new CaptureTargetOption(
                $"Screen:{index}",
                $"Screen {index + 1} ({bounds.Width}x{bounds.Height} at {bounds.Left},{bounds.Top}{primary})"));
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

    private static IReadOnlyList<BossSortModeOption> CreateBossSortModeOptions()
    {
        return
        [
            new(BossHistorySortMode.Default, "Default"),
            new(BossHistorySortMode.Time, "Time"),
            new(BossHistorySortMode.Deaths, "Deaths")
        ];
    }

    private static IReadOnlyList<BossSortDirectionOption> CreateBossSortDirectionOptions()
    {
        return
        [
            new(BossHistorySortDirection.Descending, "Descending"),
            new(BossHistorySortDirection.Ascending, "Ascending")
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
            DetectionStatus = $"Failed to open {path}";
        }
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

public sealed record BossSortModeOption(BossHistorySortMode Value, string DisplayName);

public sealed record BossSortDirectionOption(BossHistorySortDirection Value, string DisplayName);
