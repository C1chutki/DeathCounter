using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Reflection;
using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.Core.Storage;
using EldenDeathCounter.Interop;
using EldenDeathCounter.Localization;

namespace EldenDeathCounter;

public partial class OverlayWindow : Window
{
    // Default overlay background color (matches OverlayWindow.xaml OverlayChrome.Background).
    private static readonly System.Windows.Media.Color DefaultOverlayBackgroundColor =
        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0A1014");
    private static readonly System.Windows.Media.Color TimerOverlayBackgroundColor =
        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#000000");

    private readonly System.Threading.Timer _bossTimer;
    // Re-asserts the overlay into the topmost z-order band on a steady cadence. A borderless
    // fullscreen game grabbing the foreground demotes our one-shot Topmost, so without this the
    // overlay disappears behind the game. (Exclusive fullscreen cannot be overlaid either way.)
    private readonly DispatcherTimer _topMostTimer;
    // Language used for the overlay's own labels (Deaths/Śmierci, First Try). This follows the UI
    // language (AppLanguage), not the OCR GameLanguage.
    private string _appLanguage;
    private ActiveBossState? _activeBoss;
    private bool _isDetectionRunning;
    private bool _showBossTimer = true;
    private System.Windows.Media.Color _overlayBackgroundColor = DefaultOverlayBackgroundColor;
    private double _backgroundOpacity = 0.9;

    public OverlayWindow(AppSettings settings)
    {
        InitializeComponent();
        Left = settings.OverlayX;
        Top = settings.OverlayY;
        _appLanguage = settings.AppLanguage;
        _bossTimer = new System.Threading.Timer(_ => QueueBossTimerRefresh());
        _topMostTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _topMostTimer.Tick += (_, _) => ReassertTopMost();
        VersionTextBlock.Text = GetApplicationVersionText();
        UpdateDetectionState(false);
        ApplyScale(settings.OverlayFontScale);
        ApplyBackgroundOpacity(settings.OverlayBackgroundOpacity);
        ApplyBossTimerVisibility(settings.ShowBossTimer);
        ApplyDetectionStatusVisibility(settings.ShowDetectionStatus);

        // The detection status label is set in code, so {DynamicResource} cannot refresh it on a
        // live language swap. Re-apply it whenever the UI language changes.
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        Closed += (_, _) =>
        {
            LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
            _topMostTimer.Stop();
            _bossTimer.Dispose();
        };
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateDetectionState(_isDetectionRunning);
    }

    public void ApplyScale(double scale)
    {
        Dispatcher.Invoke(() =>
        {
            var safeScale = Math.Clamp(scale <= 0 ? 1.0 : scale, 0.6, 1.6);
            OverlayScaleTransform.ScaleX = safeScale;
            OverlayScaleTransform.ScaleY = safeScale;
        });
    }

    public void ApplyBackgroundOpacity(double opacity)
    {
        Dispatcher.Invoke(() =>
        {
            _backgroundOpacity = Math.Clamp(opacity, 0.0, 1.0);
            RefreshOverlayBackground();
        });
    }

    public void ApplyBossTimerVisibility(bool isVisible)
    {
        Dispatcher.Invoke(() =>
        {
            _showBossTimer = isVisible;
            TimerChrome.Visibility = isVisible && _activeBoss is not null ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    public void ApplyDetectionStatusVisibility(bool isVisible)
    {
        Dispatcher.Invoke(() =>
        {
            DetectionStatusPanel.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    public void UpdateCount(int count, ActiveBossState? activeBoss = null, string? appLanguage = null)
    {
        Dispatcher.Invoke(() =>
        {
            if (!string.IsNullOrWhiteSpace(appLanguage))
            {
                _appLanguage = appLanguage;
            }

            CounterTextBlock.Text = DeathCounterText.FormatGlobalCount(count, _appLanguage);
            _activeBoss = activeBoss;
            if (activeBoss is null)
            {
                BossTextBlock.Text = string.Empty;
                BossDeathTextBlock.Text = string.Empty;
                TimerTextBlock.Text = string.Empty;
                BossPanel.Visibility = Visibility.Collapsed;
                _bossTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                return;
            }

            BossTextBlock.Text = DeathCounterText.FormatBossOverlayName(activeBoss.Name);
            BossDeathTextBlock.Text = DeathCounterText.FormatBossDeath(activeBoss.DeathCount, _appLanguage);
            BossPanel.Visibility = Visibility.Visible;
            TimerChrome.Visibility = _showBossTimer ? Visibility.Visible : Visibility.Collapsed;
            UpdateBossTimerText();
            if (activeBoss.IsTimerRunning)
            {
                _bossTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            }
            else if (!activeBoss.IsTimerRunning)
            {
                _bossTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
        });
    }

    public void UpdateDetectionState(bool isRunning)
    {
        Dispatcher.Invoke(() =>
        {
            _isDetectionRunning = isRunning;
            DetectionStatusTextBlock.Text = LocalizationService.Instance.GetString(
                isRunning ? "Overlay_DetectionRunning" : "Overlay_DetectionStopped");
            DetectionDot.Fill = BrushFromHex(isRunning ? "#8DA46D" : "#6E6253");
        });
    }

    public void ApplyPosition(double x, double y)
    {
        Left = x;
        Top = y;
    }

    public void ApplyTheme(AppGameTheme theme)
    {
        Dispatcher.Invoke(() =>
        {
            _overlayBackgroundColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.OverlayBackground);
            RefreshOverlayBackground();
            OverlayChrome.BorderBrush = BrushFromHex(theme.OverlayBorder);
            CounterTextBlock.Foreground = BrushFromHex(theme.OverlayText);
            BossTextBlock.Foreground = BrushFromHex(theme.OverlayText);
            BossDeathTextBlock.Foreground = BrushFromHex(theme.Primary);
            TimerTextBlock.Foreground = BrushFromHex(theme.Tertiary);
            DetectionStatusTextBlock.Foreground = BrushFromHex(theme.Primary);
            TotalDeathsLabelTextBlock.Foreground = BrushFromHex(theme.Primary);
            VersionTextBlock.Foreground = BrushFromHex(theme.MutedInk);
            DividerBorder.Background = BrushFromHex(theme.Border);
            TimerChrome.BorderBrush = BrushFromHex(theme.Border);
            DetectionDot.Fill = BrushFromHex(_isDetectionRunning ? theme.Primary : theme.MutedInk);
        });
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ClickThroughWindow.Enable(this);
        ClickThroughWindow.ForceTopMost(this);
        _topMostTimer.Start();
    }

    // Only poke the z-order while the overlay is actually shown. When it is hidden (overlay
    // toggled off) we skip so we don't churn SetWindowPos on a hidden window.
    private void ReassertTopMost()
    {
        if (!IsVisible)
        {
            return;
        }

        ClickThroughWindow.ForceTopMost(this);
    }

    private void UpdateBossTimerText()
    {
        if (_activeBoss is null)
        {
            return;
        }

        TimerTextBlock.Text = FormatDuration(_activeBoss.GetElapsedDuration(DateTimeOffset.Now));
    }

    private void QueueBossTimerRefresh()
    {
        if (Dispatcher.HasShutdownStarted)
        {
            return;
        }

        Dispatcher.BeginInvoke(UpdateBossTimerText, DispatcherPriority.Render);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var safeDuration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
        return $"{(int)safeDuration.TotalHours:00}:{safeDuration.Minutes:00}:{safeDuration.Seconds:00}";
    }

    private static SolidColorBrush BrushFromHex(string color)
    {
        return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
    }

    // Rebuilds the overlay background brush from the current theme color and background
    // opacity. The opacity is applied to the brush alpha only, so overlay text stays opaque.
    private void RefreshOverlayBackground()
    {
        OverlayChrome.Background = CreateOverlayBrush(_overlayBackgroundColor, _backgroundOpacity);
        TimerChrome.Background = CreateOverlayBrush(TimerOverlayBackgroundColor, _backgroundOpacity);
    }

    private static LinearGradientBrush CreateOverlayBrush(System.Windows.Media.Color color, double opacity)
    {
        var alpha = (byte)Math.Round(Math.Clamp(opacity, 0.0, 1.0) * 255);
        var top = System.Windows.Media.Color.FromArgb(alpha, color.R, color.G, color.B);
        var bottom = LiftColor(top, 16);
        return new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(0, 1),
            GradientStops =
            {
                new GradientStop(top, 0.0),
                new GradientStop(bottom, 1.0)
            }
        };
    }

    private static System.Windows.Media.Color LiftColor(System.Windows.Media.Color color, byte amount)
    {
        return System.Windows.Media.Color.FromArgb(
            color.A,
            (byte)Math.Min(255, color.R + amount),
            (byte)Math.Min(255, color.G + amount),
            (byte)Math.Min(255, color.B + amount));
    }

    private static string GetApplicationVersionText()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null
            ? "v1.0.0"
            : $"v{version.Major}.{version.Minor}.{version.Build}";
    }
}
