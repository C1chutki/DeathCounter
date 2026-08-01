using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.Core.Detection;
using EldenDeathCounter.Core.Logging;

namespace EldenDeathCounter.Detection;

public sealed class ScreenCaptureService : IScreenCaptureService
{
    private readonly ILogService _log;
    private string? _loggedDeathTextCaptureTarget;
    private string? _loggedBossHealthBarCaptureTarget;
    private string? _loggedGameWindowFallbackCaptureTarget;

    // Resolving a game window enumerates every top-level window and opens a process handle per
    // visible window. That answer only changes when the game starts or moves monitors, so we cache it
    // briefly instead of paying the EnumWindows cost on every capture (death + boss bar, ~3×/s each).
    private static readonly TimeSpan ScreenResolutionCacheDuration = TimeSpan.FromSeconds(2);
    private readonly object _screenCacheLock = new();
    private string? _cachedScreenTarget;
    private Screen? _cachedScreen;
    private DateTimeOffset _cachedScreenAt = DateTimeOffset.MinValue;

    public ScreenCaptureService(ILogService log)
    {
        _log = log;
    }

    public string? CaptureStatus { get; private set; }

    public Task<CapturedFrame> CaptureAsync(string captureTarget, CancellationToken cancellationToken) =>
        CaptureAsync(captureTarget, gameId: null, cancellationToken);

    public Task<CapturedFrame> CaptureAsync(string captureTarget, string? gameId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var screen = SelectScreen(captureTarget);
            var bounds = screen.Bounds;

            var region = DeathTextCaptureRegionCalculator.Calculate(bounds.Width, bounds.Height, gameId);
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

    public Task<CapturedFrame> CaptureBossHealthBarAsync(string captureTarget, CancellationToken cancellationToken) =>
        CaptureBossHealthBarAsync(captureTarget, gameId: null, cancellationToken);

    public Task<CapturedFrame> CaptureBossHealthBarAsync(string captureTarget, string? gameId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var screen = SelectScreen(captureTarget);
            var bounds = screen.Bounds;
            var region = BossHealthBarCaptureRegionCalculator.Calculate(bounds.Width, bounds.Height, gameId);
            var captureLeft = bounds.Left + region.Left;
            var captureTop = bounds.Top + region.Top;
            var logKey = $"{captureTarget}|{gameId}";
            if (!string.Equals(_loggedBossHealthBarCaptureTarget, logKey, StringComparison.OrdinalIgnoreCase))
            {
                _loggedBossHealthBarCaptureTarget = logKey;
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
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(left, top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            return new CapturedFrame(bitmap);
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    private Screen SelectScreen(string captureTarget)
    {
        // Only the dynamic game-window lookup is expensive; explicit "Screen:N"/primary targets are
        // cheap, so we cache exclusively game-window paths and resolve the rest every time.
        if (!GameWindowTargetResolver.IsGameWindowTarget(captureTarget))
        {
            CaptureStatus = null;
            return SelectScreenUncached(captureTarget);
        }

        lock (_screenCacheLock)
        {
            if (_cachedScreen is not null &&
                string.Equals(_cachedScreenTarget, captureTarget, StringComparison.OrdinalIgnoreCase) &&
                DateTimeOffset.Now - _cachedScreenAt < ScreenResolutionCacheDuration)
            {
                return _cachedScreen;
            }

            var screen = SelectScreenUncached(captureTarget);
            _cachedScreen = screen;
            _cachedScreenTarget = captureTarget;
            _cachedScreenAt = DateTimeOffset.Now;
            return screen;
        }
    }

    private Screen SelectScreenUncached(string captureTarget)
    {
        var screens = Screen.AllScreens;
        if (screens.Length == 0)
        {
            throw new InvalidOperationException("No screens are available for capture.");
        }

        if (GameWindowTargetResolver.IsGameWindowTarget(captureTarget))
        {
            if (TryFindGameWindowScreen(captureTarget, screens, out var gameScreen))
            {
                CaptureStatus = null;
                _loggedGameWindowFallbackCaptureTarget = null;
                return gameScreen;
            }

            CaptureStatus = "Detection running; game window not found, using primary screen.";
            if (!string.Equals(_loggedGameWindowFallbackCaptureTarget, captureTarget, StringComparison.OrdinalIgnoreCase))
            {
                _loggedGameWindowFallbackCaptureTarget = captureTarget;
                _log.Info($"Game window for target='{captureTarget}' was not found; falling back to the primary screen.");
            }
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

    private static bool TryFindGameWindowScreen(string captureTarget, IReadOnlyList<Screen> screens, out Screen screen)
    {
        Screen? bestScreen = null;
        var bestArea = 0;

        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle) || !TryGetProcessName(handle, out var processName) || !GameWindowTargetResolver.IsMatchingProcess(captureTarget, processName))
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
            using var process = Process.GetProcessById((int)processId);
            processName = process.ProcessName;
            return true;
        }
        catch
        {
            return false;
        }
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
