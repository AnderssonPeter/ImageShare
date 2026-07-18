namespace ImageShare.Browsing;

internal static class PathHelper
{
    public static void EnsureSafePath(string path)
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

#pragma warning disable RS0030
    public static string Combine(string path1, string path2)
    {
        EnsureSafePath(path1);
        EnsureSafePath(path2);
        return Path.Combine(path1, path2);
    }

#pragma warning restore RS0030

    public static string GetFirstSegment(string path)
    {
        var index = path.IndexOf('/');
        return index < 0 ? path : path[..index];
    }

    public static bool IsInFolder(string path) => path.Contains('/');
}
