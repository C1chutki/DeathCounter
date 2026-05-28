using System.Drawing;
using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.Core.Detection;
using EldenDeathCounter.Core.Logging;
using EldenDeathCounter.Core.Storage;
using EldenDeathCounter.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class DeathDetectionServiceShutdownTests
{
    [Fact]
    public async Task StopAsyncWaitsForDetectionLoopToExitAfterCancellation()
    {
        var captureService = new BlockingCaptureService();
        var service = CreateService(captureService);

        service.Start(AppSettings.CreateDefault(Environment.CurrentDirectory, AppGameProfile.EldenRing));
        await captureService.CaptureStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await service.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(captureService.CaptureCancelled.Task.IsCompleted);
        Assert.False(service.IsRunning);
    }

    [Fact]
    public async Task StopAsyncIsIdempotentAndSafeToCallRepeatedly()
    {
        var captureService = new BlockingCaptureService();
        var service = CreateService(captureService);

        service.Start(AppSettings.CreateDefault(Environment.CurrentDirectory, AppGameProfile.EldenRing));
        await captureService.CaptureStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await service.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(service.IsRunning);
    }

    [Fact]
    public async Task StopAsyncReturnsImmediatelyWhenNeverStarted()
    {
        var service = CreateService(new BlockingCaptureService());

        await service.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(service.IsRunning);
    }

    private static DeathDetectionService CreateService(IScreenCaptureService captureService)
    {
        var log = new InMemoryLogService();
        var store = new DeathCounterStore(log);
        var counterService = new DeathCounterService(store, log, Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), new DeathCounterState());
        return new DeathDetectionService(
            captureService,
            new EmptyTextRecognitionService(),
            new NoMatchDeathSignalDetector(),
            new NoMatchBossVictorySignalDetector(),
            new EmptyBossNameDetector(),
            counterService,
            log,
            new InMemoryDetectionEventLogService());
    }

    private sealed class BlockingCaptureService : IScreenCaptureService
    {
        public TaskCompletionSource CaptureStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource CaptureCancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<CapturedFrame> CaptureAsync(string captureTarget, CancellationToken cancellationToken)
        {
            CaptureStarted.SetResult();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                CaptureCancelled.SetResult();
                throw;
            }

            throw new InvalidOperationException("The blocking capture should only complete by cancellation.");
        }

        public Task<CapturedFrame> CaptureFullScreenAsync(string captureTarget, CancellationToken cancellationToken) => CaptureAsync(captureTarget, cancellationToken);

        public Task<CapturedFrame> CaptureBossHealthBarAsync(string captureTarget, CancellationToken cancellationToken) => CaptureAsync(captureTarget, cancellationToken);
    }

    private sealed class EmptyTextRecognitionService : ITextRecognitionService
    {
        public Task<string> RecognizeTextAsync(Bitmap bitmap, CancellationToken cancellationToken) => Task.FromResult(string.Empty);
    }

    private sealed class NoMatchDeathSignalDetector : IImageDeathSignalDetector
    {
        public ImageDeathSignalMatch Analyze(Bitmap bitmap, double sensitivity, string gameLanguage) => ImageDeathSignalMatch.NoMatch;
    }

    private sealed class NoMatchBossVictorySignalDetector : IImageBossVictorySignalDetector
    {
        public ImageDeathSignalMatch Analyze(Bitmap bitmap, double sensitivity, string gameLanguage) => ImageDeathSignalMatch.NoMatch;
    }

    private sealed class EmptyBossNameDetector : IBossNameDetector
    {
        public Task<string?> DetectBossNameAsync(Bitmap bitmap, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }

    private sealed class InMemoryLogService : ILogService
    {
        public void Info(string message)
        {
        }

        public void Error(string message, Exception? exception = null)
        {
        }
    }

    private sealed class InMemoryDetectionEventLogService : IDetectionEventLogService
    {
        public void Configure(AppSettings settings, DetectionDiagnosticsState state)
        {
        }

        public void Log(DetectionEventRecord record)
        {
        }
    }
}
