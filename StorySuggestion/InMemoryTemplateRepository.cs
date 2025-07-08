namespace StorySuggestion;

public sealed class InMemoryTemplateRepository : ITemplateRepository
{
    public Template Get(TemplateId id)
    {
        var raw = StorySuggestionConfig.GetTemplate(id.Value.ToLowerInvariant());
        return new Template(id, raw);
    }
}