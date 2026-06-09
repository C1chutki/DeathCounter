using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using EldenDeathCounter.Core.Logging;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace EldenDeathCounter.Detection;

public sealed class WindowsOcrTextRecognitionService : ITextRecognitionService
{
    private readonly ILogService _log;
    private readonly IReadOnlyList<OcrLanguageEngine> _engines;

    public WindowsOcrTextRecognitionService(ILogService log)
    {
        _log = log;
        _engines = CreateEngines(log);
    }

    public Task<string> RecognizeTextAsync(Bitmap bitmap, CancellationToken cancellationToken)
        => RecognizeTextAsync(bitmap, Array.Empty<string>(), cancellationToken);

    public async Task<string> RecognizeTextAsync(
        Bitmap bitmap,
        IReadOnlyCollection<string> preferredLanguageCodes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_engines.Count == 0)
        {
            _log.Error("Windows OCR has no available English or Polish OCR engines. Install Windows OCR language support or use manual hotkeys.");
            return string.Empty;
        }

        try
        {
            var engines = SelectEngines(preferredLanguageCodes);
            // Not a `using`: when no scaling is needed ResizeForOcr returns the caller's bitmap as-is
            // (ownsResized=false). Disposing it here would destroy the detection loop's frame, which it
            // still needs after OCR returns. Only dispose the resized copy we actually allocated.
            var resized = ResizeForOcr(bitmap, out var ownsResized);
            try
            {
                using var softwareBitmap = ToSoftwareBitmap(resized);
                var lines = new List<string>();

                foreach (var engine in engines)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = await engine.Engine.RecognizeAsync(softwareBitmap);
                    if (!string.IsNullOrWhiteSpace(result.Text))
                    {
                        lines.Add(result.Text);
                    }
                }

                return string.Join(Environment.NewLine, lines.Distinct(StringComparer.OrdinalIgnoreCase));
            }
            finally
            {
                if (ownsResized)
                {
                    resized.Dispose();
                }
            }
        }
        catch (Exception exception)
        {
            _log.Error("OCR/template matching error.", exception);
            return string.Empty;
        }
    }

    private IReadOnlyList<OcrLanguageEngine> SelectEngines(IReadOnlyCollection<string> preferredLanguageCodes)
    {
        if (preferredLanguageCodes is null || preferredLanguageCodes.Count == 0)
        {
            return _engines;
        }

        var matched = _engines
            .Where(engine => preferredLanguageCodes.Any(code =>
                engine.LanguageCode.Equals(code?.Trim(), StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Fall back to every engine if the hint matches nothing we actually loaded, so a missing
        // language pack can never silently disable OCR.
        return matched.Count > 0 ? matched : _engines;
    }

    private static IReadOnlyList<OcrLanguageEngine> CreateEngines(ILogService log)
    {
        var engines = new List<OcrLanguageEngine>();
        foreach (var languageTag in new[] { "en-US", "pl-PL" })
        {
            try
            {
                var language = new Language(languageTag);
                if (!OcrEngine.IsLanguageSupported(language))
                {
                    log.Info($"Windows OCR language {languageTag} is not available.");
                    continue;
                }

                var engine = OcrEngine.TryCreateFromLanguage(language);
                if (engine is not null)
                {
                    engines.Add(new OcrLanguageEngine(LanguageCodeOf(languageTag), engine));
                    log.Info($"Windows OCR language {languageTag} initialized.");
                }
            }
            catch (Exception exception)
            {
                log.Error($"Failed to initialize Windows OCR language {languageTag}.", exception);
            }
        }

        if (engines.Count == 0)
        {
            var fallback = OcrEngine.TryCreateFromUserProfileLanguages();
            if (fallback is not null)
            {
                engines.Add(new OcrLanguageEngine(LanguageCodeOf(fallback.RecognizerLanguage.LanguageTag), fallback));
                log.Info("Windows OCR initialized from user profile languages.");
            }
        }

        return engines;
    }

    private static string LanguageCodeOf(string languageTag)
    {
        var dash = languageTag.IndexOf('-');
        return (dash > 0 ? languageTag[..dash] : languageTag).ToLowerInvariant();
    }

    private static Bitmap ResizeForOcr(Bitmap source, out bool ownsResult)
    {
        var maxDimension = (int)OcrEngine.MaxImageDimension;
        if (source.Width <= maxDimension && source.Height <= maxDimension)
        {
            // No scaling needed: hand back the caller's bitmap directly instead of cloning it.
            ownsResult = false;
            return source;
        }

        var scale = Math.Min((double)maxDimension / source.Width, (double)maxDimension / source.Height);
        var width = Math.Max(1, (int)(source.Width * scale));
        var height = Math.Max(1, (int)(source.Height * scale));
        var resized = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(resized);
        graphics.DrawImage(source, 0, 0, width, height);
        ownsResult = true;
        return resized;
    }

    // Convert the GDI bitmap straight from its BGRA pixel buffer into a SoftwareBitmap. The previous
    // implementation round-tripped through a PNG encode + decode on every frame, which dominated OCR
    // cost; a 32bpp ARGB bitmap is already byte-compatible with Bgra8 (B,G,R,A little-endian), and its
    // stride is always width*4 (no row padding), so we can copy the buffer directly. Alpha is ignored
    // because screen captures are opaque and CopyFromScreen leaves the alpha channel undefined.
    private static SoftwareBitmap ToSoftwareBitmap(Bitmap bitmap)
    {
        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var stride = Math.Abs(data.Stride);
            var byteCount = stride * data.Height;
            var bytes = new byte[byteCount];

            if (data.Stride > 0)
            {
                Marshal.Copy(data.Scan0, bytes, 0, byteCount);
            }
            else
            {
                // Bottom-up DIB: copy row by row into top-down order so the SoftwareBitmap is upright.
                for (var y = 0; y < data.Height; y++)
                {
                    var sourceRow = data.Scan0 + (data.Height - 1 - y) * stride;
                    Marshal.Copy(sourceRow, bytes, y * stride, stride);
                }
            }

            return SoftwareBitmap.CreateCopyFromBuffer(
                bytes.AsBuffer(),
                BitmapPixelFormat.Bgra8,
                bitmap.Width,
                bitmap.Height,
                BitmapAlphaMode.Ignore);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private sealed record OcrLanguageEngine(string LanguageCode, OcrEngine Engine);
}
