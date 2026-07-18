using ImageMagick;
using ImageShare.Browsing;
using Mediator;
using Microsoft.Extensions.Primitives;
using Mirality.FileProviders;

namespace ImageShare.Tests;

[MicrosoftDI]
public class ImageEndpointsTests(ISyncWritableFileProvider fileProvider, IMediator mediator, TestUser user, TestImageFactory imageFactory)
{
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
        var result = BrowsingHelpers.IsFormatAccepted(header, format);

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ServeImage_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        fileProvider.AddFile("photo.png", imageFactory.CreateTestImage(MagickFormat.Png));
        user.IsAuthenticated = false;

        // Act
        var result = await mediator.Send(new ServeImageQuery("photo.png", "", false));

        // Assert
        await Assert.That(result.IsStatusCode(401)).IsTrue();
    }

    [Test]
    public async Task ServeImage_BlockedFolder_ReturnsForbidden()
    {
        // Arrange
        fileProvider.AddFile("secret/photo.png", imageFactory.CreateTestImage(MagickFormat.Png));

        // Act
        var result = await mediator.Send(new ServeImageQuery("secret/photo.png", "", false));

        // Assert
        await Assert.That(result.IsStatusCode(403)).IsTrue();
    }

    [Test]
    public async Task ServeImage_BlockedSubfolder_ReturnsForbidden()
    {
        // Arrange
        fileProvider.AddFile("secret/nested/photo.png", imageFactory.CreateTestImage(MagickFormat.Png));

        // Act
        var result = await mediator.Send(new ServeImageQuery("secret/nested/photo.png", "", false));

        // Assert
        await Assert.That(result.IsStatusCode(403)).IsTrue();
    }

    [Test]
    public async Task ServeImage_NonExistentFile_ReturnsNotFound()
    {
        // Arrange
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery("missing.avif", "", false));

        // Assert
        await Assert.That(result.IsStatusCode(404)).IsTrue();
    }

    [Test]
    [Arguments("../etc")]
    [Arguments("/etc")]
    [Arguments("/")]
    [Arguments("/etc/passwd")]
    public async Task ServeImage_UnsafePath_ThrowsArgumentException(string path) =>
        await Assert.That(async () => await mediator.Send(new ServeImageQuery(path, "", false))).Throws<ArgumentException>();

    [Test]
    public async Task ServeImage_ThumbprintFile_ReturnsBadRequest()
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("photo.thumb.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery("photo.thumb.jpg", "", false));

        // Assert
        await Assert.That(result.IsStatusCode(400)).IsTrue();
    }

    [Test]
    public async Task ServeImage_NoAcceptHeader_ServesOriginal()
    {
        // Arrange
        fileProvider.AddFile("photo.png", imageFactory.CreateTestImage(MagickFormat.Png));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery("photo.png", "", false));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetContentType()).IsEqualTo("image/png");
    }

    [Test]
    public async Task ServeImage_FormatAccepted_ServesOriginal()
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery("photo.avif", "image/avif,image/jpeg", false));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetContentType()).IsEqualTo("image/avif");
    }

    [Test]
    public async Task ServeImage_ThumbTrue_ServesThumbprint()
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("photo.thumb.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery("photo", "", true));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetContentType()).IsEqualTo("image/jpeg");
    }

    [Test]
    public async Task ServeImage_ThumbTrue_NoThumbprint_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery("photo", "", true));

        // Assert
        await Assert.That(result.IsStatusCode(404)).IsTrue();
    }

    [Test]
    public async Task ServeImage_ThumbTrue_NoImage_ReturnsNotFound()
    {
        // Arrange
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery("missing", "", true));

        // Assert
        await Assert.That(result.IsStatusCode(404)).IsTrue();
    }

    [Test]
    public async Task ServeImage_ThumbTrue_ThumbprintNotAccepted_Returns406()
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("photo.thumb.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery("photo", "image/webp,image/png", true));

        // Assert
        await Assert.That(result.IsStatusCode(406)).IsTrue();
    }

    [Test]
    public async Task ServeImage_NoAcceptedFormats_Returns406()
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery("photo", "image/tiff", false));

        // Assert
        await Assert.That(result.IsStatusCode(406)).IsTrue();
    }

    [Test]
    public async Task ServeImage_ServesSmallestFileFirst()
    {
        // Arrange
        fileProvider.AddFile("photo.png", imageFactory.CreateTestImage(10, 10, MagickFormat.Png));
        fileProvider.AddFile("photo.jpg", imageFactory.CreateTestImage(100, 100, MagickFormat.Jpeg));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery("photo", "image/jpeg,image/png", false));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetContentType()).IsEqualTo("image/png");
    }

    [Test]
    public async Task ServeImage_ThumbTrue_ServesSmallestThumbprintFirst()
    {
        // Arrange
        fileProvider.Write("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("photo.thumb.jpg", imageFactory.CreateTestImage(10, 10, MagickFormat.Jpeg));
        fileProvider.AddFile("photo.thumb.png", imageFactory.CreateTestImage(100, 100, MagickFormat.Png));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery("photo", "image/jpeg,image/png", true));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetContentType()).IsEqualTo("image/jpeg");
    }
}
