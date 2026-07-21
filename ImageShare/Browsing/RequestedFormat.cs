namespace ImageShare.Browsing;

public sealed class RequestedFormat(string[]? formatValues)
{
    public string? Value { get; } = formatValues?.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?
        .Trim().TrimStart('.').ToLowerInvariant();

    public bool IsSpecified => Value is not null;

    public bool IsSupportedBy(IReadOnlyList<string> supportedFormats) =>
        Value is null || supportedFormats.Contains(Value, StringComparer.OrdinalIgnoreCase);

    public bool Matches(string? extension) =>
        Value is null || string.Equals(extension, Value, StringComparison.OrdinalIgnoreCase);
}
