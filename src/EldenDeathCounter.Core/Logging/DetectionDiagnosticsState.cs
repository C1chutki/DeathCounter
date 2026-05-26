namespace EldenDeathCounter.Core.Logging;

public sealed record DetectionDiagnosticsState(int DeathCount, string? ActiveBoss, bool DetectionRunning);
