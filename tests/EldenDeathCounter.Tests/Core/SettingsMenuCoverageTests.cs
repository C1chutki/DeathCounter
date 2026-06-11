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
        Assert.Contains("OverlayFontScaleInput", settingsTab);
        Assert.Contains("OverlayBackgroundOpacityInput", settingsTab);
        Assert.Contains("ShowBossTimer", settingsTab);
        Assert.Contains("ShowDetectionStatus", settingsTab);
        Assert.Contains("DetectionEnabledOnStartup", settingsTab);
        Assert.Contains("AutoDetectBossNames", settingsTab);
        Assert.Contains("SelectedDetectionModeValue", settingsTab);
        Assert.Contains("ResetDetectionSettingsCommand", settingsTab);
        Assert.Contains("DetectDeaths", settingsTab);
        Assert.Contains("DetectBossVictories", settingsTab);
        Assert.Contains("DetectionIntervalMsText", settingsTab);
        Assert.Contains("DetectionCooldownSecondsText", settingsTab);
        Assert.Contains("DetectionSensitivityText", settingsTab);
        Assert.Contains("SelectedCaptureTargetValue", settingsTab);
        Assert.Contains("SelectedGameLanguageValue", settingsTab);
        Assert.Contains("CharacterProfileNameText", settingsTab);
        Assert.Contains("ApplyCharacterProfileCommand", settingsTab);
        Assert.Contains("DataFolderPathText", settingsTab);
        Assert.Contains("OpenDataFolderCommand", settingsTab);
        Assert.Contains("ResetProfileSettingsCommand", settingsTab);
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

        // Section headers are localized via {DynamicResource}; assert on the resource keys.
        Assert.Contains("Grid.Row=\"1\"", settingsTab);
        Assert.Contains("Settings_OverlaySection", settingsTab);
        Assert.Contains("Settings_DetectionSection", settingsTab);
        Assert.Contains("Grid.Row=\"2\"", settingsTab);
        Assert.Contains("Settings_CharacterSection", settingsTab);
        Assert.Contains("Settings_LanguageSection", settingsTab);
        Assert.Contains("Settings_HotkeysSection", settingsTab);
        Assert.Contains("Grid.Row=\"3\"", settingsTab);
        Assert.Contains("Settings_ProfileSection", settingsTab);
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
        Assert.Contains("Settings_OverlaySection", firstSettingsRow);
        Assert.Contains("Settings_DetectionSection", firstSettingsRow);
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
    public void DetectionConfigurationExplainsEditableValueRanges()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var settingsTab = Regex.Match(
            xaml,
            """<TabItem Header="Settings">(?<content>[\s\S]*?)</TabItem>""",
            RegexOptions.CultureInvariant).Groups["content"].Value;

        Assert.Contains("Settings_DetectionIntervalHint", settingsTab);
        Assert.Contains("Settings_CooldownHint", settingsTab);
        Assert.Contains("Settings_SensitivityHint", settingsTab);
    }

    [Fact]
    public void SettingsNumericTextBoxesDeclareBoundsAndUseNumericInputHandlers()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var settingsTab = Regex.Match(
            xaml,
            """<TabItem Header="Settings">(?<content>[\s\S]*?)</TabItem>""",
            RegexOptions.CultureInvariant).Groups["content"].Value;
        var numericBindings = new[]
        {
            "OverlayXText",
            "OverlayYText",
            "OverlayFontScaleInput",
            "OverlayBackgroundOpacityInput",
            "DetectionIntervalMsText",
            "DetectionCooldownSecondsText",
            "DetectionSensitivityText"
        };

        foreach (var binding in numericBindings)
        {
            var textBox = Regex.Match(
                settingsTab,
                "<TextBox[^>]*Text=\"\\{Binding " + Regex.Escape(binding) + "[^\"]*\"[^>]*/>",
                RegexOptions.CultureInvariant).Value;

            Assert.NotEmpty(textBox);
            Assert.Contains("PreviewTextInput=\"NumericBox_PreviewTextInput\"", textBox);
            Assert.Contains("DataObject.Pasting=\"NumericBox_Pasting\"", textBox);
            Assert.Contains("LostFocus=\"NumericBox_LostFocus\"", textBox);
            Assert.Contains("Tag=\"", textBox);
        }
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

    [Fact]
    public void BossesTabExposesManualHistoryAddCommand()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var bossesTab = Regex.Match(
            xaml,
            """<TabItem Header="Bosses">(?<content>[\s\S]*?)</TabItem>""",
            RegexOptions.CultureInvariant).Groups["content"].Value;

        Assert.NotEmpty(bossesTab);
        Assert.Contains("Bosses_AddRecord", bossesTab);
        Assert.Contains("OpenAddBossHistoryEditorCommand", bossesTab);
    }

    [Fact]
    public void BossHistoryEditorSupportsAddAndEditModes()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var editor = Regex.Match(
            xaml,
            "Visibility=\"\\{Binding IsBossHistoryEditorOpen, Converter=\\{StaticResource BooleanToVisibilityConverter\\}\\}\">(?<content>[\\s\\S]*?)</Grid>\\s*</Window>",
            RegexOptions.CultureInvariant).Groups["content"].Value;

        Assert.NotEmpty(editor);
        Assert.Contains("BossHistoryEditorTitle", editor);
        Assert.Contains("CanDeleteBossHistoryEntry", editor);
    }

    [Fact]
    public void StatsTabShowsSummaryAndExportCommand()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var statsTab = Regex.Match(
            xaml,
            """<TabItem Header="Stats">(?<content>[\s\S]*?)</TabItem>""",
            RegexOptions.CultureInvariant).Groups["content"].Value;

        Assert.NotEmpty(statsTab);
        Assert.Contains("Stats_TotalDeaths", statsTab);
        Assert.Contains("Stats_DeathsToday", statsTab);
        Assert.Contains("Stats_SessionDeaths", statsTab);
        Assert.Contains("Stats_DeathsPerHour", statsTab);
        Assert.Contains("Stats_ActiveBoss", statsTab);
        Assert.Contains("Stats_BestBoss", statsTab);
        Assert.Contains("Stats_HardestBoss", statsTab);
        Assert.Contains("Stats_LongestBoss", statsTab);
        Assert.Contains("Stats_RecentEvents", statsTab);
        Assert.Contains("ExportProfileCommand", statsTab);
        Assert.Contains("StatsRecentEvents", statsTab);
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
