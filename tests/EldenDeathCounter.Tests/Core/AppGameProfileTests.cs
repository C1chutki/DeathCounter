using EldenDeathCounter.Core.Configuration;

namespace EldenDeathCounter.Tests.Core;

public sealed class AppGameProfileTests
{
    [Fact]
    public void EldenRingProfileUsesSharedDesktopRootAndGameSubfolder()
    {
        var desktopPath = @"C:\Users\TestUser\Desktop";

        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\EldenRing", AppGameProfile.EldenRing.GetDataFolderPath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\EldenRing\appsettings.json", AppGameProfile.EldenRing.GetSettingsFilePath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\EldenRing\deaths.json", AppGameProfile.EldenRing.GetDeathDataFilePath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\EldenRing\log.txt", AppGameProfile.EldenRing.GetLogFilePath(desktopPath));
    }

    [Fact]
    public void DarkSouls3ProfileUsesSharedDesktopRootAndGameSubfolder()
    {
        var desktopPath = @"C:\Users\TestUser\Desktop";

        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\DarkSouls3", AppGameProfile.DarkSouls3.GetDataFolderPath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\DarkSouls3\appsettings.json", AppGameProfile.DarkSouls3.GetSettingsFilePath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\DarkSouls3\deaths.json", AppGameProfile.DarkSouls3.GetDeathDataFilePath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\DarkSouls3\log.txt", AppGameProfile.DarkSouls3.GetLogFilePath(desktopPath));
    }

    [Fact]
    public void ProfileDefaultSettingsUseThatProfilesDataFolder()
    {
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop", AppGameProfile.DarkSouls3);

        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\DarkSouls3", settings.DataFolderPath);
    }
}
