using System.IO.Compression;
using ImageShare.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace ImageShare.Browsing;

internal sealed class DownloadImagesQueryHandler(
    ImageEnumerator imageEnumerator,
    IUser user)
    : IQueryHandler<DownloadImagesQuery, Results<PushStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound>>
{
    public ValueTask<Results<PushStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound>> Handle(
        DownloadImagesQuery request,
        CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated)
        {
            return new(TypedResults.Unauthorized());
        }

        var folders = NormalizeFolders(request.Folders);
        if (folders.Count == 0)
        {
            return new(TypedResults.BadRequest());
        }

        var format = BrowsingHelpers.NormalizeFormat(request.Format);
        if (format is not null && !imageEnumerator.IsSupportedFormat(format))
        {
            return new(TypedResults.BadRequest());
        }

        foreach (var folder in folders)
        {
            try
            {
                user.EnsureCanAccessFolder(folder);
            }
            catch (FolderAccessDeniedException)
            {
                return new(TypedResults.Forbid());
            }
        }

        var imageFiles = folders
            .SelectMany(folder => imageEnumerator.EnumerateImages(folder, recursive: true))
            .Where(file => format is null || string.Equals(file.Path.Extension, format, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (imageFiles.Count == 0)
        {
            return new(TypedResults.NotFound());
        }

        var result = TypedResults.Stream(
            async stream => await WriteZipAsync(imageFiles, stream, CancellationToken.None),
            "application/zip",
            "images.zip");

        return new(result);
    }

    internal static async Task WriteZipAsync(IEnumerable<(RelativePath Path, IFileInfo Info)> imageFiles, Stream output, CancellationToken cancellationToken)
    {
        await using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        foreach (var (path, info) in imageFiles)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.NoCompression);
            await using var entryStream = await entry.OpenAsync(cancellationToken);
            await using var fileStream = info.CreateReadStream();
            await fileStream.CopyToAsync(entryStream, cancellationToken);
        }
    }

    private static List<RelativePath> NormalizeFolders(StringValues folderValues) =>
        [.. folderValues.Where(value => !string.IsNullOrEmpty(value)).Cast<string>().Select(value => new RelativePath(value))];
}
