using System.Text.RegularExpressions;

public sealed class SettingsMenuCoverageTests
{
    [Fact]
    public void SettingsTabExposesAllUserEditableAppSettings()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var settingsTab = Regex.Match(
            xaml,
            """<TabItem Header="Settings">(?<content>[\s\S]*?)</TabItem>""",
            RegexOptions.CultureInvariant).Groups["content"].Value;

        Assert.NotEmpty(settingsTab);
        Assert.Contains("OverlayEnabled", settingsTab);
        Assert.Contains("OverlayXText", settingsTab);
        Assert.Contains("OverlayYText", settingsTab);
        Assert.Contains("DetectionEnabledOnStartup", settingsTab);
        Assert.Contains("AutoDetectBossNames", settingsTab);
        Assert.Contains("DetectionIntervalMsText", settingsTab);
        Assert.Contains("DetectionCooldownSecondsText", settingsTab);
        Assert.Contains("DetectionSensitivityText", settingsTab);
        Assert.Contains("SelectedCaptureTargetValue", settingsTab);
        Assert.Contains("SelectedGameLanguageValue", settingsTab);
        Assert.Contains("DataFolderPathText", settingsTab);
        Assert.Contains("ManualAddHotkeyText", settingsTab);
        Assert.Contains("ManualSubtractHotkeyText", settingsTab);
        Assert.Contains("ManualBossDefeatedHotkeyText", settingsTab);
        Assert.Contains("OverlayToggleHotkeyText", settingsTab);
        Assert.DoesNotContain("DetectionPhrasesText", settingsTab);
        Assert.DoesNotContain("BossVictoryPhrasesText", settingsTab);
    }

    [Fact]
    public void SettingsTabUsesTwoThreeOnePanelLayout()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var settingsTab = Regex.Match(
            xaml,
            """<TabItem Header="Settings">(?<content>[\s\S]*?)</TabItem>""",
            RegexOptions.CultureInvariant).Groups["content"].Value;

        Assert.Contains("Grid.Row=\"1\"", settingsTab);
        Assert.Contains("| OVERLAY", settingsTab);
        Assert.Contains("| DETECTION", settingsTab);
        Assert.Contains("Grid.Row=\"2\"", settingsTab);
        Assert.Contains("| CHARACTER &amp; SAVE GAME", settingsTab);
        Assert.Contains("| LANGUAGE", settingsTab);
        Assert.Contains("| HOTKEYS", settingsTab);
        Assert.Contains("Grid.Row=\"3\"", settingsTab);
        Assert.Contains("| PROFILE / SAVE GAME", settingsTab);
    }

    [Fact]
    public void SettingsDetectionPanelLivesBesideOverlay()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var settingsTab = Regex.Match(
            xaml,
            """<TabItem Header="Settings">(?<content>[\s\S]*?)</TabItem>""",
            RegexOptions.CultureInvariant).Groups["content"].Value;
        var firstSettingsRow = Regex.Match(
            settingsTab,
            "<Grid Grid.Row=\"1\"(?<content>[\\s\\S]*?)</Grid>\\s*<Grid Grid.Row=\"2\"",
            RegexOptions.CultureInvariant).Groups["content"].Value;

        Assert.NotEmpty(firstSettingsRow);
        Assert.Contains("| OVERLAY", firstSettingsRow);
        Assert.Contains("| DETECTION", firstSettingsRow);
        Assert.Contains("CaptureTargetOptions", firstSettingsRow);
    }

    [Fact]
    public void DetectionConfigurationShowsCaptureTargetSelector()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var settingsTab = Regex.Match(
            xaml,
            """<TabItem Header="Settings">(?<content>[\s\S]*?)</TabItem>""",
            RegexOptions.CultureInvariant).Groups["content"].Value;

        Assert.Contains("CaptureTargetOptions", settingsTab);
        Assert.Contains("SelectedCaptureTargetValue", settingsTab);
        Assert.DoesNotContain("EldenRing.exe (Main Window)", settingsTab);
    }

    [Fact]
    public void CaptureTargetSelectorsUseDarkDropdownStyle()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var captureTargetComboBoxes = Regex.Matches(
            xaml,
            """<ComboBox[^>]*CaptureTargetOptions[\s\S]*?/>""",
            RegexOptions.CultureInvariant);

        Assert.Single(captureTargetComboBoxes);
        Assert.Contains("x:Key=\"DarkComboBox\"", xaml);
        Assert.Contains("x:Key=\"DarkComboBoxItem\"", xaml);
        Assert.Contains("x:Name=\"ToggleBorder\"", xaml);
        Assert.Contains("Background=\"#050505\"", xaml);
        foreach (Match comboBox in captureTargetComboBoxes)
        {
            Assert.Contains("Style=\"{StaticResource DarkComboBox}\"", comboBox.Value);
        }
    }

    [Fact]
    public void BossHistoryCardsDisplayKillNumber()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var defeatedBossesTemplate = Regex.Match(
            xaml,
            """<ItemsControl ItemsSource="\{Binding DefeatedBosses\}">(?<content>[\s\S]*?)</ItemsControl>""",
            RegexOptions.CultureInvariant).Groups["content"].Value;

        Assert.NotEmpty(defeatedBossesTemplate);
        Assert.Contains("NumberText", defeatedBossesTemplate);
    }

    private static string GetMainWindowXamlPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EldenDeathCounter.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "src", "EldenDeathCounter", "MainWindow.xaml");
    }
}
