using ImageShare.ImageConversion;

namespace ImageShare.Browsing;

public readonly struct RelativePath : IEquatable<RelativePath>
{
    private readonly string? _value;

    public RelativePath(string value)
    {
        EnsureSafe(value);
        _value = value;
    }

    public string Value => _value ?? "";
    public bool IsEmpty => string.IsNullOrEmpty(_value);
    public bool IsInFolder => _value is not null && _value.Contains('/');

    public string FirstSegment
    {
        get
        {
            var value = Value;
            var index = value.IndexOf('/');
            return index < 0 ? value : value[..index];
        }
    }

    public string FileName => Path.GetFileName(Value);

    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(Value);

    public string? Extension
    {
        get
        {
            var extension = Path.GetExtension(Value);
            return string.IsNullOrEmpty(extension) ? null : extension.TrimStart('.');
        }
    }

    public bool HasExtension => Extension is not null;

    public string Directory => Path.GetDirectoryName(Value) ?? "";

    public bool IsThumbnail => FileName.Contains(ImageConverterOptions.ThumbnailInfix, StringComparison.Ordinal);

    public RelativePath Combine(string child)
    {
        EnsureSafe(child);
#pragma warning disable RS0030
        return new RelativePath(Path.Combine(Value, child));
#pragma warning restore RS0030
    }

    public static bool TryParse(string? value, out RelativePath path)
    {
        try
        {
            path = new RelativePath(value ?? "");
            return true;
        }
        catch (ArgumentException)
        {
            path = default;
            return false;
        }
    }

    public override string ToString() => Value;

    public bool Equals(RelativePath other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is RelativePath other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static bool operator ==(RelativePath left, RelativePath right) => left.Equals(right);

    public static bool operator !=(RelativePath left, RelativePath right) => !left.Equals(right);

    public static implicit operator string(RelativePath path) => path.Value;

    private static void EnsureSafe(string path)
    {
        if (path.Contains("..", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Path contains .., not allowed", nameof(path));
        }

        if (Path.IsPathRooted(path))
        {
            throw new ArgumentException("Path is rooted, not allowed", nameof(path));
        }
    }
}
