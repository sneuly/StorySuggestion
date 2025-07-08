using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;

namespace StorySuggestion;

public sealed class TemplateRenderer(ILookupProvider lookupProvider) : ITemplateRenderer
{
    private static readonly IReadOnlyList<IPlaceholderStrategy> NonLookupStrategies =
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

        var lookupStrategy = new LookupStrategy(primary, lookupProvider);

        var sb = new StringBuilder();

        foreach (var seg in template.Segments)
        {
            if (seg.Kind == SegmentKind.Lookup)
            {
                sb.Append(lookupStrategy.Resolve(seg.Raw, model));
                continue;
            }

            var strat = NonLookupStrategies.First(s => s.CanHandle(seg.Kind));
            sb.Append(strat.Resolve(seg.Raw, model));
        }

        return sb.ToString();
    }

    private static Dictionary<string, Dictionary<string, string>>
        ElectPrimaryDictionary(
            Template template,
            Dictionary<string, Dictionary<string, Dictionary<string, string>>> groups)
    {
        var selectors = template.Segments
                                .Where(s => s.Kind == SegmentKind.Lookup)
                                .Select(s => s.Raw.Split(':')[1])
                                .ToHashSet(StringComparer.Ordinal);

        return groups.Values
                     .OrderByDescending(g => g.Keys.Count(selectors.Contains))
                     .FirstOrDefault()
               ?? new Dictionary<string, Dictionary<string, string>>();
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

    private sealed class LookupStrategy(Dictionary<string, Dictionary<string, string>> primary,
                                        ILookupProvider fallback) : IPlaceholderStrategy
    {
        public bool CanHandle(SegmentKind kind) => kind == SegmentKind.Lookup;

        public string Resolve(string token, JObject model)
        {
            var parts    = token.Split(':', 2);
            var left     = parts[0];
            var selector = parts[1];

            var key = model[left]?.ToString()
                      ?? throw new InvalidOperationException($"Model lacks '{left}'");

            if (primary.TryGetValue(selector, out var dict) &&
                dict.TryGetValue(key, out var value))
                return value;

            if (fallback.TryResolve(selector, key, out var alt))
                return alt!;

            throw new InvalidOperationException(
                $"No mapping found for selector '{selector}' and key '{key}'.");
        }
    }
}
