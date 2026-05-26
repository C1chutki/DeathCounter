namespace EldenDeathCounter.Core.Detection;

public sealed class BossNameStabilizer
{
    private readonly int _requiredStableFrames;
    private readonly int _allowedMissingFrames;
    private string? _candidateKey;
    private string? _candidateName;
    private string? _lastPublishedKey;
    private int _candidateFrames;
    private int _missingFrames;

    public BossNameStabilizer(int requiredStableFrames = 1, int allowedMissingFrames = 1)
    {
        _requiredStableFrames = Math.Max(1, requiredStableFrames);
        _allowedMissingFrames = Math.Max(0, allowedMissingFrames);
    }

    public string? Observe(string ocrName, IReadOnlyDictionary<string, string> corrections)
    {
        var correctedName = BossNameOcrCorrector.Apply(ocrName, corrections);
        if (string.IsNullOrWhiteSpace(correctedName))
        {
            ResetCandidate();
            return null;
        }

        var correctedKey = BossNameOcrCorrector.NormalizeKey(correctedName);
        if (correctedKey.Length == 0)
        {
            ResetCandidate();
            return null;
        }

        if (string.Equals(_candidateKey, correctedKey, StringComparison.Ordinal))
        {
            _candidateFrames++;
            _missingFrames = 0;
        }
        else
        {
            _candidateKey = correctedKey;
            _candidateName = correctedName;
            _candidateFrames = 1;
            _missingFrames = 0;
        }

        if (_candidateFrames < _requiredStableFrames ||
            string.Equals(_lastPublishedKey, correctedKey, StringComparison.Ordinal))
        {
            return null;
        }

        _lastPublishedKey = correctedKey;
        return _candidateName;
    }

    public string? ObserveMissing()
    {
        if (_candidateKey is null)
        {
            return null;
        }

        _missingFrames++;
        if (_missingFrames > _allowedMissingFrames)
        {
            ResetCandidate();
        }

        return null;
    }

    public void Reset()
    {
        ResetCandidate();
        _lastPublishedKey = null;
    }

    private void ResetCandidate()
    {
        _candidateKey = null;
        _candidateName = null;
        _candidateFrames = 0;
        _missingFrames = 0;
    }
}
