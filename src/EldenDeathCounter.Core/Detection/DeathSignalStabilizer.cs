namespace EldenDeathCounter.Core.Detection;

public sealed class DeathSignalStabilizer
{
    private readonly int _requiredConsecutiveFrames;
    private readonly int _allowedMissingFrames;
    private int _candidateSignalFrames;
    private int _candidateFrames;

    public DeathSignalStabilizer(int requiredConsecutiveFrames = 2, int allowedMissingFrames = 2)
    {
        _requiredConsecutiveFrames = Math.Max(1, requiredConsecutiveFrames);
        _allowedMissingFrames = Math.Max(0, allowedMissingFrames);
    }

    public int RequiredSignalFrames => _requiredConsecutiveFrames;

    public int AllowedMissingFrames => _allowedMissingFrames;

    public int SignalFrames => _candidateSignalFrames;

    public int ObservedFrames => _candidateFrames;

    public int MissingFrames => Math.Max(0, _candidateFrames - _candidateSignalFrames);

    public bool IsPending => _candidateSignalFrames > 0 && _candidateSignalFrames < _requiredConsecutiveFrames;

    public ImageDeathSignalMatch? Observe(ImageDeathSignalMatch signal)
    {
        var canConfirmPendingSignal = IsPending && signal.CanConfirmPendingSignal;
        if (!signal.IsMatch && !canConfirmPendingSignal)
        {
            if (_candidateSignalFrames > 0)
            {
                _candidateFrames++;
                if (_candidateFrames - _candidateSignalFrames > _allowedMissingFrames)
                {
                    Reset();
                }
            }

            return null;
        }

        var observedSignal = canConfirmPendingSignal
            ? PromoteNearThresholdConfirmation(signal)
            : signal;

        if (_candidateSignalFrames == 0)
        {
            _candidateFrames = 1;
        }
        else
        {
            _candidateFrames++;
        }

        _candidateSignalFrames++;
        return _candidateSignalFrames >= _requiredConsecutiveFrames ? observedSignal : null;
    }

    public void Reset()
    {
        _candidateSignalFrames = 0;
        _candidateFrames = 0;
    }

    private static ImageDeathSignalMatch PromoteNearThresholdConfirmation(ImageDeathSignalMatch signal)
    {
        var details = string.IsNullOrWhiteSpace(signal.Details)
            ? "pendingConfirmation=near-threshold"
            : $"{signal.Details}; pendingConfirmation=near-threshold";

        return signal with
        {
            IsMatch = true,
            Details = details
        };
    }
}
