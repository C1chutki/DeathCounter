namespace EldenDeathCounter.Core.Detection;

public static class BossHealthBarCaptureRegionCalculator
{
    public static PixelRect Calculate(int screenWidth, int screenHeight) =>
        Calculate(screenWidth, screenHeight, gameId: null);

    public static PixelRect Calculate(int screenWidth, int screenHeight, string? gameId)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return new PixelRect(0, 0, 0, 0);
        }

        // Sekiro draws the boss bar at the top-left of the screen with the name plate underneath it,
        // instead of the bottom-centre bar every other supported game uses.
        var isSekiro = string.Equals(gameId?.Trim(), "Sekiro", StringComparison.OrdinalIgnoreCase);
        var top = (int)Math.Round(screenHeight * (isSekiro ? 0.02 : 0.64));
        var bottom = (int)Math.Round(screenHeight * (isSekiro ? 0.22 : 0.96));
        return new PixelRect(0, top, screenWidth, Math.Clamp(bottom, top + 1, screenHeight));
    }
}
