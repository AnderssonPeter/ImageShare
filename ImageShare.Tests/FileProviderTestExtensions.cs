using Microsoft.Extensions.FileProviders;
using Mirality.FileProviders;

namespace ImageShare.Tests;

internal static class FileProviderTestExtensions
{
    public static void AddDirectory(this ISyncWritableFileProvider fileProvider, string path) =>
        fileProvider.Write($"{path}/.keep", []);

    public static void AddFile(this ISyncWritableFileProvider fileProvider, string path) =>
        fileProvider.Write(path, []);

    public static void AddFile(this ISyncWritableFileProvider fileProvider, string path, byte[] content) =>
        fileProvider.Write(path, content);

    public static void CreateDirectory(this ISyncWritableFileProvider fileProvider, string path) =>
        fileProvider.Create(path);

    public static byte[] ReadAllBytes(this IFileInfo file)
    {
        using var stream = file.CreateReadStream();
        return stream.ReadAllBytes();
    }

    public static byte[] ReadAllBytes(this Stream stream)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
