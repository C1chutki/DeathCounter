namespace EldenDeathCounter.Core.Detection;

/// <summary>Per-frame decision returned by <see cref="BossEncounterNameTracker.BeginFrame"/>.</summary>
/// <param name="ShouldReadNames">Whether the caller should run (expensive) OCR on the bar name regions this frame.</param>
/// <param name="Rearmed">Whether the encounter just ended (bars gone long enough) and detection re-armed.</param>
public readonly record struct BossEncounterFrameDecision(bool ShouldReadNames, bool Rearmed);

/// <summary>
/// Models boss-name detection as an encounter state machine so the active boss name is read once at
/// encounter start, published when a confident boss-list match is found, then frozen for the rest of
/// the fight. Detection only re-arms after the boss bars disappear for several consecutive frames
/// (or <see cref="Reset"/> is called on death/victory/restart).
/// </summary>
public sealed class BossEncounterNameTracker
{
    private readonly int _requiredStableFrames;
    private readonly int _clearFramesRequired;
    private readonly int _maxOcrAttempts;
    private int _framesWithBars;
    private int _framesWithoutBars;
    private int _ocrAttempts;
    private bool _frozen;
    private string? _frozenName;

    public BossEncounterNameTracker(int requiredStableFrames = 2, int clearFramesRequired = 3, int maxOcrAttempts = 12)
    {
        _requiredStableFrames = Math.Max(1, requiredStableFrames);
        _clearFramesRequired = Math.Max(1, clearFramesRequired);
        _maxOcrAttempts = Math.Max(1, maxOcrAttempts);
    }

    public bool IsFrozen => _frozen;

    public string? FrozenName => _frozenName;

    /// <summary>Advances the state machine for a frame and reports whether name OCR is worthwhile.</summary>
    public BossEncounterFrameDecision BeginFrame(int barCount)
    {
        if (barCount <= 0)
        {
            _framesWithBars = 0;
            _framesWithoutBars++;
            var rearmed = false;
            if (_framesWithoutBars >= _clearFramesRequired && (_frozen || _ocrAttempts > 0))
            {
                Rearm();
                rearmed = true;
            }

            return new BossEncounterFrameDecision(false, rearmed);
        }

        _framesWithoutBars = 0;
        _framesWithBars++;
        var shouldRead = !_frozen
            && _framesWithBars >= _requiredStableFrames
            && _ocrAttempts < _maxOcrAttempts;
        return new BossEncounterFrameDecision(shouldRead, false);
    }

    /// <summary>
    /// Submits the matched names read this frame. Returns the name to publish (and freezes the
    /// encounter) only when every detected bar matched, or when the bounded OCR attempts run out
    /// with at least one confident match. Returns null while still waiting or already frozen.
    /// </summary>
    public string? SubmitNames(int barCount, int matchedCount, string? combinedName)
    {
        if (_frozen || barCount <= 0)
        {
            return null;
        }

        _ocrAttempts++;
        if (matchedCount <= 0 || string.IsNullOrWhiteSpace(combinedName))
        {
            return null;
        }

        var allBarsMatched = matchedCount >= barCount;
        if (allBarsMatched || _ocrAttempts >= _maxOcrAttempts)
        {
            _frozen = true;
            _frozenName = combinedName;
            return combinedName;
        }

        // Partial match: keep trying for a more complete read before freezing.
        return null;
    }

    /// <summary>Re-arms detection for a new encounter (call on death, boss victory, or restart).</summary>
    public void Reset() => Rearm();

    private void Rearm()
    {
        _framesWithBars = 0;
        _framesWithoutBars = 0;
        _ocrAttempts = 0;
        _frozen = false;
        _frozenName = null;
    }
}
