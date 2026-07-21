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

        var format = new RequestedFormat(request.Format);
        if (!format.IsSupportedBy(imageEnumerator.SupportedFormats))
        {
            throw new BadRequestException($"Format '{format.Value}' is not supported.");
        }

        foreach (var folder in folders)
        {
            user.EnsureCanAccessFolder(folder);
        }

        var imageFiles = folders
            .SelectMany(folder => imageEnumerator.EnumerateImages(folder, recursive: true))
            .Where(file => format.Matches(file.Path.Extension))
            .ToList();

        if (imageFiles.Count == 0)
        {
            throw new NotFoundException("No images were found matching the requested criteria.");
        }

        RelativePath? stripPrefix = folders.Count == 1 ? folders[0] : null;

        var result = TypedResults.Stream(
            async stream => await WriteZipAsync(imageFiles, stream, cancellationToken, stripPrefix),
            "application/zip",
            "images.zip");

        return new(result);
    }

    internal static async Task WriteZipAsync(
        IEnumerable<(RelativePath Path, IFileInfo Info)> imageFiles,
        Stream output,
        CancellationToken cancellationToken,
        RelativePath? stripPrefix = null)
    {
        using var memoryStream = new MemoryStream();
        await using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, info) in imageFiles)
            {
                var entryName = path.Value;
                if (stripPrefix is { } prefix)
                {
                    var prefixWithSlash = prefix.Value + "/";
                    if (entryName.StartsWith(prefixWithSlash, StringComparison.Ordinal))
                    {
                        entryName = entryName[prefixWithSlash.Length..];
                    }
                }

                var entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
                await using var entryStream = await entry.OpenAsync(cancellationToken);
                await using var fileStream = info.CreateReadStream();
                await fileStream.CopyToAsync(entryStream, cancellationToken);
            }
        }

        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(output, cancellationToken);
    }

    private static List<RelativePath> NormalizeFolders(string[] folderValues) =>
        [.. folderValues
            .Where(value => !string.IsNullOrEmpty(value))
            .Select(value => new RelativePath(value))];
}
