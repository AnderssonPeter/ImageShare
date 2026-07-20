using ImageShare.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ImageShare.Browsing;

internal sealed class ServeImageQueryHandler(
    ImageEnumerator imageEnumerator,
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

        if (request.Path.IsInFolder)
        {
            try
            {
                user.EnsureCanAccessFolder(request.Path);
            }
            catch (FolderAccessDeniedException)
            {
                return new(TypedResults.Forbid());
            }
        }

        if (request.Path.IsThumbnail)
        {
            return new(TypedResults.BadRequest());
        }

        var candidates = imageEnumerator.FindMatchingFiles(request.Path.Directory, request.Path.FileNameWithoutExtension, request.Thumbnail);

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
