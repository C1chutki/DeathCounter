namespace EldenDeathCounter.Core.Detection;

public static class DeathTextCaptureRegionCalculator
{
    public static PixelRect Calculate(int screenWidth, int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return new PixelRect(0, 0, 0, 0);
        }

        var captureWidth = Math.Min(screenWidth, Math.Max(640, (int)Math.Round(screenWidth * 0.66)));
        var captureHeight = Math.Min(screenHeight, Math.Max(260, (int)Math.Round(screenHeight * 0.26)));
        var left = (screenWidth - captureWidth) / 2;
        var centerY = (int)Math.Round(screenHeight * 0.51);
        var top = Math.Clamp(centerY - captureHeight / 2, 0, screenHeight - captureHeight);

        return new PixelRect(left, top, left + captureWidth, top + captureHeight);
    }
}
