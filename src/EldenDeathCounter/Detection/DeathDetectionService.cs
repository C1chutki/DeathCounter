using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.Core.Detection;
using EldenDeathCounter.Core.Logging;
using EldenDeathCounter.Core.Storage;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace EldenDeathCounter.Detection;

public sealed class DeathDetectionService
{
    private readonly IScreenCaptureService _captureService;
    private readonly ITextRecognitionService _textRecognitionService;
    private readonly IImageDeathSignalDetector _imageDeathSignalDetector;
    private readonly IImageBossVictorySignalDetector _bossVictorySignalDetector;
    private readonly IBossNameDetector _bossNameDetector;
    private readonly DeathCounterService _counterService;
    private readonly ILogService _log;
    private readonly IDetectionEventLogService _detectionEventLog;
    private readonly DeathPhraseMatcher _phraseMatcher = new();
    private readonly DeathDetectionGate _detectionGate = new(clearFramesRequired: 3);
    private readonly DeathDetectionGate _bossVictoryDetectionGate = new(clearFramesRequired: 3);
    private readonly DeathSignalStabilizer _deathSignalStabilizer = new(requiredConsecutiveFrames: 2);
    private readonly DeathSignalStabilizer _bossVictorySignalStabilizer = new(requiredConsecutiveFrames: 2);
    private readonly BossEncounterNameTracker _bossEncounterTracker = new();
    private BossNameMatcher? _bossNameMatcher;
    private string? _bossNameMatcherLanguage;
    private string? _lastAutoPublishedBossName;
    private DateTimeOffset _lastBossNameRejectionLog = DateTimeOffset.MinValue;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _loopTask;
    private DateTimeOffset? _lastDetectedDeath;
    private DateTimeOffset? _lastDetectedBossVictory;
    private DateTimeOffset _lastWeakImageSignalLog = DateTimeOffset.MinValue;
    private DateTimeOffset _lastWeakBossVictoryImageSignalLog = DateTimeOffset.MinValue;
    private DateTimeOffset _lastRepeatedSignalLog = DateTimeOffset.MinValue;
    private DateTimeOffset _lastRepeatedBossVictorySignalLog = DateTimeOffset.MinValue;
    private DateTimeOffset _lastPendingSignalStatus = DateTimeOffset.MinValue;
    private DateTimeOffset _lastPendingBossVictorySignalStatus = DateTimeOffset.MinValue;
    private DateTimeOffset _lastBossNameDetectionAttempt = DateTimeOffset.MinValue;
    private DateTimeOffset _lastBossBarDiagnosticLog = DateTimeOffset.MinValue;
    private int _lastLoggedBossBarCount = -1;
    private DateTimeOffset? _fullDiagnosticsUntil;
    private AppSettings? _lastSettings;
    private string? _lastCaptureStatus;
    private long _detectionFrameIndex;

    // OCR is the fallback for death/victory text the template misses. It is the single most expensive
    // step (PNG-free now, but still ~15ms × engines), so we no longer run it on every frame: only when
    // the cheap template analyzer reports something text-like, while a confirmation is pending, or as a
    // periodic safety net so a fully template-blind banner is still caught within OcrSafetyInterval.

    public DeathDetectionService(
        IScreenCaptureService captureService,
        ITextRecognitionService textRecognitionService,
        IImageDeathSignalDetector imageDeathSignalDetector,
        IImageBossVictorySignalDetector bossVictorySignalDetector,
        IBossNameDetector bossNameDetector,
        DeathCounterService counterService,
        ILogService log,
        IDetectionEventLogService detectionEventLog)
    {
        _captureService = captureService;
        _textRecognitionService = textRecognitionService;
        _imageDeathSignalDetector = imageDeathSignalDetector;
        _bossVictorySignalDetector = bossVictorySignalDetector;
        _bossNameDetector = bossNameDetector;
        _counterService = counterService;
        _log = log;
        _detectionEventLog = detectionEventLog;
    }

    public bool IsRunning => _loopTask is { IsCompleted: false };

    public event EventHandler<DetectionStatusChangedEventArgs>? StatusChanged;

    public void Start(AppSettings settings)
    {
        if (IsRunning)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _lastSettings = settings;
        _bossEncounterTracker.Reset();
        _lastAutoPublishedBossName = null;
        _lastCaptureStatus = "Detection running";
        _loopTask = Task.Run(() => RunAsync(settings, _cancellationTokenSource.Token));
        ConfigureDiagnostics(settings, detectionRunning: true);
        _log.Info("Detection started.");
        _log.Info(
            "Detection settings: " +
            $"intervalMs={DetectionTimingOptions.NormalizeBaseIntervalMs(settings.DetectionIntervalMs)}, " +
            $"burstIntervalMs={DetectionTimingOptions.BurstIntervalMs}, " +
            $"burstDurationMs={DetectionTimingOptions.BurstDurationMs}, " +
            $"cooldownSeconds={settings.DetectionCooldownSeconds}, " +
            $"sensitivity={FormatScore(settings.DetectionSensitivity)}, " +
            $"captureTarget='{settings.CaptureTarget}', " +
            $"phrases='{string.Join("|", settings.DetectionPhrases)}', " +
            $"bossVictoryPhrases='{string.Join("|", settings.BossVictoryPhrases)}', " +
            $"dataFolder='{settings.DataFolderPath}', " +
            $"stabilizerRequiredSignals={_deathSignalStabilizer.RequiredSignalFrames}, " +
            $"stabilizerAllowedMissingFrames={_deathSignalStabilizer.AllowedMissingFrames}.");
        RaiseStatus("Detection running", _lastDetectedDeath);
    }

    public void Stop()
    {
        _ = RequestStop();
    }

    public async Task StopAsync()
    {
        var runningTask = RequestStop();
        if (runningTask is null)
        {
            return;
        }

        await runningTask.ConfigureAwait(false);
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    private Task? RequestStop()
    {
        if (!IsRunning)
        {
            RaiseStatus("Detection stopped", _lastDetectedDeath);
            return null;
        }

        var runningTask = _loopTask;
        _cancellationTokenSource?.Cancel();
        _log.Info("Detection stopped.");
        ConfigureDiagnosticsFromLastSettings(detectionRunning: false);
        RaiseStatus("Detection stopped", _lastDetectedDeath);
        return runningTask;
    }

    public DateTimeOffset StartDiagnosticsSession(AppSettings settings, TimeSpan duration)
    {
        _fullDiagnosticsUntil = DateTimeOffset.Now + duration;
        ConfigureDiagnostics(settings, detectionRunning: IsRunning, overrideMode: DiagnosticsMode.FullFrames);
        _log.Info($"Full diagnostics enabled until {_fullDiagnosticsUntil.Value:O}.");
        return _fullDiagnosticsUntil.Value;
    }

    private void ConfigureDiagnosticsFromLastSettings(bool detectionRunning)
    {
        if (_lastSettings is not null)
        {
            ConfigureDiagnostics(_lastSettings, detectionRunning);
        }
    }

    private void ConfigureDiagnostics(AppSettings settings, bool detectionRunning, DiagnosticsMode? overrideMode = null)
    {
        _lastSettings = settings;
        var diagnosticsSettings = CloneSettingsForDiagnostics(settings);
        var effectiveOverride = overrideMode ?? (_fullDiagnosticsUntil is not null && DateTimeOffset.Now < _fullDiagnosticsUntil.Value
            ? DiagnosticsMode.FullFrames
            : null);
        if (effectiveOverride is not null)
        {
            diagnosticsSettings.DiagnosticsMode = effectiveOverride.Value;
        }

        _detectionEventLog.Configure(
            diagnosticsSettings,
            new DetectionDiagnosticsState(
                _counterService.State.CurrentDeathCount,
                _counterService.State.ActiveBoss?.Name,
                detectionRunning));
    }

    private static AppSettings CloneSettingsForDiagnostics(AppSettings settings)
    {
        return new AppSettings
        {
            DetectionIntervalMs = settings.DetectionIntervalMs,
            DetectionSensitivity = settings.DetectionSensitivity,
            CaptureTarget = settings.CaptureTarget,
            DataFolderPath = settings.DataFolderPath,
            DiagnosticsMode = settings.DiagnosticsMode,
            DiagnosticsMaxEventLogMb = settings.DiagnosticsMaxEventLogMb,
            DiagnosticsRetentionDays = settings.DiagnosticsRetentionDays
        };
    }

    public async Task RestartAsync(AppSettings settings)
    {
        await StopAsync();

        Start(settings);
    }

    private async Task RunAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        var baseInterval = TimeSpan.FromMilliseconds(DetectionTimingOptions.NormalizeBaseIntervalMs(settings.DetectionIntervalMs));
        var burstInterval = TimeSpan.FromMilliseconds(DetectionTimingOptions.BurstIntervalMs);
        var burstDuration = TimeSpan.FromMilliseconds(DetectionTimingOptions.BurstDurationMs);
        var burstUntil = DateTimeOffset.MinValue;
        DateTimeOffset? previousFrameStartedAt = null;
        DateTimeOffset? lastFullDiagnosticsScreenshotAt = null;
        var cooldown = TimeSpan.FromSeconds(Math.Max(1, settings.DetectionCooldownSeconds));
        var ocrLanguageHints = BuildOcrLanguageHints(settings);

        while (!cancellationToken.IsCancellationRequested)
        {
            var frameStartedAt = DateTimeOffset.Now;
            var useBurstForNextFrame = false;
            if (_fullDiagnosticsUntil is not null && DateTimeOffset.Now >= _fullDiagnosticsUntil.Value)
            {
                _fullDiagnosticsUntil = null;
                ConfigureDiagnostics(settings, detectionRunning: true);
                _log.Info("Full diagnostics session expired; returned to configured diagnostics mode.");
            }

            try
            {
                var frameIndex = Interlocked.Increment(ref _detectionFrameIndex);
                frameStartedAt = DateTimeOffset.Now;
                var frameDeltaMs = previousFrameStartedAt is null
                    ? (long?)null
                    : (long)Math.Round((frameStartedAt - previousFrameStartedAt.Value).TotalMilliseconds);
                previousFrameStartedAt = frameStartedAt;
                var timingMode = frameStartedAt < burstUntil ? "burst" : "base";
                var frameStopwatch = Stopwatch.StartNew();
                using var frame = await _captureService.CaptureAsync(settings.CaptureTarget, cancellationToken);
                var captureMs = frameStopwatch.ElapsedMilliseconds;
                RaiseCaptureStatusIfChanged();

                var signal = ImageDeathSignalMatch.NoMatch;
                var imageSignal = ImageDeathSignalMatch.NoMatch;
                var ocrText = string.Empty;
                var ocrWasRecognized = false;
                var match = _phraseMatcher.Match(ocrText, settings.DetectionPhrases, settings.DetectionSensitivity);
                long ocrMs = 0;
                long imageAnalysisMs = 0;
                var imageAnalysisStatus = "template-no-match";

                async Task<string> RecognizeFrameTextAsync()
                {
                    if (ocrWasRecognized)
                    {
                        return ocrText;
                    }

                    var ocrStartMs = frameStopwatch.ElapsedMilliseconds;
                    ocrText = await _textRecognitionService.RecognizeTextAsync(frame.Bitmap, ocrLanguageHints, cancellationToken);
                    ocrMs = frameStopwatch.ElapsedMilliseconds - ocrStartMs;
                    ocrWasRecognized = true;
                    return ocrText;
                }

                var wasPending = _deathSignalStabilizer.IsPending;
                var confirmedSignal = (ImageDeathSignalMatch?)null;
                if (settings.DetectDeaths)
                {
                    var imageStartMs = frameStopwatch.ElapsedMilliseconds;
                    imageSignal = _imageDeathSignalDetector.Analyze(
                        frame.Bitmap,
                        settings.DetectionSensitivity,
                        settings.GameId,
                        settings.GameLanguage,
                        settings.BossHealthBarStyle);
                    imageAnalysisMs = frameStopwatch.ElapsedMilliseconds - imageStartMs;
                    if (imageSignal.IsMatch)
                    {
                        signal = imageSignal;
                        imageAnalysisStatus = "template-match";
                    }
                    else if (imageSignal.Score >= 0.35)
                    {
                        imageAnalysisStatus = "weak-template";
                        LogWeakImageSignal(imageSignal);
                    }

                    if (!signal.IsMatch && DetectionOcrGate.ShouldRunOcr(imageSignal, _deathSignalStabilizer.IsPending))
                    {
                        ocrText = await RecognizeFrameTextAsync();
                        match = _phraseMatcher.Match(ocrText, settings.DetectionPhrases, settings.DetectionSensitivity);
                        if (DetectionOcrGate.ShouldAcceptOcrPhrase(match.IsMatch, imageSignal, _deathSignalStabilizer.IsPending))
                        {
                            signal = new ImageDeathSignalMatch(true, match.Score, $"ocr:{match.MatchedPhrase ?? "death phrase"}", 1)
                            {
                                Details = match.Details
                            };
                            imageAnalysisStatus = "ocr-match-after-template";
                        }
                    }

                    if (!signal.IsMatch && wasPending && imageSignal.CanConfirmPendingSignal)
                    {
                        signal = imageSignal;
                        imageAnalysisStatus = "near-threshold-template";
                    }
                }
                else
                {
                    _deathSignalStabilizer.Reset();
                }

                var stabilizerBefore = FormatStabilizerState();
                if (settings.DetectDeaths)
                {
                    confirmedSignal = _deathSignalStabilizer.Observe(signal);
                    if (confirmedSignal is null && signal.IsStrongTemplateMatch)
                    {
                        // A fully-gated template match (strong contrast/stroke/vertical coverage) is specific
                        // enough to count on a single frame. Fast-fading death banners (DS3 at 500ms) often
                        // appear above threshold on only one frame, so waiting for a second would drop the death.
                        _deathSignalStabilizer.Reset();
                        confirmedSignal = signal;
                    }
                }

                var stabilizerAfter = FormatStabilizerState();
                var frameOutcome = !settings.DetectDeaths
                    ? "death-disabled"
                    : confirmedSignal is not null
                        ? "confirmed"
                        : signal.IsMatch
                            ? "pending-signal"
                            : wasPending && !_deathSignalStabilizer.IsPending
                                ? "pending-expired"
                                : _deathSignalStabilizer.IsPending
                                    ? "pending-waiting"
                                    : "no-signal";
                frameStopwatch.Stop();
                // Building the per-frame diagnostic record allocates ~1 KB of interpolated strings that the
                // event log discards unless full-frame diagnostics is on. Skip the work entirely otherwise.
                if (_detectionEventLog.FrameDiagnosticsEnabled)
                {
                    LogDetectionFrameDiagnostics(
                        frameIndex,
                        frameStartedAt,
                        frame.Bitmap,
                        ocrText,
                        match,
                        imageSignal,
                        signal,
                        confirmedSignal,
                        imageAnalysisStatus,
                        wasPending,
                        stabilizerBefore,
                        stabilizerAfter,
                        frameOutcome,
                        captureMs,
                        ocrMs,
                        imageAnalysisMs,
                        frameStopwatch.ElapsedMilliseconds,
                        settings.DetectionSensitivity,
                        frameDeltaMs,
                        timingMode);
                }
                if (_fullDiagnosticsUntil is not null &&
                    DateTimeOffset.Now < _fullDiagnosticsUntil.Value &&
                    DetectionTimingOptions.ShouldSaveFullDiagnosticsFrame(frameStartedAt, lastFullDiagnosticsScreenshotAt, frameIndex))
                {
                    lastFullDiagnosticsScreenshotAt = frameStartedAt;
                    SaveFrameEvidence(settings, frame.Bitmap, frameStartedAt, $"diag-{timingMode}", "frame-sample", imageSignal.Score, frameIndex);
                }

                if (settings.DetectDeaths)
                {
                    if (confirmedSignal is not null)
                    {
                        await HandleDeathSignalMatchAsync(frameIndex, confirmedSignal, cooldown, settings, frame.Bitmap);
                    }
                    else if (signal.IsMatch)
                    {
                        HandlePendingSignal(frameIndex, settings, frame.Bitmap, signal);
                    }
                    else if (wasPending && !_deathSignalStabilizer.IsPending)
                    {
                        var evidencePath = SaveFrameEvidence(settings, frame.Bitmap, DateTimeOffset.Now, "pending-expired", "no-signal", null, frameIndex);
                        LogDetectionEvent("death", "pending-expired", frameIndex, signal, imageSignal, FormatCompactStabilizerState(_deathSignalStabilizer), FormatGateState(), match.Details, evidencePath);
                        _log.Info(
                            $"Death signal pending confirmation expired on frame #{frameIndex}. " +
                            $"stabilizerBefore={stabilizerBefore}, stabilizerAfter={stabilizerAfter}, " +
                            $"ocrRaw='{Compact(ocrText)}', ocrNormalized='{Compact(match.NormalizedText)}', ocrDetails='{Compact(match.Details)}', " +
                            $"image={FormatSignal(imageSignal)}, selected={FormatSignal(signal)}, " +
                            $"evidence='{evidencePath ?? "not-saved"}'.");
                        RaiseStatus("Detection running", _lastDetectedDeath);
                    }

                    useBurstForNextFrame = DetectionTimingOptions.ShouldEnterBurst(imageSignal, _deathSignalStabilizer.IsPending, signal);

                    if (!signal.IsMatch)
                    {
                        var wasGateLatched = _detectionGate.IsScreenLatched;
                        var gateBefore = FormatGateState();
                        var noSignalDecision = _detectionGate.Evaluate(false, DateTimeOffset.Now, cooldown);
                        var gateAfter = FormatGateState();
                        if (wasGateLatched || noSignalDecision == DeathDetectionDecision.Rearmed)
                        {
                            if (noSignalDecision == DeathDetectionDecision.Rearmed)
                            {
                                LogDetectionEvent("death", "rearmed", frameIndex, ImageDeathSignalMatch.NoMatch, null, FormatCompactStabilizerState(_deathSignalStabilizer), FormatGateState(), "screen-cleared");
                            }

                            _log.Info(
                                $"Death gate no-signal evaluation on frame #{frameIndex}. " +
                                $"decision={noSignalDecision}, gateBefore={gateBefore}, gateAfter={gateAfter}.");
                        }

                        if (noSignalDecision == DeathDetectionDecision.Rearmed)
                        {
                            _log.Info("Death screen signal cleared; detector rearmed.");
                        }
                    }
                }

                var hasBossVictorySignal = false;
                if (settings.DetectBossVictories)
                {
                    hasBossVictorySignal = await UpdateBossVictoryFromFrameAsync(
                        frameIndex,
                        settings,
                        frame.Bitmap,
                        RecognizeFrameTextAsync,
                        signal.IsMatch,
                        cooldown);
                }
                else
                {
                    _bossVictorySignalStabilizer.Reset();
                }

                if (settings.AutoDetectBossNames)
                {
                    await UpdateBossNameFromScreenAsync(settings, signal.IsMatch || hasBossVictorySignal, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                _log.Error("OCR/template matching error.", exception);
                RaiseStatus("Detection error; continuing", _lastDetectedDeath);
            }

            try
            {
                if (useBurstForNextFrame)
                {
                    burstUntil = DateTimeOffset.Now + burstDuration;
                }

                var nextInterval = DateTimeOffset.Now < burstUntil ? burstInterval : baseInterval;
                var delay = frameStartedAt + nextInterval - DateTimeOffset.Now;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static IReadOnlyList<string> BuildOcrLanguageHints(AppSettings settings)
    {
        // Dark Souls III renders its death/victory banners in English even when the UI/OCR language is
        // Polish, so it needs both engines; every other game only needs the configured language.
        if (string.Equals(settings.GameId?.Trim(), "DarkSouls3", StringComparison.OrdinalIgnoreCase))
        {
            return ["en", "pl"];
        }

        var language = settings.GameLanguage?.Trim();
        var isEnglish = string.Equals(language, "ENG", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(language, "EN", StringComparison.OrdinalIgnoreCase);
        return isEnglish ? ["en"] : ["pl"];
    }

    private async Task HandleDeathSignalMatchAsync(long frameIndex, ImageDeathSignalMatch match, TimeSpan cooldown, AppSettings settings, Bitmap frame)
    {
        var now = DateTimeOffset.Now;
        var gateBefore = FormatGateState();
        var decision = _detectionGate.Evaluate(true, now, cooldown);
        var gateAfter = FormatGateState();
        _log.Info(
            $"Death signal confirmed by stabilizer on frame #{frameIndex}. " +
            $"{FormatSignal(match)}, " +
            $"gateDecision={decision}, gateBefore={gateBefore}, gateAfter={gateAfter}, " +
            $"cooldownSeconds={FormatScore(cooldown.TotalSeconds)}, " +
            $"lastDetectedDeath='{_lastDetectedDeath?.ToString("O", CultureInfo.InvariantCulture) ?? "none"}'.");
        LogDetectionEvent("death", "confirmed", frameIndex, match, null, FormatCompactStabilizerState(_deathSignalStabilizer), FormatGateState(), decision.ToString());
        if (decision == DeathDetectionDecision.IgnoreActiveScreen)
        {
            LogRepeatedSignal(frameIndex, match, "same death screen is still active");
            return;
        }

        if (decision == DeathDetectionDecision.IgnoreCooldown)
        {
            LogRepeatedSignal(frameIndex, match, "cooldown is active");
            return;
        }

        if (decision != DeathDetectionDecision.Count)
        {
            return;
        }

        _lastDetectedDeath = now;
        var detectionMethod = match.Method.StartsWith("ocr:", StringComparison.Ordinal)
            ? "screen-ocr"
            : "screen-template";
        var note = $"Matched death signal '{match.Method}' with score {FormatScore(match.Score)}, scale={FormatScore(match.Scale)}.";
        var evidencePath = SaveDetectionEvidence(settings, frame, match, now, "count", frameIndex);
        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            note += $" Evidence screenshot: {evidencePath}.";
        }

        await _counterService.AddDeathAsync(detectionMethod, note);
        ConfigureDiagnostics(settings, detectionRunning: IsRunning);
        LogDetectionEvent("death", "counted", frameIndex, match, null, FormatCompactStabilizerState(_deathSignalStabilizer), FormatGateState(), null, evidencePath);
        _log.Info($"Detected death. {note}");
        RaiseStatus("Death detected", _lastDetectedDeath);
    }

    private async Task<bool> UpdateBossVictoryFromFrameAsync(
        long frameIndex,
        AppSettings settings,
        Bitmap frame,
        Func<Task<string>> recognizeFrameTextAsync,
        bool hasDeathSignal,
        TimeSpan cooldown)
    {
        if (hasDeathSignal || _counterService.State.ActiveBoss is null)
        {
            _bossVictorySignalStabilizer.Reset();
            return false;
        }

        var signal = ImageDeathSignalMatch.NoMatch;
        var imageSignal = _bossVictorySignalDetector.Analyze(frame, settings.DetectionSensitivity, settings.GameId, settings.GameLanguage);
        var wasPending = _bossVictorySignalStabilizer.IsPending;
        var ocrText = string.Empty;
        var phraseMatch = new DeathPhraseMatch(false, null, 0, string.Empty);

        if (imageSignal.IsMatch)
        {
            signal = imageSignal;
        }
        else if (imageSignal.Score >= 0.35)
        {
            LogWeakBossVictoryImageSignal(imageSignal);
        }

        if (!signal.IsMatch && DetectionOcrGate.ShouldRunOcr(imageSignal, wasPending))
        {
            ocrText = await recognizeFrameTextAsync();
            phraseMatch = _phraseMatcher.Match(
                ocrText,
                settings.BossVictoryPhrases,
                settings.DetectionSensitivity,
                suppressOwnSettingsTextGuard: true);
            if (DetectionOcrGate.ShouldAcceptOcrPhrase(phraseMatch.IsMatch, imageSignal, wasPending))
            {
                signal = new ImageDeathSignalMatch(true, phraseMatch.Score, $"boss-victory-ocr:{phraseMatch.MatchedPhrase ?? "victory phrase"}", 1)
                {
                    Details = phraseMatch.Details
                };
            }
        }

        if (!signal.IsMatch && wasPending && imageSignal.CanConfirmPendingSignal)
        {
            signal = imageSignal;
        }

        var stabilizerBefore = FormatBossVictoryStabilizerState();
        var confirmedSignal = _bossVictorySignalStabilizer.Observe(signal);
        var stabilizerAfter = FormatBossVictoryStabilizerState();
        var hasSignalForThisFrame = signal.IsMatch || confirmedSignal is not null;

        if (confirmedSignal is not null)
        {
            await HandleBossVictorySignalMatchAsync(frameIndex, confirmedSignal, cooldown, settings, frame);
        }
        else if (signal.IsMatch)
        {
            HandlePendingBossVictorySignal(frameIndex, settings, frame, signal);
        }
        else if (wasPending && !_bossVictorySignalStabilizer.IsPending)
        {
            var evidencePath = SaveFrameEvidence(settings, frame, DateTimeOffset.Now, "boss-victory-pending-expired", "no-signal", null, frameIndex);
            LogDetectionEvent("boss-victory", "pending-expired", frameIndex, signal, imageSignal, FormatCompactStabilizerState(_bossVictorySignalStabilizer), FormatBossVictoryGateState(), phraseMatch.Details, evidencePath);
            _log.Info(
                $"Boss victory signal pending confirmation expired on frame #{frameIndex}. " +
                $"stabilizerBefore={stabilizerBefore}, stabilizerAfter={stabilizerAfter}, " +
                $"ocrRaw='{Compact(ocrText)}', ocrNormalized='{Compact(phraseMatch.NormalizedText)}', ocrDetails='{Compact(phraseMatch.Details)}', " +
                $"image={FormatSignal(imageSignal)}, selected={FormatSignal(signal)}, " +
                $"evidence='{evidencePath ?? "not-saved"}'.");
            RaiseStatus("Boss victory signal expired", _lastDetectedDeath);
        }

        if (!hasSignalForThisFrame)
        {
            var wasGateLatched = _bossVictoryDetectionGate.IsScreenLatched;
            var gateBefore = FormatBossVictoryGateState();
            var noSignalDecision = _bossVictoryDetectionGate.Evaluate(false, DateTimeOffset.Now, cooldown);
            var gateAfter = FormatBossVictoryGateState();
            if (wasGateLatched || noSignalDecision == DeathDetectionDecision.Rearmed)
            {
                if (noSignalDecision == DeathDetectionDecision.Rearmed)
                {
                    LogDetectionEvent("boss-victory", "rearmed", frameIndex, ImageDeathSignalMatch.NoMatch, null, FormatCompactStabilizerState(_bossVictorySignalStabilizer), FormatBossVictoryGateState(), "screen-cleared");
                }

                _log.Info(
                    $"Boss victory gate no-signal evaluation on frame #{frameIndex}. " +
                    $"decision={noSignalDecision}, gateBefore={gateBefore}, gateAfter={gateAfter}.");
            }
        }

        return hasSignalForThisFrame;
    }

    private async Task HandleBossVictorySignalMatchAsync(long frameIndex, ImageDeathSignalMatch match, TimeSpan cooldown, AppSettings settings, Bitmap frame)
    {
        if (_counterService.State.ActiveBoss is null)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        var activeBossName = _counterService.State.ActiveBoss.Name;
        var gateBefore = FormatBossVictoryGateState();
        var decision = _bossVictoryDetectionGate.Evaluate(true, now, cooldown);
        var gateAfter = FormatBossVictoryGateState();
        _log.Info(
            $"Boss victory signal confirmed by stabilizer on frame #{frameIndex}. " +
            $"boss='{activeBossName}', " +
            $"{FormatSignal(match)}, " +
            $"gateDecision={decision}, gateBefore={gateBefore}, gateAfter={gateAfter}, " +
            $"cooldownSeconds={FormatScore(cooldown.TotalSeconds)}, " +
            $"lastDetectedBossVictory='{_lastDetectedBossVictory?.ToString("O", CultureInfo.InvariantCulture) ?? "none"}'.");
        LogDetectionEvent("boss-victory", "confirmed", frameIndex, match, null, FormatCompactStabilizerState(_bossVictorySignalStabilizer), FormatBossVictoryGateState(), decision.ToString());
        if (decision == DeathDetectionDecision.IgnoreActiveScreen)
        {
            LogRepeatedBossVictorySignal(frameIndex, match, "same victory screen is still active");
            return;
        }

        if (decision == DeathDetectionDecision.IgnoreCooldown)
        {
            LogRepeatedBossVictorySignal(frameIndex, match, "cooldown is active");
            return;
        }

        if (decision != DeathDetectionDecision.Count)
        {
            return;
        }

        _lastDetectedBossVictory = now;
        var detectionMethod = match.Method.Contains("ocr:", StringComparison.Ordinal)
            ? "boss-victory-ocr"
            : "boss-victory-template";
        var note = $"Matched boss victory signal '{match.Method}' with score {FormatScore(match.Score)}, scale={FormatScore(match.Scale)}.";
        var evidencePath = SaveFrameEvidence(settings, frame, now, "boss-victory-count", match.Method, match.Score, frameIndex);
        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            note += $" Evidence screenshot: {evidencePath}.";
        }

        await _counterService.MarkActiveBossDefeatedAsync(detectionMethod);
        ConfigureDiagnostics(settings, detectionRunning: IsRunning);
        LogDetectionEvent("boss-victory", "counted", frameIndex, match, null, FormatCompactStabilizerState(_bossVictorySignalStabilizer), FormatBossVictoryGateState(), null, evidencePath, activeBossName);
        _log.Info($"Detected boss victory for '{activeBossName}'. {note}");
        RaiseStatus("Boss defeated detected", _lastDetectedDeath);
    }

    private void RaiseStatus(string status, DateTimeOffset? lastDetectedDeath)
    {
        StatusChanged?.Invoke(this, new DetectionStatusChangedEventArgs(status, lastDetectedDeath));
    }

    private void RaiseCaptureStatusIfChanged()
    {
        var status = _captureService.CaptureStatus ?? "Detection running";
        if (string.Equals(_lastCaptureStatus, status, StringComparison.Ordinal))
        {
            return;
        }

        _lastCaptureStatus = status;
        RaiseStatus(status, _lastDetectedDeath);
    }

    private async Task UpdateBossNameFromScreenAsync(AppSettings settings, bool hasDeathOrVictorySignal, CancellationToken cancellationToken)
    {
        try
        {
            if (hasDeathOrVictorySignal)
            {
                // A death or boss-victory signal ends/changes the encounter; re-arm name detection.
                // _lastAutoPublishedBossName is intentionally preserved: the active boss survives a
                // death, so it still reflects a name we set (not a manual edit).
                _bossEncounterTracker.Reset();
                return;
            }

            var now = DateTimeOffset.Now;
            if (now - _lastBossNameDetectionAttempt < TimeSpan.FromMilliseconds(500))
            {
                return;
            }

            _lastBossNameDetectionAttempt = now;

            var matcher = GetBossNameMatcher(settings);
            using var screenshot = await _captureService.CaptureBossHealthBarAsync(settings.CaptureTarget, cancellationToken);

            // 1) Detect boss HP bars first (cheap). Boss-name OCR is gated entirely on this.
            var bars = _bossNameDetector.AnalyzeBars(screenshot.Bitmap, settings.GameId, settings.BossHealthBarStyle);
            LogBossBarDiagnostics(screenshot.Bitmap, bars, now);
            var decision = _bossEncounterTracker.BeginFrame(bars.Count);
            if (decision.Rearmed)
            {
                _log.Info("Boss bars cleared; boss-name detection re-armed for the next encounter.");
            }

            if (!decision.ShouldReadNames)
            {
                // No stable bar yet, or the name is already frozen for this encounter.
                return;
            }

            // 2) Only now run OCR, restricted to each detected bar's name region.
            var result = await _bossNameDetector.ReadBossNamesAsync(screenshot.Bitmap, bars, matcher, cancellationToken);
            var proposed = _bossEncounterTracker.SubmitNames(result.BarCount, result.MatchedCount, result.CombinedName);
            if (proposed is null)
            {
                LogRejectedBossNameCandidates(result);
                return;
            }

            // 3) Never overwrite a manually edited active boss name during an encounter.
            var activeName = _counterService.State.ActiveBoss?.Name;
            switch (BossNamePublishDecision.Decide(proposed, activeName, _lastAutoPublishedBossName))
            {
                case BossNamePublishOutcome.KeepManual:
                    _log.Info($"Auto-detected boss '{proposed}' suppressed; keeping manually set boss '{activeName}'.");
                    _lastAutoPublishedBossName = activeName;
                    return;
                case BossNamePublishOutcome.Publish:
                    await _counterService.SetActiveBossAsync(proposed, resetIfChanged: false);
                    _lastAutoPublishedBossName = proposed;
                    _log.Info(
                        $"Boss name detected and frozen: '{proposed}' " +
                        $"(bars={result.BarCount}, matched={result.MatchedCount}, confidence={FormatBossConfidences(result)}).");
                    return;
                default:
                    return;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _log.Error("Boss name auto-detection error.", exception);
        }
    }

    private BossNameMatcher GetBossNameMatcher(AppSettings settings)
    {
        var language = string.IsNullOrWhiteSpace(settings.GameLanguage) ? "ENG" : settings.GameLanguage.Trim();
        var gameId = string.IsNullOrWhiteSpace(settings.GameId) ? "EldenRing" : settings.GameId.Trim();

        // Cache key is game+language so switching games (e.g. Elden Ring -> Dark Souls III) reloads the
        // matcher with that game's boss list instead of reusing the previous game's names.
        var bossHealthBarStyle = BossHealthBarStyles.Normalize(settings.BossHealthBarStyle);
        var cacheKey = $"{gameId}|{language}|{bossHealthBarStyle}";
        if (_bossNameMatcher is not null &&
            string.Equals(_bossNameMatcherLanguage, cacheKey, StringComparison.OrdinalIgnoreCase))
        {
            return _bossNameMatcher;
        }

        var fileNames = GameBossListFiles.ResolveForMatcher(gameId, language, bossHealthBarStyle);
        var names = new List<string>();
        foreach (var fileName in fileNames)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
            try
            {
                if (File.Exists(path))
                {
                    names.AddRange(BossNameMatcher.ParseList(File.ReadAllLines(path)));
                    continue;
                }

                _log.Error($"Boss list file '{fileName}' was not found at '{path}'. Boss-name auto-detection will ignore that list.");
            }
            catch (Exception exception)
            {
                _log.Error($"Failed to load boss list '{fileName}'.", exception);
            }
        }

        _bossNameMatcher = new BossNameMatcher(names);
        _bossNameMatcherLanguage = cacheKey;
        _log.Info($"Boss-name matcher loaded {names.Count} boss names for game '{gameId}' language '{language}' style '{bossHealthBarStyle}' from '{string.Join(", ", fileNames)}'.");
        return _bossNameMatcher;
    }

    private void LogBossBarDiagnostics(Bitmap capture, IReadOnlyList<BossHealthBarRegion> bars, DateTimeOffset now)
    {
        // Always log the moment the detected bar count changes (e.g. 0 -> 1 when a fight starts), and
        // otherwise throttle to one line every few seconds so an idle screen does not flood the log.
        var countChanged = bars.Count != _lastLoggedBossBarCount;
        if (!countChanged && now - _lastBossBarDiagnosticLog < TimeSpan.FromSeconds(3))
        {
            return;
        }

        _lastBossBarDiagnosticLog = now;
        _lastLoggedBossBarCount = bars.Count;

        if (bars.Count == 0)
        {
            var probe = ProbeRedBand(capture);
            _log.Info($"Boss bar diagnostics: bars=0 in capture {capture.Width}x{capture.Height}. {probe}");
            return;
        }

        var geometry = string.Join(
            "; ",
            bars.Select((bar, index) =>
                $"#{index}: bar={bar.Bar}, name={bar.NameRegion}"));
        _log.Info($"Boss bar diagnostics: bars={bars.Count} in capture {capture.Width}x{capture.Height}: {geometry}.");
    }

    /// <summary>
    /// Diagnostics-only probe used when the strict bar detector finds nothing. Re-scans the same band
    /// with a deliberately loose "red-ish" rule (red is simply the dominant channel) and reports the
    /// widest run it finds plus that run's average colour, so we can see exactly which strict gate the
    /// real on-screen bar fails (e.g. green/blue too high from fire bloom or in-game brightness).
    /// </summary>
    private static string ProbeRedBand(Bitmap capture)
    {
        try
        {
            var width = capture.Width;
            var height = capture.Height;
            if (width <= 0 || height <= 0)
            {
                return "Red-band probe: empty capture.";
            }

            var rectangle = new Rectangle(0, 0, width, height);
            var data = capture.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var byteCount = Math.Abs(data.Stride) * data.Height;
                var bytes = new byte[byteCount];
                Marshal.Copy(data.Scan0, bytes, 0, byteCount);

                var left = (int)(width * 0.16);
                var right = (int)(width * 0.86);
                var bottom = (int)(height * 0.92);

                var bestRunLength = 0;
                var bestRunY = -1;
                long bestSumR = 0, bestSumG = 0, bestSumB = 0;
                var bestCount = 0;

                for (var y = 0; y < bottom; y += 2)
                {
                    var row = data.Stride > 0 ? y : data.Height - 1 - y;
                    var rowOffset = row * Math.Abs(data.Stride);

                    var runStart = -1;
                    long sumR = 0, sumG = 0, sumB = 0;
                    var count = 0;

                    for (var x = left; x < right; x += 2)
                    {
                        var offset = rowOffset + x * 4;
                        var b = bytes[offset];
                        var g = bytes[offset + 1];
                        var r = bytes[offset + 2];

                        // Loose: red is the dominant channel and not too dark. No green/blue ceiling.
                        if (r >= 50 && r >= g && r >= b)
                        {
                            if (runStart < 0)
                            {
                                runStart = x;
                                sumR = sumG = sumB = 0;
                                count = 0;
                            }

                            sumR += r;
                            sumG += g;
                            sumB += b;
                            count++;
                            continue;
                        }

                        if (runStart >= 0)
                        {
                            var runLength = x - runStart;
                            if (runLength > bestRunLength)
                            {
                                bestRunLength = runLength;
                                bestRunY = y;
                                bestSumR = sumR;
                                bestSumG = sumG;
                                bestSumB = sumB;
                                bestCount = count;
                            }
                        }

                        runStart = -1;
                    }

                    if (runStart >= 0)
                    {
                        var runLength = right - runStart;
                        if (runLength > bestRunLength)
                        {
                            bestRunLength = runLength;
                            bestRunY = y;
                            bestSumR = sumR;
                            bestSumG = sumG;
                            bestSumB = sumB;
                            bestCount = count;
                        }
                    }
                }

                if (bestCount == 0)
                {
                    return $"Red-band probe: no red-dominant pixels found in scan band (x {left}..{right}, y 0..{bottom}).";
                }

                var avgR = bestSumR / bestCount;
                var avgG = bestSumG / bestCount;
                var avgB = bestSumB / bestCount;
                var minSpan = (int)(width * 0.32);
                return
                    $"Red-band probe: widest red-ish run={bestRunLength}px (strict minSpan={minSpan}px) at cropY={bestRunY}, " +
                    $"avg RGB=({avgR},{avgG},{avgB}). Strict needs R>=80, R>=G*1.7, R>=B*1.5, G<=75, B<=85.";
            }
            finally
            {
                capture.UnlockBits(data);
            }
        }
        catch (Exception exception)
        {
            return $"Red-band probe failed: {exception.Message}";
        }
    }

    private void LogRejectedBossNameCandidates(BossNameDetectionResult result)
    {
        var rejected = result.Candidates
            .Where(candidate => !candidate.Match.IsMatch && !string.IsNullOrWhiteSpace(candidate.RawText))
            .ToList();
        if (rejected.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        if (now - _lastBossNameRejectionLog < TimeSpan.FromSeconds(15))
        {
            return;
        }

        _lastBossNameRejectionLog = now;
        var details = string.Join(
            "; ",
            rejected.Select(candidate => $"'{Compact(candidate.RawText, 60)}' -> no-match (confidence={FormatScore(candidate.Match.Confidence)})"));
        _log.Info($"Boss-name OCR candidates rejected (bars={result.BarCount}): {details}.");
    }

    private static string FormatBossConfidences(BossNameDetectionResult result)
    {
        return string.Join(
            ",",
            result.Candidates.Where(candidate => candidate.Match.IsMatch).Select(candidate => FormatScore(candidate.Match.Confidence)));
    }

    private void LogWeakImageSignal(ImageDeathSignalMatch match)
    {
        var now = DateTimeOffset.Now;
        if (now - _lastWeakImageSignalLog < TimeSpan.FromSeconds(15))
        {
            return;
        }

        _lastWeakImageSignalLog = now;
        LogDetectionEvent("death", "weak-signal", _detectionFrameIndex, ImageDeathSignalMatch.NoMatch, match, FormatCompactStabilizerState(_deathSignalStabilizer), FormatGateState(), match.Details);
        _log.Info($"Weak image death-text candidate did not pass gate. Method='{match.Method}', score={FormatScore(match.Score)}, threshold={FormatScore(match.Threshold)}, scale={FormatScore(match.Scale)}.");
    }

    private void LogWeakBossVictoryImageSignal(ImageDeathSignalMatch match)
    {
        var now = DateTimeOffset.Now;
        if (now - _lastWeakBossVictoryImageSignalLog < TimeSpan.FromSeconds(15))
        {
            return;
        }

        _lastWeakBossVictoryImageSignalLog = now;
        LogDetectionEvent("boss-victory", "weak-signal", _detectionFrameIndex, ImageDeathSignalMatch.NoMatch, match, FormatCompactStabilizerState(_bossVictorySignalStabilizer), FormatBossVictoryGateState(), match.Details);
        _log.Info($"Weak image boss-victory candidate did not pass gate. Method='{match.Method}', score={FormatScore(match.Score)}, threshold={FormatScore(match.Threshold)}, scale={FormatScore(match.Scale)}.");
    }

    private void HandlePendingSignal(long frameIndex, AppSettings settings, Bitmap frame, ImageDeathSignalMatch signal)
    {
        var now = DateTimeOffset.Now;
        var shouldUpdateStatus = now - _lastPendingSignalStatus >= TimeSpan.FromSeconds(5);

        var evidencePath = shouldUpdateStatus
            ? SaveDetectionEvidence(settings, frame, signal, now, "pending", frameIndex)
            : null;
        LogDetectionEvent("death", "pending", frameIndex, signal, null, FormatCompactStabilizerState(_deathSignalStabilizer), FormatGateState(), "awaiting-stabilizer-confirmation", evidencePath);
        _log.Info(
            $"Death signal pending confirmation on frame #{frameIndex}. " +
            $"{FormatSignal(signal)}, " +
            $"stabilizer={FormatStabilizerState()}, " +
            $"statusUpdated={shouldUpdateStatus}, " +
            $"evidence='{evidencePath ?? "not-saved"}'.");
        if (shouldUpdateStatus)
        {
            _lastPendingSignalStatus = now;
            RaiseStatus("Death signal pending confirmation", _lastDetectedDeath);
        }
    }

    private void HandlePendingBossVictorySignal(long frameIndex, AppSettings settings, Bitmap frame, ImageDeathSignalMatch signal)
    {
        var now = DateTimeOffset.Now;
        var shouldUpdateStatus = now - _lastPendingBossVictorySignalStatus >= TimeSpan.FromSeconds(5);

        var evidencePath = shouldUpdateStatus
            ? SaveFrameEvidence(settings, frame, now, "boss-victory-pending", signal.Method, signal.Score, frameIndex)
            : null;
        LogDetectionEvent("boss-victory", "pending", frameIndex, signal, null, FormatCompactStabilizerState(_bossVictorySignalStabilizer), FormatBossVictoryGateState(), "awaiting-stabilizer-confirmation", evidencePath);
        _log.Info(
            $"Boss victory signal pending confirmation on frame #{frameIndex}. " +
            $"{FormatSignal(signal)}, " +
            $"stabilizer={FormatBossVictoryStabilizerState()}, " +
            $"statusUpdated={shouldUpdateStatus}, " +
            $"evidence='{evidencePath ?? "not-saved"}'.");
        if (shouldUpdateStatus)
        {
            _lastPendingBossVictorySignalStatus = now;
            RaiseStatus("Boss victory signal pending confirmation", _lastDetectedDeath);
        }
    }

    private void LogRepeatedSignal(long frameIndex, ImageDeathSignalMatch match, string reason)
    {
        var now = DateTimeOffset.Now;
        if (now - _lastRepeatedSignalLog < TimeSpan.FromSeconds(10))
        {
            return;
        }

        _lastRepeatedSignalLog = now;
        LogDetectionEvent("death", "ignored", frameIndex, match, null, FormatCompactStabilizerState(_deathSignalStabilizer), FormatGateState(), reason);
        _log.Info($"Ignored repeated death signal on frame #{frameIndex} because {reason}. {FormatSignal(match)}.");
        RaiseStatus("Repeated death signal ignored", _lastDetectedDeath);
    }

    private void LogRepeatedBossVictorySignal(long frameIndex, ImageDeathSignalMatch match, string reason)
    {
        var now = DateTimeOffset.Now;
        if (now - _lastRepeatedBossVictorySignalLog < TimeSpan.FromSeconds(10))
        {
            return;
        }

        _lastRepeatedBossVictorySignalLog = now;
        LogDetectionEvent("boss-victory", "ignored", frameIndex, match, null, FormatCompactStabilizerState(_bossVictorySignalStabilizer), FormatBossVictoryGateState(), reason);
        _log.Info($"Ignored repeated boss victory signal on frame #{frameIndex} because {reason}. {FormatSignal(match)}.");
        RaiseStatus("Repeated boss victory signal ignored", _lastDetectedDeath);
    }

    private string? SaveDetectionEvidence(AppSettings settings, Bitmap frame, ImageDeathSignalMatch match, DateTimeOffset timestamp, string prefix, long frameIndex)
    {
        return SaveFrameEvidence(settings, frame, timestamp, prefix, match.Method, match.Score, frameIndex);
    }

    private string? SaveFrameEvidence(AppSettings settings, Bitmap frame, DateTimeOffset timestamp, string prefix, string reason, double? score, long frameIndex)
    {
        try
        {
            CleanupDiagnostics(settings);
            var packageName = $"{timestamp:yyyyMMdd-HHmmssfff}-frame-{frameIndex}-{prefix}-{SanitizeFileName(reason)}";
            var directory = Path.Combine(settings.DataFolderPath, "diagnostics", packageName);
            Directory.CreateDirectory(directory);
            var scorePart = score is null ? string.Empty : $"-{FormatScore(score.Value)}";
            var framePath = Path.Combine(directory, $"frame{scorePart}.png");
            frame.Save(framePath, ImageFormat.Png);
            var summaryPath = Path.Combine(directory, "summary.json");
            var summary = new
            {
                time = timestamp,
                frame = frameIndex,
                type = prefix,
                reason,
                score,
                screenshot = Path.GetFileName(framePath),
                capture = $"{frame.Width}x{frame.Height}"
            };
            File.WriteAllText(summaryPath, System.Text.Json.JsonSerializer.Serialize(summary, EldenDeathCounter.Core.Storage.JsonFileOptions.Value));
            return summaryPath;
        }
        catch (Exception exception)
        {
            _log.Error("Failed to save detection evidence screenshot.", exception);
            return null;
        }
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(character => invalid.Contains(character) ? '-' : character).ToArray();
        return new string(chars).Replace(' ', '-');
    }

    private void LogDetectionFrameDiagnostics(
        long frameIndex,
        DateTimeOffset frameStartedAt,
        Bitmap frame,
        string ocrText,
        DeathPhraseMatch phraseMatch,
        ImageDeathSignalMatch imageSignal,
        ImageDeathSignalMatch selectedSignal,
        ImageDeathSignalMatch? confirmedSignal,
        string imageAnalysisStatus,
        bool wasPending,
        string stabilizerBefore,
        string stabilizerAfter,
        string frameOutcome,
        long captureMs,
        long ocrMs,
        long imageAnalysisMs,
        long totalMs,
        double sensitivity,
        long? frameDeltaMs,
        string timingMode)
    {
        _detectionEventLog.Log(new DetectionEventRecord
        {
            Time = frameStartedAt,
            Frame = frameIndex,
            Kind = "death",
            Outcome = "frame",
            SelectedSignal = selectedSignal.IsMatch ? selectedSignal.Method : null,
            OcrScore = phraseMatch.Score,
            ImageScore = imageSignal.Score,
            Stabilizer = FormatCompactStabilizerState(_deathSignalStabilizer),
            Gate = FormatGateState(),
            Reason =
                $"outcome={frameOutcome}; imageAnalysis={imageAnalysisStatus}; capture={frame.Width}x{frame.Height}; " +
                $"ocr='{Compact(ocrText, 500)}'; normalized='{Compact(phraseMatch.NormalizedText, 500)}'; " +
                $"phraseMatch={phraseMatch.IsMatch}; phrase='{phraseMatch.MatchedPhrase ?? ""}'; details='{Compact(phraseMatch.Details)}'; " +
                $"image={FormatSignal(imageSignal)}; selected={FormatSignal(selectedSignal)}; " +
                $"confirmed={(confirmedSignal is null ? "none" : FormatSignal(confirmedSignal))}; wasPending={wasPending}; " +
                $"before={stabilizerBefore}; after={stabilizerAfter}; sensitivity={FormatScore(sensitivity)}; " +
                $"timingMode={timingMode}; frameDeltaMs={frameDeltaMs?.ToString(CultureInfo.InvariantCulture) ?? "none"}; " +
                $"targetBaseMs={DetectionTimingOptions.MinimumBaseIntervalMs}; targetBurstMs={DetectionTimingOptions.BurstIntervalMs}",
            CaptureMs = captureMs,
            OcrMs = ocrMs,
            ImageMs = imageAnalysisMs,
            TotalMs = totalMs,
            FrameDeltaMs = frameDeltaMs,
            TimingMode = timingMode,
            IsFrameDiagnostic = true
        });
    }

    private string FormatGateState()
    {
        return
            $"latched={_detectionGate.IsScreenLatched}," +
            $"clearFrames={_detectionGate.ClearFrames}/{_detectionGate.ClearFramesRequired}," +
            $"lastCountedAt='{_detectionGate.LastCountedAt?.ToString("O", CultureInfo.InvariantCulture) ?? "none"}'";
    }

    private string FormatBossVictoryGateState()
    {
        return
            $"latched={_bossVictoryDetectionGate.IsScreenLatched}," +
            $"clearFrames={_bossVictoryDetectionGate.ClearFrames}/{_bossVictoryDetectionGate.ClearFramesRequired}," +
            $"lastCountedAt='{_bossVictoryDetectionGate.LastCountedAt?.ToString("O", CultureInfo.InvariantCulture) ?? "none"}'";
    }

    private string FormatStabilizerState()
    {
        return
            $"signals={_deathSignalStabilizer.SignalFrames}/{_deathSignalStabilizer.RequiredSignalFrames}," +
            $"observedFrames={_deathSignalStabilizer.ObservedFrames}," +
            $"missingFrames={_deathSignalStabilizer.MissingFrames}/{_deathSignalStabilizer.AllowedMissingFrames}," +
            $"pending={_deathSignalStabilizer.IsPending}";
    }

    private string FormatBossVictoryStabilizerState()
    {
        return
            $"signals={_bossVictorySignalStabilizer.SignalFrames}/{_bossVictorySignalStabilizer.RequiredSignalFrames}," +
            $"observedFrames={_bossVictorySignalStabilizer.ObservedFrames}," +
            $"missingFrames={_bossVictorySignalStabilizer.MissingFrames}/{_bossVictorySignalStabilizer.AllowedMissingFrames}," +
            $"pending={_bossVictorySignalStabilizer.IsPending}";
    }

    private static string FormatCompactStabilizerState(DeathSignalStabilizer stabilizer)
    {
        return $"{stabilizer.SignalFrames}/{stabilizer.RequiredSignalFrames}";
    }

    private void LogDetectionEvent(
        string kind,
        string outcome,
        long frameIndex,
        ImageDeathSignalMatch selectedSignal,
        ImageDeathSignalMatch? imageSignal,
        string stabilizer,
        string gate,
        string? reason,
        string? evidence = null,
        string? activeBoss = null)
    {
        _detectionEventLog.Log(new DetectionEventRecord
        {
            Time = DateTimeOffset.Now,
            Frame = frameIndex,
            Kind = kind,
            Outcome = outcome,
            ActiveBoss = activeBoss ?? _counterService.State.ActiveBoss?.Name,
            SelectedSignal = selectedSignal.IsMatch ? selectedSignal.Method : null,
            OcrScore = selectedSignal.Method.Contains("ocr", StringComparison.OrdinalIgnoreCase) ? selectedSignal.Score : null,
            ImageScore = imageSignal?.Score ?? (selectedSignal.Method.Contains("template", StringComparison.OrdinalIgnoreCase) ? selectedSignal.Score : null),
            Stabilizer = stabilizer,
            Gate = gate,
            Reason = reason,
            Evidence = evidence
        });
    }

    private static void CleanupDiagnostics(AppSettings settings)
    {
        var diagnosticsDirectory = Path.Combine(settings.DataFolderPath, "diagnostics");
        if (!Directory.Exists(diagnosticsDirectory))
        {
            return;
        }

        var cutoff = DateTimeOffset.Now - TimeSpan.FromDays(Math.Max(1, settings.DiagnosticsRetentionDays));
        foreach (var directory in Directory.EnumerateDirectories(diagnosticsDirectory))
        {
            var info = new DirectoryInfo(directory);
            if (info.CreationTimeUtc < cutoff.UtcDateTime)
            {
                info.Delete(recursive: true);
            }
        }

        var retained = new DirectoryInfo(diagnosticsDirectory)
            .EnumerateDirectories()
            .OrderByDescending(directory => directory.CreationTimeUtc)
            .ToList();
        foreach (var directory in retained.Skip(200))
        {
            directory.Delete(recursive: true);
        }
    }

    private static string FormatSignal(ImageDeathSignalMatch match)
    {
        return
            $"isMatch={match.IsMatch},method='{match.Method}',score={FormatScore(match.Score)},scale={FormatScore(match.Scale)},details='{Compact(match.Details)}'";
    }

    private static string Compact(string value, int maxLength = 180)
    {
        var compact = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
        return compact.Length <= maxLength ? compact : compact[..maxLength] + "...";
    }

    private static string FormatScore(double value)
    {
        return value.ToString("0.000", CultureInfo.InvariantCulture);
    }
}
