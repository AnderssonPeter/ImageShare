using ImageShare.Authentication;
using ImageShare.Errors;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;

namespace ImageShare.Browsing;

internal sealed class ServeImageQueryHandler(
    ImageEnumerator imageEnumerator,
    IContentTypeProvider contentTypeProvider,
    IUser user)
    : IQueryHandler<ServeImageQuery, FileStreamHttpResult>
{
    public ValueTask<FileStreamHttpResult> Handle(
        ServeImageQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Path.IsInFolder)
        {
            user.EnsureCanAccessFolder(request.Path);
        }

        if (request.Path.IsThumbnail)
        {
            throw new BadRequestException("Thumbnail files cannot be served directly; request a thumbnail via the thumbnail flag instead.");
        }

        var candidates = imageEnumerator.FindMatchingFiles(request.Path.Directory, request.Path.FileNameWithoutExtension, request.Thumbnail);

        if (candidates.Count == 0)
        {
            throw new NotFoundException($"Image '{request.Path.FileNameWithoutExtension}' was not found.");
        }

        return new(BrowsingHelpers.ServeBestMatch(candidates, contentTypeProvider, request.Accept));
    }
}
