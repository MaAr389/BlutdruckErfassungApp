using System.Text.RegularExpressions;

namespace BlutdruckErfassungApp.Services;

public sealed partial class OcrParsingService
{
    [GeneratedRegex(@"(?i)(sys|systole|systolic)\D{0,6}(\d{2,3})")]
    private static partial Regex SystolicRegex();

    [GeneratedRegex(@"(?i)(dia|diastole|diastolic)\D{0,6}(\d{2,3})")]
    private static partial Regex DiastolicRegex();

    [GeneratedRegex(@"(?i)(pul|pulse|puls)\D{0,6}(\d{2,3})")]
    private static partial Regex PulseRegex();

    [GeneratedRegex(@"\b(\d{1,2}[\.\/-]\d{1,2}[\.\/-]\d{2,4}|\d{4}[\.\/-]\d{1,2}[\.\/-]\d{1,2})\b")]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"\b(\d{1,2}:\d{2})\b")]
    private static partial Regex TimeRegex();

    [GeneratedRegex(@"\b(\d{2,3})\b")]
    private static partial Regex NumberRegex();

    public ParsedBloodPressureResult Parse(string rawText, DateTimeOffset fallbackTimestamp)
    {
        var cleanedText = rawText?.Trim() ?? string.Empty;

        var result = new ParsedBloodPressureResult
        {
            RawText = cleanedText,
            Systolic = ParseLabeledValue(SystolicRegex(), cleanedText),
            Diastolic = ParseLabeledValue(DiastolicRegex(), cleanedText),
            Pulse = ParseLabeledValue(PulseRegex(), cleanedText),
            MeasuredAt = ParseDateTime(cleanedText, fallbackTimestamp)
        };

        if (result.Systolic.HasValue && result.Diastolic.HasValue && result.Pulse.HasValue)
        {
            return result;
        }

        var numericValues = NumberRegex()
            .Matches(cleanedText)
            .Select(m => int.TryParse(m.Value, out var value) ? value : (int?)null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .Where(v => v is >= 20 and <= 300)
            .ToList();

        if (!result.Systolic.HasValue && numericValues.Count >= 1)
        {
            result.Systolic = numericValues[0];
        }

        if (!result.Diastolic.HasValue && numericValues.Count >= 2)
        {
            result.Diastolic = numericValues[1];
        }

        if (!result.Pulse.HasValue && numericValues.Count >= 3)
        {
            result.Pulse = numericValues[2];
        }

        return result;
    }

    private static int? ParseLabeledValue(Regex regex, string text)
    {
        var match = regex.Match(text);
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Groups[2].Value, out var parsedValue) ? parsedValue : null;
    }

    private static DateTimeOffset ParseDateTime(string text, DateTimeOffset fallbackTimestamp)
    {
        var datePart = DateRegex().Match(text).Value;
        var timePart = TimeRegex().Match(text).Value;

        if (string.IsNullOrWhiteSpace(datePart) || string.IsNullOrWhiteSpace(timePart))
        {
            return fallbackTimestamp;
        }

        var combined = $"{datePart} {timePart}";

        if (DateTime.TryParse(combined, out var parsedDateTime))
        {
            return new DateTimeOffset(parsedDateTime.ToUniversalTime());
        }

        return fallbackTimestamp;
    }
}
