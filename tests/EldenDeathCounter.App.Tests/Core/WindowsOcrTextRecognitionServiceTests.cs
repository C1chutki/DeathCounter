using System.Drawing;
using System.Drawing.Imaging;
using EldenDeathCounter.Core.Logging;
using EldenDeathCounter.Detection;
using Windows.Globalization;
using Windows.Media.Ocr;

namespace EldenDeathCounter.App.Tests.Core;

// Guards the optimized OCR path: SoftwareBitmap is now built straight from the GDI BGRA buffer with
// alpha ignored (no PNG round-trip). A wrong pixel format or premultiplying an undefined alpha channel
// would blank the image and make OCR return nothing, so we verify real text is still read end-to-end.
public sealed class WindowsOcrTextRecognitionServiceTests
{
    [Fact]
    public async Task RecognizesWhiteOnBlackBannerThroughBufferConversion()
    {
        if (!AnyOcrEngineAvailable())
        {
            // No Windows OCR language pack on this machine; nothing to verify here.
            return;
        }

        var service = new WindowsOcrTextRecognitionService(new NullLog());
        using var bitmap = RenderBanner("YOU DIED");

        var text = await service.RecognizeTextAsync(bitmap, ["en"], CancellationToken.None);

        Assert.Contains("DIED", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DoesNotDisposeCallerBitmapWhenNoResizeNeeded()
    {
        if (!AnyOcrEngineAvailable())
        {
            return;
        }

        var service = new WindowsOcrTextRecognitionService(new NullLog());
        // Small bitmap: ResizeForOcr returns it as-is, so a stray dispose would destroy this instance.
        using var bitmap = RenderBanner("TEST");

        await service.RecognizeTextAsync(bitmap, ["en"], CancellationToken.None);

        // The detection loop reads frame.Width right after OCR; this must not throw "Parameter is not valid".
        var width = bitmap.Width;
        Assert.True(width > 0);
    }

    private static bool AnyOcrEngineAvailable()
    {
        foreach (var tag in new[] { "en-US", "pl-PL" })
        {
            try
            {
                if (OcrEngine.IsLanguageSupported(new Language(tag)))
                {
                    return true;
                }
            }
            catch
            {
                // Ignore and try the next language.
            }
        }

        return OcrEngine.TryCreateFromUserProfileLanguages() is not null;
    }

    private static Bitmap RenderBanner(string text)
    {
        var bitmap = new Bitmap(700, 200, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Black);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        using var font = new Font("Arial", 64, FontStyle.Bold);
        graphics.DrawString(text, font, Brushes.White, 20, 50);
        return bitmap;
    }

    private sealed class NullLog : ILogService
    {
        public void Info(string message)
        {
        }

        public void Error(string message, Exception? exception = null)
        {
        }
    }
}
