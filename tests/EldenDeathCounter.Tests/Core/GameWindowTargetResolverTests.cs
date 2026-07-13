using EldenDeathCounter.Core.Configuration;

namespace EldenDeathCounter.Tests.Core;

public sealed class GameWindowTargetResolverTests
{
    public static IEnumerable<object[]> Profiles =>
    [
        [AppGameProfile.EldenRing, "EldenRingWindow", "eldenring"],
        [AppGameProfile.DarkSouls1, "DarkSouls1Window", "DarkSoulsRemastered"],
        [AppGameProfile.DarkSouls2, "DarkSouls2Window", "DarkSoulsII"],
        [AppGameProfile.DarkSouls3, "DarkSouls3Window", "DarkSoulsIII"]
    ];

    [Theory]
    [MemberData(nameof(Profiles))]
    public void ProfilesResolveTheirOwnCaptureTargetAndProcess(AppGameProfile profile, string captureTarget, string processName)
    {
        Assert.Equal(captureTarget, GameWindowTargetResolver.GetCaptureTarget(profile));
        Assert.True(GameWindowTargetResolver.IsGameWindowTarget(captureTarget));
        Assert.True(GameWindowTargetResolver.IsMatchingProcess(captureTarget, processName));
        Assert.False(GameWindowTargetResolver.IsMatchingProcess(captureTarget, "not-a-game"));
    }
}
