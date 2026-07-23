using System.Drawing;
using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.Core.Detection;
using EldenDeathCounter.Core.Logging;
using EldenDeathCounter.Core.Storage;
using EldenDeathCounter.Detection;

namespace EldenDeathCounter.App.Tests.Core;

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

    [Fact]
    public async Task BossVictoryRunsOcrWhenDeathImageDoesNotRequireIt()
    {
        var log = new InMemoryLogService();
        var counterService = new DeathCounterService(
            new DeathCounterStore(log),
            log,
            Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()),
            new DeathCounterState());
        await counterService.SetActiveBossAsync("Margit");

        var ocrService = new CountingTextRecognitionService("GREAT ENEMY FELLED");
        var service = new DeathDetectionService(
            new RepeatingCaptureService(),
            ocrService,
            new NoMatchDeathSignalDetector(),
            new WeakBossVictorySignalDetector(),
            new EmptyBossNameDetector(),
            counterService,
            log,
            new InMemoryDetectionEventLogService());
        var bossDefeated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        counterService.StateChanged += (_, _) =>
        {
            if (counterService.State.BossHistory.Count == 1)
            {
                bossDefeated.TrySetResult();
            }
        };
        var settings = AppSettings.CreateDefault(Environment.CurrentDirectory, AppGameProfile.EldenRing);
        settings.AutoDetectBossNames = false;
        settings.BossVictoryPhrases = ["GREAT ENEMY FELLED"];

        service.Start(settings);
        try
        {
            await bossDefeated.Task.WaitAsync(TimeSpan.FromSeconds(3));

            Assert.Null(counterService.State.ActiveBoss);
            Assert.Single(counterService.State.BossHistory);
            Assert.Equal(2, ocrService.CallCount);
        }
        finally
        {
            await service.StopAsync();
        }
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

    private sealed class CountingTextRecognitionService(string text) : ITextRecognitionService
    {
        public int CallCount { get; private set; }

        public Task<string> RecognizeTextAsync(Bitmap bitmap, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(text);
        }
    }

    private sealed class NoMatchDeathSignalDetector : IImageDeathSignalDetector
    {
        public ImageDeathSignalMatch Analyze(
            Bitmap bitmap,
            double sensitivity,
            string gameId,
            string gameLanguage,
            string bossHealthBarStyle) =>
            ImageDeathSignalMatch.NoMatch;
    }

    private sealed class NoMatchBossVictorySignalDetector : IImageBossVictorySignalDetector
    {
        public ImageDeathSignalMatch Analyze(Bitmap bitmap, double sensitivity, string gameId, string gameLanguage) => ImageDeathSignalMatch.NoMatch;
    }

    private sealed class WeakBossVictorySignalDetector : IImageBossVictorySignalDetector
    {
        public ImageDeathSignalMatch Analyze(Bitmap bitmap, double sensitivity, string gameId, string gameLanguage) =>
            new(false, DetectionOcrGate.WeakImageSignalFloor, "template:boss-victory", 1);
    }

    private sealed class RepeatingCaptureService : IScreenCaptureService
    {
        public Task<CapturedFrame> CaptureAsync(string captureTarget, CancellationToken cancellationToken) =>
            Task.FromResult(new CapturedFrame(new Bitmap(1, 1)));

        public Task<CapturedFrame> CaptureFullScreenAsync(string captureTarget, CancellationToken cancellationToken) => CaptureAsync(captureTarget, cancellationToken);

        public Task<CapturedFrame> CaptureBossHealthBarAsync(string captureTarget, CancellationToken cancellationToken) => CaptureAsync(captureTarget, cancellationToken);
    }

    private sealed class EmptyBossNameDetector : IBossNameDetector
    {
        public IReadOnlyList<BossHealthBarRegion> AnalyzeBars(Bitmap screenshot, string gameId, string bossHealthBarStyle) =>
            Array.Empty<BossHealthBarRegion>();

        public Task<BossNameDetectionResult> ReadBossNamesAsync(
            Bitmap screenshot,
            IReadOnlyList<BossHealthBarRegion> bars,
            BossNameMatcher matcher,
            CancellationToken cancellationToken) =>
            Task.FromResult(BossNameDetectionResult.FromMatches(0, Array.Empty<BossNameCandidate>()));
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
        public bool FrameDiagnosticsEnabled => false;

        public void Configure(AppSettings settings, DetectionDiagnosticsState state)
        {
        }

        public void Log(DetectionEventRecord record)
        {
        }
    }
}
