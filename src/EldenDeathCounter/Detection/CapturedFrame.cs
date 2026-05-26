using System.Drawing;

namespace EldenDeathCounter.Detection;

public sealed class CapturedFrame : IDisposable
{
    public CapturedFrame(Bitmap bitmap)
    {
        Bitmap = bitmap;
    }

    public Bitmap Bitmap { get; }

    public void Dispose() => Bitmap.Dispose();
}
