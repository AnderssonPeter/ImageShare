using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.SpaServices.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;

namespace ImageShare.Tests;

public class CompressedStaticFilesTests : IntegrationTestBase
{
    private static readonly byte[] originalContent = [.. Enumerable.Repeat((byte)'A', 100)];
    private static readonly byte[] brotliContent = [.. Enumerable.Repeat((byte)'B', 10)];
    private static readonly byte[] gzipContent = [.. Enumerable.Repeat((byte)'C', 20)];
    private static readonly byte[] zstdContent = [.. Enumerable.Repeat((byte)'D', 30)];

    private PhysicalFileProvider spaFileProvider = null!;
    private string spaDirectory = null!;

    protected override async Task SetupAsync()
    {
        // CompressedStaticFiles restores the original content type from IFileInfo.PhysicalPath, which is
        // null for in-memory providers, so the SPA assets must live on disk behind a PhysicalFileProvider.
        spaDirectory = Path.Combine(Path.GetTempPath(), $"imageshare-spa-{Guid.NewGuid():N}");
        Directory.CreateDirectory(spaDirectory);
        File.WriteAllBytes(Path.Combine(spaDirectory, "index.html"), originalContent);
        File.WriteAllBytes(Path.Combine(spaDirectory, "index.html.br"), brotliContent);
        File.WriteAllBytes(Path.Combine(spaDirectory, "index.html.gz"), gzipContent);
        File.WriteAllBytes(Path.Combine(spaDirectory, "index.html.zst"), zstdContent);
        spaFileProvider = new PhysicalFileProvider(spaDirectory);

        await base.SetupAsync();
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);
        services.RemoveAll<ISpaStaticFileProvider>();
        services.AddSingleton<ISpaStaticFileProvider>(new SpaStaticFileProvider(spaFileProvider));
    }

    [After(Test)]
    public void CleanupSpaDirectory()
    {
        spaFileProvider.Dispose();
        if (Directory.Exists(spaDirectory))
        {
            Directory.Delete(spaDirectory, recursive: true);
        }
    }

    [Test]
    public async Task SpaFile_NoAcceptEncoding_ServesUncompressedOriginal()
    {
        // Arrange
        var client = CreateClientWithApiKey();

        // Act
        var response = await client.GetAsync("/index.html");

        // Assert — with no accepted encoding the original file is served unchanged
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentEncoding.Any()).IsFalse();
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("text/html");
        var body = await response.Content.ReadAsByteArrayAsync();
        await Assert.That(body).IsEquivalentTo(originalContent);
    }

    [Test]
    public async Task SpaFile_BrotliAcceptEncoding_ServesBrotliVariant()
    {
        // Arrange
        var client = CreateClientWithApiKey();
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

        // Act
        var response = await client.GetAsync("/index.html");

        // Assert — the precompressed .br file is served with the original content type
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentEncoding).Contains("br");
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("text/html");
        var body = await response.Content.ReadAsByteArrayAsync();
        await Assert.That(body).IsEquivalentTo(brotliContent);
    }

    [Test]
    public async Task SpaFile_GzipAcceptEncoding_ServesGzipVariant()
    {
        // Arrange
        var client = CreateClientWithApiKey();
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        // Act
        var response = await client.GetAsync("/index.html");

        // Assert — the precompressed .gz file is served with the original content type
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentEncoding).Contains("gzip");
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("text/html");
        var body = await response.Content.ReadAsByteArrayAsync();
        await Assert.That(body).IsEquivalentTo(gzipContent);
    }

    [Test]
    public async Task SpaFile_ZstdAcceptEncoding_ServesZstdVariant()
    {
        // Arrange
        var client = CreateClientWithApiKey();
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("zstd"));

        // Act
        var response = await client.GetAsync("/index.html");

        // Assert — the precompressed .zst file is served with the original content type
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentEncoding).Contains("zstd");
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("text/html");
        var body = await response.Content.ReadAsByteArrayAsync();
        await Assert.That(body).IsEquivalentTo(zstdContent);
    }

    [Test]
    public async Task SpaFile_MultipleAcceptEncodings_ServesSmallestVariant()
    {
        // Arrange — brotli (10 bytes) is smaller than gzip (20) and zstd (30), so it wins
        var client = CreateClientWithApiKey();
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("zstd"));

        // Act
        var response = await client.GetAsync("/index.html");

        // Assert — the smallest precompressed variant is preferred
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentEncoding).Contains("br");
        var body = await response.Content.ReadAsByteArrayAsync();
        await Assert.That(body).IsEquivalentTo(brotliContent);
    }

    [Test]
    public async Task SpaRoute_UnmatchedClientPath_ReturnsIndexHtml()
    {
        // Arrange — a client-side route that maps to no static file must fall back to index.html
        // so the SPA router can handle deep links and refreshes.
        var client = CreateClientWithApiKey();

        // Act
        var response = await client.GetAsync("/browse/vacation/2024");

        // Assert — the SPA shell is served with the original content type
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("text/html");
        var body = await response.Content.ReadAsByteArrayAsync();
        await Assert.That(body).IsEquivalentTo(originalContent);
    }

    private sealed class SpaStaticFileProvider(IFileProvider fileProvider) : ISpaStaticFileProvider
    {
        public IFileProvider FileProvider => fileProvider;
    }
}
