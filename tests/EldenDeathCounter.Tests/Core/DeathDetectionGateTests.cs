using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class DeathDetectionGateTests
{
    [Fact]
    public void RepeatedPositiveSignalDoesNotCountAgainUntilScreenClears()
    {
        var gate = new DeathDetectionGate(clearFramesRequired: 3);
        var start = DateTimeOffset.Parse("2026-05-23T18:00:00+02:00");
        var cooldown = TimeSpan.FromSeconds(1);

        Assert.Equal(DeathDetectionDecision.Count, gate.Evaluate(true, start, cooldown));
        Assert.Equal(DeathDetectionDecision.IgnoreActiveScreen, gate.Evaluate(true, start.AddSeconds(2), cooldown));
        Assert.Equal(DeathDetectionDecision.IgnoreActiveScreen, gate.Evaluate(true, start.AddSeconds(30), cooldown));
    }

    [Fact]
    public void ReArmsAfterEnoughClearFrames()
    {
        var gate = new DeathDetectionGate(clearFramesRequired: 3);
        var start = DateTimeOffset.Parse("2026-05-23T18:00:00+02:00");
        var cooldown = TimeSpan.FromSeconds(1);

        Assert.Equal(DeathDetectionDecision.Count, gate.Evaluate(true, start, cooldown));
        Assert.Equal(DeathDetectionDecision.NoSignal, gate.Evaluate(false, start.AddSeconds(1), cooldown));
        Assert.Equal(DeathDetectionDecision.NoSignal, gate.Evaluate(false, start.AddSeconds(2), cooldown));
        Assert.Equal(DeathDetectionDecision.Rearmed, gate.Evaluate(false, start.AddSeconds(3), cooldown));
        Assert.Equal(DeathDetectionDecision.Count, gate.Evaluate(true, start.AddSeconds(4), cooldown));
    }

    [Fact]
    public void CooldownStillBlocksNewSignalAfterScreenClearsTooQuickly()
    {
        var gate = new DeathDetectionGate(clearFramesRequired: 1);
        var start = DateTimeOffset.Parse("2026-05-23T18:00:00+02:00");
        var cooldown = TimeSpan.FromSeconds(25);

        Assert.Equal(DeathDetectionDecision.Count, gate.Evaluate(true, start, cooldown));
        Assert.Equal(DeathDetectionDecision.Rearmed, gate.Evaluate(false, start.AddSeconds(1), cooldown));
        Assert.Equal(DeathDetectionDecision.IgnoreCooldown, gate.Evaluate(true, start.AddSeconds(2), cooldown));
    }
}
