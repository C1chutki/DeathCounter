using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Reflection;
using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.Core.Storage;
using EldenDeathCounter.Interop;

namespace EldenDeathCounter;

public partial class OverlayWindow : Window
{
    private const double CounterBaseFontSize = 26;
    private const double BossBaseFontSize = 20;
    private const double BossBaseLineHeight = 25;
    private const double BossDeathBaseFontSize = 14;
    private const double TimerBaseFontSize = 20;
    private const double BossNameBaseMaxWidth = 620;
    private const double HeaderBaseFontSize = 12;
    private const double ChromeBaseMinWidth = 356;
    private static readonly Thickness ChromeBasePadding = new Thickness(22, 20, 22, 18);

    private readonly DispatcherTimer _bossTimer;
    private string _gameLanguage;
    private ActiveBossState? _activeBoss;
    private bool _isDetectionRunning;

    public OverlayWindow(AppSettings settings)
    {
        InitializeComponent();
        Left = settings.OverlayX;
        Top = settings.OverlayY;
        _gameLanguage = settings.GameLanguage;
        _bossTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _bossTimer.Tick += (_, _) => UpdateBossTimerText();
        VersionTextBlock.Text = GetApplicationVersionText();
        UpdateDetectionState(false);
        ApplyFontScale(settings.OverlayFontScale);
    }

    public void ApplyFontScale(double scale)
    {
        Dispatcher.Invoke(() =>
        {
            var safeScale = Math.Clamp(scale <= 0 ? 1.0 : scale, 0.6, 1.6);
            CounterTextBlock.FontSize = CounterBaseFontSize * safeScale;
            BossTextBlock.FontSize = BossBaseFontSize * safeScale;
            BossTextBlock.LineHeight = BossBaseLineHeight * safeScale;
            BossTextBlock.MaxWidth = BossNameBaseMaxWidth * safeScale;
            BossDeathTextBlock.FontSize = BossDeathBaseFontSize * safeScale;
            TimerTextBlock.FontSize = TimerBaseFontSize * safeScale;
            DetectionStatusTextBlock.FontSize = HeaderBaseFontSize * safeScale;
            TotalDeathsLabelTextBlock.FontSize = HeaderBaseFontSize * safeScale;
            VersionTextBlock.FontSize = HeaderBaseFontSize * safeScale;
            OverlayChrome.MinWidth = ChromeBaseMinWidth * safeScale;
            OverlayChrome.Padding = new Thickness(
                ChromeBasePadding.Left * safeScale,
                ChromeBasePadding.Top * safeScale,
                ChromeBasePadding.Right * safeScale,
                ChromeBasePadding.Bottom * safeScale);
        });
    }

    public void UpdateCount(int count, ActiveBossState? activeBoss = null, string? gameLanguage = null)
    {
        Dispatcher.Invoke(() =>
        {
            if (!string.IsNullOrWhiteSpace(gameLanguage))
            {
                _gameLanguage = gameLanguage;
            }

            CounterTextBlock.Text = DeathCounterText.FormatGlobalCount(count, _gameLanguage);
            _activeBoss = activeBoss;
            if (activeBoss is null)
            {
                BossTextBlock.Text = string.Empty;
                BossDeathTextBlock.Text = string.Empty;
                TimerTextBlock.Text = string.Empty;
                BossPanel.Visibility = Visibility.Collapsed;
                _bossTimer.Stop();
                return;
            }

            BossTextBlock.Text = DeathCounterText.FormatBossOverlayName(activeBoss.Name);
            BossDeathTextBlock.Text = $"{DeathCounterText.FormatDeathLabel(_gameLanguage)}: {activeBoss.DeathCount}";
            BossPanel.Visibility = Visibility.Visible;
            UpdateBossTimerText();
            if (activeBoss.IsTimerRunning && !_bossTimer.IsEnabled)
            {
                _bossTimer.Start();
            }
            else if (!activeBoss.IsTimerRunning)
            {
                _bossTimer.Stop();
            }
        });
    }

    public void UpdateDetectionState(bool isRunning)
    {
        Dispatcher.Invoke(() =>
        {
            _isDetectionRunning = isRunning;
            DetectionStatusTextBlock.Text = isRunning ? "DETECTION RUNNING" : "DETECTION STOPPED";
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
            OverlayChrome.Background = BuildOverlayGradient(theme.OverlayBackground);
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
    }

    private void UpdateBossTimerText()
    {
        if (_activeBoss is null)
        {
            return;
        }

        TimerTextBlock.Text = FormatDuration(_activeBoss.GetElapsedDuration(DateTimeOffset.Now));
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

    private static LinearGradientBrush BuildOverlayGradient(string color)
    {
        var top = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color);
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
