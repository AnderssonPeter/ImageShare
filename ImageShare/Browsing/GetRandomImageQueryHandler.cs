using ImageShare.Authentication;
using ImageShare.Errors;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;

namespace ImageShare.Browsing;

internal sealed class GetRandomImageQueryHandler(
    ImageEnumerator imageEnumerator,
    IContentTypeProvider contentTypeProvider,
    IUser user)
    : IQueryHandler<GetRandomImageQuery, FileStreamHttpResult>
{
    public ValueTask<FileStreamHttpResult> Handle(
        GetRandomImageQuery request,
        CancellationToken cancellationToken)
    {
        var folders = NormalizeFolders(request.Folders);
        if (folders.Count == 0)
        {
            throw new BadRequestException("At least one folder must be specified.");
        }

        foreach (var folder in folders)
        {
            user.EnsureCanAccessFolder(folder);
        }

        var baseNames = folders
            .SelectMany(folder => imageEnumerator.EnumerateImages(folder, request.Recursive))
            .Select(file => file.Path.FileNameWithoutExtension)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (baseNames.Count == 0)
        {
            throw new NotFoundException("No images were found in the requested folders.");
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
            throw new NotFoundException($"Image '{randomBaseName}' was not found.");
        }

        return new(BrowsingHelpers.ServeBestMatch(candidates, contentTypeProvider, request.Accept));
    }

    private static List<RelativePath> NormalizeFolders(string[] folderValues) =>
        [.. folderValues.Where(value => !string.IsNullOrEmpty(value)).Select(value => new RelativePath(value))];
}
