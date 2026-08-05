using System.Globalization;

namespace ImageShare.UsageAgreement;

/// <summary>
/// Selects the best matching agreement from <see cref="UsageAgreementOptions"/> based on the
/// HTTP <c>Accept-Language</c> header. Only the primary language subtag is considered, so
/// <c>sv-EN</c> matches the <c>sv</c> agreement. When no preference matches, the <c>en</c>
/// agreement is used (falling back to the first configured agreement if <c>en</c> is absent).
/// </summary>
public static class UsageAgreementOptionsExtensions
{
    public static UsageAgreementText? FindBestMatch(this UsageAgreementOptions options, string? acceptLanguage)
    {
        if (!options.IsEnabled)
        {
            return null;
        }

        var preferences = ParseAcceptLanguage(acceptLanguage);
        foreach (var preference in preferences)
        {
            var primary = preference.Split('-')[0];
            var match = options.Agreements.FirstOrDefault(agreement =>
                string.Equals(agreement.Language.Split('-')[0], primary, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        var english = options.Agreements.FirstOrDefault(agreement =>
            string.Equals(agreement.Language.Split('-')[0], "en", StringComparison.OrdinalIgnoreCase));
        return english ?? options.Agreements[0];
    }

    private static IReadOnlyList<string> ParseAcceptLanguage(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return [];
        }

        var parts = acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var ranked = new List<(string Language, double Quality)>();

        foreach (var part in parts)
        {
            var segments = part.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var language = segments[0];
            var quality = 1.0;

            for (var index = 1; index < segments.Length; index++)
            {
                var segment = segments[index];
                if (segment.StartsWith("q=", StringComparison.OrdinalIgnoreCase) &&
                    double.TryParse(segment[2..], CultureInfo.InvariantCulture, out var parsed))
                {
                    quality = parsed;
                }
            }

            if (quality > 0)
            {
                ranked.Add((language, quality));
            }
        }

        return ranked
            .OrderByDescending(item => item.Quality)
            .Select(item => item.Language)
            .ToList();
    }
}
