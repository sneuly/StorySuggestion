using Newtonsoft.Json.Linq;

namespace StorySuggestion;

public sealed class StorySuggestionService
{
    private readonly ITemplateRenderer   _renderer;
    private readonly IModelValidator     _validator;
    private readonly ITemplateRepository _repo;

    public StorySuggestionService(
        ITemplateRenderer   renderer,
        IModelValidator     validator,
        ITemplateRepository repo)
    {
        _renderer  = renderer;
        _validator = validator;
        _repo      = repo;
    }

    public string Suggest(string type, JObject model)
        => Suggest(new TemplateId(type), model);

    public string Suggest(TemplateId id, JObject model)
    {
        var template = _repo.Get(id);

        var missing = _validator.GetMissingMembers(template, model);
        if (missing.Count > 0)
            throw new ArgumentException(
                $"Model lacks required members: {string.Join(", ", missing)}");

        return _renderer.Render(template, model);
    }
}