namespace BlutdruckErfassungApp.Services;

public sealed class ParsedBloodPressureResult
{
    public int? Systolic { get; set; }
    public int? Diastolic { get; set; }
    public int? Pulse { get; set; }
    public DateTimeOffset? MeasuredAt { get; set; }
    public string RawText { get; set; } = string.Empty;
}
