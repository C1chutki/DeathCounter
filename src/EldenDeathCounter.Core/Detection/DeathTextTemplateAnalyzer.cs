using System.Globalization;

namespace EldenDeathCounter.Core.Detection;

public sealed class DeathTextTemplateAnalyzer
{
    private static readonly double[] ScaleFactors = [0.60, 0.70, 0.85, 0.95, 1.00, 1.05, 1.15];

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
        var threshold = 0.24 + clampedSensitivity * 0.06;
        var searchLeft = pixelProfile.Name == TextSignalPixelProfile.BossVictory.Name
            ? (int)(width * 0.05)
            : (int)(width * 0.18);
        var searchRight = pixelProfile.Name == TextSignalPixelProfile.BossVictory.Name
            ? (int)(width * 0.95)
            : (int)(width * 0.82);
        var searchTop = (int)(height * 0.18);
        var searchBottom = pixelProfile.Name == TextSignalPixelProfile.BossVictory.Name
            ? (int)(height * 0.92)
            : (int)(height * 0.78);
        var best = ImageDeathSignalMatch.NoMatch;
        var edgePixels = CreateDeathTextMaskEdges(width, height, getPixel, pixelProfile);
        var edgeIntegral = BuildIntegralImage(edgePixels, width, height);

        foreach (var template in templates)
        {
            foreach (var scale in ScaleFactors)
            {
                var candidateWidth = Math.Max(12, (int)Math.Round(template.EdgeWidth * scale));
                var candidateHeight = Math.Max(8, (int)Math.Round(template.EdgeHeight * scale));
                if (candidateWidth >= width || candidateHeight >= height)
                {
                    continue;
                }

                var templateEdgePoints = BuildScaledTemplateEdgePoints(template, candidateWidth, candidateHeight);
                if (templateEdgePoints.Count == 0)
                {
                    continue;
                }

                var candidate = MatchScale(
                    edgePixels,
                    edgeIntegral,
                    width,
                    height,
                    templateEdgePoints,
                    candidateWidth,
                    candidateHeight,
                    searchLeft,
                    searchTop,
                    searchRight,
                    searchBottom,
                    pixelProfile);
                if (candidate.Score > best.Score)
                {
                    var contrast = AnalyzeDeathTextContrast(
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
                        $"template:{template.Name}",
                        scale)
                    {
                        Threshold = threshold,
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
        }

        return best.IsMatch ? best : best with { IsMatch = false };
    }

    private static TemplateMatchCandidate MatchScale(
        byte[] edgePixels,
        int[] edgeIntegral,
        int imageWidth,
        int imageHeight,
        IReadOnlyList<TemplateEdgePoint> templateEdgePoints,
        int templateWidth,
        int templateHeight,
        int searchLeft,
        int searchTop,
        int searchRight,
        int searchBottom,
        TextSignalPixelProfile pixelProfile)
    {
        var roiLeft = Math.Clamp(searchLeft, 0, imageWidth - 1);
        var roiTop = Math.Clamp(searchTop, 0, imageHeight - 1);
        if (roiLeft + templateWidth > imageWidth || roiTop + templateHeight > imageHeight)
        {
            return TemplateMatchCandidate.None;
        }

        var roiRight = Math.Clamp(searchRight, roiLeft + templateWidth, imageWidth);
        var roiBottom = Math.Clamp(searchBottom, roiTop + templateHeight, imageHeight);
        if (roiRight - roiLeft < templateWidth || roiBottom - roiTop < templateHeight)
        {
            return TemplateMatchCandidate.None;
        }

        var searchStep = ChooseSearchStep(templateWidth, templateHeight, pixelProfile);
        var best = SearchBestCandidate(
            edgePixels,
            edgeIntegral,
            imageWidth,
            templateEdgePoints,
            templateWidth,
            templateHeight,
            roiLeft,
            roiTop,
            roiRight - templateWidth,
            roiBottom - templateHeight,
            searchStep);

        if (searchStep == 1 || best.Score <= 0)
        {
            return best;
        }

        var refineLeft = Math.Max(roiLeft, best.Left - searchStep);
        var refineTop = Math.Max(roiTop, best.Top - searchStep);
        var refineRight = Math.Min(roiRight - templateWidth, best.Left + searchStep);
        var refineBottom = Math.Min(roiBottom - templateHeight, best.Top + searchStep);
        var refined = SearchBestCandidate(
            edgePixels,
            edgeIntegral,
            imageWidth,
            templateEdgePoints,
            templateWidth,
            templateHeight,
            refineLeft,
            refineTop,
            refineRight,
            refineBottom,
            1);

        return refined.Score >= best.Score ? refined : best;
    }

    private static TemplateMatchCandidate SearchBestCandidate(
        byte[] edgePixels,
        int[] edgeIntegral,
        int imageWidth,
        IReadOnlyList<TemplateEdgePoint> templateEdgePoints,
        int templateWidth,
        int templateHeight,
        int left,
        int top,
        int right,
        int bottom,
        int step)
    {
        var best = TemplateMatchCandidate.None;
        for (var y = top; y <= bottom; y += step)
        {
            for (var x = left; x <= right; x += step)
            {
                var score = ScoreCandidate(
                    edgePixels,
                    edgeIntegral,
                    imageWidth,
                    templateEdgePoints,
                    x,
                    y,
                    templateWidth,
                    templateHeight);
                if (score > best.Score)
                {
                    best = new TemplateMatchCandidate(score, x, y);
                }
            }
        }

        return best;
    }

    private static double ScoreCandidate(
        byte[] edgePixels,
        int[] edgeIntegral,
        int imageWidth,
        IReadOnlyList<TemplateEdgePoint> templateEdgePoints,
        int left,
        int top,
        int templateWidth,
        int templateHeight)
    {
        if (CountEdges(edgeIntegral, imageWidth, left, top, templateWidth, templateHeight) == 0)
        {
            return 0;
        }

        var hits = 0;
        var rowBuckets = new bool[16];
        foreach (var point in templateEdgePoints)
        {
            var x = left + point.X;
            var y = top + point.Y;
            if (edgePixels[y * imageWidth + x] != 0)
            {
                hits++;
                var bucket = Math.Clamp((int)((double)point.Y / Math.Max(1, templateHeight) * rowBuckets.Length), 0, rowBuckets.Length - 1);
                rowBuckets[bucket] = true;
            }
        }

        if (hits == 0)
        {
            return 0;
        }

        var templateCoverage = (double)hits / templateEdgePoints.Count;
        var verticalCoverage = (double)rowBuckets.Count(isHit => isHit) / rowBuckets.Length;
        var normalized = templateCoverage * Math.Sqrt(verticalCoverage);
        return double.IsNaN(normalized) ? 0 : Math.Clamp(normalized, 0, 1);
    }

    private static int ChooseSearchStep(int templateWidth, int templateHeight, TextSignalPixelProfile pixelProfile)
    {
        if (pixelProfile.Name == TextSignalPixelProfile.BossVictory.Name)
        {
            var bossShortestSide = Math.Min(templateWidth, templateHeight);
            if (bossShortestSide >= 140)
            {
                return 4;
            }

            return bossShortestSide >= 80 ? 3 : 2;
        }

        var shortestSide = Math.Min(templateWidth, templateHeight);
        if (shortestSide >= 140)
        {
            return 8;
        }

        return shortestSide >= 80 ? 6 : 3;
    }

    private static IReadOnlyList<TemplateEdgePoint> BuildScaledTemplateEdgePoints(
        DeathTextTemplate template,
        int targetWidth,
        int targetHeight)
    {
        var uniquePoints = new HashSet<int>();
        var points = new List<TemplateEdgePoint>(template.EdgePoints.Count);
        foreach (var sourcePoint in template.EdgePoints)
        {
            var targetX = Math.Clamp((int)Math.Round(sourcePoint.X * targetWidth - 0.5), 0, targetWidth - 1);
            var targetY = Math.Clamp((int)Math.Round(sourcePoint.Y * targetHeight - 0.5), 0, targetHeight - 1);
            var key = targetY * targetWidth + targetX;
            if (uniquePoints.Add(key))
            {
                points.Add(new TemplateEdgePoint(targetX, targetY));
            }
        }

        return points;
    }

    private static int[] BuildIntegralImage(byte[] edgePixels, int width, int height)
    {
        var stride = width + 1;
        var integral = new int[stride * (height + 1)];
        for (var y = 0; y < height; y++)
        {
            var rowSum = 0;
            for (var x = 0; x < width; x++)
            {
                if (edgePixels[y * width + x] != 0)
                {
                    rowSum++;
                }

                integral[(y + 1) * stride + x + 1] = integral[y * stride + x + 1] + rowSum;
            }
        }

        return integral;
    }

    private static int CountEdges(int[] integral, int imageWidth, int left, int top, int width, int height)
    {
        var stride = imageWidth + 1;
        var right = left + width;
        var bottom = top + height;
        return integral[bottom * stride + right] -
               integral[top * stride + right] -
               integral[bottom * stride + left] +
               integral[top * stride + left];
    }

    private static DeathTextContrastStats AnalyzeDeathTextContrast(
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
        var strokeStats = SampleDeathTextPixels(template.StrokePoints, left, top, width, height, imageWidth, imageHeight, getPixel, pixelProfile);
        var backgroundStats = SampleDeathTextPixels(template.BackgroundPoints, left, top, width, height, imageWidth, imageHeight, getPixel, pixelProfile);
        var hasContrast = strokeStats.Ratio >= 0.30 &&
                          strokeStats.Ratio >= backgroundStats.Ratio + 0.16 &&
                          strokeStats.VerticalCoverage >= 0.42;
        return new DeathTextContrastStats(
            hasContrast,
            strokeStats.Ratio,
            backgroundStats.Ratio,
            strokeStats.VerticalCoverage);
    }

    private static DeathTextSampleStats SampleDeathTextPixels(
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
            return new DeathTextSampleStats(0, 0);
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

        return new DeathTextSampleStats(
            (double)hits / points.Count,
            (double)rowBuckets.Count(isHit => isHit) / rowBuckets.Length);
    }

    private static byte[] CreateDeathTextMaskEdges(
        int width,
        int height,
        Func<int, int, RgbPixel> getPixel,
        TextSignalPixelProfile pixelProfile)
    {
        var mask = new bool[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                mask[y * width + x] = pixelProfile.IsTextPixel(getPixel(x, y));
            }
        }

        var dilatedEdges = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (ReadMask(mask, width, height, x, y) &&
                    (!ReadMask(mask, width, height, x - 1, y) ||
                     !ReadMask(mask, width, height, x + 1, y) ||
                     !ReadMask(mask, width, height, x, y - 1) ||
                     !ReadMask(mask, width, height, x, y + 1)))
                {
                    DilateEdgePixel(dilatedEdges, width, height, x, y);
                }
            }
        }

        return dilatedEdges;
    }

    private static void DilateEdgePixel(byte[] edges, int width, int height, int centerX, int centerY)
    {
        for (var y = Math.Max(0, centerY - 1); y <= Math.Min(height - 1, centerY + 1); y++)
        {
            for (var x = Math.Max(0, centerX - 1); x <= Math.Min(width - 1, centerX + 1); x++)
            {
                edges[y * width + x] = 255;
            }
        }
    }

    private static bool ReadMask(bool[] mask, int width, int height, int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height && mask[y * width + x];
    }

    private readonly record struct TemplateMatchCandidate(double Score, int Left, int Top)
    {
        public static readonly TemplateMatchCandidate None = new(0, 0, 0);
    }

    private readonly record struct TemplateEdgePoint(int X, int Y);

    private readonly record struct DeathTextSampleStats(double Ratio, double VerticalCoverage);

    private readonly record struct DeathTextContrastStats(
        bool HasContrast,
        double StrokeRatio,
        double BackgroundRatio,
        double VerticalCoverage);

    private static string FormatScore(double value)
    {
        return value.ToString("0.000", CultureInfo.InvariantCulture);
    }
}
