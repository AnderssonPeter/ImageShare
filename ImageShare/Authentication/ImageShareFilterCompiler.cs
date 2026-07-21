using System.Text.RegularExpressions;

namespace ImageShare.Authentication;

public class ImageShareFilterCompiler
{
    private readonly Dictionary<string, Regex> _cache = [];
    private readonly Lock _lock = new();

    public Regex Compile(string imageShareFilter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageShareFilter);
        lock (_lock)
        {
            if (_cache.TryGetValue(imageShareFilter, out var cachedRegex))
            {
                return cachedRegex;
            }

            var patterns = imageShareFilter.Split('|');
            var regexParts = new List<string>();

            foreach (var pattern in patterns)
            {
                var escaped = Regex.Escape(pattern);
                escaped = escaped.Replace("\\*", "[^/]*");
                escaped = escaped.Replace("\\?", "[^/]");
                regexParts.Add(escaped);
            }

            var regex = new Regex("^(" + string.Join('|', regexParts) + ")$", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(0.25));
            _cache[imageShareFilter] = regex;
            return regex;
        }
    }
}
