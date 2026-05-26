using System.Text.Json.Serialization;

namespace EldenDeathCounter.Core.Logging;

public sealed class DetectionEventRecord
{
    public DateTimeOffset Time { get; set; } = DateTimeOffset.Now;

    public long Frame { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string Outcome { get; set; } = string.Empty;

    public string? ActiveBoss { get; set; }

    public string? SelectedSignal { get; set; }

    public double? OcrScore { get; set; }

    public double? ImageScore { get; set; }

    public string? Stabilizer { get; set; }

    public string? Gate { get; set; }

    public string? Reason { get; set; }

    public string? Evidence { get; set; }

    public long? CaptureMs { get; set; }

    public long? OcrMs { get; set; }

    public long? ImageMs { get; set; }

    public long? TotalMs { get; set; }

    [JsonIgnore]
    public bool IsFrameDiagnostic { get; set; }
}
