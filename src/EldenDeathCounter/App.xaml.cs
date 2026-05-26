using System.IO;
using System.Windows;
using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.Core.Logging;
using EldenDeathCounter.Core.Storage;
using EldenDeathCounter.Detection;
using EldenDeathCounter.Hotkeys;
using EldenDeathCounter.ViewModels;

namespace EldenDeathCounter;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = @"Local\EldenDeathCounter.SingleInstance";

    private DeathDetectionService? _detectionService;
    private DeathCounterService? _counterService;
    private GlobalHotkeyService? _hotkeyService;
    private SwitchableLogService? _log;
    private DetectionEventLogService? _detectionEventLog;
    private Mutex? _singleInstanceMutex;

    protected override async void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            System.Windows.MessageBox.Show(
                "Another EldenDeathCounter instance is already running.",
                "EldenDeathCounter",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Shutdown();
            return;
        }

        base.OnStartup(e);

        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var startupProfile = AppGameProfile.EldenRing;
        var defaultDataFolder = startupProfile.GetDataFolderPath(desktopPath);
        Directory.CreateDirectory(defaultDataFolder);

        _log = new SwitchableLogService(startupProfile.GetLogFilePath(desktopPath));
        _detectionEventLog = new DetectionEventLogService();
        _log.Info("App started.");

        try
        {
            var settingsPath = startupProfile.GetSettingsFilePath(desktopPath);
            var settingsStore = new AppSettingsStore(_log);
            var settings = await settingsStore.LoadAsync(settingsPath, desktopPath, startupProfile);
            settings.DataFolderPath = Environment.ExpandEnvironmentVariables(settings.DataFolderPath);
            Directory.CreateDirectory(settings.DataFolderPath);

            var activeLogPath = Path.Combine(settings.DataFolderPath, "log.txt");
            if (!Path.GetFullPath(activeLogPath).Equals(Path.GetFullPath(startupProfile.GetLogFilePath(desktopPath)), StringComparison.OrdinalIgnoreCase))
            {
                _log.SwitchTo(activeLogPath);
                _log.Info("App started with custom data folder.");
            }

            var dataFilePath = Path.Combine(settings.DataFolderPath, "deaths.json");
            var deathStore = new DeathCounterStore(_log);
            var state = await deathStore.LoadAsync(dataFilePath);
            var counterService = new DeathCounterService(deathStore, _log, dataFilePath, state);
            _counterService = counterService;
            _detectionEventLog.Configure(settings, new DetectionDiagnosticsState(state.CurrentDeathCount, state.ActiveBoss?.Name, false));

            var overlayWindow = new OverlayWindow(settings);
            overlayWindow.UpdateCount(state.CurrentDeathCount, state.ActiveBoss, settings.GameLanguage);
            if (settings.OverlayEnabled)
            {
                overlayWindow.Show();
            }

            var captureService = new ScreenCaptureService(_log);
            var ocrService = new WindowsOcrTextRecognitionService(_log);
            var imageSignalDetector = new TemplateDeathTextImageSignalDetector(_log);
            var bossVictorySignalDetector = new TemplateBossVictoryTextImageSignalDetector(_log);
            var bossNameDetector = new BossNameDetector(ocrService, _log);
            _detectionService = new DeathDetectionService(captureService, ocrService, imageSignalDetector, bossVictorySignalDetector, bossNameDetector, counterService, _log, _detectionEventLog);
            _hotkeyService = new GlobalHotkeyService(_log);

            var viewModel = new MainWindowViewModel(
                settings,
                settingsPath,
                settingsStore,
                counterService,
                _detectionService,
                _hotkeyService,
                overlayWindow,
                _log,
                _detectionEventLog,
                desktopPath);

            var mainWindow = new MainWindow(viewModel);
            MainWindow = mainWindow;
            mainWindow.Show();

            if (settings.DetectionEnabledOnStartup)
            {
                await viewModel.StartDetectionAsync();
            }
        }
        catch (Exception exception)
        {
            _log?.Error("Fatal startup error.", exception);
            System.Windows.MessageBox.Show(
                $"EldenDeathCounter failed to start.{Environment.NewLine}{exception.Message}",
                "EldenDeathCounter",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_counterService is not null)
        {
            _counterService.PauseActiveBossTimerAsync().GetAwaiter().GetResult();
        }

        if (_detectionService is not null)
        {
            _detectionService.StopAsync().GetAwaiter().GetResult();
        }

        _hotkeyService?.Dispose();
        _log?.Info("App stopped.");
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
