using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class GameDeathScreenTemplatesTests
{
    private static string Ds3(string file) => Path.Combine("Dark souls 3", file);

    private static string Ds2(string file) => Path.Combine("Dark souls 2", file);

    private static string Er(string file) => Path.Combine("Elden Ring", file);

    private static string Se(string file) => Path.Combine("Sekiro", file);

    [Fact]
    public void DarkSouls2DeathTemplatesUseEnglishYouDiedAndNeverEldenRingScreens()
    {
        // DS2 draws "YOU DIED" in English and we only ship the English reference, so both languages
        // resolve to the DS2 English screen and never fall back to the Elden Ring death screens.
        var pl = GameDeathScreenTemplates.DeathTemplateFiles("DarkSouls2", "PL");
        var eng = GameDeathScreenTemplates.DeathTemplateFiles("DarkSouls2", "ENG");

        Assert.Equal([Ds2("ENG_YouDied.jpg")], pl);
        Assert.Equal([Ds2("ENG_YouDied.jpg")], eng);
        Assert.DoesNotContain(pl, file => file.Contains("Elden Ring", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DarkSouls2HasNoVictoryTemplatesAndNeverUsesEldenRingScreens()
    {
        // No DS2 victory reference asset exists, so DS2 returns no victory templates (never Elden Ring's).
        var files = GameDeathScreenTemplates.VictoryTemplateFiles("DarkSouls2", "PL");

        Assert.Empty(files);
    }

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

        Assert.Contains(Er("PL_Death_Screen.png"), files);
        Assert.DoesNotContain(files, file => file.Contains("Dark souls 3", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EldenRingEnglishDeathTemplatesUseEnglishScreens()
    {
        var files = GameDeathScreenTemplates.DeathTemplateFiles("EldenRing", "ENG");

        Assert.Contains(Er("ENG_Death_Screen.png"), files);
        Assert.DoesNotContain(Er("PL_Death_Screen.png"), files);
    }

    [Fact]
    public void EldenRingEnglishDeathTemplatesUseSixVisuallyDiverseScreens()
    {
        var files = GameDeathScreenTemplates.DeathTemplateFiles("EldenRing", "ENG");

        Assert.Equal(
            [
                Er("ENG_Death_Screen_v9.png"),
                Er("ENG_Death_Screen_v3.png"),
                Er("ENG_Death_Screen.png"),
                Er("ENG_Death_Screen_v8.png"),
                Er("ENG_Death_Screen_v7.png"),
                Er("ENG_Death_Screen_v11.png")
            ],
            files);
    }

    [Fact]
    public void EldenRingReforgedDeathTemplatesIncludeReforgedYouDiedScreen()
    {
        var files = GameDeathScreenTemplates.DeathTemplateFiles("EldenRing", "ENG", BossHealthBarStyles.Reforged);

        Assert.Contains(Path.Combine("Elden Ring", "Reforge", "YouDied_Reforge.png"), files);
        Assert.Contains(Er("ENG_Death_Screen.png"), files);
    }

    [Fact]
    public void EldenRingVanillaDeathTemplatesDoNotIncludeReforgedYouDiedScreen()
    {
        var files = GameDeathScreenTemplates.DeathTemplateFiles("EldenRing", "ENG", BossHealthBarStyles.Vanilla);

        Assert.DoesNotContain(Path.Combine("Elden Ring", "Reforge", "YouDied_Reforge.png"), files);
    }

    [Theory]
    [InlineData("PL")]
    [InlineData("ENG")]
    public void SekiroUsesItsOwnLanguageIndependentDeathScreenAndHasNoVictoryTemplate(string language)
    {
        // The 死 death screen is identical in every language, and a boss kill shows only the 忍殺 kanji,
        // so Sekiro shares its fade-stage references and has no victory reference (never Elden Ring's).
        Assert.Equal(
            [
                Se("ENG_Death_Screen.png"),
                Se("ENG_Death_Screen_v2.png"),
                Se("ENG_Death_Screen_v3.png"),
                Se("ENG_Death_Screen_v4.png")
            ],
            GameDeathScreenTemplates.DeathTemplateFiles("Sekiro", language));
        Assert.Empty(GameDeathScreenTemplates.VictoryTemplateFiles("Sekiro", language));
    }
}
