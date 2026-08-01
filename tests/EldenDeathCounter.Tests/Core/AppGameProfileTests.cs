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
    public void DarkSouls1ProfileUsesSharedDesktopRootAndGameSubfolder()
    {
        var desktopPath = @"C:\Users\TestUser\Desktop";

        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\DarkSouls1", AppGameProfile.DarkSouls1.GetDataFolderPath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\DarkSouls1\appsettings.json", AppGameProfile.DarkSouls1.GetSettingsFilePath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\DarkSouls1\deaths.json", AppGameProfile.DarkSouls1.GetDeathDataFilePath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\DarkSouls1\log.txt", AppGameProfile.DarkSouls1.GetLogFilePath(desktopPath));
    }

    [Fact]
    public void DarkSouls2ProfileUsesSharedDesktopRootAndGameSubfolder()
    {
        var desktopPath = @"C:\Users\TestUser\Desktop";

        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\DarkSouls2", AppGameProfile.DarkSouls2.GetDataFolderPath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\DarkSouls2\appsettings.json", AppGameProfile.DarkSouls2.GetSettingsFilePath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\DarkSouls2\deaths.json", AppGameProfile.DarkSouls2.GetDeathDataFilePath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\DarkSouls2\log.txt", AppGameProfile.DarkSouls2.GetLogFilePath(desktopPath));
    }

    [Fact]
    public void SekiroProfileUsesSharedDesktopRootAndGameSubfolder()
    {
        var desktopPath = @"C:\Users\TestUser\Desktop";

        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\Sekiro", AppGameProfile.Sekiro.GetDataFolderPath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\Sekiro\appsettings.json", AppGameProfile.Sekiro.GetSettingsFilePath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\Sekiro\deaths.json", AppGameProfile.Sekiro.GetDeathDataFilePath(desktopPath));
        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\Sekiro\log.txt", AppGameProfile.Sekiro.GetLogFilePath(desktopPath));
    }

    [Fact]
    public void ProfileDefaultSettingsUseThatProfilesDataFolder()
    {
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop", AppGameProfile.DarkSouls3);

        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\DarkSouls3", settings.DataFolderPath);
    }

    [Fact]
    public void CharacterProfileUsesSanitizedSubfolderInsideGameProfile()
    {
        var desktopPath = @"C:\Users\TestUser\Desktop";

        Assert.Equal(
            @"C:\Users\TestUser\Desktop\DeathCounter\EldenRing\Characters\Kamil",
            AppCharacterProfile.GetDataFolderPath(desktopPath, AppGameProfile.EldenRing, " Kamil "));
        Assert.Equal(
            @"C:\Users\TestUser\Desktop\DeathCounter\EldenRing\Characters\Kamil_save_1",
            AppCharacterProfile.GetDataFolderPath(desktopPath, AppGameProfile.EldenRing, "Kamil: save/1"));
    }

    [Fact]
    public void EmptyCharacterProfileNameFallsBackToGameProfileFolder()
    {
        var desktopPath = @"C:\Users\TestUser\Desktop";

        Assert.Equal(
            AppGameProfile.EldenRing.GetDataFolderPath(desktopPath),
            AppCharacterProfile.GetDataFolderPath(desktopPath, AppGameProfile.EldenRing, "   "));
    }
}
