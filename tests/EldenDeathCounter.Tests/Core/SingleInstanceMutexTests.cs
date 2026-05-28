namespace EldenDeathCounter.Tests.Core;

public sealed class SingleInstanceMutexTests
{
    // Mirrors App's single-instance pattern without requiring the WPF host. Verifies that a
    // normally released mutex can be acquired again (no stale "already running" lock) and that a
    // genuinely held mutex still blocks a second acquisition.
    [Fact]
    public void MutexCanBeReacquiredAfterNormalRelease()
    {
        var name = $"Local\\EldenDeathCounter.SingleInstanceTests.{Guid.NewGuid():N}";

        var first = new Mutex(initiallyOwned: true, name, out var firstCreatedNew);
        Assert.True(firstCreatedNew);
        first.ReleaseMutex();
        first.Dispose();

        using var second = new Mutex(initiallyOwned: true, name, out var secondCreatedNew);
        Assert.True(secondCreatedNew);
    }

    [Fact]
    public void SecondInstanceDetectedWhileMutexHeld()
    {
        var name = $"Local\\EldenDeathCounter.SingleInstanceTests.{Guid.NewGuid():N}";

        using var first = new Mutex(initiallyOwned: true, name, out var firstCreatedNew);
        Assert.True(firstCreatedNew);

        using var second = new Mutex(initiallyOwned: true, name, out var secondCreatedNew);
        Assert.False(secondCreatedNew);

        first.ReleaseMutex();
    }
}
