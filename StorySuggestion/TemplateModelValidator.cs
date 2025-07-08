using Newtonsoft.Json.Linq;

namespace StorySuggestion;

public sealed class TemplateModelValidator : IModelValidator
{
    public IReadOnlyCollection<string> GetMissingMembers(Template template, JObject model)
    {
        var required = template.Segments
            .Where(s => s.Kind is not SegmentKind.Text)
            .Select(s => s.Raw.Split(':')[0])
            .Distinct();

        var missing = required.Except(model.Properties().Select(p => p.Name)).ToList();
        return missing;
    }
}