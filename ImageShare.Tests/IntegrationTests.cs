using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using ImageMagick;
using ImageShare.Browsing;

namespace ImageShare.Tests;

public class IntegrationTests
{
    private static readonly TestImageFactory ImageFactory = new();

    private sealed class TestApp : IDisposable
    {
        public ImageShareWebApplicationFactory Factory { get; } = new();
        public HttpClient Client { get; }

        public TestApp() => Client = Factory.CreateClientWithApiKey();

        public void Dispose()
        {
            Client.Dispose();
            Factory.Dispose();
        }
    }

    [Test]
    public async Task FoldersRoot_WithoutPath_ReturnsEntries()
    {
        // Arrange
        using var app = new TestApp();
        app.Factory.FileProvider.AddDirectory("vacation");
        app.Factory.FileProvider.AddFile("vacation/photo.png", ImageFactory.CreateTestImage(MagickFormat.Png));

        // Act
        var response = await app.Client.GetAsync("/folders");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<FolderEntry>>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Items.Count).IsEqualTo(1);
        await Assert.That(result.Items[0].Name).IsEqualTo("vacation");
        await Assert.That(result.Items[0].Type).IsEqualTo(EntryType.Folder);
    }

    [Test]
    public async Task FoldersRoot_WithEmptyRoot_ReturnsEntries()
    {
        // Arrange
        using var app = new TestApp();
        app.Factory.FileProvider.AddDirectory("album");
        app.Factory.FileProvider.AddFile("album/photo.jpg", ImageFactory.CreateTestImage(MagickFormat.Jpeg));

        // Act
        var response = await app.Client.GetAsync("/folders/");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<FolderEntry>>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Items.Count).IsEqualTo(1);
        await Assert.That(result.Items[0].Name).IsEqualTo("album");
    }

    [Test]
    public async Task FoldersNested_WithPath_ReturnsEntries()
    {
        // Arrange
        using var app = new TestApp();
        app.Factory.FileProvider.AddFile("vacation/photo.png", ImageFactory.CreateTestImage(MagickFormat.Png));
        app.Factory.FileProvider.AddFile("vacation/picture.jpg", ImageFactory.CreateTestImage(MagickFormat.Jpeg));

        // Act
        var response = await app.Client.GetAsync("/folders/vacation");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<FolderEntry>>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TotalCount).IsEqualTo(2);
    }

    [Test]
    public async Task ImagesDownload_ReturnsZipStream()
    {
        // Arrange
        using var app = new TestApp();
        var photoData = ImageFactory.CreateTestImage(MagickFormat.Avif);
        app.Factory.FileProvider.AddFile("vacation/photo.avif", photoData);

        // Act
        var response = await app.Client.GetAsync("/images/download?folders=vacation");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/zip");

        var zipBytes = await response.Content.ReadAsByteArrayAsync();
        using var memoryStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
        await Assert.That(archive.Entries.Count).IsEqualTo(1);
        await Assert.That(archive.Entries[0].FullName).IsEqualTo("vacation/photo.avif");
    }

    [Test]
    public async Task ImagesDownload_MultipleFolders_ReturnsZipWithAllImages()
    {
        // Arrange
        using var app = new TestApp();
        app.Factory.FileProvider.AddFile("album-a/photo.avif", ImageFactory.CreateTestImage(MagickFormat.Avif));
        app.Factory.FileProvider.AddFile("album-b/picture.jpg", ImageFactory.CreateTestImage(MagickFormat.Jpeg));

        // Act
        var response = await app.Client.GetAsync("/images/download?folders=album-a&folders=album-b");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var zipBytes = await response.Content.ReadAsByteArrayAsync();
        using var memoryStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
        await Assert.That(archive.Entries.Count).IsEqualTo(2);
        var entryNames = archive.Entries.Select(entry => entry.FullName).ToList();
        await Assert.That(entryNames).Contains("album-a/photo.avif");
        await Assert.That(entryNames).Contains("album-b/picture.jpg");
    }

    [Test]
    public async Task ImagesServe_RootPath_ReturnsImage()
    {
        // Arrange
        using var app = new TestApp();
        var photoData = ImageFactory.CreateTestImage(MagickFormat.Png);
        app.Factory.FileProvider.AddFile("photo.png", photoData);

        // Act
        var response = await app.Client.GetAsync("/images/photo.png");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("image/png");
        var servedBytes = await response.Content.ReadAsByteArrayAsync();
        await Assert.That(servedBytes).IsEquivalentTo(photoData);
    }

    [Test]
    public async Task ImagesServe_NestedPath_ReturnsImage()
    {
        // Arrange
        using var app = new TestApp();
        var photoData = ImageFactory.CreateTestImage(MagickFormat.Avif);
        app.Factory.FileProvider.AddFile("vacation/photo.avif", photoData);

        // Act
        var response = await app.Client.GetAsync("/images/vacation/photo.avif");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("image/avif");
        var servedBytes = await response.Content.ReadAsByteArrayAsync();
        await Assert.That(servedBytes).IsEquivalentTo(photoData);
    }

    [Test]
    public async Task ImagesServe_NestedPathWithUrlEncoding_ReturnsImage()
    {
        // Arrange
        using var app = new TestApp();
        var photoData = ImageFactory.CreateTestImage(MagickFormat.Avif);
        app.Factory.FileProvider.AddFile("vacation/photo.avif", photoData);

        // Act — simulate Scalar URL-encoding the path separator
        var response = await app.Client.GetAsync("/images/vacation%2Fphoto.avif");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("image/avif");
        var servedBytes = await response.Content.ReadAsByteArrayAsync();
        await Assert.That(servedBytes).IsEquivalentTo(photoData);
    }

    [Test]
    public async Task ImagesServe_DeeplyNestedPath_ReturnsImage()
    {
        // Arrange
        using var app = new TestApp();
        var photoData = ImageFactory.CreateTestImage(MagickFormat.Jpeg);
        app.Factory.FileProvider.AddFile("album/2024/trip/photo.jpg", photoData);

        // Act
        var response = await app.Client.GetAsync("/images/album/2024/trip/photo.jpg");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("image/jpeg");
        var servedBytes = await response.Content.ReadAsByteArrayAsync();
        await Assert.That(servedBytes).IsEquivalentTo(photoData);
    }

    [Test]
    public async Task HealthCheck_ReturnsPong()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        var response = await app.Client.GetAsync("/");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).IsEqualTo("\"pong\"");
    }
}
