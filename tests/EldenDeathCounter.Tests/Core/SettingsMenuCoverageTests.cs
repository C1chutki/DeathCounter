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
        Assert.Contains("DetectionPhrasesText", settingsTab);
        Assert.Contains("BossVictoryPhrasesText", settingsTab);
        Assert.Contains("ManualAddHotkeyText", settingsTab);
        Assert.Contains("ManualSubtractHotkeyText", settingsTab);
        Assert.Contains("ManualBossDefeatedHotkeyText", settingsTab);
        Assert.Contains("OverlayToggleHotkeyText", settingsTab);
    }

    [Fact]
    public void SettingsDetectionPhrasesTextBoxUsesDarkTextBoxStyle()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var settingsTab = Regex.Match(
            xaml,
            """<TabItem Header="Settings">(?<content>[\s\S]*?)</TabItem>""",
            RegexOptions.CultureInvariant).Groups["content"].Value;
        var detectionPhrasesTextBox = Regex.Match(
            settingsTab,
            """<TextBox[^>]*DetectionPhrasesText[^>]*/>""",
            RegexOptions.CultureInvariant).Value;

        Assert.NotEmpty(detectionPhrasesTextBox);
        Assert.DoesNotContain("Background=\"#F3F3F3\"", detectionPhrasesTextBox);
        Assert.DoesNotContain("Foreground=\"#222222\"", detectionPhrasesTextBox);
    }

    [Fact]
    public void SettingsBossVictoryPhrasesTextBoxUsesDarkTextBoxStyle()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var settingsTab = Regex.Match(
            xaml,
            """<TabItem Header="Settings">(?<content>[\s\S]*?)</TabItem>""",
            RegexOptions.CultureInvariant).Groups["content"].Value;
        var bossVictoryPhrasesTextBox = Regex.Match(
            settingsTab,
            """<TextBox[^>]*BossVictoryPhrasesText[^>]*/>""",
            RegexOptions.CultureInvariant).Value;

        Assert.NotEmpty(bossVictoryPhrasesTextBox);
        Assert.DoesNotContain("Background=\"#F3F3F3\"", bossVictoryPhrasesTextBox);
        Assert.DoesNotContain("Foreground=\"#222222\"", bossVictoryPhrasesTextBox);
    }

    [Fact]
    public void DetectionConfigurationPanelScrollsWhenWindowIsShort()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var detectionTab = Regex.Match(
            xaml,
            """<TabItem Header="Detection">(?<content>[\s\S]*?)</TabItem>""",
            RegexOptions.CultureInvariant).Groups["content"].Value;
        var configurationPanel = Regex.Match(
            detectionTab,
            """<Border Style="\{StaticResource PanelBorder\}">(?<content>[\s\S]*?)<Border Grid\.Column="2" Style="\{StaticResource PanelBorder\}">""",
            RegexOptions.CultureInvariant).Groups["content"].Value;

        Assert.NotEmpty(configurationPanel);
        Assert.Contains("""<ScrollViewer VerticalScrollBarVisibility="Auto">""", configurationPanel);
    }

    [Fact]
    public void DetectionConfigurationShowsCaptureTargetSelector()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var detectionTab = Regex.Match(
            xaml,
            """<TabItem Header="Detection">(?<content>[\s\S]*?)</TabItem>""",
            RegexOptions.CultureInvariant).Groups["content"].Value;

        Assert.Contains("CaptureTargetOptions", detectionTab);
        Assert.Contains("SelectedCaptureTargetValue", detectionTab);
        Assert.DoesNotContain("EldenRing.exe (Main Window)", detectionTab);
    }

    [Fact]
    public void CaptureTargetSelectorsUseDarkDropdownStyle()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var captureTargetComboBoxes = Regex.Matches(
            xaml,
            """<ComboBox[^>]*CaptureTargetOptions[\s\S]*?/>""",
            RegexOptions.CultureInvariant);

        Assert.Equal(2, captureTargetComboBoxes.Count);
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
