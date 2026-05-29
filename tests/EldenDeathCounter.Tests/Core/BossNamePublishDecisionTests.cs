using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class BossNamePublishDecisionTests
{
    [Fact]
    public void PublishesWhenNoActiveBoss()
    {
        Assert.Equal(
            BossNamePublishOutcome.Publish,
            BossNamePublishDecision.Decide(proposedName: "Margit, the Fell Omen", activeBossName: null, lastAutoPublishedName: null));
    }

    [Fact]
    public void KeepsManualNameWhenActiveBossWasNotSetByAutoDetection()
    {
        Assert.Equal(
            BossNamePublishOutcome.KeepManual,
            BossNamePublishDecision.Decide(proposedName: "Margit, the Fell Omen", activeBossName: "My Manual Boss", lastAutoPublishedName: null));
    }

    [Fact]
    public void AllowsOverwritingOurOwnPreviousAutoDetectedName()
    {
        // A new encounter after re-arm: active boss still holds the previous auto name.
        Assert.Equal(
            BossNamePublishOutcome.Publish,
            BossNamePublishDecision.Decide(proposedName: "Godrick the Grafted", activeBossName: "Margit, the Fell Omen", lastAutoPublishedName: "Margit, the Fell Omen"));
    }

    [Fact]
    public void KeepsManualNameWhenUserEditedAfterAutoDetection()
    {
        // We auto-published "Godrick", the user renamed it to "Margit".
        Assert.Equal(
            BossNamePublishOutcome.KeepManual,
            BossNamePublishDecision.Decide(proposedName: "Godrick the Grafted", activeBossName: "Margit, the Fell Omen", lastAutoPublishedName: "Godrick the Grafted"));
    }

    [Fact]
    public void NoChangeWhenNothingProposed()
    {
        Assert.Equal(
            BossNamePublishOutcome.NoChange,
            BossNamePublishDecision.Decide(proposedName: null, activeBossName: "Anything", lastAutoPublishedName: null));
    }

    [Fact]
    public void ComparisonIgnoresCaseAndSurroundingWhitespace()
    {
        Assert.Equal(
            BossNamePublishOutcome.Publish,
            BossNamePublishDecision.Decide(proposedName: "Margit, the Fell Omen", activeBossName: "  margit, the fell omen ", lastAutoPublishedName: "Margit, the Fell Omen"));
    }
}
