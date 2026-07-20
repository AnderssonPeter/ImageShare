using ImageShare.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
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

        var folders = BrowsingHelpers.NormalizeFolders(request.Folders);
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
            var folderPath = new RelativePath(folder);
            if (!user.CanAccessFolder(folderPath.FirstSegment))
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
            async stream => await BrowsingHelpers.WriteZipAsync(imageFiles, stream, CancellationToken.None),
            "application/zip",
            "images.zip");

        return new(result);
    }
}
