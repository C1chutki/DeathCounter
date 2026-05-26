using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using EldenDeathCounter.Core.Logging;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace EldenDeathCounter.Detection;

public sealed class WindowsOcrTextRecognitionService : ITextRecognitionService
{
    private readonly ILogService _log;
    private readonly IReadOnlyList<OcrEngine> _engines;

    public WindowsOcrTextRecognitionService(ILogService log)
    {
        _log = log;
        _engines = CreateEngines(log);
    }

    public async Task<string> RecognizeTextAsync(Bitmap bitmap, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_engines.Count == 0)
        {
            _log.Error("Windows OCR has no available English or Polish OCR engines. Install Windows OCR language support or use manual hotkeys.");
            return string.Empty;
        }

        try
        {
            using var resized = ResizeForOcr(bitmap);
            using var softwareBitmap = await ToSoftwareBitmapAsync(resized);
            var lines = new List<string>();

            foreach (var engine in _engines)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await engine.RecognizeAsync(softwareBitmap);
                if (!string.IsNullOrWhiteSpace(result.Text))
                {
                    lines.Add(result.Text);
                }
            }

            return string.Join(Environment.NewLine, lines.Distinct(StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            _log.Error("OCR/template matching error.", exception);
            return string.Empty;
        }
    }

    private static IReadOnlyList<OcrEngine> CreateEngines(ILogService log)
    {
        var engines = new List<OcrEngine>();
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
                    engines.Add(engine);
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
                engines.Add(fallback);
                log.Info("Windows OCR initialized from user profile languages.");
            }
        }

        return engines;
    }

    private static Bitmap ResizeForOcr(Bitmap source)
    {
        var maxDimension = (int)OcrEngine.MaxImageDimension;
        if (source.Width <= maxDimension && source.Height <= maxDimension)
        {
            return new Bitmap(source);
        }

        var scale = Math.Min((double)maxDimension / source.Width, (double)maxDimension / source.Height);
        var width = Math.Max(1, (int)(source.Width * scale));
        var height = Math.Max(1, (int)(source.Height * scale));
        var resized = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(resized);
        graphics.DrawImage(source, 0, 0, width, height);
        return resized;
    }

    private static async Task<SoftwareBitmap> ToSoftwareBitmapAsync(Bitmap bitmap)
    {
        await using var memory = new MemoryStream();
        bitmap.Save(memory, ImageFormat.Png);
        var bytes = memory.ToArray();

        using var randomAccessStream = new InMemoryRandomAccessStream();
        using var writer = new DataWriter(randomAccessStream);
        writer.WriteBytes(bytes);
        await writer.StoreAsync();
        await writer.FlushAsync();
        writer.DetachStream();

        randomAccessStream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        return await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    }
}
