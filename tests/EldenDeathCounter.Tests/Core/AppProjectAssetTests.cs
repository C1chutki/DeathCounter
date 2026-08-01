using System.Text.RegularExpressions;
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

        // The wildcard Assets include copies root assets, and the Elden Ring subfolder include copies
        // the relocated Elden Ring boss lists and reference images to the build output.
        Assert.Contains(@"..\..\Assets\*.*", includes);
        Assert.Contains(@"..\..\Assets\Icons\*.*", includes);
        Assert.Contains(@"..\..\Assets\Elden Ring\*.*", includes);
        Assert.Contains(@"..\..\Assets\Elden Ring\Reforge\*.*", includes);
        Assert.Contains(@"..\..\Assets\Elden Ring\Convergence\*.*", includes);

        var eldenRingRoot = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(projectPath)!, "..", "..", "Assets", "Elden Ring"));
        foreach (var file in new[]
                 {
                     "ENG_ER_BossList.txt", "PL_ER_BossList.txt",
                     "ENG_DoubleBoss.jpg", "ENG_DoubleBoss_02.jpg",
                     "ENG_TrippleBoss.jpg", "ENG_TrippleBoss_02.jpg",
                     Path.Combine("Reforge", "BossBar_Reforge.png"),
                     Path.Combine("Reforge", "YouDied_Reforge.png"),
                     Path.Combine("Convergence", "ENG_ER_Convergence_BossList.txt"),
                 })
        {
            Assert.True(File.Exists(Path.Combine(eldenRingRoot, file)), $"Missing asset: {file}");
        }

        // Both boss lists must provide a source of truth for the matcher.
        Assert.True(BossNameMatcher.ParseList(File.ReadAllLines(Path.Combine(eldenRingRoot, "ENG_ER_BossList.txt"))).Count > 100);
        Assert.True(BossNameMatcher.ParseList(File.ReadAllLines(Path.Combine(eldenRingRoot, "PL_ER_BossList.txt"))).Count > 100);
        var convergenceNames = BossNameMatcher.ParseList(File.ReadAllLines(Path.Combine(
            eldenRingRoot,
            "Convergence",
            "ENG_ER_Convergence_BossList.txt")));
        Assert.Contains("Bloodflame Dragon Sanguivaros", convergenceNames);
        Assert.Contains("Daergarf, Underworld Archmage", convergenceNames);
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

        Assert.Contains(@"..\..\Assets\Elden Ring\*.*", includes);

        var eldenRingRoot = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(projectPath)!, "..", "..", "Assets", "Elden Ring"));
        Assert.True(File.Exists(Path.Combine(eldenRingRoot, "PL_Death_Screen.png")));
        Assert.True(File.Exists(Path.Combine(eldenRingRoot, "PL_Death_Screen_v2.jpg")));
        Assert.True(File.Exists(Path.Combine(eldenRingRoot, "PL_Death_Screen_v3.jpg")));
        Assert.True(File.Exists(Path.Combine(eldenRingRoot, "PL_Win_screen.jpg")));
        Assert.True(File.Exists(Path.Combine(eldenRingRoot, "ENG_Death_Screen.png")));
        Assert.True(File.Exists(Path.Combine(eldenRingRoot, "ENG_Death_Screen_v3.png")));
        Assert.True(File.Exists(Path.Combine(eldenRingRoot, "ENG_Death_Screen_v7.png")));
        Assert.True(File.Exists(Path.Combine(eldenRingRoot, "ENG_Death_Screen_v8.png")));
        Assert.True(File.Exists(Path.Combine(eldenRingRoot, "ENG_Death_Screen_v9.png")));
        Assert.True(File.Exists(Path.Combine(eldenRingRoot, "ENG_Death_Screen_v11.png")));
        Assert.True(File.Exists(Path.Combine(eldenRingRoot, "ENG_Win_Screen.jpg")));
    }

    [Fact]
    public void DeathTemplateLoaderIncludesAllEnglishDeathScreenTemplates()
    {
        // English death-screen templates are resolved by the per-game template helper.
        var english = GameDeathScreenTemplates.DeathTemplateFiles("EldenRing", "ENG");

        Assert.Contains(Path.Combine("Elden Ring", "ENG_Death_Screen.png"), english);
        Assert.Contains(Path.Combine("Elden Ring", "ENG_Death_Screen_v3.png"), english);
        Assert.Contains(Path.Combine("Elden Ring", "ENG_Death_Screen_v7.png"), english);
        Assert.Contains(Path.Combine("Elden Ring", "ENG_Death_Screen_v8.png"), english);
        Assert.Contains(Path.Combine("Elden Ring", "ENG_Death_Screen_v9.png"), english);
        Assert.Contains(Path.Combine("Elden Ring", "ENG_Death_Screen_v11.png"), english);
        Assert.Equal(6, english.Count);
    }

    [Fact]
    public void AppProjectCopiesDarkSouls3DetectionAssets()
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

        // The DS3 subfolder must be copied to output or the per-game templates can't be loaded at runtime.
        Assert.Contains(@"..\..\Assets\Dark souls 3\*.*", includes);

        var ds3Root = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(projectPath)!, "..", "..", "Assets", "Dark souls 3"));
        foreach (var file in new[] { "ENG_YouDied.jpg", "PL_YouDied.jpg", "PL_Victory.jpg" })
        {
            Assert.True(File.Exists(Path.Combine(ds3Root, file)), $"Missing DS3 asset: {file}");
        }
    }

    [Fact]
    public void AppProjectCopiesDarkSouls2DetectionAssets()
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

        // The DS2 subfolder must be copied to output or the per-game death template, boss bar reference,
        // and boss list can't be loaded at runtime.
        Assert.Contains(@"..\..\Assets\Dark souls 2\*.*", includes);

        var ds2Root = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(projectPath)!, "..", "..", "Assets", "Dark souls 2"));
        foreach (var file in new[] { "ENG_YouDied.jpg", "ENG_BossBar.jpg", "ENG_DS2_BossList.txt" })
        {
            Assert.True(File.Exists(Path.Combine(ds2Root, file)), $"Missing DS2 asset: {file}");
        }

        // The English DS2 boss list must parse and contain the reference bosses.
        var bossNames = BossNameMatcher.ParseList(File.ReadAllLines(Path.Combine(ds2Root, "ENG_DS2_BossList.txt")));
        Assert.Contains("The Last Giant", bossNames);
        Assert.Contains("Throne Defender", bossNames);
        Assert.Contains("Throne Watcher", bossNames);
    }

    [Fact]
    public void AppProjectCopiesSekiroBossList()
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

        Assert.Contains(@"..\..\Assets\Sekiro\*.*", includes);

        var sekiroRoot = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(projectPath)!, "..", "..", "Assets", "Sekiro"));
        var bossListPath = Path.Combine(sekiroRoot, "ENG_SE_BossList.txt");
        Assert.True(File.Exists(bossListPath), $"Missing Sekiro asset: {bossListPath}");

        var bossNames = BossNameMatcher.ParseList(File.ReadAllLines(bossListPath));
        Assert.Contains("Genichiro Ashina", bossNames);
        Assert.Contains("Isshin, the Sword Saint", bossNames);
        Assert.Contains("Guardian Ape", bossNames);
        Assert.True(bossNames.Count > 30);
    }

    [Fact]
    public void BuildOutputContainsDarkSouls2DetectionAssets()
    {
        // After compiling the WPF app, the DS2 assets must physically exist in the app build output.
        var appProjectDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "EldenDeathCounter"));
        var binDir = Path.Combine(appProjectDir, "bin");
        Assert.True(Directory.Exists(binDir), $"App build output not found at {binDir}; build the app first.");

        foreach (var file in new[] { "ENG_YouDied.jpg", "ENG_BossBar.jpg", "ENG_DS2_BossList.txt" })
        {
            var matches = Directory.EnumerateFiles(binDir, file, SearchOption.AllDirectories)
                .Where(path => path.Contains(Path.Combine("Assets", "Dark souls 2"), StringComparison.OrdinalIgnoreCase))
                .ToList();
            Assert.True(matches.Count > 0, $"DS2 asset '{file}' missing from app build output under {binDir}.");
        }
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
    public void OverlayBossTimerUsesThreadPoolRefreshInsteadOfDispatcherTimer()
    {
        var overlayCodePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "EldenDeathCounter",
            "OverlayWindow.xaml.cs"));
        var overlayCode = File.ReadAllText(overlayCodePath);

        Assert.Contains("System.Threading.Timer", overlayCode, StringComparison.Ordinal);
        Assert.Contains("Change(TimeSpan.Zero, TimeSpan.FromSeconds(1))", overlayCode, StringComparison.Ordinal);
        Assert.Contains("Dispatcher.BeginInvoke(UpdateBossTimerText", overlayCode, StringComparison.Ordinal);
        Assert.DoesNotContain("private readonly DispatcherTimer _bossTimer", overlayCode, StringComparison.Ordinal);
    }

    [Fact]
    public void OverlayBackgroundOpacityAlsoControlsTimerChrome()
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

        Assert.DoesNotContain("Background=\"#B3000000\"", overlayXaml, StringComparison.Ordinal);
        Assert.Contains("TimerChrome.Background = CreateOverlayBrush(TimerOverlayBackgroundColor, _backgroundOpacity)", overlayCode, StringComparison.Ordinal);
        Assert.Contains("OverlayChrome.Background = CreateOverlayBrush(_overlayBackgroundColor, _backgroundOpacity)", overlayCode, StringComparison.Ordinal);
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

        Assert.Contains("<ScrollViewer Panel.ZIndex=\"1\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"DashboardBackdropBrush\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Color=\"#000000\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"DashboardParticleGold\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Key=\"DashboardParticleGoldSoft\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Key=\"DashboardParticleRuneRed\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Color=\"#191D1E\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Color=\"#283139\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DashboardStage\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DashboardParticleField\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DashboardParticleBall01\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DashboardParticleBall12\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DashboardParticleBall20\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DashboardParticleBall40\"", dashboardTab, StringComparison.Ordinal);
        Assert.Equal(40, Regex.Matches(dashboardTab, "x:Name=\"DashboardParticleBall\\d{2}\"").Count);
        Assert.Contains("<Canvas.Resources>", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("TargetType=\"{x:Type Ellipse}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Fill\" Value=\"{DynamicResource DashboardParticleGold}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("<BlurEffect Radius=\"3.6\"", dashboardTab, StringComparison.Ordinal);
        Assert.DoesNotContain("Fill=\"{StaticResource DashboardParticleGoldSoft}\"", dashboardTab, StringComparison.Ordinal);
        Assert.DoesNotContain("Fill=\"{StaticResource DashboardParticleRuneRed}\"", dashboardTab, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"DashboardLightPrimary\"", dashboardTab, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"DashboardLightSecondary\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"DashboardAmbientStoryboard\"", xaml, StringComparison.Ordinal);
        Assert.Contains("RepeatBehavior=\"Forever\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("DoubleAnimation", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Text=\"YOU HAVE DIED\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding StatusDeathCountText}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DashboardCountGlow\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DashboardCountTextBlur\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("BlurRadius=\"156\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("<BlurEffect Radius=\"10\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Text=\"TIMES\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DashboardCircleButton}\"", dashboardTab, StringComparison.Ordinal);
        Assert.DoesNotContain("DashboardResetCircleButton", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"R\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Storyboard.TargetName=\"ButtonScale\"", xaml, StringComparison.Ordinal);
        Assert.Contains("RenderTransformOrigin=\"0.5,0.5\"", xaml, StringComparison.Ordinal);
        Assert.Contains("To=\"1.08\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("From=\"0.5\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Command=\"{Binding AddDeathCommand}\"\r\n                                        Style=\"{StaticResource DashboardCircleButton}\"\r\n                                        BorderBrush=", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Duration=\"0:0:0.2\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ActiveEncounterBar\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Background=\"Transparent\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("<Grid Width=\"72\" Height=\"72\">", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("<Border Width=\"50\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ActiveBossDeathCountText}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("FontSize=\"28\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Content=\"-\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding SubtractDeathCommand}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Content=\"+\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding AddDeathCommand}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ResetCounterCommand}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("ImageSource=\"pack://siteoforigin:,,,/Assets/Icons/Reset.png\"", dashboardTab, StringComparison.Ordinal);
        Assert.DoesNotContain("Path Data=\"M19,9", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource EncounterButton}\"", dashboardTab, StringComparison.Ordinal);
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
        Assert.Contains("Command=\"{Binding ToggleDetectionCommand}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DashboardDetectionToggleButton}\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Text=\"F8\"", dashboardTab, StringComparison.Ordinal);
        Assert.Contains("Text=\"F9\"", dashboardTab, StringComparison.Ordinal);
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
        Assert.Contains("x:Name=\"HeaderBorder\" Grid.ColumnSpan=\"2\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SectionTitleText\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Dashboard\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<ColumnDefinition Width=\"260\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("<ColumnDefinition Width=\"*\" MinWidth=\"80\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("Grid.Column=\"1\"", xaml, StringComparison.Ordinal);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", xaml, StringComparison.Ordinal);
        Assert.Contains("FontSize=\"30\"", xaml, StringComparison.Ordinal);
        Assert.Contains("FontWeight=\"Bold\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Foreground=\"{DynamicResource Gold}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalAlignment=\"Center\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"HeaderBorder\" Grid.ColumnSpan=\"2\" Grid.Row=\"0\" Background=\"Transparent\" BorderBrush=\"{DynamicResource Gold2}\" BorderThickness=\"0,0,0,1\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource TopStatusDot}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding DetectionStatus}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"LAST\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding LastDetectedDeathText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("StackPanel Orientation=\"Vertical\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Elden Ring Death Counter\"", xaml, StringComparison.Ordinal);

        var ds = xaml.IndexOf("x:Name=\"DarkSouls1Button\"", StringComparison.Ordinal);
        var ds2 = xaml.IndexOf("x:Name=\"DarkSouls2Button\"", StringComparison.Ordinal);
        var ds3 = xaml.IndexOf("x:Name=\"DarkSouls3Button\"", StringComparison.Ordinal);
        var er = xaml.IndexOf("x:Name=\"EldenRingButton\"", StringComparison.Ordinal);
        Assert.True(ds >= 0 && ds < ds2 && ds2 < ds3 && ds3 < er);

        // The narrow icon rail provides the emblem and vector navigation.
        Assert.Contains("Style=\"{StaticResource RailNavButton}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Value=\"Segoe MDL2 Assets\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"&#xE7F4;\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"&#xE7B3;\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"&#xE8FD;\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"&#xE9D2;\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"&#xE713;\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"&#x25A6;\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"&#x25CE;\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"&#x25C7;\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"&#x25A9;\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"&#x2630;\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("HeaderBorder.BorderThickness = index == 0", code, StringComparison.Ordinal);
        Assert.Contains("HeaderBorder.BorderBrush = BrushFromHex(theme.Primary)", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ToUpperInvariant", code, StringComparison.Ordinal);
        Assert.DoesNotContain("string.Join", code, StringComparison.Ordinal);
    }

    [Fact]
    public void SidebarEmblemUsesApplicationIcon()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));
        var xamlPath = Path.Combine(projectRoot, "src", "EldenDeathCounter", "MainWindow.xaml");
        var xaml = File.ReadAllText(xamlPath);

        var emblemStart = xaml.IndexOf("<!-- Emblem -->", StringComparison.Ordinal);
        Assert.True(emblemStart >= 0);
        var emblemEnd = xaml.IndexOf("<!-- Navigation -->", emblemStart, StringComparison.Ordinal);
        Assert.True(emblemEnd > emblemStart);
        var emblem = xaml[emblemStart..emblemEnd];

        Assert.Contains("<Image Width=\"52\"", emblem, StringComparison.Ordinal);
        Assert.Contains("Source=\"pack://siteoforigin:,,,/Assets/Icons/AppIcon.png\"", emblem, StringComparison.Ordinal);
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
