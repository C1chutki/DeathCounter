namespace EldenDeathCounter.Tests.Core;

public sealed class BossesTabLayoutTests
{
    [Fact]
    public void BossesTabDoesNotRenderGreatEnemyFelledTitle()
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

        var bossesTabStart = xaml.IndexOf("<TabItem Header=\"Bosses\">", StringComparison.Ordinal);
        Assert.True(bossesTabStart >= 0);
        var settingsTabStart = xaml.IndexOf("<TabItem Header=\"Settings\">", bossesTabStart, StringComparison.Ordinal);
        Assert.True(settingsTabStart > bossesTabStart);
        var bossesTab = xaml[bossesTabStart..settingsTabStart];

        Assert.DoesNotContain("Bosses_Title", bossesTab, StringComparison.Ordinal);
        Assert.DoesNotContain("Great Enemy Felled", bossesTab, StringComparison.OrdinalIgnoreCase);
    }
}
