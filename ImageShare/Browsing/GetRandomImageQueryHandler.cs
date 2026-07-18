using ImageShare.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;

namespace ImageShare.Browsing;

internal sealed class GetRandomImageQueryHandler(
    IFileProvider fileProvider,
    IOptions<ImageFormatOptions> imageFormats,
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

        var folders = BrowsingHelpers.NormalizeFolders(request.Folders);
        if (folders.Count == 0)
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

        var baseNames = CollectBaseNames(folders, request.Recursive);
        if (baseNames.Count == 0)
        {
            return new(TypedResults.NotFound());
        }

        var randomBaseName = baseNames[Random.Shared.Next(baseNames.Count)];

        var candidates = FindCandidates(folders, randomBaseName, request.Recursive, request.Thumbnail);
        if (candidates.Count == 0)
        {
            return new(TypedResults.NotFound());
        }

        candidates.Sort((left, right) => left.Length.CompareTo(right.Length));

        var bestResult = BrowsingHelpers.ServeBestMatch(candidates, contentTypeProvider, request.Accept);

        return new(bestResult.Result switch
        {
            FileStreamHttpResult file => (Results<FileStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound, StatusCodeHttpResult>)file,
            StatusCodeHttpResult status => status,
            _ => throw new UnreachableException("Failed to find a matching result type")
        });
    }

    private List<string> CollectBaseNames(List<string> folders, bool recursive)
    {
        var baseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in folders)
        {
            if (recursive)
            {
                foreach (var (_, info) in BrowsingHelpers.EnumerateImageFiles(fileProvider, imageFormats.Value, folder))
                {
                    baseNames.Add(Path.GetFileNameWithoutExtension(info.Name));
                }
            }
            else
            {
                foreach (var name in BrowsingHelpers.GetImageBaseNames(fileProvider, folder, imageFormats.Value))
                {
                    baseNames.Add(name);
                }
            }
        }

        return [.. baseNames];
    }

    private List<IFileInfo> FindCandidates(List<string> folders, string baseName, bool recursive, bool thumbnail)
    {
        var candidates = new List<IFileInfo>();
        foreach (var folder in folders)
        {
            if (recursive)
            {
                candidates.AddRange(BrowsingHelpers.FindMatchingFilesRecursive(fileProvider, imageFormats.Value, folder, baseName, thumbnail));
            }
            else
            {
                candidates.AddRange(BrowsingHelpers.FindMatchingFiles(fileProvider, imageFormats.Value, folder, baseName, thumbnail));
            }
        }

        return candidates;
    }
}
