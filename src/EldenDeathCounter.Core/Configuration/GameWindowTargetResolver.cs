namespace EldenDeathCounter.Core.Configuration;

public static class GameWindowTargetResolver
{
    private static readonly IReadOnlyDictionary<string, string[]> ProcessNamesByCaptureTarget =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["EldenRingWindow"] = ["eldenring", "start_protected_game"],
            ["DarkSouls1Window"] = ["DarkSoulsRemastered", "DARKSOULS"],
            ["DarkSouls2Window"] = ["DarkSoulsII"],
            ["DarkSouls3Window"] = ["DarkSoulsIII"]
        };

    public static string GetCaptureTarget(AppGameProfile profile) => profile.Id switch
    {
        "DarkSouls1" => "DarkSouls1Window",
        "DarkSouls2" => "DarkSouls2Window",
        "DarkSouls3" => "DarkSouls3Window",
        _ => "EldenRingWindow"
    };

    public static bool IsGameWindowTarget(string captureTarget) =>
        ProcessNamesByCaptureTarget.ContainsKey(captureTarget);

    public static bool IsMatchingProcess(string captureTarget, string processName) =>
        ProcessNamesByCaptureTarget.TryGetValue(captureTarget, out var processNames) &&
        Array.Exists(processNames, name => name.Equals(processName, StringComparison.OrdinalIgnoreCase));
}
