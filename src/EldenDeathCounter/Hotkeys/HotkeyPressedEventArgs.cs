namespace EldenDeathCounter.Hotkeys;

public sealed class HotkeyPressedEventArgs : EventArgs
{
    public HotkeyPressedEventArgs(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
