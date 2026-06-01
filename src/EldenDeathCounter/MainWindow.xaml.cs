using System.Windows;
using System.Windows.Media;
using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.ViewModels;

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

    private void NavigationRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.RadioButton { Tag: string tag } && int.TryParse(tag, out var index))
        {
            MainTabs.SelectedIndex = index;
        }
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

    private async Task ApplyGameProfileAsync(AppGameProfile profile)
    {
        if (await _viewModel.SwitchGameProfileAsync(profile))
        {
            ApplyGameTheme(profile.Theme);
        }
    }

    private void ApplyGameTheme(AppGameTheme theme)
    {
        Title = theme.Title;
        AppTitleTextBlock.Text = theme.Title;
        Background = BrushFromHex(theme.Neutral);
        MainTabs.Background = BrushFromHex(theme.Neutral);
        SidebarBorder.Background = BrushFromHex(theme.Secondary);
        SidebarBorder.BorderBrush = BrushFromHex(theme.Border);
        HeaderBorder.Background = BrushFromHex(theme.Panel);
        HeaderBorder.BorderBrush = BrushFromHex(theme.Border);
        BottomStatusBar.Background = BrushFromHex(theme.Panel);
        BottomStatusBar.BorderBrush = BrushFromHex(theme.Border);

        SetResourceBrush("Gold", theme.Primary);
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

        DarkSouls1Button.Background = BrushFromHex(AppGameTheme.DarkSouls1.Primary);
        DarkSouls1Button.BorderBrush = BrushFromHex(AppGameTheme.DarkSouls1.Primary);
        DarkSouls1Button.Foreground = BrushFromHex(AppGameTheme.DarkSouls1.Neutral);

        DarkSouls2Button.Background = BrushFromHex(AppGameTheme.DarkSouls2.Primary);
        DarkSouls2Button.BorderBrush = BrushFromHex(AppGameTheme.DarkSouls2.Primary);
        DarkSouls2Button.Foreground = BrushFromHex(AppGameTheme.DarkSouls2.Neutral);

        DarkSouls3Button.Background = BrushFromHex(AppGameTheme.DarkSouls3.Primary);
        DarkSouls3Button.BorderBrush = BrushFromHex(AppGameTheme.DarkSouls3.Primary);
        DarkSouls3Button.Foreground = BrushFromHex(AppGameTheme.DarkSouls3.Neutral);

        _viewModel.ApplyGameTheme(theme);
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
}
