using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class BossVictoryTextTemplateReferenceImageTests
{
    [Fact]
    public void BuildsTemplateFromProvidedWinScreenAndMatchesItBack()
    {
        var imagePath = GetAssetPath("PL_Win_screen.jpg");
        Assert.True(File.Exists(imagePath), imagePath);

        using var bitmap = new Bitmap(imagePath);
        var crop = DeathTextTemplateReferenceRegion.BossVictory(bitmap.Width, bitmap.Height);

        var template = WithLockedPixels(bitmap, getPixel => DeathTextTemplate.FromReference(
            "POKONANO WROGA",
            crop.Width,
            crop.Height,
            (x, y) => getPixel(crop.Left + x, crop.Top + y),
            TextSignalPixelProfile.BossVictory));
        var analyzer = new DeathTextTemplateAnalyzer();
        var capture = DeathTextCaptureRegionCalculator.Calculate(bitmap.Width, bitmap.Height);
        var result = WithLockedPixels(bitmap, getPixel => analyzer.Analyze(
            capture.Width,
            capture.Height,
            (x, y) => getPixel(capture.Left + x, capture.Top + y),
            [template],
            0.8,
            TextSignalPixelProfile.BossVictory));

        Assert.True(template.StrokePoints.Count > 50, $"strokePoints={template.StrokePoints.Count}, edgePoints={template.EdgePoints.Count}");
        Assert.True(
            result.IsMatch,
            $"score={result.Score:0.000}, scale={result.Scale:0.00}, method={result.Method}, template={template.Width}x{template.Height}, strokePoints={template.StrokePoints.Count}, edgePoints={template.EdgePoints.Count}, details={result.Details}");
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
