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
        var format = new RequestedFormat(request.Format);
        if (!format.IsSupportedBy(imageEnumerator.SupportedFormats))
        {
            throw new BadRequestException($"Format '{format.Value}' is not supported.");
        }

        user.EnsureCanAccessFolder(request.Folder);

        var imageFiles = imageEnumerator.EnumerateImages(request.Folder, recursive: true)
            .Where(file => format.Matches(file.Path.Extension))
            .ToList();

        if (imageFiles.Count == 0)
        {
            throw new NotFoundException("No images were found matching the requested criteria.");
        }

        var result = TypedResults.Stream(
            async stream => await WriteZipAsync(imageFiles, stream, cancellationToken, request.Folder),
            "application/zip",
            "images.zip");

        return new(result);
    }

    internal static async Task WriteZipAsync(
        IEnumerable<(RelativePath Path, IFileInfo Info)> imageFiles,
        Stream output,
        CancellationToken cancellationToken,
        RelativePath stripPrefix)
    {
        var prefixWithSlash = stripPrefix.Value + "/";

        var temporaryPath = Path.GetTempFileName();
        await using var temporaryStream = new FileStream(
            temporaryPath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);

        await using (var archive = new ZipArchive(temporaryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, info) in imageFiles)
            {
                var entryName = path.Value;
                if (entryName.StartsWith(prefixWithSlash, StringComparison.Ordinal))
                {
                    entryName = entryName[prefixWithSlash.Length..];
                }

                var entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
                await using var entryStream = await entry.OpenAsync(cancellationToken);
                await using var fileStream = info.CreateReadStream();
                await fileStream.CopyToAsync(entryStream, cancellationToken);
            }
        }

        temporaryStream.Position = 0;
        await temporaryStream.CopyToAsync(output, cancellationToken);
    }
}
