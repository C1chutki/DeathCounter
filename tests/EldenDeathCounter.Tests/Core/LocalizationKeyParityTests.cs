using System.Xml.Linq;
using System.Text.RegularExpressions;

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

    [Fact]
    public void SettingsDynamicResourcesExistInLocalizationDictionaries()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var settingsTab = Regex.Match(
            xaml,
            """<TabItem Header="Settings">(?<content>[\s\S]*?)</TabItem>""",
            RegexOptions.CultureInvariant).Groups["content"].Value;
        var resourceKeys = Regex.Matches(
                settingsTab,
                """DynamicResource (?<key>[A-Za-z0-9_]+)""",
                RegexOptions.CultureInvariant)
            .Select(match => match.Groups["key"].Value)
            .Where(key => key.StartsWith("Settings_", StringComparison.Ordinal) ||
                          key.StartsWith("DetectionMode_", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key)
            .ToList();
        var englishKeys = ReadKeys("Strings.en.xaml");
        var polishKeys = ReadKeys("Strings.pl.xaml");

        var missingInEnglish = resourceKeys.Except(englishKeys, StringComparer.Ordinal).ToList();
        var missingInPolish = resourceKeys.Except(polishKeys, StringComparer.Ordinal).ToList();

        Assert.True(
            missingInEnglish.Count == 0 && missingInPolish.Count == 0,
            $"Settings localization resources are missing.{Environment.NewLine}" +
            $"Missing in English: {string.Join(", ", missingInEnglish)}{Environment.NewLine}" +
            $"Missing in Polish: {string.Join(", ", missingInPolish)}");
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

    private static string GetMainWindowXamlPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EldenDeathCounter.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory!.FullName, "src", "EldenDeathCounter", "MainWindow.xaml");
    }
}
