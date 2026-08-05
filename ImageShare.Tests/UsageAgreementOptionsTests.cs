using System.ComponentModel.DataAnnotations;
using ImageShare.UsageAgreement;
using TUnit.Core;

namespace ImageShare.Tests;

public class UsageAgreementOptionsTests
{
    [Test]
    public async Task IsEnabled_NoAgreements_ReturnsFalse()
    {
        // Arrange
        var options = new UsageAgreementOptions();

        // Act
        // Assert
        await Assert.That(options.IsEnabled).IsFalse();
    }

    [Test]
    public async Task IsEnabled_WithAgreements_ReturnsTrue()
    {
        // Arrange
        var options = new UsageAgreementOptions
        {
            Agreements = [new UsageAgreementText { Language = "en", Text = "agreement" }]
        };

        // Act
        // Assert
        await Assert.That(options.IsEnabled).IsTrue();
    }

    [Test]
    public async Task Validation_MissingLanguage_ReturnsError()
    {
        // Arrange
        var options = new UsageAgreementOptions
        {
            Agreements = [new UsageAgreementText { Language = "", Text = "agreement" }]
        };
        var context = new ValidationContext(options);

        // Act
        var results = options.Validate(context);

        // Assert
        await Assert.That(results.Any(r => r.ErrorMessage!.Contains("Language"))).IsTrue();
    }

    [Test]
    public async Task Validation_MissingText_ReturnsError()
    {
        // Arrange
        var options = new UsageAgreementOptions
        {
            Agreements = [new UsageAgreementText { Language = "en", Text = "" }]
        };
        var context = new ValidationContext(options);

        // Act
        var results = options.Validate(context);

        // Assert
        await Assert.That(results.Any(r => r.ErrorMessage!.Contains("Text"))).IsTrue();
    }

    [Test]
    [Arguments("en-US,en;q=0.9", "en")]
    [Arguments("nl-NL,nl;q=0.9,en;q=0.8", "nl")]
    [Arguments("en,nl;q=0.5", "en")]
    [Arguments("nl,en;q=0.5", "nl")]
    [Arguments("de-DE", "en")]
    [Arguments("", "en")]
    [Arguments(null, "en")]
    public async Task FindBestMatch_SelectsCorrectAgreement(string? acceptLanguage, string expectedLanguage)
    {
        // Arrange
        var options = new UsageAgreementOptions
        {
            Agreements =
            [
                new UsageAgreementText { Language = "en", Text = "English" },
                new UsageAgreementText { Language = "nl", Text = "Nederlands" },
            ]
        };

        // Act
        var match = options.FindBestMatch(acceptLanguage);

        // Assert
        await Assert.That(match!.Language).IsEqualTo(expectedLanguage);
    }

    [Test]
    public async Task FindBestMatch_Disabled_ReturnsNull()
    {
        // Arrange
        var options = new UsageAgreementOptions();

        // Act
        var match = options.FindBestMatch("en");

        // Assert
        await Assert.That(match).IsNull();
    }

    [Test]
    public async Task FindBestMatch_OnlyPrimarySubtagIsUsed_SvEnYieldsSwedish()
    {
        // Arrange — "sv-EN" has a Swedish primary subtag and an English region; only the
        // primary subtag is considered, so the Swedish agreement is selected.
        var options = new UsageAgreementOptions
        {
            Agreements =
            [
                new UsageAgreementText { Language = "en", Text = "English" },
                new UsageAgreementText { Language = "sv", Text = "Svenska" },
            ]
        };

        // Act
        var match = options.FindBestMatch("sv-EN");

        // Assert
        await Assert.That(match!.Language).IsEqualTo("sv");
    }

    [Test]
    public async Task FindBestMatch_NoPreferenceMatches_DefaultsToEnglish()
    {
        // Arrange — no preference matches; English is the default even when not first.
        var options = new UsageAgreementOptions
        {
            Agreements =
            [
                new UsageAgreementText { Language = "de", Text = "Deutsch" },
                new UsageAgreementText { Language = "en", Text = "English" },
            ]
        };

        // Act
        var match = options.FindBestMatch("fr");

        // Assert
        await Assert.That(match!.Language).IsEqualTo("en");
    }
}
