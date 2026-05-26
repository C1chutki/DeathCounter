using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EldenDeathCounter.Core.Detection;
using EldenDeathCounter.Core.Logging;

namespace EldenDeathCounter.Detection;

public sealed class ScreenCaptureService : IScreenCaptureService
{
    private readonly ILogService _log;
    private string? _loggedDeathTextCaptureTarget;
    private string? _loggedBossHealthBarCaptureTarget;

    public ScreenCaptureService(ILogService log)
    {
        _log = log;
    }

    public Task<CapturedFrame> CaptureAsync(string captureTarget, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var screen = SelectScreen(captureTarget);
            var bounds = screen.Bounds;

            var region = DeathTextCaptureRegionCalculator.Calculate(bounds.Width, bounds.Height);
            var captureLeft = bounds.Left + region.Left;
            var captureTop = bounds.Top + region.Top;
            if (!string.Equals(_loggedDeathTextCaptureTarget, captureTarget, StringComparison.OrdinalIgnoreCase))
            {
                _loggedDeathTextCaptureTarget = captureTarget;
                _log.Info($"Death text capture region: target='{captureTarget}', screen={bounds.Width}x{bounds.Height}@{bounds.Left},{bounds.Top}, region={region}, captureLeft={captureLeft}, captureTop={captureTop}.");
            }

            return Task.FromResult(CaptureRegion(captureLeft, captureTop, region.Width, region.Height));
        }
        catch (Exception exception)
        {
            _log.Error("Screenshot capture error.", exception);
            throw;
        }
    }

    public Task<CapturedFrame> CaptureFullScreenAsync(string captureTarget, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var screen = SelectScreen(captureTarget);
            var bounds = screen.Bounds;
            return Task.FromResult(CaptureRegion(bounds.Left, bounds.Top, bounds.Width, bounds.Height));
        }
        catch (Exception exception)
        {
            _log.Error("Full-screen screenshot capture error.", exception);
            throw;
        }
    }

    public Task<CapturedFrame> CaptureBossHealthBarAsync(string captureTarget, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var screen = SelectScreen(captureTarget);
            var bounds = screen.Bounds;
            var region = BossHealthBarCaptureRegionCalculator.Calculate(bounds.Width, bounds.Height);
            var captureLeft = bounds.Left + region.Left;
            var captureTop = bounds.Top + region.Top;
            if (!string.Equals(_loggedBossHealthBarCaptureTarget, captureTarget, StringComparison.OrdinalIgnoreCase))
            {
                _loggedBossHealthBarCaptureTarget = captureTarget;
                _log.Info($"Boss health bar capture region: target='{captureTarget}', screen={bounds.Width}x{bounds.Height}@{bounds.Left},{bounds.Top}, region={region}, captureLeft={captureLeft}, captureTop={captureTop}.");
            }

            return Task.FromResult(CaptureRegion(captureLeft, captureTop, region.Width, region.Height));
        }
        catch (Exception exception)
        {
            _log.Error("Boss health bar screenshot capture error.", exception);
            throw;
        }
    }

    private static CapturedFrame CaptureRegion(int left, int top, int width, int height)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(left, top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        return new CapturedFrame(bitmap);
    }

    private static Screen SelectScreen(string captureTarget)
    {
        var screens = Screen.AllScreens;
        if (screens.Length == 0)
        {
            throw new InvalidOperationException("No screens are available for capture.");
        }

        if (captureTarget.Equals("EldenRingWindow", StringComparison.OrdinalIgnoreCase) &&
            TryFindGameWindowScreen(screens, out var gameScreen))
        {
            return gameScreen;
        }

        if (captureTarget.StartsWith("Screen:", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(captureTarget["Screen:".Length..], out var screenIndex) &&
            screenIndex >= 0 &&
            screenIndex < screens.Length)
        {
            return screens[screenIndex];
        }

        return Screen.PrimaryScreen ?? screens[0];
    }

    private static bool TryFindGameWindowScreen(IReadOnlyList<Screen> screens, out Screen screen)
    {
        Screen? bestScreen = null;
        var bestArea = 0;

        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle) || !TryGetProcessName(handle, out var processName) || !IsEldenRingProcess(processName))
            {
                return true;
            }

            if (!GetWindowRect(handle, out var rect))
            {
                return true;
            }

            var windowBounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
            if (windowBounds.Width <= 0 || windowBounds.Height <= 0)
            {
                return true;
            }

            foreach (var candidate in screens)
            {
                var intersection = Rectangle.Intersect(candidate.Bounds, windowBounds);
                var area = intersection.Width * intersection.Height;
                if (area > bestArea)
                {
                    bestArea = area;
                    bestScreen = candidate;
                }
            }

            return true;
        }, IntPtr.Zero);

        screen = bestScreen ?? Screen.PrimaryScreen ?? screens[0];
        return bestScreen is not null;
    }

    private static bool TryGetProcessName(IntPtr windowHandle, out string processName)
    {
        processName = string.Empty;
        _ = GetWindowThreadProcessId(windowHandle, out var processId);
        if (processId == 0)
        {
            return false;
        }

        try
        {
            processName = Process.GetProcessById((int)processId).ProcessName;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsEldenRingProcess(string processName)
    {
        return processName.Equals("eldenring", StringComparison.OrdinalIgnoreCase) ||
               processName.Equals("start_protected_game", StringComparison.OrdinalIgnoreCase);
    }

    private delegate bool EnumWindowsProc(IntPtr handle, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr handle, out WindowRect rect);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct WindowRect
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;
    }
}
