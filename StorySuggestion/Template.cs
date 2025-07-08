using System.Text.RegularExpressions;

namespace StorySuggestion;

public sealed class Template
{
    private static readonly Regex _tokenRegex =
        new(@"\{(?<token>[^{}]+)\}", RegexOptions.Compiled);

    public TemplateId Id { get; }
    public string RawText { get; }
    public IReadOnlyList<TemplateSegment> Segments { get; }

    public Template(TemplateId id, string rawText)
    {
        Id       = id;
        RawText  = rawText;
        Segments = Parse(rawText);
    }

    private static List<TemplateSegment> Parse(string text)
    {
        var segments = new List<TemplateSegment>();
        var lastPos  = 0;

        foreach (Match m in _tokenRegex.Matches(text))
        {
            if (m.Index > lastPos)                       // leading plain text
                segments.Add(new TemplateSegment(text[lastPos..m.Index], SegmentKind.Text));

            var token = m.Groups["token"].Value;
            segments.Add(new TemplateSegment(token, Classify(token)));

            lastPos = m.Index + m.Length;
        }

        if (lastPos < text.Length)                       // trailing plain text
            segments.Add(new TemplateSegment(text[lastPos..], SegmentKind.Text));

        return segments;
    }

    private static SegmentKind Classify(string token)
        => token.Equals("date", StringComparison.OrdinalIgnoreCase)
            ? SegmentKind.Date
            : token.Contains(':')
                ? SegmentKind.Lookup
                : SegmentKind.Property;
}