using ImageMagick;
using ImageShare.Authentication;
using ImageShare.Browsing;
using ImageShare.Thumbnail;
using Microsoft.AspNetCore.Http;
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

    private sealed class TestThumbnailService : IThumbnailService
    {
        public ReadOnlyMemory<byte> GenerateThumbnail(ReadOnlySpan<byte> imageData, ThumbnailOptions? options = null)
        {
            using var image = new MagickImage(imageData);
            image.Format = MagickFormat.Jpeg;
            image.Quality = 80;
            image.Resize(50, 50);
            return image.ToByteArray();
        }
    }

    private static byte[] CreateTestImage(MagickFormat format)
    {
        using var image = new MagickImage(MagickColors.DodgerBlue, 100, 100);
        image.Format = format;
        return image.ToByteArray();
    }

    private static void AddFile(InMemoryFileProvider fs, string path, MagickFormat format) =>
        fs.Write(path, CreateTestImage(format));

    private static void AddThumbFile(InMemoryFileProvider fs, string originalName) =>
        fs.Write($"{Path.GetFileNameWithoutExtension(originalName)}.thumb.jpg", CreateTestImage(MagickFormat.Jpeg));

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
    public async Task GetMimeType_KnownExtension_ReturnsMimeType()
    {
        var result = ImageEndpoints.GetMimeType(".avif");

        await Assert.That(result).IsEqualTo("image/avif");
    }

    [Test]
    public async Task GetMimeType_JpgExtension_ReturnsJpeg()
    {
        var result = ImageEndpoints.GetMimeType(".jpg");

        await Assert.That(result).IsEqualTo("image/jpeg");
    }

    [Test]
    public async Task GetMimeType_UnknownExtension_ReturnsNull()
    {
        var result = ImageEndpoints.GetMimeType(".xyz");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ServeImageAsync_Unauthenticated_ReturnsUnauthorized()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "photo.png", MagickFormat.Png);
        var thumbnailService = new TestThumbnailService();
        var user = new TestUser { IsAuthenticated = false };
        var result = await ImageEndpoints.ServeImageAsync(fs, thumbnailService, user, "photo.png", StringValues.Empty, CancellationToken.None);

        await Assert.That(IsStatusCode(result, 401)).IsTrue();
    }

    [Test]
    public async Task ServeImageAsync_BlockedFolder_ReturnsNotFound()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "secret/photo.png", MagickFormat.Png);
        var thumbnailService = new TestThumbnailService();
        var user = new TestUser();
        var result = await ImageEndpoints.ServeImageAsync(fs, thumbnailService, user, "secret/photo.png", StringValues.Empty, CancellationToken.None);

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task ServeImageAsync_BlockedSubfolder_ReturnsNotFound()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "secret/nested/photo.png", MagickFormat.Png);
        var thumbnailService = new TestThumbnailService();
        var user = new TestUser();
        var result = await ImageEndpoints.ServeImageAsync(fs, thumbnailService, user, "secret/nested/photo.png", StringValues.Empty, CancellationToken.None);

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task ServeImageAsync_NonExistentFile_ReturnsNotFound()
    {
        var fs = new InMemoryFileProvider();
        var thumbnailService = new TestThumbnailService();
        var user = new TestUser().Allow("vacation");
        var result = await ImageEndpoints.ServeImageAsync(fs, thumbnailService, user, "missing.avif", StringValues.Empty, CancellationToken.None);

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task ServeImageAsync_PathTraversal_ReturnsNotFound()
    {
        var fs = new InMemoryFileProvider();
        var thumbnailService = new TestThumbnailService();
        var user = new TestUser().Allow("vacation");
        var result = await ImageEndpoints.ServeImageAsync(fs, thumbnailService, user, "../etc", StringValues.Empty, CancellationToken.None);

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task ServeImageAsync_ThumbprintFile_ReturnsNotFound()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "photo.avif", MagickFormat.Avif);
        AddThumbFile(fs, "photo.avif");
        var thumbnailService = new TestThumbnailService();
        var user = new TestUser().Allow("vacation");
        var result = await ImageEndpoints.ServeImageAsync(fs, thumbnailService, user, "photo.thumb.jpg", StringValues.Empty, CancellationToken.None);

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task ServeImageAsync_NoAcceptHeader_ServesOriginal()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "photo.png", MagickFormat.Png);
        var thumbnailService = new TestThumbnailService();
        var user = new TestUser().Allow("vacation");
        var result = await ImageEndpoints.ServeImageAsync(fs, thumbnailService, user, "photo.png", StringValues.Empty, CancellationToken.None);

        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        await Assert.That(GetContentType(result)).IsEqualTo("image/png");
    }

    [Test]
    public async Task ServeImageAsync_FormatAccepted_ServesOriginal()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "photo.avif", MagickFormat.Avif);
        var thumbnailService = new TestThumbnailService();
        var user = new TestUser().Allow("vacation");
        var result = await ImageEndpoints.ServeImageAsync(fs, thumbnailService, user, "photo.avif", "image/avif,image/jpeg", CancellationToken.None);

        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        await Assert.That(GetContentType(result)).IsEqualTo("image/avif");
    }

    [Test]
    public async Task ServeImageAsync_FormatNotAccepted_ServesThumbnail()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "photo.avif", MagickFormat.Avif);
        AddThumbFile(fs, "photo.avif");
        var thumbnailService = new TestThumbnailService();
        var user = new TestUser().Allow("vacation");
        var result = await ImageEndpoints.ServeImageAsync(fs, thumbnailService, user, "photo.avif", "image/jpeg", CancellationToken.None);

        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        await Assert.That(GetContentType(result)).IsEqualTo("image/jpeg");
    }

    [Test]
    public async Task ServeImageAsync_NoThumbnail_ConvertsOnTheFly()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "photo.avif", MagickFormat.Avif);
        var thumbnailService = new TestThumbnailService();
        var user = new TestUser().Allow("vacation");
        var result = await ImageEndpoints.ServeImageAsync(fs, thumbnailService, user, "photo.avif", "image/jpeg", CancellationToken.None);

        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        await Assert.That(GetContentType(result)).IsEqualTo("image/jpeg");
    }

    [Test]
    public async Task ServeImageAsync_AvifToPng_ConvertsOnTheFly()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "photo.avif", MagickFormat.Avif);
        var thumbnailService = new TestThumbnailService();
        var user = new TestUser().Allow("vacation");
        var result = await ImageEndpoints.ServeImageAsync(fs, thumbnailService, user, "photo.avif", "image/png,image/gif", CancellationToken.None);

        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        await Assert.That(GetContentType(result)).IsEqualTo("image/png");
    }

    [Test]
    public async Task ServeImageAsync_NoAcceptedFormats_ReturnsOriginalAsFallback()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "photo.tiff", MagickFormat.Tiff);
        var thumbnailService = new TestThumbnailService();
        var user = new TestUser().Allow("vacation");
        var result = await ImageEndpoints.ServeImageAsync(fs, thumbnailService, user, "photo.tiff", "", CancellationToken.None);

        await Assert.That(IsStatusCode(result, 200)).IsTrue();
    }

    private static bool IsStatusCode(IResult result, int statusCode)
    {
        return result switch
        {
            Microsoft.AspNetCore.Http.HttpResults.NotFound => statusCode == 404,
            Microsoft.AspNetCore.Http.HttpResults.BadRequest => statusCode == 400,
            Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult => statusCode == 401,
            _ => statusCode == 200,
        };
    }

    private static string? GetContentType(IResult result)
    {
        var type = result.GetType();
        var prop = type.GetProperty("ContentType");
        return prop?.GetValue(result) as string;
    }
}
