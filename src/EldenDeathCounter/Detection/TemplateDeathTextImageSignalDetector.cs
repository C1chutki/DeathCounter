using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using EldenDeathCounter.Core.Detection;
using EldenDeathCounter.Core.Logging;

namespace EldenDeathCounter.Detection;

public sealed class TemplateDeathTextImageSignalDetector : IImageDeathSignalDetector
{
    private readonly ILogService _log;
    private readonly string? _templatePath;
    private readonly OpenCvDeathTextTemplateAnalyzer _analyzer = new();
    private readonly object _cacheLock = new();
    private readonly Dictionary<string, IReadOnlyList<DeathTextTemplate>> _templatesByGameLanguage =
        new(StringComparer.OrdinalIgnoreCase);

    public TemplateDeathTextImageSignalDetector(ILogService log, string? templatePath = null)
    {
        _log = log;
        _templatePath = templatePath;
    }

    public ImageDeathSignalMatch Analyze(Bitmap bitmap, double sensitivity, string gameId, string gameLanguage)
    {
        var language = NormalizeLanguage(gameLanguage);
        var templates = GetTemplates(gameId, language);
        if (templates.Count == 0)
        {
            return ImageDeathSignalMatch.NoMatch;
        }

        try
        {
            return WithLockedPixels(bitmap, getPixel => _analyzer.Analyze(bitmap.Width, bitmap.Height, getPixel, templates, sensitivity));
        }
        catch (Exception exception)
        {
            _log.Error("Death text template matching error.", exception);
            return ImageDeathSignalMatch.NoMatch;
        }
    }

    private IReadOnlyList<DeathTextTemplate> GetTemplates(string gameId, string language)
    {
        var key = $"{gameId?.Trim() ?? string.Empty}|{language}";
        lock (_cacheLock)
        {
            if (_templatesByGameLanguage.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var fileNames = GameDeathScreenTemplates.DeathTemplateFiles(gameId ?? string.Empty, language);
            var templates = LoadTemplates(FindTemplatePaths(_templatePath, fileNames), _log);
            _templatesByGameLanguage[key] = templates;
            return templates;
        }
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
                    (x, y) => getPixel(crop.Left + x, crop.Top + y)));
                templates.Add(template);
                log.Info($"Loaded death text template from {path}. Template={template.Width}x{template.Height}, strokes={template.StrokePoints.Count}, edges={template.EdgePoints.Count}.");
            }
            catch (Exception exception)
            {
                log.Error($"Failed to load death text template from {path}.", exception);
            }
        }

        if (templates.Count == 0)
        {
            log.Info("No death text template loaded. Template fallback is disabled.");
        }

        return templates;
    }

    private static PixelRect GetReferenceTextSearchRegion(int width, int height)
    {
        return DeathTextTemplateReferenceRegion.DeathScreen(width, height);
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
