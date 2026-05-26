namespace EldenDeathCounter.Core.Detection;

public sealed class DeathDetectionGate
{
    private readonly int _clearFramesRequired;
    private DateTimeOffset? _lastCountedAt;
    private bool _screenLatched;
    private int _clearFrames;

    public DeathDetectionGate(int clearFramesRequired = 3)
    {
        _clearFramesRequired = Math.Max(1, clearFramesRequired);
    }

    public int ClearFramesRequired => _clearFramesRequired;

    public int ClearFrames => _clearFrames;

    public bool IsScreenLatched => _screenLatched;

    public DateTimeOffset? LastCountedAt => _lastCountedAt;

    public DeathDetectionDecision Evaluate(bool hasDeathSignal, DateTimeOffset now, TimeSpan cooldown)
    {
        if (!hasDeathSignal)
        {
            if (!_screenLatched)
            {
                return DeathDetectionDecision.NoSignal;
            }

            _clearFrames++;
            if (_clearFrames < _clearFramesRequired)
            {
                return DeathDetectionDecision.NoSignal;
            }

            _screenLatched = false;
            _clearFrames = 0;
            return DeathDetectionDecision.Rearmed;
        }

        _clearFrames = 0;

        if (_screenLatched)
        {
            return DeathDetectionDecision.IgnoreActiveScreen;
        }

        if (_lastCountedAt is not null && now - _lastCountedAt.Value < cooldown)
        {
            _screenLatched = true;
            return DeathDetectionDecision.IgnoreCooldown;
        }

        _screenLatched = true;
        _lastCountedAt = now;
        return DeathDetectionDecision.Count;
    }
}
