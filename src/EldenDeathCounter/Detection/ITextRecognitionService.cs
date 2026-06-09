using System.Drawing;

namespace EldenDeathCounter.Detection;

public interface ITextRecognitionService
{
    Task<string> RecognizeTextAsync(Bitmap bitmap, CancellationToken cancellationToken);

    /// <summary>
    /// Recognizes text using only the OCR engines whose language matches one of
    /// <paramref name="preferredLanguageCodes"/> (two-letter codes such as "en" or "pl"). Falls back to
    /// every available engine when none match or the list is empty. Default implementation ignores the
    /// hint and runs all engines, so fakes/tests need not implement it.
    /// </summary>
    Task<string> RecognizeTextAsync(Bitmap bitmap, IReadOnlyCollection<string> preferredLanguageCodes, CancellationToken cancellationToken)
        => RecognizeTextAsync(bitmap, cancellationToken);
}
