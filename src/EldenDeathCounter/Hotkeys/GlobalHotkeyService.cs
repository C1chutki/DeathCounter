using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using EldenDeathCounter.Core.Hotkeys;
using EldenDeathCounter.Core.Logging;

namespace EldenDeathCounter.Hotkeys;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    private readonly ILogService _log;
    private readonly Dictionary<int, string> _registrations = [];
    private HwndSource? _source;
    private IntPtr _windowHandle;
    private int _nextId = 5000;

    public GlobalHotkeyService(ILogService log)
    {
        _log = log;
    }

    public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    public string Register(Window window, string hotkeyText, string name)
    {
        EnsureHook(window);

        var definition = HotkeyDefinition.Parse(hotkeyText);
        if (!definition.IsValid)
        {
            var message = $"{name}: {definition.Error}";
            _log.Error($"Hotkey registration failed. {message}");
            return message;
        }

        var id = _nextId++;
        var modifiers = ToNativeModifiers(definition);
        var virtualKey = ToVirtualKey(definition.Key);
        if (virtualKey == 0 || !RegisterHotKey(_windowHandle, id, modifiers, virtualKey))
        {
            var message = $"{name}: failed to register {hotkeyText}. It may already be used by another app.";
            _log.Error($"Hotkey registration failed. {message}");
            return message;
        }

        _registrations[id] = name;
        var success = $"{name}: {hotkeyText}";
        _log.Info($"Hotkey registered. {success}");
        return success;
    }

    public void Clear()
    {
        foreach (var id in _registrations.Keys.ToArray())
        {
            UnregisterHotKey(_windowHandle, id);
        }

        _registrations.Clear();
    }

    public void Dispose()
    {
        Clear();
        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
            _source = null;
        }
    }

    private void EnsureHook(Window window)
    {
        if (_source is not null)
        {
            return;
        }

        _windowHandle = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmHotkey && _registrations.TryGetValue(wParam.ToInt32(), out var name))
        {
            handled = true;
            HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(name));
        }

        return IntPtr.Zero;
    }

    private static uint ToNativeModifiers(HotkeyDefinition definition)
    {
        uint modifiers = 0;
        if (definition.Control)
        {
            modifiers |= ModControl;
        }

        if (definition.Alt)
        {
            modifiers |= ModAlt;
        }

        if (definition.Shift)
        {
            modifiers |= ModShift;
        }

        if (definition.Windows)
        {
            modifiers |= ModWin;
        }

        return modifiers;
    }

    private static uint ToVirtualKey(string key)
    {
        var wpfKey = key switch
        {
            "PAGEUP" => Key.PageUp,
            "PAGEDOWN" => Key.PageDown,
            _ => Enum.TryParse<Key>(key, ignoreCase: true, out var parsed) ? parsed : Key.None
        };

        return wpfKey == Key.None ? 0 : (uint)KeyInterop.VirtualKeyFromKey(wpfKey);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
