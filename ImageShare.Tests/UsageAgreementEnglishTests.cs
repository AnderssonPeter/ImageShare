using System.Net;
using System.Net.Http.Json;
using ImageMagick;
using ImageShare.UsageAgreement;
using Microsoft.Extensions.Configuration;

namespace ImageShare.Tests;

public class UsageAgreementEnglishTests : IntegrationTestBase
{
    private static readonly TestImageFactory imageFactory = new();

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["UsageAgreement:Agreements:0:Language"] = "en",
            ["UsageAgreement:Agreements:0:Text"] = "English agreement",
        });
    }

    [Test]
    public async Task UsageAgreement_Get_FallsBackToFirstWhenNoLanguageMatches()
    {
        // Arrange
        var client = CreateClientWithApiKey();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr");

        // Act
        var response = await client.GetAsync("/api/usage-agreement");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var agreement = await response.Content.ReadFromJsonAsync<UsageAgreementResponse>();
        await Assert.That(agreement!.Language).IsEqualTo("en");
    }

    [Test]
    public async Task UsageAgreement_Accept_SetsCookieAndUnblocksDownload()
    {
        // Arrange
        FileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        var client = CreateClientWithApiKey();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en");

        // Act — accept the agreement, then replay the issued cookie on the download request
        var acceptResponse = await client.PostAsync("/api/usage-agreement/accept", null);
        var setCookie = acceptResponse.Headers.TryGetValues("Set-Cookie", out var cookies) ? string.Join("; ", cookies) : null;

        client.DefaultRequestHeaders.Remove("Cookie");
        if (!string.IsNullOrEmpty(setCookie))
        {
            client.DefaultRequestHeaders.Add("Cookie", setCookie.Split(';')[0]);
        }

        var downloadResponse = await client.GetAsync("/api/content/download/vacation");

        // Assert
        await Assert.That(acceptResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(setCookie).StartsWith("usage-agreement=");
        await Assert.That(downloadResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(downloadResponse.Content.Headers.ContentType?.MediaType).IsEqualTo("application/zip");
    }

    [Test]
    public async Task UsageAgreement_NotAccepted_DownloadReturnsForbidden()
    {
        // Arrange
        FileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act — no accept performed
        var downloadResponse = await CreateClientWithApiKey().GetAsync("/api/content/download/vacation");

        // Assert
        await Assert.That(downloadResponse.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task UsageAgreement_NotAccepted_FullImageReturnsForbidden()
    {
        // Arrange
        FileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act
        var response = await CreateClientWithApiKey().GetAsync("/api/content/image/photo.avif");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task UsageAgreement_NotAccepted_ThumbnailIsAllowed()
    {
        // Arrange
        FileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        FileProvider.AddFile("photo.thumb.jpg", imageFactory.CreateTestImage(10, 10, MagickFormat.Jpeg));

        // Act
        var response = await CreateClientWithApiKey().GetAsync("/api/content/image/photo.avif?thumbnail=true");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task UsageAgreement_NotAccepted_RandomImageIsAllowed()
    {
        // Arrange
        FileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act
        var response = await CreateClientWithApiKey().GetAsync("/api/content/random");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
