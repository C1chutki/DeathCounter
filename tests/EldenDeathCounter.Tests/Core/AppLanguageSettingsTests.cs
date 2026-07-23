using System.Text.Json;
using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.Core.Storage;

namespace EldenDeathCounter.Tests.Core;

public sealed class AppLanguageSettingsTests
{
    [Fact]
    public void AppLanguageDefaultsToEnglish()
    {
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");

        Assert.Equal("en", settings.AppLanguage);
    }

    [Fact]
    public void AppLanguageDefaultsToEnglishForExplicitProfile()
    {
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop", AppGameProfile.EldenRing);

        Assert.Equal("en", settings.AppLanguage);
    }

    [Fact]
    public void AppLanguageSurvivesJsonRoundTrip()
    {
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");
        settings.AppLanguage = "pl";

        var json = JsonSerializer.Serialize(settings, JsonFileOptions.Value);
        var restored = JsonSerializer.Deserialize<AppSettings>(json, JsonFileOptions.Value);

        Assert.NotNull(restored);
        Assert.Equal("pl", restored!.AppLanguage);
    }

    [Fact]
    public void AppLanguageIsIndependentOfGameLanguage()
    {
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");
        settings.AppLanguage = "pl";

        // AppLanguage (UI) must not alter GameLanguage (OCR/detection).
        Assert.Equal("PL", settings.GameLanguage);
        Assert.Equal("pl", settings.AppLanguage);

        var json = JsonSerializer.Serialize(settings, JsonFileOptions.Value);
        var restored = JsonSerializer.Deserialize<AppSettings>(json, JsonFileOptions.Value);

        Assert.NotNull(restored);
        Assert.Equal("PL", restored!.GameLanguage);
        Assert.Equal("pl", restored.AppLanguage);
    }
}
