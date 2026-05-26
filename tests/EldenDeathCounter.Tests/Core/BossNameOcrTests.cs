using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class BossNameOcrTests
{
    [Theory]
    [InlineData("strÖnik Driewa")]
    [InlineData("Strainik Drzewa")]
    [InlineData("Straznik Drzewa")]
    public void AppliesConfiguredPolishBossNameCorrections(string ocrName)
    {
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");

        var corrected = BossNameOcrCorrector.Apply(ocrName, settings.BossNameCorrections);

        Assert.Equal("Strażnik Drzewa", corrected);
    }

    [Fact]
    public void StabilizerWaitsForRepeatedCorrectedNameBeforePublishing()
    {
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");
        var stabilizer = new BossNameStabilizer(requiredStableFrames: 3);

        Assert.Null(stabilizer.Observe("strÖnik Driewa", settings.BossNameCorrections));
        Assert.Null(stabilizer.Observe("Strainik Drzewa", settings.BossNameCorrections));

        var stable = stabilizer.Observe("Straznik Drzewa", settings.BossNameCorrections);

        Assert.Equal("Strażnik Drzewa", stable);
    }

    [Fact]
    public void DefaultStabilizerPublishesCorrectedBossNameAfterOneStableRead()
    {
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");
        var stabilizer = new BossNameStabilizer();

        var stable = stabilizer.Observe("Straznik Drzewa", settings.BossNameCorrections);

        Assert.Equal(BossNameOcrCorrector.Apply("Straznik Drzewa", settings.BossNameCorrections), stable);
    }

    [Fact]
    public void StabilizerDoesNotPublishRapidlyChangingDifferentNames()
    {
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");
        var stabilizer = new BossNameStabilizer(requiredStableFrames: 3);

        Assert.Null(stabilizer.Observe("Strażnik Drzewa", settings.BossNameCorrections));
        Assert.Null(stabilizer.Observe("Margit", settings.BossNameCorrections));
        Assert.Null(stabilizer.Observe("Godrick", settings.BossNameCorrections));
    }

    [Fact]
    public void StabilizerKeepsCandidateAcrossOneMissedOcrFrame()
    {
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");
        var stabilizer = new BossNameStabilizer(requiredStableFrames: 2, allowedMissingFrames: 1);

        Assert.Null(stabilizer.Observe("Strainik Drzewa", settings.BossNameCorrections));
        Assert.Null(stabilizer.ObserveMissing());

        var stable = stabilizer.Observe("Straznik Drzewa", settings.BossNameCorrections);

        Assert.Equal(BossNameOcrCorrector.Apply("Strainik Drzewa", settings.BossNameCorrections), stable);
    }
}
