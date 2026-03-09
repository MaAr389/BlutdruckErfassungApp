using System.ComponentModel.DataAnnotations;

namespace BlutdruckErfassungApp.Data;

public sealed class BloodPressureReading
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(120)]
    public string UserNameNormalized { get; set; } = string.Empty;

    public DateTimeOffset MeasuredAtUtc { get; set; }

    [Range(30, 300)]
    public int Systolic { get; set; }

    [Range(20, 200)]
    public int Diastolic { get; set; }

    [Range(20, 250)]
    public int Pulse { get; set; }

    [StringLength(4000)]
    public string RawOcrText { get; set; } = string.Empty;

    [StringLength(260)]
    public string SourceFileName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
