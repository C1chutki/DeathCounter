using System.Windows;
using Application = System.Windows.Application;

namespace EldenDeathCounter.Localization;

/// <summary>
/// Swaps the active UI-string <see cref="ResourceDictionary"/> (Strings.en.xaml / Strings.pl.xaml)
/// merged into the application resources so that <c>{DynamicResource}</c> bindings update live, and
/// exposes <see cref="GetString"/> for runtime strings built in code.
/// </summary>
public sealed class LocalizationService
{
    private const string LanguageDictionaryMarker = "/Localization/Strings.";
    private const string DefaultLanguage = "en";

    private static readonly IReadOnlyList<LocalizationLanguage> SupportedLanguages =
    [
        new("en", "English"),
        new("pl", "Polski")
    ];

    public static LocalizationService Instance { get; } = new();

    private string _currentLanguage = DefaultLanguage;

    public event EventHandler? LanguageChanged;

    public string CurrentLanguage => _currentLanguage;

    public IReadOnlyList<LocalizationLanguage> AvailableLanguages => SupportedLanguages;

    /// <summary>
    /// Normalizes an arbitrary language code to a supported one, defaulting to English.
    /// </summary>
    public static string NormalizeLanguage(string? language)
    {
        var trimmed = language?.Trim().ToLowerInvariant();
        return SupportedLanguages.Any(option => option.Code == trimmed) ? trimmed! : DefaultLanguage;
    }

    /// <summary>
    /// Swaps the merged language dictionary to the requested language and raises
    /// <see cref="LanguageChanged"/> when the language actually changes.
    /// </summary>
    public void SetLanguage(string? language)
    {
        var normalized = NormalizeLanguage(language);
        var application = Application.Current;
        if (application is null)
        {
            _currentLanguage = normalized;
            return;
        }

        var newDictionary = new ResourceDictionary
        {
            Source = new Uri(
                $"pack://application:,,,/EldenDeathCounter;component/Localization/Strings.{normalized}.xaml",
                UriKind.Absolute)
        };

        var merged = application.Resources.MergedDictionaries;
        var existing = merged
            .Where(dictionary => dictionary.Source is not null &&
                                 dictionary.Source.OriginalString.Contains(LanguageDictionaryMarker, StringComparison.OrdinalIgnoreCase))
            .ToList();

        merged.Add(newDictionary);
        foreach (var dictionary in existing)
        {
            merged.Remove(dictionary);
        }

        var changed = _currentLanguage != normalized;
        _currentLanguage = normalized;
        if (changed)
        {
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Resolves a localized string by key from the active dictionaries, falling back to the key
    /// itself when no resource is found (so missing keys are visible rather than throwing).
    /// </summary>
    public string GetString(string key)
    {
        return Application.Current?.TryFindResource(key) as string ?? key;
    }
}

public sealed record LocalizationLanguage(string Code, string DisplayName);
