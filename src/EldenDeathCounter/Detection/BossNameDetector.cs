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
    private static readonly Regex AllowedTextRegex = new(@"[^\p{L}\p{N}'’\-\s,()]+", RegexOptions.Compiled);
    private readonly ITextRecognitionService _textRecognitionService;
    private readonly ILogService _log;
    private readonly BossHealthBarAnalyzer _analyzer = new();

    public BossNameDetector(ITextRecognitionService textRecognitionService, ILogService log)
    {
        _textRecognitionService = textRecognitionService;
        _log = log;
    }

    public IReadOnlyList<BossHealthBarRegion> AnalyzeBars(Bitmap screenshot, string gameId)
    {
        try
        {
            var rectangle = new Rectangle(0, 0, screenshot.Width, screenshot.Height);
            var data = screenshot.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var byteCount = Math.Abs(data.Stride) * data.Height;
                var bytes = new byte[byteCount];
                Marshal.Copy(data.Scan0, bytes, 0, byteCount);

                return _analyzer.Analyze(screenshot.Width, screenshot.Height, gameId, (x, y) =>
                {
                    var row = data.Stride > 0 ? y : data.Height - 1 - y;
                    var offset = row * Math.Abs(data.Stride) + x * 4;
                    return new RgbPixel(bytes[offset + 2], bytes[offset + 1], bytes[offset]);
                });
            }
            finally
            {
                screenshot.UnlockBits(data);
            }
        }
        catch (Exception exception)
        {
            _log.Error("Boss health bar analysis error.", exception);
            return [];
        }
    }

    public async Task<BossNameDetectionResult> ReadBossNamesAsync(
        Bitmap screenshot,
        IReadOnlyList<BossHealthBarRegion> bars,
        BossNameMatcher matcher,
        CancellationToken cancellationToken)
    {
        var candidates = new List<BossNameCandidate>();
        foreach (var bar in bars.Take(3))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var nameBitmap = Crop(screenshot, bar.NameRegion);
            var ocrText = await _textRecognitionService.RecognizeTextAsync(nameBitmap, cancellationToken);
            var rawName = ExtractBossName(ocrText) ?? string.Empty;
            var match = string.IsNullOrWhiteSpace(rawName)
                ? BossNameMatch.None
                : matcher.Match(rawName);
            candidates.Add(new BossNameCandidate(rawName, match));
        }

        return BossNameDetectionResult.FromMatches(bars.Count, candidates);
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
