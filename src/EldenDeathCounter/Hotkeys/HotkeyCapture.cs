using System.Windows.Input;

namespace EldenDeathCounter.Hotkeys;

/// <summary>
/// Turns a live keyboard press into the textual hotkey format understood by
/// <see cref="Core.Hotkeys.HotkeyDefinition"/> (e.g. "CTRL+SHIFT+A").
/// </summary>
public static class HotkeyCapture
{
    /// <summary>
    /// Builds a hotkey string from a pressed key plus the modifiers held at that moment.
    /// Returns <c>null</c> when the press is not a committable hotkey on its own — either a
    /// bare modifier (still waiting for the real key) or an unsupported key — so the caller
    /// should leave the current binding untouched.
    /// </summary>
    public static string? TryBuild(Key key, ModifierKeys modifiers)
    {
        if (IsModifierKey(key))
        {
            return null;
        }

        var token = ToToken(key);
        if (token is null)
        {
            return null;
        }

        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            parts.Add("CTRL");
        }

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            parts.Add("ALT");
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            parts.Add("SHIFT");
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            parts.Add("WIN");
        }

        parts.Add(token);
        return string.Join("+", parts);
    }

    private static bool IsModifierKey(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin;
    }

    private static string? ToToken(Key key)
    {
        if (key is >= Key.A and <= Key.Z)
        {
            return key.ToString();
        }

        if (key is >= Key.D0 and <= Key.D9)
        {
            return ((char)('0' + (key - Key.D0))).ToString();
        }

        if (key is >= Key.NumPad0 and <= Key.NumPad9)
        {
            return ((char)('0' + (key - Key.NumPad0))).ToString();
        }

        if (key is >= Key.F1 and <= Key.F24)
        {
            return "F" + (key - Key.F1 + 1);
        }

        return key switch
        {
            Key.Space => "SPACE",
            Key.Insert => "INSERT",
            Key.Delete => "DELETE",
            Key.Home => "HOME",
            Key.End => "END",
            Key.PageUp => "PAGEUP",
            Key.PageDown => "PAGEDOWN",
            _ => null
        };
    }
}
