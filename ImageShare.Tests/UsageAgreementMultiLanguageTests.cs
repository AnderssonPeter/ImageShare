using System.Net;
using System.Net.Http.Json;
using ImageShare.UsageAgreement;
using Microsoft.Extensions.Configuration;

namespace ImageShare.Tests;

public class UsageAgreementMultiLanguageTests : IntegrationTestBase
{
    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["UsageAgreement:Agreements:0:Language"] = "en",
            ["UsageAgreement:Agreements:0:Text"] = "English agreement",
            ["UsageAgreement:Agreements:1:Language"] = "nl",
            ["UsageAgreement:Agreements:1:Text"] = "Nederlandse overeenkomst",
        });
    }

    [Test]
    public async Task UsageAgreement_Get_ReturnsMatchingLanguage()
    {
        // Arrange
        var client = CreateClientWithApiKey();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("nl-NL,nl;q=0.9,en;q=0.8");

        // Act
        var response = await client.GetAsync("/api/usage-agreement");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var agreement = await response.Content.ReadFromJsonAsync<UsageAgreementResponse>();
        await Assert.That(agreement).IsNotNull();
        await Assert.That(agreement!.Language).IsEqualTo("nl");
        await Assert.That(agreement.Text).IsEqualTo("Nederlandse overeenkomst");
        await Assert.That(agreement.Accepted).IsFalse();
    }
}
