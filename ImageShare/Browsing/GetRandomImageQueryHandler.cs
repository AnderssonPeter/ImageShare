using ImageShare.Authentication;
using ImageShare.ImageConversion;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;

namespace ImageShare.Browsing;

internal sealed class GetRandomImageQueryHandler(
    ImageEnumerator imageEnumerator,
    IContentTypeProvider contentTypeProvider,
    IUser user)
    : IQueryHandler<GetRandomImageQuery, Results<FileStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound, StatusCodeHttpResult>>
{
    public ValueTask<Results<FileStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound, StatusCodeHttpResult>> Handle(
        GetRandomImageQuery request,
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

        var baseNames = folders
            .SelectMany(folder => imageEnumerator.EnumerateImages(folder, request.Recursive))
            .Select(file => file.Path.FileNameWithoutExtension)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (baseNames.Count == 0)
        {
            return new(TypedResults.NotFound());
        }

        var randomBaseName = baseNames[Random.Shared.Next(baseNames.Count)];
        var targetName = request.Thumbnail ? imageEnumerator.BuildThumbnailName(randomBaseName) : randomBaseName;

        var candidates = folders
            .SelectMany(folder => imageEnumerator.EnumerateImages(folder, request.Recursive, thumbnails: request.Thumbnail))
            .Where(file => string.Equals(file.Path.FileNameWithoutExtension, targetName, StringComparison.OrdinalIgnoreCase))
            .Select(file => file.Info)
            .OrderBy(file => file.Length)
            .ToList();

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

    private static List<RelativePath> NormalizeFolders(StringValues folderValues) =>
        [.. folderValues.Where(value => !string.IsNullOrEmpty(value)).Cast<string>().Select(value => new RelativePath(value))];
}
