namespace EldenDeathCounter.Core.Detection;

public static class BossHealthBarCaptureRegionCalculator
{
    public static PixelRect Calculate(int screenWidth, int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return new PixelRect(0, 0, 0, 0);
        }

        var top = (int)Math.Round(screenHeight * 0.64);
        var bottom = (int)Math.Round(screenHeight * 0.96);
        return new PixelRect(0, top, screenWidth, Math.Clamp(bottom, top + 1, screenHeight));
    }
}
