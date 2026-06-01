using System.Text.RegularExpressions;

namespace EldenDeathCounter.Core.Configuration;

public static class AppCharacterProfile
{
    public static string GetDataFolderPath(string desktopPath, AppGameProfile gameProfile, string characterName)
    {
        var folderName = CreateFolderName(characterName);
        return string.IsNullOrWhiteSpace(folderName)
            ? gameProfile.GetDataFolderPath(desktopPath)
            : Path.Combine(gameProfile.GetDataFolderPath(desktopPath), "Characters", folderName);
    }

    public static string NormalizeName(string characterName)
    {
        return characterName.Trim();
    }

    private static string CreateFolderName(string characterName)
    {
        var normalized = NormalizeName(characterName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var chars = normalized
            .Select(character => invalidChars.Contains(character) ? '_' : character)
            .ToArray();
        var folderName = new string(chars).Trim('.', ' ');
        folderName = Regex.Replace(folderName, @"\s*_\s*", "_");
        folderName = Regex.Replace(folderName, "_+", "_");

        return folderName.Trim('_', '.', ' ');
    }
}
