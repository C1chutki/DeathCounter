namespace EldenDeathCounter.Core.Configuration;

public static class DeathCounterText
{
    public static string FormatGlobalCount(int count, string? gameLanguage)
    {
        return $"{FormatDeathLabel(gameLanguage)}: {count}";
    }

    public static string FormatDeathLabel(string? gameLanguage)
    {
        var normalizedLanguage = gameLanguage?.Trim().ToUpperInvariant();
        return normalizedLanguage is "ENG" or "EN"
            ? "Deaths"
            : "\u015Amierci";
    }

    public static string FormatBossDeath(int deathCount, string? gameLanguage)
    {
        if (deathCount <= 0)
        {
            var normalizedLanguage = gameLanguage?.Trim().ToUpperInvariant();
            return normalizedLanguage is "ENG" or "EN"
                ? "First Try"
                : "Pierwsze podejście";
        }

        return $"{FormatDeathLabel(gameLanguage)}: {deathCount}";
    }

    public static string FormatBossOverlayName(string bossName)
    {
        var names = bossName
            .Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return names.Length <= 1
            ? bossName.Trim()
            : string.Join($"{Environment.NewLine}+{Environment.NewLine}", names);
    }
}
