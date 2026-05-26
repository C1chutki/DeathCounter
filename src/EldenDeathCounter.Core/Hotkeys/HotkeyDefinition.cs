namespace EldenDeathCounter.Core.Hotkeys;

public sealed class HotkeyDefinition
{
    private static readonly HashSet<string> ModifierTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "CTRL",
        "CONTROL",
        "ALT",
        "SHIFT",
        "WIN",
        "WINDOWS"
    };

    public bool IsValid { get; init; }

    public string Key { get; init; } = string.Empty;

    public bool Control { get; init; }

    public bool Alt { get; init; }

    public bool Shift { get; init; }

    public bool Windows { get; init; }

    public string Error { get; init; } = string.Empty;

    public static HotkeyDefinition Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Invalid("Hotkey is empty.");
        }

        var tokens = value.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        string? key = null;
        var control = false;
        var alt = false;
        var shift = false;
        var windows = false;

        foreach (var token in tokens)
        {
            var upper = token.ToUpperInvariant();
            if (ModifierTokens.Contains(upper))
            {
                control |= upper is "CTRL" or "CONTROL";
                alt |= upper is "ALT";
                shift |= upper is "SHIFT";
                windows |= upper is "WIN" or "WINDOWS";
                continue;
            }

            if (key is not null)
            {
                return Invalid("Hotkey contains more than one key.");
            }

            key = upper;
        }

        if (key is null)
        {
            return Invalid("Hotkey must include a key.");
        }

        if (!IsSupportedKey(key))
        {
            return Invalid($"Unsupported hotkey key '{key}'.");
        }

        return new HotkeyDefinition
        {
            IsValid = true,
            Key = key,
            Control = control,
            Alt = alt,
            Shift = shift,
            Windows = windows
        };
    }

    private static HotkeyDefinition Invalid(string error)
    {
        return new HotkeyDefinition
        {
            IsValid = false,
            Error = error
        };
    }

    private static bool IsSupportedKey(string key)
    {
        if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
        {
            return true;
        }

        if (key.Length is 2 or 3 && key[0] == 'F' && int.TryParse(key[1..], out var functionKey))
        {
            return functionKey is >= 1 and <= 24;
        }

        return key is "SPACE" or "INSERT" or "DELETE" or "HOME" or "END" or "PAGEUP" or "PAGEDOWN";
    }
}
