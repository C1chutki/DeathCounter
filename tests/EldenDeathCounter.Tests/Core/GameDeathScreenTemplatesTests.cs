using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class GameDeathScreenTemplatesTests
{
    private static string Ds3(string file) => Path.Combine("Dark souls 3", file);

    [Fact]
    public void DarkSouls3DeathTemplatesIncludeEnglishYouDiedEvenWhenLanguageIsPolish()
    {
        // DS3's on-screen death text is English ("YOU DIED") even with the game/app set to Polish,
        // so DS3 must load the English template regardless of the configured language.
        var files = GameDeathScreenTemplates.DeathTemplateFiles("DarkSouls3", "PL");

        Assert.Contains(Ds3("ENG_YouDied.jpg"), files);
        Assert.Contains(Ds3("PL_YouDied.jpg"), files);
    }

    [Fact]
    public void DarkSouls3DeathTemplatesLoadBothLanguagesRegardlessOfSetting()
    {
        var pl = GameDeathScreenTemplates.DeathTemplateFiles("DarkSouls3", "PL");
        var eng = GameDeathScreenTemplates.DeathTemplateFiles("DarkSouls3", "ENG");

        Assert.Equal(pl, eng);
    }

    [Fact]
    public void DarkSouls3VictoryTemplatesUseDarkSouls3Assets()
    {
        var files = GameDeathScreenTemplates.VictoryTemplateFiles("DarkSouls3", "PL");

        Assert.Contains(Ds3("PL_Victory.jpg"), files);
    }

    [Fact]
    public void EldenRingDeathTemplatesUsePolishScreensAndNotDarkSouls3Assets()
    {
        var files = GameDeathScreenTemplates.DeathTemplateFiles("EldenRing", "PL");

        Assert.Contains("PL_Death_Screen.png", files);
        Assert.DoesNotContain(files, file => file.Contains("Dark souls 3", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EldenRingEnglishDeathTemplatesUseEnglishScreens()
    {
        var files = GameDeathScreenTemplates.DeathTemplateFiles("EldenRing", "ENG");

        Assert.Contains("ENG_Death_Screen.jpg", files);
        Assert.DoesNotContain("PL_Death_Screen.png", files);
    }
}
