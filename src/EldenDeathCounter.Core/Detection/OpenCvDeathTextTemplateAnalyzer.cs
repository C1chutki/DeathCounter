using System.Globalization;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace EldenDeathCounter.Core.Detection;

public sealed class OpenCvDeathTextTemplateAnalyzer
{
    private static readonly double[] ScaleFactors = [0.60, 0.85, 1.00, 1.15];

    public ImageDeathSignalMatch Analyze(
        int width,
        int height,
        Func<int, int, RgbPixel> getPixel,
        IReadOnlyList<DeathTextTemplate> templates,
        double sensitivity,
        TextSignalPixelProfile? pixelProfile = null)
    {
        if (width <= 0 || height <= 0 || templates.Count == 0)
        {
            return ImageDeathSignalMatch.NoMatch;
        }

        pixelProfile ??= TextSignalPixelProfile.Death;
        var clampedSensitivity = Math.Clamp(sensitivity, 0.1, 1.0);
        var threshold = 0.34 + clampedSensitivity * 0.12;
        using var edgePixels = CreateDilatedTextEdges(width, height, getPixel, pixelProfile);
        var searchRegion = CreateSearchRegion(width, height, pixelProfile);
        var best = ImageDeathSignalMatch.NoMatch;

        foreach (var template in templates)
        {
            using var templateEdges = CreateTemplateEdgeMat(template);
            foreach (var scale in ScaleFactors)
            {
                var candidateWidth = Math.Max(12, (int)Math.Round(template.EdgeWidth * scale));
                var candidateHeight = Math.Max(8, (int)Math.Round(template.EdgeHeight * scale));
                if (candidateWidth >= searchRegion.Width || candidateHeight >= searchRegion.Height)
                {
                    continue;
                }

                using var scaledTemplate = new Mat();
                Cv2.Resize(templateEdges, scaledTemplate, new Size(candidateWidth, candidateHeight), 0, 0, InterpolationFlags.Nearest);
                var candidate = FindBestCandidate(edgePixels, scaledTemplate, searchRegion);
                if (candidate.Score <= best.Score)
                {
                    continue;
                }

                var contrast = AnalyzeTextContrast(
                    candidate.Left,
                    candidate.Top,
                    candidateWidth,
                    candidateHeight,
                    template,
                    width,
                    height,
                    getPixel,
                    pixelProfile);
                best = new ImageDeathSignalMatch(
                    candidate.Score >= threshold && contrast.HasContrast,
                    candidate.Score,
                    $"template:{template.Name}:opencv",
                    scale)
                {
                    Details =
                        $"threshold={FormatScore(threshold)}; " +
                        $"candidate={candidate.Left},{candidate.Top},{candidateWidth}x{candidateHeight}; " +
                        $"contrast={contrast.HasContrast}; " +
                        $"strokeRatio={FormatScore(contrast.StrokeRatio)}; " +
                        $"backgroundRatio={FormatScore(contrast.BackgroundRatio)}; " +
                        $"verticalCoverage={FormatScore(contrast.VerticalCoverage)}"
                };
            }
        }

        return best.IsMatch ? best : best with { IsMatch = false };
    }

    private static Mat CreateDilatedTextEdges(
        int width,
        int height,
        Func<int, int, RgbPixel> getPixel,
        TextSignalPixelProfile pixelProfile)
    {
        var maskBytes = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (pixelProfile.IsTextPixel(getPixel(x, y)))
                {
                    maskBytes[y * width + x] = 255;
                }
            }
        }

        var mask = new Mat(height, width, MatType.CV_8UC1);
        Marshal.Copy(maskBytes, 0, mask.Data, maskBytes.Length);
        using var edges = new Mat();
        Cv2.Canny(mask, edges, 40, 120);
        mask.Dispose();

        var dilated = new Mat();
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
        Cv2.Dilate(edges, dilated, kernel);
        return dilated;
    }

    private static Mat CreateTemplateEdgeMat(DeathTextTemplate template)
    {
        var mat = new Mat(template.EdgeHeight, template.EdgeWidth, MatType.CV_8UC1);
        Marshal.Copy(template.EdgePixels, 0, mat.Data, template.EdgePixels.Length);
        return mat;
    }

    private static PixelRect CreateSearchRegion(int width, int height, TextSignalPixelProfile pixelProfile)
    {
        var left = pixelProfile.Name == TextSignalPixelProfile.BossVictory.Name
            ? (int)(width * 0.05)
            : (int)(width * 0.18);
        var right = pixelProfile.Name == TextSignalPixelProfile.BossVictory.Name
            ? (int)(width * 0.95)
            : (int)(width * 0.82);
        var top = (int)(height * 0.18);
        var bottom = pixelProfile.Name == TextSignalPixelProfile.BossVictory.Name
            ? (int)(height * 0.92)
            : (int)(height * 0.78);
        return new PixelRect(left, top, right, bottom);
    }

    private static TemplateMatchCandidate FindBestCandidate(Mat imageEdges, Mat templateEdges, PixelRect searchRegion)
    {
        var roi = new Rect(searchRegion.Left, searchRegion.Top, searchRegion.Width, searchRegion.Height);
        using var searchImage = new Mat(imageEdges, roi);
        using var result = new Mat();
        Cv2.MatchTemplate(searchImage, templateEdges, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out _, out var maxValue, out _, out var maxLocation);
        if (double.IsNaN(maxValue) || double.IsInfinity(maxValue))
        {
            return TemplateMatchCandidate.None;
        }

        return new TemplateMatchCandidate(Math.Clamp(maxValue, 0, 1), searchRegion.Left + maxLocation.X, searchRegion.Top + maxLocation.Y);
    }

    private static TextContrastStats AnalyzeTextContrast(
        int left,
        int top,
        int width,
        int height,
        DeathTextTemplate template,
        int imageWidth,
        int imageHeight,
        Func<int, int, RgbPixel> getPixel,
        TextSignalPixelProfile pixelProfile)
    {
        var strokeStats = SampleTextPixels(template.StrokePoints, left, top, width, height, imageWidth, imageHeight, getPixel, pixelProfile);
        var backgroundStats = SampleTextPixels(template.BackgroundPoints, left, top, width, height, imageWidth, imageHeight, getPixel, pixelProfile);
        var hasContrast = strokeStats.Ratio >= 0.30 &&
                          strokeStats.Ratio >= backgroundStats.Ratio + 0.16 &&
                          strokeStats.VerticalCoverage >= 0.42;
        return new TextContrastStats(
            hasContrast,
            strokeStats.Ratio,
            backgroundStats.Ratio,
            strokeStats.VerticalCoverage);
    }

    private static TextSampleStats SampleTextPixels(
        IReadOnlyList<DeathTextTemplatePoint> points,
        int left,
        int top,
        int width,
        int height,
        int imageWidth,
        int imageHeight,
        Func<int, int, RgbPixel> getPixel,
        TextSignalPixelProfile pixelProfile)
    {
        if (points.Count == 0)
        {
            return new TextSampleStats(0, 0);
        }

        var hits = 0;
        var rowBuckets = new bool[16];
        foreach (var point in points)
        {
            var x = Math.Clamp(left + (int)Math.Round(point.X * width), 0, imageWidth - 1);
            var y = Math.Clamp(top + (int)Math.Round(point.Y * height), 0, imageHeight - 1);
            if (pixelProfile.IsTextPixel(getPixel(x, y)))
            {
                hits++;
                var bucket = Math.Clamp((int)(point.Y * rowBuckets.Length), 0, rowBuckets.Length - 1);
                rowBuckets[bucket] = true;
            }
        }

        return new TextSampleStats(
            (double)hits / points.Count,
            (double)rowBuckets.Count(isHit => isHit) / rowBuckets.Length);
    }

    private readonly record struct TemplateMatchCandidate(double Score, int Left, int Top)
    {
        public static readonly TemplateMatchCandidate None = new(0, 0, 0);
    }

    private readonly record struct TextSampleStats(double Ratio, double VerticalCoverage);

    private readonly record struct TextContrastStats(
        bool HasContrast,
        double StrokeRatio,
        double BackgroundRatio,
        double VerticalCoverage);

    private static string FormatScore(double value)
    {
        return value.ToString("0.000", CultureInfo.InvariantCulture);
    }
}
