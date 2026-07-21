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
        user.EnsureCanAccessFolder(request.Folder);

        var baseNames = imageEnumerator.EnumerateImages(request.Folder, request.Recursive)
            .Select(file => file.Path.FileNameWithoutExtension)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (baseNames.Count == 0)
        {
            throw new NotFoundException("No images were found in the requested folder.");
        }

        var randomBaseName = baseNames[Random.Shared.Next(baseNames.Count)];
        var targetName = request.Thumbnail ? imageEnumerator.BuildThumbnailName(randomBaseName) : randomBaseName;

        var candidates = imageEnumerator.EnumerateImages(request.Folder, request.Recursive, thumbnails: request.Thumbnail)
            .Where(file => string.Equals(file.Path.FileNameWithoutExtension, targetName, StringComparison.OrdinalIgnoreCase))
            .Select(file => file.Info)
            .OrderBy(file => file.Length)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new NotFoundException($"Image '{randomBaseName}' was not found.");
        }

        return new(new ImageCandidates(candidates, contentTypeProvider).ServeBest(request.Accept));
    }
}
