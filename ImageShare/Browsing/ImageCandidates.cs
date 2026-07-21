using ImageShare.Errors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace ImageShare.Browsing;

public sealed class ImageCandidates
{
    private readonly IReadOnlyList<IFileInfo> _files;
    private readonly IContentTypeProvider _contentTypeProvider;

    public ImageCandidates(IReadOnlyList<IFileInfo> files, IContentTypeProvider contentTypeProvider)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(contentTypeProvider);
        _files = files;
        _contentTypeProvider = contentTypeProvider;
    }

    public FileStreamHttpResult ServeBest(StringValues acceptHeader)
    {
        foreach (var file in _files)
        {
            var filePath = new RelativePath(file.Name);
            var mimeType = _contentTypeProvider.GetContentType($".{filePath.Extension}");

            if (acceptHeader.Accepts(mimeType))
            {
                return TypedResults.Stream(file.CreateReadStream(), mimeType);
            }
        }

        throw new NotAcceptableException("None of the available image formats match the requested Accept header.");
    }
}
