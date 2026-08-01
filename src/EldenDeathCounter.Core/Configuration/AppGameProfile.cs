namespace EldenDeathCounter.Core.Configuration;

public sealed record AppGameProfile(string Id, string FolderName, AppGameTheme Theme)
{
    public static AppGameProfile EldenRing { get; } = new("EldenRing", "EldenRing", AppGameTheme.EldenRing);

    public static AppGameProfile DarkSouls1 { get; } = new("DarkSouls1", "DarkSouls1", AppGameTheme.DarkSouls1);

    public static AppGameProfile DarkSouls2 { get; } = new("DarkSouls2", "DarkSouls2", AppGameTheme.DarkSouls2);

    public static AppGameProfile DarkSouls3 { get; } = new("DarkSouls3", "DarkSouls3", AppGameTheme.DarkSouls3);

    public static AppGameProfile Sekiro { get; } = new("Sekiro", "Sekiro", AppGameTheme.Sekiro);

    public string GetDataFolderPath(string desktopPath)
    {
        return Path.Combine(desktopPath, "DeathCounter", FolderName);
    }

    public string GetSettingsFilePath(string desktopPath)
    {
        return Path.Combine(GetDataFolderPath(desktopPath), "appsettings.json");
    }

    public string GetDeathDataFilePath(string desktopPath)
    {
        return Path.Combine(GetDataFolderPath(desktopPath), "deaths.json");
    }

    public string GetLogFilePath(string desktopPath)
    {
        return Path.Combine(GetDataFolderPath(desktopPath), "log.txt");
    }
}
