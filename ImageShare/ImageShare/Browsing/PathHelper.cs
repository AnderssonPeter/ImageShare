using System.Diagnostics;

namespace ImageShare.Browsing;

internal static class PathHelper
{
#pragma warning disable RS0030
    public static string Combine(string path1, string path2)
    {
        if (path1.Contains("..", StringComparison.Ordinal) || path2.Contains("..", StringComparison.Ordinal))
        {
            ThrowPathTraversal(path1, path2);
        }

        return Path.Combine(path1, path2);
    }
#pragma warning restore RS0030

    public static string GetFirstSegment(string path)
    {
        var index = path.IndexOf('/');
        return index < 0 ? path : path[..index];
    }

    public static bool IsInFolder(string path) => path.Contains('/');

    [DebuggerHidden, StackTraceHidden]
    private static void ThrowPathTraversal(string path1, string path2)
        => throw new ArgumentException("Path traversal detected ('..' not allowed)", nameof(path1));
}
