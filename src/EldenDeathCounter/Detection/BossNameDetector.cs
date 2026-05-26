using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using EldenDeathCounter.Core.Detection;
using EldenDeathCounter.Core.Logging;

namespace EldenDeathCounter.Detection;

public sealed partial class BossNameDetector : IBossNameDetector
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex AllowedTextRegex = new(@"[^\p{L}\p{N}'’\-\s,]+", RegexOptions.Compiled);
    private readonly ITextRecognitionService _textRecognitionService;
    private readonly ILogService _log;
    private readonly BossHealthBarAnalyzer _analyzer = new();

    public BossNameDetector(ITextRecognitionService textRecognitionService, ILogService log)
    {
        _textRecognitionService = textRecognitionService;
        _log = log;
    }

    public async Task<string?> DetectBossNameAsync(Bitmap screenshot, CancellationToken cancellationToken)
    {
        var bars = AnalyzeBars(screenshot);
        if (bars.Count == 0)
        {
            return null;
        }

        var names = new List<string>();
        foreach (var bar in bars.Take(2))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var nameBitmap = Crop(screenshot, bar.NameRegion);
            var ocrText = await _textRecognitionService.RecognizeTextAsync(nameBitmap, cancellationToken);
            var name = ExtractBossName(ocrText);
            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }
            else
            {
                _log.Info($"Boss bar found, but OCR did not return a usable boss name. Region={bar.NameRegion}.");
            }
        }

        var distinctNames = names
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return distinctNames.Count == 0 ? null : string.Join(" + ", distinctNames);
    }

    private IReadOnlyList<BossHealthBarRegion> AnalyzeBars(Bitmap bitmap)
    {
        try
        {
            var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var byteCount = Math.Abs(data.Stride) * data.Height;
                var bytes = new byte[byteCount];
                Marshal.Copy(data.Scan0, bytes, 0, byteCount);

                return _analyzer.Analyze(bitmap.Width, bitmap.Height, (x, y) =>
                {
                    var row = data.Stride > 0 ? y : data.Height - 1 - y;
                    var offset = row * Math.Abs(data.Stride) + x * 4;
                    return new RgbPixel(bytes[offset + 2], bytes[offset + 1], bytes[offset]);
                });
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
        catch (Exception exception)
        {
            _log.Error("Boss health bar analysis error.", exception);
            return [];
        }
    }

    private static Bitmap Crop(Bitmap source, PixelRect rect)
    {
        var left = Math.Clamp(rect.Left, 0, source.Width - 1);
        var top = Math.Clamp(rect.Top, 0, source.Height - 1);
        var width = Math.Clamp(rect.Width, 1, source.Width - left);
        var height = Math.Clamp(rect.Height, 1, source.Height - top);
        return source.Clone(new Rectangle(left, top, width, height), PixelFormat.Format32bppArgb);
    }

    private static string? ExtractBossName(string ocrText)
    {
        var candidates = ocrText
            .Split(['\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(line => AllowedTextRegex.Replace(line, " "))
            .Select(line => WhitespaceRegex.Replace(line, " ").Trim())
            .Where(line => line.Length >= 3 && line.Any(char.IsLetter))
            .Where(line => !line.Contains("Śmierci", StringComparison.CurrentCultureIgnoreCase))
            .Where(line => !line.Contains("Smierci", StringComparison.CurrentCultureIgnoreCase))
            .OrderByDescending(line => line.Length)
            .ToList();

        return candidates.FirstOrDefault();
    }
}
