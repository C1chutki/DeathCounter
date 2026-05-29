using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class BossNameDetectionResultTests
{
    private static BossNameCandidate Matched(string raw, string canonical, string display) =>
        new(raw, new BossNameMatch(true, canonical, display, 0.95));

    private static BossNameCandidate Rejected(string raw) =>
        new(raw, BossNameMatch.None);

    [Fact]
    public void CombinesTwoDistinctMatchedNamesWithPlus()
    {
        var result = BossNameDetectionResult.FromMatches(2, new[]
        {
            Matched("Crystalian (Spear)", "Crystalian", "Crystalian (Spear)"),
            Matched("Crystalian (Staff)", "Crystalian", "Crystalian (Staff)"),
        });

        Assert.Equal(2, result.BarCount);
        Assert.Equal(2, result.MatchedCount);
        Assert.Equal("Crystalian (Spear) + Crystalian (Staff)", result.CombinedName);
    }

    [Fact]
    public void CombinesThreeDistinctMatchedNamesWithPlus()
    {
        var result = BossNameDetectionResult.FromMatches(3, new[]
        {
            Matched("Putrid Crystalian (Staff)", "Putrid Crystalian", "Putrid Crystalian (Staff)"),
            Matched("Putrid Crystalian (Ringblade)", "Putrid Crystalian", "Putrid Crystalian (Ringblade)"),
            Matched("Putrid Crystalian (Spear)", "Putrid Crystalian", "Putrid Crystalian (Spear)"),
        });

        Assert.Equal(3, result.MatchedCount);
        Assert.Equal(
            "Putrid Crystalian (Staff) + Putrid Crystalian (Ringblade) + Putrid Crystalian (Spear)",
            result.CombinedName);
    }

    [Fact]
    public void DeduplicatesIdenticalDisplayNames()
    {
        var result = BossNameDetectionResult.FromMatches(3, new[]
        {
            Matched("Putrid Crystalian", "Putrid Crystalian", "Putrid Crystalian"),
            Matched("Putrid Crystalian", "Putrid Crystalian", "Putrid Crystalian"),
            Matched("Putrid Crystalian", "Putrid Crystalian", "Putrid Crystalian"),
        });

        Assert.Equal(3, result.MatchedCount);
        Assert.Equal("Putrid Crystalian", result.CombinedName);
    }

    [Fact]
    public void DropsRejectedCandidatesAndNeverInsertsCorruptedText()
    {
        var result = BossNameDetectionResult.FromMatches(2, new[]
        {
            Matched("Godskin Apostle", "Godskin Apostle", "Godskin Apostle"),
            Rejected("Cruäßle"),
        });

        Assert.Equal(1, result.MatchedCount);
        Assert.Equal("Godskin Apostle", result.CombinedName);
        Assert.DoesNotContain("Cru", result.CombinedName!);
    }

    [Fact]
    public void ReturnsNullCombinedNameWhenNothingMatches()
    {
        var result = BossNameDetectionResult.FromMatches(2, new[]
        {
            Rejected("Talk"),
            Rejected("rd Âv"),
        });

        Assert.Equal(0, result.MatchedCount);
        Assert.Null(result.CombinedName);
    }

    [Fact]
    public void NoBarsProducesEmptyResult()
    {
        var result = BossNameDetectionResult.FromMatches(0, Array.Empty<BossNameCandidate>());

        Assert.Equal(0, result.BarCount);
        Assert.Equal(0, result.MatchedCount);
        Assert.Null(result.CombinedName);
    }
}
