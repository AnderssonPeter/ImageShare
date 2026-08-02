using System.Text.RegularExpressions;

namespace ImageShare.Authentication;

public class ImageShareFilterCompiler
{
    private readonly Dictionary<string, Regex> cache = [];
    private readonly Lock cacheLock = new();

    public Regex Compile(string imageShareFilter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageShareFilter);
        lock (cacheLock)
        {
            if (cache.TryGetValue(imageShareFilter, out var cachedRegex))
            {
                return cachedRegex;
            }

            var patterns = imageShareFilter.Split('|');
            var allowParts = new List<string>();
            var denyParts = new List<string>();

            foreach (var pattern in patterns)
            {
                var isDeny = pattern.StartsWith('!');
                var body = isDeny ? pattern[1..] : pattern;
                if (string.IsNullOrWhiteSpace(body))
                {
                    throw new ArgumentException($"Filter contains an empty pattern: '{imageShareFilter}'", nameof(imageShareFilter));
                }

                var escaped = Regex.Escape(body);
                escaped = escaped.Replace("\\*", "[^/]*");
                escaped = escaped.Replace("\\?", "[^/]");

                (isDeny ? denyParts : allowParts).Add(escaped);
            }

            if (allowParts.Count == 0)
            {
                throw new ArgumentException($"Filter must contain at least one allow pattern: '{imageShareFilter}'", nameof(imageShareFilter));
            }

            var allowPattern = string.Join('|', allowParts);
            var patternText = denyParts.Count == 0
                ? "^(" + allowPattern + ")$"
                : "^(?!(" + string.Join('|', denyParts) + ")$)(" + allowPattern + ")$";

            var regex = new Regex(patternText, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(0.25));
            cache[imageShareFilter] = regex;
            return regex;
        }
    }
}
