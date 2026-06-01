using System.Xml.Linq;

namespace EldenDeathCounter.Tests.Core;

public sealed class LocalizationKeyParityTests
{
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void EnglishAndPolishDictionariesDefineIdenticalKeys()
    {
        var englishKeys = ReadKeys("Strings.en.xaml");
        var polishKeys = ReadKeys("Strings.pl.xaml");

        var missingInPolish = englishKeys.Except(polishKeys).OrderBy(key => key).ToList();
        var missingInEnglish = polishKeys.Except(englishKeys).OrderBy(key => key).ToList();

        Assert.True(
            missingInPolish.Count == 0 && missingInEnglish.Count == 0,
            $"Localization key mismatch.{Environment.NewLine}" +
            $"Missing in Polish: {string.Join(", ", missingInPolish)}{Environment.NewLine}" +
            $"Missing in English: {string.Join(", ", missingInEnglish)}");
    }

    [Fact]
    public void DictionariesAreNonEmptyAndHaveNoDuplicateKeys()
    {
        foreach (var fileName in new[] { "Strings.en.xaml", "Strings.pl.xaml" })
        {
            var keys = ReadKeysRaw(fileName);
            Assert.NotEmpty(keys);
            var duplicates = keys.GroupBy(key => key).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            Assert.True(duplicates.Count == 0, $"{fileName} has duplicate keys: {string.Join(", ", duplicates)}");
        }
    }

    private static HashSet<string> ReadKeys(string fileName)
    {
        return ReadKeysRaw(fileName).ToHashSet(StringComparer.Ordinal);
    }

    private static List<string> ReadKeysRaw(string fileName)
    {
        var document = XDocument.Load(GetLocalizationFilePath(fileName));
        return document
            .Descendants()
            .Select(element => element.Attribute(XamlNamespace + "Key")?.Value)
            .Where(key => !string.IsNullOrEmpty(key))
            .Select(key => key!)
            .ToList();
    }

    private static string GetLocalizationFilePath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EldenDeathCounter.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory!.FullName, "src", "EldenDeathCounter", "Localization", fileName);
    }
}
