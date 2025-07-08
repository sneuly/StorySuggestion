using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;

namespace StorySuggestion;

public sealed class TemplateRenderer(ILookupProvider lookupProvider) : ITemplateRenderer
{
    // strategies that do NOT require lookup tables
    private readonly IReadOnlyList<IPlaceholderStrategy> _nonLookupStrategies =
    [
        new TextStrategy(),
        new PropertyStrategy(),
        new DateStrategy()
    ];

    public string Render(Template template, JObject model)
    {
        var primary = ElectPrimaryDictionary(
            template,
            (lookupProvider as InMemoryLookupProvider)?.AllGroups
                 ?? new Dictionary<string,
                        Dictionary<string, Dictionary<string, string>>>());

        var sb = new StringBuilder();

        foreach (var seg in template.Segments)
        {
            if (seg.Kind == SegmentKind.Lookup)
            {
                sb.Append(ResolveLookup(seg.Raw, model, primary));
                continue;
            }

            var strategy = _nonLookupStrategies.First(s => s.CanHandle(seg.Kind));
            sb.Append(strategy.Resolve(seg.Raw, model));
        }

        return sb.ToString();
    }

    private static Dictionary<string, Dictionary<string, string>>
        ElectPrimaryDictionary(
            Template template,
            Dictionary<string, Dictionary<string, Dictionary<string, string>>> groups)
    {
        // collect selector names: “form1”, “readable”, etc.
        var selectors = template.Segments
                                .Where(s => s.Kind == SegmentKind.Lookup)
                                .Select(s => s.Raw.Split(':')[1])
                                .ToHashSet(StringComparer.Ordinal);

        // pick the dictionary that shares the most selectors with the template
        return groups.Values
                     .OrderByDescending(g => g.Keys.Count(selectors.Contains))
                     .FirstOrDefault()
            ?? new Dictionary<string, Dictionary<string, string>>();
    }

    private string ResolveLookup(
        string token,
        JObject model,
        Dictionary<string, Dictionary<string, string>> primary)
    {
        var parts    = token.Split(':', 2);
        var left     = parts[0];
        var selector = parts[1];

        var key = model[left]?.ToString()
                  ?? throw new InvalidOperationException($"Model lacks '{left}'");

        // primary dictionary first
        if (primary.TryGetValue(selector, out var dict) &&
            dict.TryGetValue(key, out var value))
            return value;

        // fall back to any other dictionaries
        if (lookupProvider.TryResolve(selector, key, out var fallback))
            return fallback ?? string.Empty;

        throw new InvalidOperationException(
            $"No mapping found for selector '{selector}' and key '{key}'.");
    }
    
    private interface IPlaceholderStrategy
    {
        bool CanHandle(SegmentKind kind);
        string Resolve(string token, JObject model);
    }

    private sealed class TextStrategy : IPlaceholderStrategy
    {
        public bool CanHandle(SegmentKind kind) => kind == SegmentKind.Text;
        public string Resolve(string token, JObject _) => token;
    }

    private sealed class PropertyStrategy : IPlaceholderStrategy
    {
        public bool CanHandle(SegmentKind kind) => kind == SegmentKind.Property;
        public string Resolve(string token, JObject model)
            => model[token]?.ToString() ?? string.Empty;
    }

    private sealed class DateStrategy : IPlaceholderStrategy
    {
        private const string InputFormat = "dd/MM/yyyy";
        public bool CanHandle(SegmentKind kind) => kind == SegmentKind.Date;

        public string Resolve(string token, JObject model)
        {
            var raw = model[token]?.ToString()
                      ?? throw new InvalidOperationException($"Model lacks '{token}'");

            var dt = DateTime.ParseExact(raw, InputFormat, CultureInfo.InvariantCulture);
            return dt.Year.ToString();
        }
    }
}
