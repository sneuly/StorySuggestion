namespace StorySuggestion;

public interface ITemplateRepository
{
    Template Get(TemplateId id);
}