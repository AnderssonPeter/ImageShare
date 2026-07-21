using System.Text.Json.Serialization;
using ImageShare.ImageConversion;

namespace ImageShare.Browsing;

[JsonConverter(typeof(RelativePathJsonConverter))]
public readonly struct RelativePath : IEquatable<RelativePath>
{
    public const string SafePathPattern = @"^(?!.*\.\.)[^/\\].*$";

    private readonly string? path;
    public static readonly RelativePath Root = new("");
    public RelativePath(string value)
    {
        var normalized = Normalize(value);
        EnsureSafe(normalized);
        path = normalized;
    }

    public string Value => path ?? "";
    public bool HasRootFolder => !string.IsNullOrEmpty(path);
    public bool IsInFolder => path is not null && path.Contains('/');

    public string RootFolder
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

    public RelativePath Directory => new(Normalize(Path.GetDirectoryName(Value) ?? ""));

    public bool IsThumbnail => FileName.Contains(ImageConverterOptions.ThumbnailInfix, StringComparison.Ordinal);

    public RelativePath Combine(string child)
    {
        var normalizedChild = Normalize(child);
        EnsureSafe(normalizedChild);
        var basePath = Value;
        var combined = string.IsNullOrEmpty(basePath) ? normalizedChild : basePath + "/" + normalizedChild;
        return new RelativePath(combined);
    }

    public static bool TryParse(string? value, out RelativePath path)
    {
        try
        {
            var decoded = value is null ? "" : Uri.UnescapeDataString(value);
            path = new RelativePath(decoded);
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

    private static string Normalize(string value)
    {
        var normalized = value.Replace('\\', '/');
        if (normalized.Length > 0 && normalized[^1] == '/')
        {
            normalized = normalized[..^1];
        }

        return normalized;
    }

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
