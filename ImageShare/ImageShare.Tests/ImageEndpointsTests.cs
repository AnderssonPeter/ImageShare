using ImageMagick;
using ImageShare.Authentication;
using ImageShare.Browsing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Primitives;
using Mirality.FileProviders.InMemory;

namespace ImageShare.Tests;

public class ImageEndpointsTests
{
    private sealed class TestUser : IUser
    {
        public bool IsAuthenticated { get; init; } = true;
        public string Name { get; init; } = "test";
        private readonly HashSet<string> _allowedFolders = [];

        public TestUser Allow(string folder)
        {
            _allowedFolders.Add(folder);
            return this;
        }

        public bool CanAccessFolder(string folder) => _allowedFolders.Contains(folder);
    }

    private static readonly ImageFormatOptions DefaultImageFormats = new()
    {
        SupportedFormats = ["avif", "webp", "jpg", "png"]
    };

    private static readonly IContentTypeProvider ContentTypeProvider;

    static ImageEndpointsTests()
    {
        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".avif"] = "image/avif";
        ContentTypeProvider = provider;
    }

    private static byte[] CreateTestImage(MagickFormat format)
    {
        using var image = new MagickImage(MagickColors.DodgerBlue, 100, 100);
        image.Format = format;
        return image.ToByteArray();
    }

    private static void AddFile(InMemoryFileProvider fileProvider, string path, MagickFormat format) =>
        fileProvider.Write(path, CreateTestImage(format));

    private static void AddThumbFile(InMemoryFileProvider fileProvider, string originalName) =>
        fileProvider.Write($"{Path.GetFileNameWithoutExtension(originalName)}.thumb.jpg", CreateTestImage(MagickFormat.Jpeg));

    [Test]
    public async Task IsFormatAccepted_EmptyHeader_ReturnsTrue()
    {
        var result = ImageEndpoints.IsFormatAccepted(StringValues.Empty, "image/jpeg");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsFormatAccepted_ExactMatch_ReturnsTrue()
    {
        var result = ImageEndpoints.IsFormatAccepted("image/avif", "image/avif");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsFormatAccepted_WildcardSubtype_ReturnsTrue()
    {
        var result = ImageEndpoints.IsFormatAccepted("image/*", "image/avif");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsFormatAccepted_WildcardAny_ReturnsTrue()
    {
        var result = ImageEndpoints.IsFormatAccepted("*/*", "image/avif");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsFormatAccepted_NoMatch_ReturnsFalse()
    {
        var result = ImageEndpoints.IsFormatAccepted("image/png,image/webp", "image/avif");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsFormatAccepted_MultipleValues_MatchesAny()
    {
        var result = ImageEndpoints.IsFormatAccepted("image/png,image/avif", "image/avif");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsFormatAccepted_IgnoresQualityParameter()
    {
        var result = ImageEndpoints.IsFormatAccepted("image/webp;q=0.8,image/avif;q=1.0", "image/avif");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ServeImage_Unauthenticated_ReturnsUnauthorized()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "photo.png", MagickFormat.Png);
        var user = new TestUser { IsAuthenticated = false };
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "photo.png", StringValues.Empty, thumbnail: false);

        await Assert.That(IsStatusCode(result, 401)).IsTrue();
    }

    [Test]
    public async Task ServeImage_BlockedFolder_ReturnsForbidden()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "secret/photo.png", MagickFormat.Png);
        var user = new TestUser();
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "secret/photo.png", StringValues.Empty, thumbnail: false);

        await Assert.That(IsStatusCode(result, 403)).IsTrue();
    }

    [Test]
    public async Task ServeImage_BlockedSubfolder_ReturnsForbidden()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "secret/nested/photo.png", MagickFormat.Png);
        var user = new TestUser();
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "secret/nested/photo.png", StringValues.Empty, thumbnail: false);

        await Assert.That(IsStatusCode(result, 403)).IsTrue();
    }

    [Test]
    public async Task ServeImage_NonExistentFile_ReturnsNotFound()
    {
        var fileProvider = new InMemoryFileProvider();
        var user = new TestUser().Allow("vacation");
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "missing.avif", StringValues.Empty, thumbnail: false);

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task ServeImage_PathTraversal_ReturnsBadRequest()
    {
        var fileProvider = new InMemoryFileProvider();
        var user = new TestUser().Allow("vacation");
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "../etc", StringValues.Empty, thumbnail: false);

        await Assert.That(IsStatusCode(result, 400)).IsTrue();
    }

    [Test]
    public async Task ServeImage_ThumbprintFile_ReturnsBadRequest()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "photo.avif", MagickFormat.Avif);
        AddThumbFile(fileProvider, "photo.avif");
        var user = new TestUser().Allow("vacation");
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "photo.thumb.jpg", StringValues.Empty, thumbnail: false);

        await Assert.That(IsStatusCode(result, 400)).IsTrue();
    }

    [Test]
    public async Task ServeImage_NoAcceptHeader_ServesOriginal()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "photo.png", MagickFormat.Png);
        var user = new TestUser().Allow("vacation");
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "photo.png", StringValues.Empty, thumbnail: false);

        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        await Assert.That(GetContentType(result)).IsEqualTo("image/png");
    }

    [Test]
    public async Task ServeImage_FormatAccepted_ServesOriginal()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "photo.avif", MagickFormat.Avif);
        var user = new TestUser().Allow("vacation");
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "photo.avif", "image/avif,image/jpeg", thumbnail: false);

        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        await Assert.That(GetContentType(result)).IsEqualTo("image/avif");
    }

    [Test]
    public async Task ServeImage_ThumbTrue_ServesThumbprint()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "photo.avif", MagickFormat.Avif);
        AddThumbFile(fileProvider, "photo.avif");
        var user = new TestUser().Allow("vacation");
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "photo", StringValues.Empty, thumbnail: true);

        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        await Assert.That(GetContentType(result)).IsEqualTo("image/jpeg");
    }

    [Test]
    public async Task ServeImage_ThumbTrue_NoThumbprint_ReturnsNotFound()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "photo.avif", MagickFormat.Avif);
        var user = new TestUser().Allow("vacation");
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "photo", StringValues.Empty, thumbnail: true);

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task ServeImage_ThumbTrue_NoImage_ReturnsNotFound()
    {
        var fileProvider = new InMemoryFileProvider();
        var user = new TestUser().Allow("vacation");
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "missing", StringValues.Empty, thumbnail: true);

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task ServeImage_ThumbTrue_ThumbprintNotAccepted_Returns406()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "photo.avif", MagickFormat.Avif);
        AddThumbFile(fileProvider, "photo.avif");
        var user = new TestUser().Allow("vacation");
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "photo", "image/webp,image/png", thumbnail: true);

        await Assert.That(IsStatusCode(result, 406)).IsTrue();
    }

    [Test]
    public async Task ServeImage_NoAcceptedFormats_Returns406()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "photo.avif", MagickFormat.Avif);
        var user = new TestUser().Allow("vacation");
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "photo", "image/tiff", thumbnail: false);

        await Assert.That(IsStatusCode(result, 406)).IsTrue();
    }

    [Test]
    public async Task ServeImage_ServesSmallestFileFirst()
    {
        var fileProvider = new InMemoryFileProvider();
        using var small = new MagickImage(MagickColors.DodgerBlue, 10, 10);
        small.Format = MagickFormat.Png;
        fileProvider.Write("photo.png", small.ToByteArray());
        using var large = new MagickImage(MagickColors.DodgerBlue, 100, 100);
        large.Format = MagickFormat.Jpeg;
        fileProvider.Write("photo.jpg", large.ToByteArray());

        var user = new TestUser().Allow("vacation");
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "photo", "image/jpeg,image/png", thumbnail: false);

        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        await Assert.That(GetContentType(result)).IsEqualTo("image/png");
    }

    [Test]
    public async Task ServeImage_ThumbTrue_ServesSmallestThumbprintFirst()
    {
        var fileProvider = new InMemoryFileProvider();
        fileProvider.Write("photo.avif", CreateTestImage(MagickFormat.Avif));
        using var smallThumb = new MagickImage(MagickColors.DodgerBlue, 10, 10);
        smallThumb.Format = MagickFormat.Jpeg;
        fileProvider.Write("photo.thumb.jpg", smallThumb.ToByteArray());
        using var largeThumb = new MagickImage(MagickColors.DodgerBlue, 100, 100);
        largeThumb.Format = MagickFormat.Png;
        fileProvider.Write("photo.thumb.png", largeThumb.ToByteArray());

        var user = new TestUser().Allow("vacation");
        var result = ImageEndpoints.ServeImage(fileProvider, DefaultImageFormats, ContentTypeProvider, user, "photo", "image/jpeg,image/png", thumbnail: true);

        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        await Assert.That(GetContentType(result)).IsEqualTo("image/jpeg");
    }

    private static bool IsStatusCode(IResult result, int statusCode)
    {
        return Unwrap(result) switch
        {
            NotFound => statusCode == 404,
            BadRequest => statusCode == 400,
            UnauthorizedHttpResult => statusCode == 401,
            ForbidHttpResult => statusCode == 403,
            IStatusCodeHttpResult statusResult => statusResult.StatusCode == statusCode,
            _ => statusCode == 200,
        };
    }

    private static string? GetContentType(IResult result)
    {
        var inner = Unwrap(result);
        var type = inner.GetType();
        var property = type.GetProperty("ContentType");
        return property?.GetValue(inner) as string;
    }

    private static IResult Unwrap(IResult result) =>
        result is INestedHttpResult nested ? (IResult)nested.Result : result;
}
