using System.Xml.Linq;
using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class AppProjectAssetTests
{
    [Fact]
    public void AppProjectCopiesBossListsAndMultiBossReferenceImages()
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "EldenDeathCounter", "EldenDeathCounter.csproj"));
        var project = XDocument.Load(projectPath);
        var includes = project
            .Descendants("Content")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // The wildcard Assets include copies the boss lists and reference images to the build output.
        Assert.Contains(@"..\..\Assets\*.*", includes);

        var assetRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!, "..", "..", "Assets"));
        foreach (var file in new[]
                 {
                     "ENG_BossList.txt", "PL_BossList.txt",
                     "ENG_DoubleBoss.jpg", "ENG_DoubleBoss_02.jpg",
                     "ENG_TrippleBoss.jpg", "ENG_TrippleBoss_02.jpg",
                 })
        {
            Assert.True(File.Exists(Path.Combine(assetRoot, file)), $"Missing asset: {file}");
        }

        // Both boss lists must provide a source of truth for the matcher.
        Assert.True(BossNameMatcher.ParseList(File.ReadAllLines(Path.Combine(assetRoot, "ENG_BossList.txt"))).Count > 100);
        Assert.True(BossNameMatcher.ParseList(File.ReadAllLines(Path.Combine(assetRoot, "PL_BossList.txt"))).Count > 100);
    }

    [Fact]
    public void AppProjectCopiesAllDefaultDeathScreenTemplates()
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "EldenDeathCounter",
            "EldenDeathCounter.csproj"));
        var project = XDocument.Load(projectPath);
        var includes = project
            .Descendants("Content")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(@"..\..\Assets\*.*", includes);

        var assetRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!, "..", "..", "Assets"));
        Assert.True(File.Exists(Path.Combine(assetRoot, "PL_Death_Screen.png")));
        Assert.True(File.Exists(Path.Combine(assetRoot, "PL_Death_Screen_v2.jpg")));
        Assert.True(File.Exists(Path.Combine(assetRoot, "PL_Death_Screen_v3.jpg")));
        Assert.True(File.Exists(Path.Combine(assetRoot, "PL_Win_screen.jpg")));
        Assert.True(File.Exists(Path.Combine(assetRoot, "ENG_Death_Screen.jpg")));
        Assert.True(File.Exists(Path.Combine(assetRoot, "ENG_Death_Screen_v2.jpg")));
        Assert.True(File.Exists(Path.Combine(assetRoot, "ENG_Win_Screen.jpg")));
    }

    [Fact]
    public void DeathTemplateLoaderIncludesAllEnglishDeathScreenTemplates()
    {
        var detectorPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "EldenDeathCounter",
            "Detection",
            "TemplateDeathTextImageSignalDetector.cs"));
        var detectorCode = File.ReadAllText(detectorPath);

        Assert.Contains("\"ENG_Death_Screen.jpg\"", detectorCode, StringComparison.Ordinal);
        Assert.Contains("\"ENG_Death_Screen_v2.jpg\"", detectorCode, StringComparison.Ordinal);
    }

    [Fact]
    public void CoreProjectReferencesOpenCvTemplateMatchingRuntime()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));
        var projectPath = Path.Combine(projectRoot, "src", "EldenDeathCounter.Core", "EldenDeathCounter.Core.csproj");
        var project = XDocument.Load(projectPath);
        var packageNames = project
            .Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        Assert.Contains("OpenCvSharp4", packageNames);
        Assert.Contains("OpenCvSharp4.runtime.win", packageNames);
    }

    [Fact]
    public void OverlayUsesLocalizedDeathCounterText()
    {
        var appProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "EldenDeathCounter"));
        var overlayXaml = File.ReadAllText(Path.Combine(appProjectPath, "OverlayWindow.xaml"));
        var overlayCode = File.ReadAllText(Path.Combine(appProjectPath, "OverlayWindow.xaml.cs"));
        var formatterCode = File.ReadAllText(Path.Combine(appProjectPath, "..", "EldenDeathCounter.Core", "Configuration", "DeathCounterText.cs"));

        Assert.Contains("Śmierci: 0", overlayXaml, StringComparison.Ordinal);
        Assert.Contains("DeathCounterText.FormatGlobalCount(count, _appLanguage)", overlayCode, StringComparison.Ordinal);
        Assert.Contains("\"Deaths\"", formatterCode, StringComparison.Ordinal);
        Assert.Contains("\\u015Amierci", formatterCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Ĺ", overlayXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Ĺ", overlayCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Ĺ", formatterCode, StringComparison.Ordinal);
    }

    [Fact]
    public void DashboardMonitorStatusUsesSingleStatefulToggleButton()
    {
        var xamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "EldenDeathCounter",
            "MainWindow.xaml"));
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("MonitorToggleButton", xaml, StringComparison.Ordinal);
        Assert.Contains("ToggleDetectionCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("IsDetectionRunning", xaml, StringComparison.Ordinal);
        Assert.Contains("Value=\"{DynamicResource Monitor_Start}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Value=\"{DynamicResource Monitor_Stop}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("#17301F", xaml, StringComparison.Ordinal);
        Assert.Contains("#3A1010", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void DashboardTabScrollsInsteadOfClippingBindingReminders()
    {
        var xamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "EldenDeathCounter",
            "MainWindow.xaml"));
        var xaml = File.ReadAllText(xamlPath);
        var dashboardStart = xaml.IndexOf("<TabItem Header=\"Dashboard\">", StringComparison.Ordinal);
        Assert.True(dashboardStart >= 0);

        var dashboardEnd = xaml.IndexOf("<TabItem Header=\"Detection\">", dashboardStart, StringComparison.Ordinal);
        Assert.True(dashboardEnd > dashboardStart);
        var dashboardTab = xaml[dashboardStart..dashboardEnd];

        Assert.Contains("<ScrollViewer VerticalScrollBarVisibility=\"Auto\">", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Dash_BindingReminders", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Dash_BossDefeatedReminder", dashboardTab, StringComparison.Ordinal);
    }

    [Fact]
    public void BossesTabUsesEditableSearchBoxBoundToBossSearchText()
    {
        var xamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "EldenDeathCounter",
            "MainWindow.xaml"));
        var xaml = File.ReadAllText(xamlPath);
        var bossesStart = xaml.IndexOf("<TabItem Header=\"Bosses\">", StringComparison.Ordinal);
        Assert.True(bossesStart >= 0);

        var bossesEnd = xaml.IndexOf("<TabItem Header=\"Settings\">", bossesStart, StringComparison.Ordinal);
        Assert.True(bossesEnd > bossesStart);
        var bossesTab = xaml[bossesStart..bossesEnd];

        Assert.Contains("<TextBox Text=\"{Binding BossSearchText, UpdateSourceTrigger=PropertyChanged}\"", bossesTab, StringComparison.Ordinal);
        Assert.Contains("Bosses_SearchPlaceholder", bossesTab, StringComparison.Ordinal);
    }

    [Fact]
    public void BossesTabSeparatesDefeatedBossAttemptsDurationAndRecordedTime()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));
        var xamlPath = Path.Combine(projectRoot, "src", "EldenDeathCounter", "MainWindow.xaml");
        var viewModelPath = Path.Combine(projectRoot, "src", "EldenDeathCounter", "ViewModels", "MainWindowViewModel.cs");
        var xaml = File.ReadAllText(xamlPath);
        var viewModel = File.ReadAllText(viewModelPath);
        var bossesStart = xaml.IndexOf("<TabItem Header=\"Bosses\">", StringComparison.Ordinal);
        Assert.True(bossesStart >= 0);

        var bossesEnd = xaml.IndexOf("<TabItem Header=\"Settings\">", bossesStart, StringComparison.Ordinal);
        Assert.True(bossesEnd > bossesStart);
        var bossesTab = xaml[bossesStart..bossesEnd];

        Assert.Contains("Binding FightDurationText", bossesTab, StringComparison.Ordinal);
        Assert.Contains("Binding RecordedText", bossesTab, StringComparison.Ordinal);
        Assert.DoesNotContain("| Recorded", viewModel, StringComparison.Ordinal);
        Assert.Contains("string FightDurationText", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void DetectionTabUsesSingleStatefulToggleButton()
    {
        var xamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "EldenDeathCounter",
            "MainWindow.xaml"));
        var xaml = File.ReadAllText(xamlPath);
        var detectionHeaderStart = xaml.IndexOf("Detect_SystemEngine", StringComparison.Ordinal);
        Assert.True(detectionHeaderStart >= 0);

        var detectionHeaderEnd = xaml.IndexOf("<Border Grid.Row=\"1\"", detectionHeaderStart, StringComparison.Ordinal);
        Assert.True(detectionHeaderEnd > detectionHeaderStart);
        var detectionHeader = xaml[detectionHeaderStart..detectionHeaderEnd];

        Assert.Contains("ToggleDetectionCommand", detectionHeader, StringComparison.Ordinal);
        Assert.Contains("MonitorToggleButton", detectionHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("StopDetectionCommand", detectionHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("STOP ENGINE", detectionHeader, StringComparison.Ordinal);
    }

    [Fact]
    public void BottomStatusBarUsesCompactSegmentedGameHudStyling()
    {
        var xamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "EldenDeathCounter",
            "MainWindow.xaml"));
        var xaml = File.ReadAllText(xamlPath);
        var statusBarStart = xaml.IndexOf("x:Name=\"BottomStatusBar\"", StringComparison.Ordinal);
        Assert.True(statusBarStart >= 0);

        var statusBarEnd = xaml.IndexOf("</Border>", statusBarStart, StringComparison.Ordinal);
        Assert.True(statusBarEnd > statusBarStart);
        var statusBar = xaml[statusBarStart..statusBarEnd];

        Assert.DoesNotContain("Grid.Column=\"1\"", statusBar, StringComparison.Ordinal);
        Assert.Contains("Grid.ColumnSpan=\"2\"", statusBar, StringComparison.Ordinal);
        Assert.Contains("Background=\"{DynamicResource StatusBarSurface}\"", statusBar, StringComparison.Ordinal);
        Assert.Contains("BorderBrush=\"{DynamicResource Separator}\"", statusBar, StringComparison.Ordinal);
        Assert.Contains("Value=\"{DynamicResource AppFontFamily}\"", statusBar, StringComparison.Ordinal);
        Assert.Contains("Value=\"12\"", statusBar, StringComparison.Ordinal);
        Assert.Contains("Value=\"Bold\"", statusBar, StringComparison.Ordinal);
        Assert.Contains("StatusDeathCountText", statusBar, StringComparison.Ordinal);
        Assert.Contains("StatusOverlayStateText", statusBar, StringComparison.Ordinal);
        Assert.Contains("StatusDetectionStateText", statusBar, StringComparison.Ordinal);
        Assert.Contains("Foreground=\"{DynamicResource Gold}\"", statusBar, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowThemeApplicationDoesNotMutateResourceBrushColors()
    {
        var codePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "EldenDeathCounter",
            "MainWindow.xaml.cs"));
        var code = File.ReadAllText(codePath);

        Assert.DoesNotContain(".Color = ColorFromHex", code, StringComparison.Ordinal);
        Assert.Contains("Resources[resourceKey] = BrushFromHex(color)", code, StringComparison.Ordinal);
    }

    [Fact]
    public void AppShutsDownWhenMainWindowCloses()
    {
        var xamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "EldenDeathCounter",
            "App.xaml"));
        var appXaml = File.ReadAllText(xamlPath);

        Assert.Contains("ShutdownMode=\"OnMainWindowClose\"", appXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void AppGuardsAgainstMultipleInstances()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EldenDeathCounter.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var appCodePath = Path.Combine(directory.FullName, "src", "EldenDeathCounter", "App.xaml.cs");
        var appCode = File.ReadAllText(appCodePath);

        Assert.Contains("Mutex", appCode, StringComparison.Ordinal);
        Assert.Contains("EldenDeathCounter.SingleInstance", appCode, StringComparison.Ordinal);
        Assert.Contains("Another EldenDeathCounter instance is already running.", appCode, StringComparison.Ordinal);
    }
}
