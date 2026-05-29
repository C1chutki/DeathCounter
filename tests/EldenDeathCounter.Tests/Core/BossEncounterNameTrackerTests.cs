using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class BossEncounterNameTrackerTests
{
    // ---- Group B: OCR gating / false positives ----

    [Fact]
    public void DoesNotRequestOcrWhenNoBarIsPresent()
    {
        var tracker = new BossEncounterNameTracker(requiredStableFrames: 2);

        var decision = tracker.BeginFrame(0);

        Assert.False(decision.ShouldReadNames);
    }

    [Fact]
    public void NeverPublishesWhenNoBarIsPresentEvenIfNamesSubmitted()
    {
        var tracker = new BossEncounterNameTracker(requiredStableFrames: 1);

        // Simulates "Talk"/"Sit" text appearing near the bottom with no boss bar.
        var published = tracker.SubmitNames(barCount: 0, matchedCount: 1, combinedName: "Talk");

        Assert.Null(published);
        Assert.False(tracker.IsFrozen);
    }

    [Fact]
    public void DoesNotRequestOcrUntilBarsAreStableForRequiredFrames()
    {
        var tracker = new BossEncounterNameTracker(requiredStableFrames: 2);

        Assert.False(tracker.BeginFrame(1).ShouldReadNames);
        Assert.True(tracker.BeginFrame(1).ShouldReadNames);
    }

    // ---- Group F: encounter state machine ----

    [Fact]
    public void PublishesMatchedNameOnceBarsAreStableThenFreezes()
    {
        var tracker = new BossEncounterNameTracker(requiredStableFrames: 2);

        var name = RunFrame(tracker, barCount: 1, matchedCount: 1, combinedName: "Margit, the Fell Omen");
        Assert.Null(name); // first frame: not yet stable

        name = RunFrame(tracker, barCount: 1, matchedCount: 1, combinedName: "Margit, the Fell Omen");

        Assert.Equal("Margit, the Fell Omen", name);
        Assert.True(tracker.IsFrozen);
        Assert.Equal("Margit, the Fell Omen", tracker.FrozenName);
    }

    [Fact]
    public void FrozenEncounterStopsRequestingOcr()
    {
        var tracker = StableEncounterWithPublishedName("Margit, the Fell Omen");

        Assert.False(tracker.BeginFrame(1).ShouldReadNames);
    }

    [Fact]
    public void LowConfidenceResultDoesNotOverwriteFrozenName()
    {
        var tracker = StableEncounterWithPublishedName("Margit, the Fell Omen");

        var published = tracker.SubmitNames(barCount: 1, matchedCount: 0, combinedName: null);

        Assert.Null(published);
        Assert.Equal("Margit, the Fell Omen", tracker.FrozenName);
    }

    [Fact]
    public void InteractionPromptDoesNotOverwriteFrozenName()
    {
        var tracker = StableEncounterWithPublishedName("Margit, the Fell Omen");

        // Even if the gate were bypassed, a frozen encounter never re-publishes.
        var published = tracker.SubmitNames(barCount: 1, matchedCount: 1, combinedName: "Talk");

        Assert.Null(published);
        Assert.Equal("Margit, the Fell Omen", tracker.FrozenName);
    }

    [Fact]
    public void DoesNotRearmBeforeBarsDisappearForRequiredFrames()
    {
        var tracker = StableEncounterWithPublishedName("Margit, the Fell Omen");

        Assert.False(tracker.BeginFrame(0).Rearmed);
        Assert.False(tracker.BeginFrame(0).Rearmed);
        Assert.True(tracker.IsFrozen);
    }

    [Fact]
    public void RearmsAfterBarsDisappearForRequiredFramesThenDetectsNewBoss()
    {
        var tracker = StableEncounterWithPublishedName("Margit, the Fell Omen");

        tracker.BeginFrame(0);
        tracker.BeginFrame(0);
        var rearm = tracker.BeginFrame(0); // clearFramesRequired defaults to 3

        Assert.True(rearm.Rearmed);
        Assert.False(tracker.IsFrozen);

        // A clearly new encounter can now publish a different boss.
        RunFrame(tracker, 1, 1, "Godrick the Grafted");
        var name = RunFrame(tracker, 1, 1, "Godrick the Grafted");
        Assert.Equal("Godrick the Grafted", name);
    }

    [Fact]
    public void ResetReArmsImmediately()
    {
        var tracker = StableEncounterWithPublishedName("Margit, the Fell Omen");

        tracker.Reset();

        Assert.False(tracker.IsFrozen);
        Assert.Null(tracker.FrozenName);
    }

    [Fact]
    public void WaitsForCompleteMultiBossMatchBeforeFreezing()
    {
        var tracker = new BossEncounterNameTracker(requiredStableFrames: 1);

        // 3 bars present, but only 2 names matched this attempt -> wait.
        var partial = RunFrame(tracker, barCount: 3, matchedCount: 2, combinedName: "A + B");
        Assert.Null(partial);
        Assert.False(tracker.IsFrozen);

        // All 3 matched -> publish full combined name.
        var full = RunFrame(tracker, barCount: 3, matchedCount: 3, combinedName: "A + B + C");
        Assert.Equal("A + B + C", full);
        Assert.True(tracker.IsFrozen);
    }

    [Fact]
    public void StopsRequestingOcrAfterMaxAttemptsWithoutMatch()
    {
        var tracker = new BossEncounterNameTracker(requiredStableFrames: 1, maxOcrAttempts: 2);

        RunFrame(tracker, 1, 0, null);
        RunFrame(tracker, 1, 0, null);

        Assert.False(tracker.BeginFrame(1).ShouldReadNames);
        Assert.False(tracker.IsFrozen);
    }

    private static BossEncounterNameTracker StableEncounterWithPublishedName(string name)
    {
        var tracker = new BossEncounterNameTracker(requiredStableFrames: 2);
        RunFrame(tracker, 1, 1, name);
        var published = RunFrame(tracker, 1, 1, name);
        Assert.Equal(name, published);
        return tracker;
    }

    private static string? RunFrame(BossEncounterNameTracker tracker, int barCount, int matchedCount, string? combinedName)
    {
        var decision = tracker.BeginFrame(barCount);
        return decision.ShouldReadNames
            ? tracker.SubmitNames(barCount, matchedCount, combinedName)
            : null;
    }
}
