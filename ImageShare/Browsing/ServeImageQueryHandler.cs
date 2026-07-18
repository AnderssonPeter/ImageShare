using System.Diagnostics;
using ImageShare.Authentication;
using ImageShare.ImageConversion;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace ImageShare.Browsing;

internal sealed class ServeImageQueryHandler(
    IFileProvider fileProvider,
    IOptions<ImageFormatOptions> imageFormats,
    IContentTypeProvider contentTypeProvider,
    IUser user)
    : IQueryHandler<ServeImageQuery, Results<FileStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound, StatusCodeHttpResult>>
{
    public ValueTask<Results<FileStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound, StatusCodeHttpResult>> Handle(
        ServeImageQuery request,
        CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated)
        {
            return new(TypedResults.Unauthorized());
        }

        PathHelper.EnsureSafePath(request.Path);

        if (!PathHelper.IsInFolder(request.Path) || !user.CanAccessFolder(PathHelper.GetFirstSegment(request.Path)))
        {
            return new(TypedResults.Forbid());
        }

        var baseName = Path.GetFileNameWithoutExtension(request.Path);

        if (ImageConverterJob.IsThumbprintFile(baseName))
        {
            return new(TypedResults.BadRequest());
        }

        var directory = Path.GetDirectoryName(request.Path) ?? "";
        var candidates = BrowsingHelpers.FindMatchingFiles(fileProvider, imageFormats.Value, directory, baseName, request.Thumbnail);

        if (candidates.Count == 0)
        {
            return new(TypedResults.NotFound());
        }

        var bestResult = BrowsingHelpers.ServeBestMatch(candidates, contentTypeProvider, request.Accept);

        return new(bestResult.Result switch
        {
            FileStreamHttpResult file => (Results<FileStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound, StatusCodeHttpResult>)file,
            StatusCodeHttpResult status => status,
            _ => throw new UnreachableException("Failed to find a matching result type")
        });
    }
}
