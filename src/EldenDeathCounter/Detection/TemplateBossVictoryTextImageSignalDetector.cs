using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using EldenDeathCounter.Core.Detection;
using EldenDeathCounter.Core.Logging;

namespace EldenDeathCounter.Detection;

public sealed class TemplateBossVictoryTextImageSignalDetector : IImageBossVictorySignalDetector
{
    private static readonly string[] DefaultTemplateFileNames =
    [
        "PL_Win_screen.jpg"
    ];

    private static readonly string[] EnglishTemplateFileNames =
    [
        "ENG_Win_Screen.jpg",
        "ENG_Win_Screen_v2.jpg"
    ];

    private readonly ILogService _log;
    private readonly OpenCvDeathTextTemplateAnalyzer _analyzer = new();
    private readonly IReadOnlyDictionary<string, IReadOnlyList<DeathTextTemplate>> _templatesByLanguage;

    public TemplateBossVictoryTextImageSignalDetector(ILogService log, string? templatePath = null)
    {
        _log = log;
        _templatesByLanguage = LoadTemplatesByLanguage(templatePath, log);
    }

    public ImageDeathSignalMatch Analyze(Bitmap bitmap, double sensitivity, string gameLanguage)
    {
        var language = NormalizeLanguage(gameLanguage);
        var templates = _templatesByLanguage.TryGetValue(language, out var languageTemplates)
            ? languageTemplates
            : [];

        if (templates.Count == 0)
        {
            return ImageDeathSignalMatch.NoMatch;
        }

        try
        {
            return WithLockedPixels(bitmap, getPixel => _analyzer.Analyze(
                bitmap.Width,
                bitmap.Height,
                getPixel,
                templates,
                sensitivity,
                TextSignalPixelProfile.BossVictory));
        }
        catch (Exception exception)
        {
            _log.Error("Boss victory text template matching error.", exception);
            return ImageDeathSignalMatch.NoMatch;
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<DeathTextTemplate>> LoadTemplatesByLanguage(string? templatePath, ILogService log)
    {
        return new Dictionary<string, IReadOnlyList<DeathTextTemplate>>(StringComparer.OrdinalIgnoreCase)
        {
            ["PL"] = LoadTemplates(FindTemplatePaths(templatePath, DefaultTemplateFileNames), log),
            ["ENG"] = LoadTemplates(FindTemplatePaths(templatePath, EnglishTemplateFileNames), log)
        };
    }

    private static IReadOnlyList<string> FindTemplatePaths(string? templatePath, IReadOnlyList<string> templateFileNames)
    {
        var paths = new List<string>();
        if (!string.IsNullOrWhiteSpace(templatePath))
        {
            paths.Add(templatePath);
        }

        foreach (var fileName in templateFileNames)
        {
            paths.Add(Path.Combine(AppContext.BaseDirectory, "Assets", fileName));
            paths.Add(Path.Combine(Environment.CurrentDirectory, "Assets", fileName));
        }

        return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string NormalizeLanguage(string? language)
    {
        return language?.Trim().ToUpperInvariant() switch
        {
            "ENG" => "ENG",
            "EN" => "ENG",
            _ => "PL"
        };
    }

    private static IReadOnlyList<DeathTextTemplate> LoadTemplates(IEnumerable<string> paths, ILogService log)
    {
        var templates = new List<DeathTextTemplate>();
        foreach (var path in paths)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                using var bitmap = new Bitmap(path);
                var crop = GetReferenceTextSearchRegion(bitmap.Width, bitmap.Height);
                var template = WithLockedPixels(bitmap, getPixel => DeathTextTemplate.FromReference(
                    Path.GetFileName(path),
                    crop.Width,
                    crop.Height,
                    (x, y) => getPixel(crop.Left + x, crop.Top + y),
                    TextSignalPixelProfile.BossVictory));
                templates.Add(template);
                log.Info($"Loaded boss victory text template from {path}. Template={template.Width}x{template.Height}, strokes={template.StrokePoints.Count}, edges={template.EdgePoints.Count}.");
            }
            catch (Exception exception)
            {
                log.Error($"Failed to load boss victory text template from {path}.", exception);
            }
        }

        if (templates.Count == 0)
        {
            log.Info("No boss victory text template loaded. Template fallback is disabled.");
        }

        return templates;
    }

    private static PixelRect GetReferenceTextSearchRegion(int width, int height)
    {
        return new PixelRect(
            (int)(width * 0.24),
            (int)(height * 0.43),
            (int)(width * 0.78),
            (int)(height * 0.62));
    }

    private static T WithLockedPixels<T>(Bitmap bitmap, Func<Func<int, int, RgbPixel>, T> read)
    {
        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var stride = Math.Abs(data.Stride);
            var byteCount = stride * data.Height;
            var bytes = new byte[byteCount];
            Marshal.Copy(data.Scan0, bytes, 0, byteCount);

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
}
