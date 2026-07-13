using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class OpenCvDeathTextTemplateAnalyzerTests
{
    [Fact]
    public void MatchesMaintainedEnglishDeathScreenQuickly()
    {
        var screenshotPath = GetAssetPath(Path.Combine("Elden Ring", "ENG_Death_Screen_v9.png"));
        Assert.True(File.Exists(screenshotPath), screenshotPath);

        using var bitmap = new Bitmap(screenshotPath);
        var template = CreateTemplate(bitmap, "ENG_Death_Screen_v9.png");
        var analyzer = new OpenCvDeathTextTemplateAnalyzer();
        var elapsed = WithLockedPixels(bitmap, getPixel =>
        {
            var stopwatch = Stopwatch.StartNew();
            var result = analyzer.Analyze(
                bitmap.Width,
                bitmap.Height,
                getPixel,
                [template],
                0.8);
            stopwatch.Stop();

            Assert.True(result.IsMatch, $"score={result.Score:0.000}, scale={result.Scale:0.00}, details={result.Details}");
            return stopwatch.Elapsed;
        });

        Assert.True(elapsed < TimeSpan.FromMilliseconds(750), $"OpenCV analyzer took {elapsed.TotalMilliseconds:0.0} ms.");
    }

    [Fact]
    public void IgnoresRedBackgroundWithoutDeathTextShape()
    {
        using var bitmap = new Bitmap(GetAssetPath(Path.Combine("Elden Ring", "ENG_Death_Screen.png")));
        var template = CreateTemplate(bitmap, "ENG_Death_Screen.jpg");
        var analyzer = new OpenCvDeathTextTemplateAnalyzer();

        var result = analyzer.Analyze(
            640,
            220,
            (_, _) => new RgbPixel(155, 25, 28),
            [template],
            0.8);

        Assert.False(result.IsMatch, $"score={result.Score:0.000}, details={result.Details}");
    }

    [Fact]
    public void IgnoresBossBarWithoutDeathTextShape()
    {
        using var bitmap = new Bitmap(GetAssetPath(Path.Combine("Elden Ring", "ENG_Death_Screen.png")));
        var template = CreateTemplate(bitmap, "ENG_Death_Screen.jpg");
        var analyzer = new OpenCvDeathTextTemplateAnalyzer();

        var result = analyzer.Analyze(
            640,
            220,
            (x, y) =>
            {
                var inBossBar = x is > 120 and < 520 && y is > 170 and < 180;
                return inBossBar ? new RgbPixel(145, 12, 18) : new RgbPixel(32, 34, 36);
            },
            [template],
            0.8);

        Assert.False(result.IsMatch, $"score={result.Score:0.000}, details={result.Details}");
    }

    private static DeathTextTemplate CreateTemplate(Bitmap bitmap, string name)
    {
        var crop = DeathTextTemplateReferenceRegion.DeathScreen(bitmap.Width, bitmap.Height);

        return WithLockedPixels(bitmap, getPixel => DeathTextTemplate.FromReference(
            name,
            crop.Width,
            crop.Height,
            (x, y) => getPixel(crop.Left + x, crop.Top + y)));
    }

    private static T WithLockedPixels<T>(Bitmap bitmap, Func<Func<int, int, RgbPixel>, T> read)
    {
        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var stride = Math.Abs(data.Stride);
            var bytes = new byte[stride * data.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            return read((x, y) =>
            {
                var clampedX = Math.Clamp(x, 0, data.Width - 1);
                var clampedY = Math.Clamp(y, 0, data.Height - 1);
                var row = data.Stride > 0 ? clampedY : data.Height - 1 - clampedY;
                var offset = row * stride + clampedX * 4;
                return new RgbPixel(bytes[offset + 2], bytes[offset + 1], bytes[offset]);
            });
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static string GetAssetPath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EldenDeathCounter.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "Assets", fileName);
    }
}
