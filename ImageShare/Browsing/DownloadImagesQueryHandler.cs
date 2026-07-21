using System.IO.Compression;
using ImageShare.Authentication;
using ImageShare.Errors;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.FileProviders;

namespace ImageShare.Browsing;

internal sealed class DownloadImagesQueryHandler(
    ImageEnumerator imageEnumerator,
    IUser user)
    : IQueryHandler<DownloadImagesQuery, PushStreamHttpResult>
{
    public ValueTask<PushStreamHttpResult> Handle(
        DownloadImagesQuery request,
        CancellationToken cancellationToken)
    {
        var folders = NormalizeFolders(request.Folders);
        if (folders.Count == 0)
        {
            throw new BadRequestException("At least one folder must be specified.");
        }

        var format = BrowsingHelpers.NormalizeFormat(request.Format);
        if (format is not null && !imageEnumerator.IsSupportedFormat(format))
        {
            throw new BadRequestException($"Format '{format}' is not supported.");
        }

        foreach (var folder in folders)
        {
            user.EnsureCanAccessFolder(folder);
        }

        var imageFiles = folders
            .SelectMany(folder => imageEnumerator.EnumerateImages(folder, recursive: true))
            .Where(file => format is null || string.Equals(file.Path.Extension, format, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (imageFiles.Count == 0)
        {
            throw new NotFoundException("No images were found matching the requested criteria.");
        }

        var result = TypedResults.Stream(
            async stream => await WriteZipAsync(imageFiles, stream, cancellationToken),
            "application/zip",
            "images.zip");

        return new(result);
    }

    internal static async Task WriteZipAsync(IEnumerable<(RelativePath Path, IFileInfo Info)> imageFiles, Stream output, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, info) in imageFiles)
            {
                var entry = archive.CreateEntry(path, CompressionLevel.NoCompression);
                await using var entryStream = await entry.OpenAsync(cancellationToken);
                await using var fileStream = info.CreateReadStream();
                await fileStream.CopyToAsync(entryStream, cancellationToken);
            }
        }

        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(output, cancellationToken);
    }

    private static List<RelativePath> NormalizeFolders(string[] folderValues) =>
        [.. folderValues.Where(value => !string.IsNullOrEmpty(value)).Select(value => new RelativePath(value))];
}
