using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using ImageMagick;
using ImageShare.Browsing;

namespace ImageShare.Tests;

public class IntegrationTests
{
    private static readonly TestImageFactory imageFactory = new();

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
    public async Task ContentRoot_WithoutPath_ReturnsEntries()
    {
        // Arrange
        using var app = new TestApp();
        app.Factory.FileProvider.AddDirectory("vacation");
        app.Factory.FileProvider.AddFile("vacation/photo.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));

        // Act
        var response = await app.Client.GetAsync("/api/content");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<FolderEntry>>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Items.Count).IsEqualTo(1);
        await Assert.That(result.Items[0].Name).IsEqualTo("vacation");
        await Assert.That(result.Items[0].Type).IsEqualTo(EntryType.Folder);
    }

    [Test]
    public async Task ContentRoot_WithTrailingSlash_ReturnsEntries()
    {
        // Arrange
        using var app = new TestApp();
        app.Factory.FileProvider.AddDirectory("album");
        app.Factory.FileProvider.AddFile("album/photo.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));

        // Act
        var response = await app.Client.GetAsync("/api/content/");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<FolderEntry>>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Items.Count).IsEqualTo(1);
        await Assert.That(result.Items[0].Name).IsEqualTo("album");
    }

    [Test]
    public async Task ContentNested_WithPath_ReturnsEntries()
    {
        // Arrange
        using var app = new TestApp();
        app.Factory.FileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        app.Factory.FileProvider.AddFile("vacation/picture.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));

        // Act
        var response = await app.Client.GetAsync("/api/content/vacation");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<FolderEntry>>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TotalCount).IsEqualTo(2);
    }

    [Test]
    public async Task ContentDownload_ReturnsZipStream()
    {
        // Arrange
        using var app = new TestApp();
        var photoData = imageFactory.CreateTestImage(MagickFormat.Avif);
        app.Factory.FileProvider.AddFile("vacation/photo.avif", photoData);

        // Act
        var response = await app.Client.GetAsync("/api/content/download/vacation");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/zip");

        var zipBytes = await response.Content.ReadAsByteArrayAsync();
        using var memoryStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
        await Assert.That(archive.Entries.Count).IsEqualTo(1);
        await Assert.That(archive.Entries[0].FullName).IsEqualTo("photo.avif");
    }

    [Test]
    public async Task ContentServe_RootPath_ReturnsImage()
    {
        // Arrange
        using var app = new TestApp();
        var photoData = imageFactory.CreateTestImage(MagickFormat.Avif);
        app.Factory.FileProvider.AddFile("photo.avif", photoData);

        // Act
        var response = await app.Client.GetAsync("/api/content/image/photo.avif");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("image/avif");
        var servedBytes = await response.Content.ReadAsByteArrayAsync();
        await Assert.That(servedBytes).IsEquivalentTo(photoData);
    }

    [Test]
    public async Task ContentServe_NestedPath_ReturnsImage()
    {
        // Arrange
        using var app = new TestApp();
        var photoData = imageFactory.CreateTestImage(MagickFormat.Avif);
        app.Factory.FileProvider.AddFile("vacation/photo.avif", photoData);

        // Act
        var response = await app.Client.GetAsync("/api/content/image/vacation/photo.avif");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("image/avif");
        var servedBytes = await response.Content.ReadAsByteArrayAsync();
        await Assert.That(servedBytes).IsEquivalentTo(photoData);
    }

    [Test]
    public async Task ContentServe_NestedPathWithUrlEncoding_ReturnsImage()
    {
        // Arrange
        using var app = new TestApp();
        var photoData = imageFactory.CreateTestImage(MagickFormat.Avif);
        app.Factory.FileProvider.AddFile("vacation/photo.avif", photoData);

        // Act — simulate Scalar URL-encoding the path separator
        var response = await app.Client.GetAsync("/api/content/image/vacation%2Fphoto.avif");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("image/avif");
        var servedBytes = await response.Content.ReadAsByteArrayAsync();
        await Assert.That(servedBytes).IsEquivalentTo(photoData);
    }

    [Test]
    public async Task ContentServe_DeeplyNestedPath_ReturnsImage()
    {
        // Arrange
        using var app = new TestApp();
        var photoData = imageFactory.CreateTestImage(MagickFormat.Jpeg);
        app.Factory.FileProvider.AddFile("album/2024/trip/photo.jpg", photoData);

        // Act
        var response = await app.Client.GetAsync("/api/content/image/album/2024/trip/photo.jpg");

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
        var response = await app.Client.GetAsync("/api/health");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).IsEqualTo("\"pong\"");
    }

    [Test]
    public async Task RateLimit_UnauthenticatedJwtLogin_ExceedsLimit_Returns429()
    {
        // Arrange — the test factory sets PermitLimit=3, WindowSeconds=60
        using var app = new TestApp();
        var client = app.Factory.CreateClient();

        // Act — send requests that exceed the permit limit
        var responses = new List<HttpResponseMessage>();
        for (var index = 0; index < 5; index++)
        {
            responses.Add(await client.GetAsync("/api/authentication/login/jwt/invalid-token"));
        }

        // Assert — first requests within the limit should not be 429; the excess should be 429
        var statusCodes = responses.Select(response => response.StatusCode).ToList();
        await Assert.That(statusCodes.Count(statusCode => statusCode == HttpStatusCode.TooManyRequests)).IsEqualTo(2);
        await Assert.That(statusCodes.Count(statusCode => statusCode != HttpStatusCode.TooManyRequests)).IsEqualTo(3);
    }

    [Test]
    public async Task RateLimit_AuthenticatedEndpoint_NotRateLimited()
    {
        // Arrange — authenticated client uses API key; /content requires authorization and is not rate-limited
        using var app = new TestApp();
        app.Factory.FileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act — send more requests than the unauthenticated permit limit
        var responses = new List<HttpResponseMessage>();
        for (var index = 0; index < 5; index++)
        {
            responses.Add(await app.Client.GetAsync("/api/content"));
        }

        // Assert — all requests succeed because authenticated endpoints are not rate-limited
        await Assert.That(responses.All(response => response.StatusCode == HttpStatusCode.OK)).IsTrue();
    }
}
