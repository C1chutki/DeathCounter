namespace EldenDeathCounter.Core.Detection;

public enum BossNamePublishOutcome
{
    /// <summary>Apply the auto-detected name to the active boss.</summary>
    Publish,

    /// <summary>The active boss name was set or edited by the user; keep it, don't overwrite.</summary>
    KeepManual,

    /// <summary>Nothing to publish this frame.</summary>
    NoChange,
}

/// <summary>
/// Decides whether an auto-detected boss name may overwrite the current active boss. A name the user
/// set or edited manually (i.e. one we did not auto-publish) is never overwritten by auto-detection.
/// </summary>
public static class BossNamePublishDecision
{
    public static BossNamePublishOutcome Decide(string? proposedName, string? activeBossName, string? lastAutoPublishedName)
    {
        if (string.IsNullOrWhiteSpace(proposedName))
        {
            return BossNamePublishOutcome.NoChange;
        }

        if (!string.IsNullOrWhiteSpace(activeBossName) && !NamesEqual(activeBossName, lastAutoPublishedName))
        {
            return BossNamePublishOutcome.KeepManual;
        }

        return BossNamePublishOutcome.Publish;
    }

    private static bool NamesEqual(string? a, string? b)
        => string.Equals(a?.Trim(), b?.Trim(), StringComparison.CurrentCultureIgnoreCase);
}
