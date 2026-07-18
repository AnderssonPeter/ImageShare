using ImageShare.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace ImageShare.Browsing;

internal sealed class DownloadImagesQueryHandler(
    IFileProvider fileProvider,
    IOptions<ImageFormatOptions> imageFormats,
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

        var folders = BrowsingHelpers.NormalizeFolders(request.Folders);
        if (folders.Count == 0)
        {
            return new(TypedResults.BadRequest());
        }

        var format = BrowsingHelpers.NormalizeFormat(request.Format);
        if (format is not null && !imageFormats.Value.SupportedFormats.Contains(format, StringComparer.OrdinalIgnoreCase))
        {
            return new(TypedResults.BadRequest());
        }

        foreach (var folder in folders)
        {
            PathHelper.EnsureSafePath(folder);
            if (!user.CanAccessFolder(PathHelper.GetFirstSegment(folder)))
            {
                return new(TypedResults.Forbid());
            }
        }

        var imageFiles = folders
            .SelectMany(folder => BrowsingHelpers.EnumerateImageFiles(fileProvider, imageFormats.Value, folder))
            .Where(file => format is null || string.Equals(Path.GetExtension(file.Info.Name).TrimStart('.'), format, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (imageFiles.Count == 0)
        {
            return new(TypedResults.NotFound());
        }

        var result = TypedResults.Stream(
            async stream => await BrowsingHelpers.WriteZipAsync(imageFiles, stream, CancellationToken.None),
            "application/zip",
            "images.zip");

        return new(result);
    }
}
