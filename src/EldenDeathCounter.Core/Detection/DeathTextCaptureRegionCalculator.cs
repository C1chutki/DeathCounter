namespace EldenDeathCounter.Core.Detection;

public static class DeathTextCaptureRegionCalculator
{
    // Elden Ring / Dark Souls III center their "YOU DIED" text near mid-screen (~y 0.517). Dark Souls II
    // draws it much lower (~y 0.65..0.79), so it needs its own taller, lower band or the live capture
    // never contains the text. Other games keep the mid-screen band, so their capture is unchanged.
    public static PixelRect Calculate(int screenWidth, int screenHeight) =>
        Calculate(screenWidth, screenHeight, gameId: null);

    public static PixelRect Calculate(int screenWidth, int screenHeight, string? gameId)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return new PixelRect(0, 0, 0, 0);
        }

        var isDarkSouls2 = string.Equals(gameId?.Trim(), "DarkSouls2", StringComparison.OrdinalIgnoreCase);
        var heightFraction = isDarkSouls2 ? 0.24 : 0.15;
        var centerFraction = isDarkSouls2 ? 0.72 : 0.517;

        var captureWidth = Math.Min(screenWidth, Math.Max(640, (int)Math.Round(screenWidth * 0.66)));
        var captureHeight = Math.Min(screenHeight, Math.Max(160, (int)Math.Round(screenHeight * heightFraction)));
        var left = (screenWidth - captureWidth) / 2;
        var centerY = (int)Math.Round(screenHeight * centerFraction);
        var top = Math.Clamp(centerY - captureHeight / 2, 0, screenHeight - captureHeight);

        return new PixelRect(left, top, left + captureWidth, top + captureHeight);
    }
}
