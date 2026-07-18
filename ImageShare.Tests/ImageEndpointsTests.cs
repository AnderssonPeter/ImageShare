using ImageMagick;
using ImageShare.Browsing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Mirality.FileProviders;

namespace ImageShare.Tests;

[MicrosoftDI]
public class ImageEndpointsTests(ISyncWritableFileProvider fileProvider, IContentTypeProvider contentTypeProvider, IOptions<ImageFormatOptions> imageFormats, TestUser user)
{
    private static byte[] CreateTestImage(MagickFormat format)
    {
        using var image = new MagickImage(MagickColors.DodgerBlue, 100, 100);
        image.Format = format;
        return image.ToByteArray();
    }

    private void AddFile(string path, MagickFormat format) =>
        fileProvider.Write(path, CreateTestImage(format));

    private void AddThumbnailFile(string originalName) =>
        fileProvider.Write($"{Path.GetFileNameWithoutExtension(originalName)}.thumb.jpg", CreateTestImage(MagickFormat.Jpeg));

    [Test]
    [Arguments("", "image/jpeg", true)]
    [Arguments("image/avif", "image/avif", true)]
    [Arguments("image/*", "image/avif", true)]
    [Arguments("*/*", "image/avif", true)]
    [Arguments("image/png,image/webp", "image/avif", false)]
    [Arguments("image/png,image/avif", "image/avif", true)]
    [Arguments("image/webp;q=0.8,image/avif;q=1.0", "image/avif", true)]
    public async Task IsFormatAccepted_MatchesExpectedBehavior(StringValues header, string format, bool expected)
    {
        // Act
        var result = ImageEndpoints.IsFormatAccepted(header, format);

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ServeImage_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        AddFile("photo.png", MagickFormat.Png);
        user.IsAuthenticated = false;

        // Act
        var result = ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "photo.png", StringValues.Empty, thumbnail: false);

        // Assert
        await Assert.That(IsStatusCode(result, 401)).IsTrue();
    }

    [Test]
    public async Task ServeImage_BlockedFolder_ReturnsForbidden()
    {
        // Arrange
        AddFile("secret/photo.png", MagickFormat.Png);

        // Act
        var result = ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "secret/photo.png", StringValues.Empty, thumbnail: false);

        // Assert
        await Assert.That(IsStatusCode(result, 403)).IsTrue();
    }

    [Test]
    public async Task ServeImage_BlockedSubfolder_ReturnsForbidden()
    {
        // Arrange
        AddFile("secret/nested/photo.png", MagickFormat.Png);

        // Act
        var result = ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "secret/nested/photo.png", StringValues.Empty, thumbnail: false);

        // Assert
        await Assert.That(IsStatusCode(result, 403)).IsTrue();
    }

    [Test]
    public async Task ServeImage_NonExistentFile_ReturnsNotFound()
    {
        // Arrange
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "missing.avif", StringValues.Empty, thumbnail: false);

        // Assert
        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task ServeImage_PathTraversal_ThrowsArgumentException() =>
        await Assert.That(() => ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "../etc", StringValues.Empty, thumbnail: false)).Throws<ArgumentException>();

    [Test]
    public async Task ServeImage_ThumbprintFile_ReturnsBadRequest()
    {
        // Arrange
        AddFile("photo.avif", MagickFormat.Avif);
        AddThumbnailFile("photo.avif");
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "photo.thumb.jpg", StringValues.Empty, thumbnail: false);

        // Assert
        await Assert.That(IsStatusCode(result, 400)).IsTrue();
    }

    [Test]
    public async Task ServeImage_NoAcceptHeader_ServesOriginal()
    {
        // Arrange
        AddFile("photo.png", MagickFormat.Png);
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "photo.png", StringValues.Empty, thumbnail: false);

        // Assert
        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        await Assert.That(GetContentType(result)).IsEqualTo("image/png");
    }

    [Test]
    public async Task ServeImage_FormatAccepted_ServesOriginal()
    {
        // Arrange
        AddFile("photo.avif", MagickFormat.Avif);
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "photo.avif", "image/avif,image/jpeg", thumbnail: false);

        // Assert
        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        await Assert.That(GetContentType(result)).IsEqualTo("image/avif");
    }

    [Test]
    public async Task ServeImage_ThumbTrue_ServesThumbprint()
    {
        // Arrange
        AddFile("photo.avif", MagickFormat.Avif);
        AddThumbnailFile("photo.avif");
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "photo", StringValues.Empty, thumbnail: true);

        // Assert
        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        await Assert.That(GetContentType(result)).IsEqualTo("image/jpeg");
    }

    [Test]
    public async Task ServeImage_ThumbTrue_NoThumbprint_ReturnsNotFound()
    {
        // Arrange
        AddFile("photo.avif", MagickFormat.Avif);
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "photo", StringValues.Empty, thumbnail: true);

        // Assert
        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task ServeImage_ThumbTrue_NoImage_ReturnsNotFound()
    {
        // Arrange
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "missing", StringValues.Empty, thumbnail: true);

        // Assert
        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task ServeImage_ThumbTrue_ThumbprintNotAccepted_Returns406()
    {
        // Arrange
        AddFile("photo.avif", MagickFormat.Avif);
        AddThumbnailFile("photo.avif");
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "photo", "image/webp,image/png", thumbnail: true);

        // Assert
        await Assert.That(IsStatusCode(result, 406)).IsTrue();
    }

    [Test]
    public async Task ServeImage_NoAcceptedFormats_Returns406()
    {
        // Arrange
        AddFile("photo.avif", MagickFormat.Avif);
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "photo", "image/tiff", thumbnail: false);

        // Assert
        await Assert.That(IsStatusCode(result, 406)).IsTrue();
    }

    [Test]
    public async Task ServeImage_ServesSmallestFileFirst()
    {
        // Arrange
        using var small = new MagickImage(MagickColors.DodgerBlue, 10, 10);
        small.Format = MagickFormat.Png;
        fileProvider.Write("photo.png", small.ToByteArray());
        using var large = new MagickImage(MagickColors.DodgerBlue, 100, 100);
        large.Format = MagickFormat.Jpeg;
        fileProvider.Write("photo.jpg", large.ToByteArray());
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "photo", "image/jpeg,image/png", thumbnail: false);

        // Assert
        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        await Assert.That(GetContentType(result)).IsEqualTo("image/png");
    }

    [Test]
    public async Task ServeImage_ThumbTrue_ServesSmallestThumbprintFirst()
    {
        // Arrange
        fileProvider.Write("photo.avif", CreateTestImage(MagickFormat.Avif));
        using var smallThumb = new MagickImage(MagickColors.DodgerBlue, 10, 10);
        smallThumb.Format = MagickFormat.Jpeg;
        fileProvider.Write("photo.thumb.jpg", smallThumb.ToByteArray());
        using var largeThumb = new MagickImage(MagickColors.DodgerBlue, 100, 100);
        largeThumb.Format = MagickFormat.Png;
        fileProvider.Write("photo.thumb.png", largeThumb.ToByteArray());
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, "photo", "image/jpeg,image/png", thumbnail: true);

        // Assert
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
