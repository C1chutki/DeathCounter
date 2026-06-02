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
    public void DashboardUsesReferenceCounterStageLayout()
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

        Assert.DoesNotContain("<ScrollViewer", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DashboardStage\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Text=\"YOU HAVE DIED\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding StatusDeathCountText}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Text=\"TIMES\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DashboardCircleButton}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DashboardResetCircleButton}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ActiveEncounterBar\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding SetActiveBossCommand}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding BossDefeatedCommand}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding SkipBossCommand}\"", dashboardTab, StringComparison.Ordinal);
    }

    [Fact]
    public void DashboardKeepsManualCounterSetInputNearReferenceControls()
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

        Assert.Contains("Text=\"{Binding ManualCounterText, UpdateSourceTrigger=PropertyChanged}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding SubtractDeathCommand}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding SetCounterCommand}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding AddDeathCommand}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ResetCounterCommand}\"", dashboardTab, StringComparison.Ordinal);
        Assert.DoesNotContain("Dash_QuickSettings", dashboardTab, StringComparison.Ordinal);
        Assert.DoesNotContain("Dash_BindingReminders", dashboardTab, StringComparison.Ordinal);
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
    public void TopBarReplacesBottomStatusBarWithLiveStatusAndSectionTitle()
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
        var codePath = Path.Combine(Path.GetDirectoryName(xamlPath)!, "MainWindow.xaml.cs");
        var xaml = File.ReadAllText(xamlPath);
        var code = File.ReadAllText(codePath);

        // The Claude Design chrome drops the bottom status bar in favour of a top bar.
        Assert.DoesNotContain("x:Name=\"BottomStatusBar\"", xaml, StringComparison.Ordinal);

        // The header carries the centered section title, game order from the reference, and live status.
        Assert.Contains("x:Name=\"SectionTitleText\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource TopStatusDot}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding DetectionStatus}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"LAST\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding LastDetectedDeathText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Elden Ring Death Counter\"", xaml, StringComparison.Ordinal);

        var ds = xaml.IndexOf("x:Name=\"DarkSouls1Button\"", StringComparison.Ordinal);
        var ds2 = xaml.IndexOf("x:Name=\"DarkSouls2Button\"", StringComparison.Ordinal);
        var ds3 = xaml.IndexOf("x:Name=\"DarkSouls3Button\"", StringComparison.Ordinal);
        var er = xaml.IndexOf("x:Name=\"EldenRingButton\"", StringComparison.Ordinal);
        Assert.True(ds >= 0 && ds < ds2 && ds2 < ds3 && ds3 < er);

        // The narrow icon rail provides the emblem and glyph navigation.
        Assert.Contains("EmblemFontFamily", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource RailNavButton}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("string.Join", code, StringComparison.Ordinal);
    }

    [Fact]
    public void SidebarEmblemUsesDarkFantasyDeathCounterInitial()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));
        var xamlPath = Path.Combine(projectRoot, "src", "EldenDeathCounter", "MainWindow.xaml");
        var appXamlPath = Path.Combine(projectRoot, "src", "EldenDeathCounter", "App.xaml");
        var xaml = File.ReadAllText(xamlPath);
        var appXaml = File.ReadAllText(appXamlPath);

        var emblemStart = xaml.IndexOf("<!-- Emblem -->", StringComparison.Ordinal);
        Assert.True(emblemStart >= 0);
        var emblemEnd = xaml.IndexOf("<!-- Navigation -->", emblemStart, StringComparison.Ordinal);
        Assert.True(emblemEnd > emblemStart);
        var emblem = xaml[emblemStart..emblemEnd];

        Assert.Contains("Text=\"D\"", emblem, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"M\"", emblem, StringComparison.Ordinal);
        Assert.Contains("EmblemFontFamily", emblem, StringComparison.Ordinal);
        Assert.Contains("#UnifrakturCook", appXaml, StringComparison.Ordinal);
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
