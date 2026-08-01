using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Input;
using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.ViewModels;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace EldenDeathCounter;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private bool _shutdownStarted;
    private bool _shutdownComplete;

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
        ApplyGameTheme(AppGameProfile.EldenRing.Theme);
        UpdateActiveGamePill(AppGameProfile.EldenRing);
        SetSectionTitle(0);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _viewModel.AttachWindow(this);
    }

    protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Run graceful async shutdown while the dispatcher is still pumping, then close for real.
        // Cancelling the first close keeps the message pump alive so UI-affined continuations from
        // the detection/save tasks can resume, avoiding the sync-over-async deadlock that left the
        // process running in the background.
        if (_shutdownComplete)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        if (_shutdownStarted)
        {
            return;
        }

        _shutdownStarted = true;
        await _viewModel.ShutdownAsync();
        _shutdownComplete = true;
        Close();
    }

    private void HotkeyBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox box)
        {
            return;
        }

        // Alt-combinations arrive as Key.System with the real key in SystemKey.
        var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;

        // Let Tab fall through so the field can still be left via keyboard navigation.
        if (key == System.Windows.Input.Key.Tab)
        {
            return;
        }

        e.Handled = true;

        // Escape abandons the capture without changing the existing binding.
        if (key == System.Windows.Input.Key.Escape)
        {
            System.Windows.Input.Keyboard.ClearFocus();
            return;
        }

        var hotkey = Hotkeys.HotkeyCapture.TryBuild(key, System.Windows.Input.Keyboard.Modifiers);
        if (hotkey is not null)
        {
            // Two-way binding with UpdateSourceTrigger=PropertyChanged pushes this to the view model.
            box.Text = hotkey;
            box.CaretIndex = box.Text.Length;
        }
    }

    private void NumericBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not WpfTextBox box || !TryGetNumericBounds(box, out var bounds))
        {
            return;
        }

        e.Handled = !IsPotentialNumericText(GetTextAfterInput(box, e.Text), bounds);
    }

    private void NumericBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not WpfTextBox box || !TryGetNumericBounds(box, out var bounds))
        {
            return;
        }

        if (!e.DataObject.GetDataPresent(System.Windows.DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var pasted = e.DataObject.GetData(System.Windows.DataFormats.Text) as string ?? string.Empty;
        if (!IsPotentialNumericText(GetTextAfterInput(box, pasted), bounds))
        {
            e.CancelCommand();
        }
    }

    private void NumericBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfTextBox box || !TryGetNumericBounds(box, out var bounds))
        {
            return;
        }

        if (!double.TryParse(NormalizeNumberText(box.Text), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            value = bounds.Min;
        }

        var clamped = Math.Clamp(value, bounds.Min, bounds.Max);
        box.Text = bounds.Decimals == 0
            ? Math.Round(clamped).ToString("0", CultureInfo.InvariantCulture)
            : clamped.ToString("0." + new string('0', bounds.Decimals), CultureInfo.InvariantCulture);
        box.CaretIndex = box.Text.Length;
    }

    private static string GetTextAfterInput(WpfTextBox box, string input)
    {
        var start = Math.Min(box.SelectionStart, box.Text.Length);
        var length = Math.Min(box.SelectionLength, box.Text.Length - start);
        return box.Text.Remove(start, length).Insert(start, input);
    }

    private static bool IsPotentialNumericText(string text, NumericBounds bounds)
    {
        var normalized = NormalizeNumberText(text);
        if (string.IsNullOrEmpty(normalized))
        {
            return true;
        }

        if (normalized == "-" && bounds.Min < 0)
        {
            return true;
        }

        if (bounds.Decimals == 0 && normalized.Contains('.', StringComparison.Ordinal))
        {
            return false;
        }

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    private static string NormalizeNumberText(string text)
    {
        return text.Trim().Replace(',', '.');
    }

    private static bool TryGetNumericBounds(WpfTextBox box, out NumericBounds bounds)
    {
        bounds = default;
        var parts = (box.Tag as string)?.Split('|');
        if (parts?.Length != 3 ||
            !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var min) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var max) ||
            !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var decimals))
        {
            return false;
        }

        bounds = new NumericBounds(min, max, decimals);
        return true;
    }

    private void NavigationRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.RadioButton { Tag: string tag } && int.TryParse(tag, out var index))
        {
            if (MainTabs is not null)
            {
                MainTabs.SelectedIndex = index;
            }

            SetSectionTitle(index);
        }
    }

    private void SetSectionTitle(int index)
    {
        if (SectionTitleText is null)
        {
            return;
        }

        var key = index switch
        {
            1 => "Section_Detection",
            2 => "Section_Bosses",
            3 => "Section_Stats",
            4 => "Section_Settings",
            _ => "Section_Dashboard",
        };

        SectionTitleText.Text = TryFindResource(key) as string ?? string.Empty;

        // Header chrome stays transparent; the bottom rule follows the active game accent.
    }

    private async void DarkSouls1Button_Click(object sender, RoutedEventArgs e)
    {
        await ApplyGameProfileAsync(AppGameProfile.DarkSouls1);
    }

    private async void DarkSouls2Button_Click(object sender, RoutedEventArgs e)
    {
        await ApplyGameProfileAsync(AppGameProfile.DarkSouls2);
    }

    private async void DarkSouls3Button_Click(object sender, RoutedEventArgs e)
    {
        await ApplyGameProfileAsync(AppGameProfile.DarkSouls3);
    }

    private async void EldenRingButton_Click(object sender, RoutedEventArgs e)
    {
        await ApplyGameProfileAsync(AppGameProfile.EldenRing);
    }

    private async void SekiroButton_Click(object sender, RoutedEventArgs e)
    {
        await ApplyGameProfileAsync(AppGameProfile.Sekiro);
    }

    private async Task ApplyGameProfileAsync(AppGameProfile profile)
    {
        if (await _viewModel.SwitchGameProfileAsync(profile))
        {
            ApplyGameTheme(profile.Theme);
            UpdateActiveGamePill(profile);
        }
    }

    private void UpdateActiveGamePill(AppGameProfile profile)
    {
        if (EldenRingButton is null)
        {
            return;
        }

        var active = (System.Windows.Style)FindResource("GamePillActive");
        var inactive = (System.Windows.Style)FindResource("GamePill");

        EldenRingButton.Style = profile.Id == AppGameProfile.EldenRing.Id ? active : inactive;
        DarkSouls1Button.Style = profile.Id == AppGameProfile.DarkSouls1.Id ? active : inactive;
        DarkSouls2Button.Style = profile.Id == AppGameProfile.DarkSouls2.Id ? active : inactive;
        DarkSouls3Button.Style = profile.Id == AppGameProfile.DarkSouls3.Id ? active : inactive;
        SekiroButton.Style = profile.Id == AppGameProfile.Sekiro.Id ? active : inactive;
    }

    private void ApplyGameTheme(AppGameTheme theme)
    {
        Title = theme.Title;
        Background = BuildBackgroundBrush(theme);
        MainTabs.Background = System.Windows.Media.Brushes.Transparent;
        SidebarBorder.Background = BrushFromHex(theme.Secondary);
        SidebarBorder.BorderBrush = BrushFromHex(theme.Border);
        HeaderBorder.BorderBrush = BrushFromHex(theme.Primary);

        SetResourceBrush("Gold", theme.Primary);
        SetResourceBrush("Gold2", theme.Primary);
        SetResourceBrush("Ink", theme.Ink);
        SetResourceBrush("MutedInk", theme.MutedInk);
        SetResourceBrush("Panel", theme.Panel);
        SetResourceBrush("PanelAlt", theme.PanelAlt);
        SetResourceBrush("Secondary", theme.Secondary);
        SetResourceBrush("Neutral", theme.Neutral);
        SetResourceBrush("HeaderSurface", theme.Panel);
        SetResourceBrush("HoverSurface", theme.PanelAlt);
        SetResourceBrush("AccentBorder", theme.Primary);
        SetResourceBrush("Separator", theme.Border);
        SetResourceBrush("ThumbSurface", theme.Primary);
        SetResourceBrush("HistoryCard", theme.PanelAlt);
        SetResourceBrush("DimBorder", theme.Border);
        SetResourceBrush("StatusBarSurface", theme.Panel);
        SetResourceBrush("BorderGold", theme.Border);

        // Accent variants derived from the primary so every game recolors its chrome
        // (emblem, rail nav icons, active game pill, dashboard circle buttons).
        Resources["RailActiveFill"] = new SolidColorBrush(Darken(theme.Primary, 0.18));
        Resources["CircleRestBorder"] = new SolidColorBrush(Darken(theme.Primary, 0.50));
        Resources["GoldPill"] = BuildPrimaryPill(theme.Primary);
        Resources["DashboardParticleGold"] = new SolidColorBrush(Darken(theme.Primary, 0.88));

        _viewModel.ApplyGameTheme(theme);
    }

    private static System.Windows.Media.LinearGradientBrush BuildPrimaryPill(string primary)
    {
        var brush = new System.Windows.Media.LinearGradientBrush(
            ColorFromHex(primary),
            Darken(primary, 0.70),
            90);
        brush.Freeze();
        return brush;
    }

    private static System.Windows.Media.Color Darken(string hex, double factor)
    {
        var c = ColorFromHex(hex);
        return System.Windows.Media.Color.FromRgb(
            (byte)(c.R * factor),
            (byte)(c.G * factor),
            (byte)(c.B * factor));
    }

    private static System.Windows.Media.RadialGradientBrush BuildBackgroundBrush(AppGameTheme theme)
    {
        var brush = new System.Windows.Media.RadialGradientBrush
        {
            GradientOrigin = new System.Windows.Point(0.5, -0.1),
            Center = new System.Windows.Point(0.5, -0.1),
            RadiusX = 1.5,
            RadiusY = 1.2,
        };
        brush.GradientStops.Add(new System.Windows.Media.GradientStop(ColorFromHex(theme.PanelAlt), 0));
        brush.GradientStops.Add(new System.Windows.Media.GradientStop(ColorFromHex(theme.Neutral), 0.65));
        brush.Freeze();
        return brush;
    }

    private void SetResourceBrush(string resourceKey, string color)
    {
        Resources[resourceKey] = BrushFromHex(color);
    }

    private static SolidColorBrush BrushFromHex(string color)
    {
        return new SolidColorBrush(ColorFromHex(color));
    }

    private static System.Windows.Media.Color ColorFromHex(string color)
    {
        return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color);
    }

    private readonly record struct NumericBounds(double Min, double Max, int Decimals);
}
