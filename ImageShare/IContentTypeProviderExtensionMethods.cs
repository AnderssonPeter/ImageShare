using Microsoft.AspNetCore.StaticFiles;

namespace ImageShare;

public static class IContentTypeProviderExtensionMethods
{
    public static string GetContentType(this IContentTypeProvider provider, string extension) =>
        provider.TryGetContentType(extension, out var contentType) ? contentType : throw new InvalidOperationException("Unsupported format");
}
