namespace EldenDeathCounter.Core.Detection;

public sealed class DeathTextTemplate
{
    private DeathTextTemplate(
        string name,
        int width,
        int height,
        IReadOnlyList<DeathTextTemplatePoint> strokePoints,
        IReadOnlyList<DeathTextTemplatePoint> backgroundPoints,
        IReadOnlyList<DeathTextTemplatePoint> edgePoints,
        byte[] edgePixels,
        int edgeWidth,
        int edgeHeight)
    {
        Name = name;
        Width = width;
        Height = height;
        StrokePoints = strokePoints;
        BackgroundPoints = backgroundPoints;
        EdgePoints = edgePoints;
        EdgePixels = edgePixels;
        EdgeWidth = edgeWidth;
        EdgeHeight = edgeHeight;
    }

    public string Name { get; }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<DeathTextTemplatePoint> StrokePoints { get; }

    public IReadOnlyList<DeathTextTemplatePoint> BackgroundPoints { get; }

    public IReadOnlyList<DeathTextTemplatePoint> EdgePoints { get; }

    public byte[] EdgePixels { get; }

    public int EdgeWidth { get; }

    public int EdgeHeight { get; }

    public static DeathTextTemplate FromReference(
        string name,
        int width,
        int height,
        Func<int, int, RgbPixel> getPixel,
        TextSignalPixelProfile? pixelProfile = null)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Template dimensions must be positive.");
        }

        pixelProfile ??= TextSignalPixelProfile.Death;
        var mask = new bool[width * height];
        var left = width;
        var right = -1;
        var top = height;
        var bottom = -1;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (!pixelProfile.IsTextPixel(getPixel(x, y)))
                {
                    continue;
                }

                mask[y * width + x] = true;
                left = Math.Min(left, x);
                right = Math.Max(right, x);
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
            }
        }

        if (right < left || bottom < top)
        {
            throw new InvalidOperationException("Reference image does not contain a detectable death-text template.");
        }

        left = Math.Max(0, left - 4);
        right = Math.Min(width - 1, right + 4);
        top = Math.Max(0, top - 4);
        bottom = Math.Min(height - 1, bottom + 4);

        var templateWidth = right - left + 1;
        var templateHeight = bottom - top + 1;
        var strokePoints = new List<DeathTextTemplatePoint>();
        var backgroundPoints = new List<DeathTextTemplatePoint>();
        var edgePoints = new List<DeathTextTemplatePoint>();
        var sampleStep = Math.Max(1, Math.Min(templateWidth, templateHeight) / 48);

        for (var y = top; y <= bottom; y += sampleStep)
        {
            for (var x = left; x <= right; x += sampleStep)
            {
                var isStroke = mask[y * width + x];
                var point = ToPoint(x, y, left, top, templateWidth, templateHeight);
                if (isStroke)
                {
                    strokePoints.Add(point);
                    if (IsMaskEdge(mask, width, height, x, y))
                    {
                        edgePoints.Add(point);
                    }
                }
                else if (((x + y) / sampleStep) % 3 == 0)
                {
                    backgroundPoints.Add(point);
                }
            }
        }

        if (strokePoints.Count == 0 || backgroundPoints.Count == 0 || edgePoints.Count == 0)
        {
            throw new InvalidOperationException("Reference image produced an unusable death-text template.");
        }

        var edgePixels = CreateTextMaskEdges(mask, width, height, left, top, templateWidth, templateHeight);
        return new DeathTextTemplate(name, templateWidth, templateHeight, strokePoints, backgroundPoints, edgePoints, edgePixels, templateWidth, templateHeight);
    }

    private static byte[] CreateTextMaskEdges(bool[] mask, int sourceWidth, int sourceHeight, int left, int top, int width, int height)
    {
        var edgePixels = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sourceX = left + x;
                var sourceY = top + y;
                if (ReadMask(mask, sourceWidth, sourceHeight, sourceX, sourceY) &&
                    IsMaskEdge(mask, sourceWidth, sourceHeight, sourceX, sourceY))
                {
                    edgePixels[y * width + x] = 255;
                }
            }
        }

        return edgePixels;
    }

    private static DeathTextTemplatePoint ToPoint(int x, int y, int left, int top, int width, int height)
    {
        return new DeathTextTemplatePoint(
            (double)(x - left + 0.5) / width,
            (double)(y - top + 0.5) / height);
    }

    private static bool IsMaskEdge(bool[] mask, int width, int height, int x, int y)
    {
        return !ReadMask(mask, width, height, x - 1, y) ||
               !ReadMask(mask, width, height, x + 1, y) ||
               !ReadMask(mask, width, height, x, y - 1) ||
               !ReadMask(mask, width, height, x, y + 1);
    }

    private static bool ReadMask(bool[] mask, int width, int height, int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height && mask[y * width + x];
    }

}
